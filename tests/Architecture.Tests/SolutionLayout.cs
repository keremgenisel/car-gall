using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// Mimari testler kaynak ağacını doğrudan okur. Derlenmiş assembly'lere referans vermek,
/// test projesini her yeni modüle referans vermeye zorlardı; kaynak tarama ise modül
/// eklendiği anda kendiliğinden kapsar — kuralın unutulacak bir adımı kalmaz.
/// </summary>
internal static class SolutionLayout
{
    internal static string Root { get; } = FindRoot();

    internal static string SourceRoot => Path.Combine(Root, "src");

    internal static IReadOnlyList<string> ProjectFiles() =>
        Directory.Exists(SourceRoot)
            ? [.. Directory.EnumerateFiles(SourceRoot, "*.csproj", SearchOption.AllDirectories).Order()]
            : [];

    internal static IReadOnlyList<string> SourceFiles() =>
        Directory.Exists(SourceRoot)
            ? [.. Directory.EnumerateFiles(SourceRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsGenerated(path))
                .Order()]
            : [];

    internal static string TestRoot => Path.Combine(Root, "tests");

    /// <summary>
    /// Test ağacının kaynak dosyaları — `SourceFiles()`'tan BİLEREK ayrı.
    ///
    /// <para>Kuralların çoğu `src/` hakkındadır (katman sınırları, şema sahipliği, uç
    /// yayınlama) ve test ağacını onlara dâhil etmek yanlış yerde kırmızı verirdi: bir
    /// test dosyası başka modülün şemasını okumakta, sahte bir uç kurmakta serbesttir.
    /// İki ağacı birlikte tarayan şey yalnız METİN kuralıdır — yorum biçimi gibi,
    /// nerede yazıldığından bağımsız olanlar.</para>
    ///
    /// <para>Bu ayrımın bedeli bir kez ödendi: yorum/üye kusuru için kurulan kapı
    /// yalnız `src/` altına bakıyordu ve kusur dördüncü kez <b>tests/</b> altında
    /// döndü — kapı yeşil kaldı.</para>
    /// </summary>
    internal static IReadOnlyList<string> TestFiles() =>
        Directory.Exists(TestRoot)
            ? [.. Directory.EnumerateFiles(TestRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !IsGenerated(path))
                .Order()]
            : [];

    internal static IReadOnlyList<string> ProjectReferences(string projectFile)
    {
        XDocument document = XDocument.Load(projectFile);
        string projectDirectory = Path.GetDirectoryName(projectFile)!;

        return
        [
            .. document.Descendants("ProjectReference")
                .Select(element => element.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include!.Replace('\\', '/'))))
        ];
    }

    /// <summary>
    /// Bir dosyanın YORUM OLMAYAN satırları.
    ///
    /// <para>Metin tarayan kurallar bunu kullanır, çünkü bu deponun kuralları kendilerini
    /// yorumda anlatıyor: yasakladığı ifadeyi gerekçesinde yazan bir dosya, ham metin
    /// taramasında <b>ihlal gibi görünür</b>. `0007`/AC-9 tam bunu yaşadı — `FOR UPDATE`
    /// yazmayan bir dosya, <i>"neden `FOR UPDATE` değil"</i> diye yazdığı için yakalandı.
    /// `InvariantGuardSecenekleriTests` aynı tuzağı daha önce kendi kopyasıyla çözmüştü;
    /// ortak desen tek bir yerde yaşar (`0007` R3'ün aynı ilkesi).</para>
    ///
    /// <para>Kural zayıflamıyor: yorumda ifadeyi anmak serbest, kodda yazmak değil.</para>
    /// </summary>
    internal static IReadOnlyList<string> CodeLines(string path) =>
    [
        .. File.ReadAllLines(path).Where(line =>
        {
            string trimmed = line.TrimStart();

            return !trimmed.StartsWith("//", StringComparison.Ordinal)
                && !trimmed.StartsWith("*", StringComparison.Ordinal);
        })
    ];

    internal static string Relative(string absolutePath) =>
        Path.GetRelativePath(Root, absolutePath).Replace('\\', '/');

    /// <summary>Modül projesi mi (<c>src/Modules/X/X</c>), yoksa sözleşme projesi mi (<c>X.Contracts</c>)?</summary>
    internal static bool IsContractsProject(string projectFile) =>
        Path.GetFileNameWithoutExtension(projectFile).EndsWith(".Contracts", StringComparison.Ordinal);

    internal static bool IsModuleProject(string projectFile) =>
        Relative(projectFile).StartsWith("src/Modules/", StringComparison.Ordinal);

    internal static string ModuleNameOf(string projectFile)
    {
        string[] segments = Relative(projectFile).Split('/');
        return segments.Length >= 3 ? segments[2] : string.Empty;
    }

    /// <summary>
    /// Başka bir modülün şemasına dokunan satırı yakalayan desen (Yasak Listesi 1).
    /// İki biçimi birlikte arıyor: nitelikli tablo adı (<c>dealership.branches</c>, ham
    /// SQL'de böyle görünür) ve dize sabiti (<c>"dealership"</c>, EF yapılandırmasında
    /// böyle görünür).
    ///
    /// <para><b>Burada yaşıyor çünkü iki sınır testi onu paylaşıyor</b> (`0009`/AC-30 ve
    /// `0011`/AC-35). İkinci kopyayı yazarken daha zayıf bir desen kurdum
    /// (<c>[".]şema[".]</c>) ve bir sonda onu yakaladı: <c>"SELECT * FROM
    /// dealership.branches"</c> satırını <b>kaçırıyordu</b>, çünkü şema adının öncesinde
    /// boşluk vardı. İki kopyanın ayrışması tam olarak böyle görünüyor — kuralın bir
    /// yarısı sessizce zayıflıyor.</para>
    /// </summary>
    internal static Regex SchemaReference(string schema)
    {
        string kacisli = Regex.Escape(schema);

        // Nitelikli tablo adı: ham SQL'de `dealership.branches` gibi görünür.
        string nitelikli = $@"\b{kacisli}\.[a-z_]{{2,}}\b";

        // Dize sabiti: EF yapılandırmasında `"dealership"` gibi görünür.
        string dizeSabiti = $@"""{kacisli}""";

        return new Regex(
            $"{nitelikli}|{dizeSabiti}",
            RegexOptions.None,
            TimeSpan.FromSeconds(2));
    }

    private static bool IsGenerated(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            // SDK 10 çözümü .slnx olarak üretir; .sln hâlâ geçerli bir biçimdir.
            if (Directory.EnumerateFiles(directory.FullName, "CarGallery.sln*").Any())
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("CarGallery.sln bulunamadı; mimari testler kaynak ağacını konumlandıramıyor.");
    }
}
