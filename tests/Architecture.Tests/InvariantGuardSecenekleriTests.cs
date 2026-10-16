using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// spec `0012c` **D57** · **C9** — invariant korumasının seçeneği **modül başına** tiplenir
/// ve hiçbir modül paylaşılan bir seçenek kaydına bağımlı kalmaz.
///
/// <para><b>Bu kapı iki kez ödenmiş bir dersin üçüncü ödemesini önlüyor.</b> İlki #52:
/// `IOutboxWriter` non-generic bir arayüzdü, üç modül onu kendi `DbContext`'iyle kaydediyor
/// ve DI'da son kayıt kazandığı için bir modülün duyurusu <b>hiç</b> kalıcı olmuyordu.
/// İkincisi `0012b`'nin kaçan hatası (**D55**): `VehicleCatalog` `InvariantGuardOptions`'ı
/// hiç kaydetmiyordu ve uygulama yalnızca `Identity` ile `Dealership` aynı tipi kaydettiği
/// için ayakta kalıyordu. D55 kaydı ekledi ama <b>tuzak sınıfını kapatmadı</b>: üç modül
/// aynı non-generic tipi kaydetmeye devam etti, üçü de <c>.Default</c> olduğu için fark
/// görünmedi. Bağımlılık kalkmadı, yön değiştirdi.</para>
///
/// <para><b>Birincil koruma bu kapı DEĞİL,</b> tip parametresidir: artık her modülün
/// seçeneği ayrı bir tip ve `InvariantGuard.ExecuteAsync` imzasında onu bağlamla eşliyor.
/// Kapı, non-generic bir paylaşılan seçeneğin geri gelmesini yasaklıyor ve ikinci
/// iddiasıyla asıl kuralı ölçüyor — korumayı çağıran her modül seçeneğini KENDİ
/// `DbContext`'iyle kaydeder.</para>
/// </summary>
public sealed class InvariantGuardSecenekleriTests
{
    private const string GuardCall = "InvariantGuard.ExecuteAsync";

    /// <summary>
    /// `BuildingBlocks`'ta tanımlı ve tip parametresi OLMAYAN bir seçenek kaydı. Modül
    /// kabına böyle bir tip girdiği an iki modül birbirinin değerini ezebilir hâle gelir.
    /// </summary>
    [Fact(DisplayName = "Mimari test: hiçbir modül BuildingBlocks'un non-generic bir seçenek tipini kaydetmez")]
    public void Kaydetmez_HicbirModul_NonGenericPaylasilanSecenegi()
    {
        IReadOnlyList<string> secenekTipleri = NonGenericSecenekTipleri();

        // Kapının kendisi de ölçülür: tip listesi boşsa test hiçbir şey sormadan yeşil
        // kalır ve bunu sessizce yapardı.
        Assert.True(
            secenekTipleri.Count >= 1,
            "`BuildingBlocks`'ta non-generic seçenek tipi bulunamadı; kapı yanlış yere bakıyor.");

        List<string> ihlaller = [];

        foreach (string tip in secenekTipleri)
        {
            Regex kayit = new(
                $@"Add(Singleton|Scoped|Transient)\s*(<\s*{Regex.Escape(tip)}\s*>\s*)?\(\s*(new\s+)?{Regex.Escape(tip)}\b",
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            ihlaller.AddRange(ModulDosyalari().SelectMany(yol => KodSatirlari(yol)
                .Where(satir => kayit.IsMatch(satir))
                .Select(satir => $"{SolutionLayout.Relative(yol)}: {satir.Trim()}")));
        }

        Assert.True(
            ihlaller.Count == 0,
            "Paylaşılan ve non-generic bir seçenek tipi modül kabına kaydedilmez: kap tek, "
            + "SON kayıt kazanır ve bir modülün değeri sessizce diğerinin değerine dönüşür "
            + $"(**D57**, #52'nin tuzağı):{Environment.NewLine}{string.Join(Environment.NewLine, ihlaller)}");
    }

    /// <summary>
    /// `0012b`'nin kaçan hatasının kapısı, bu kez DÖRT modül için: korumayı çağıran bir
    /// modül seçeneğini kaydetmezse uygulama başka modülün kaydıyla ayakta kalır ve o
    /// satır kalktığı gün modülün bütün yazma yolları çalışma zamanında düşer.
    /// </summary>
    [Fact(DisplayName = "Mimari test: invariant korumasını çağıran her modül kilit seçeneğini kendi DbContext'iyle kaydeder")]
    public void Kaydeder_HerKorumaModulu_KendiSecenegini()
    {
        IReadOnlyList<string> korumaModulleri = KorumayiCagiranModuller();

        Assert.True(
            korumaModulleri.Count >= 3,
            $"Korumayı çağıran modül sayısı beklenenden az ({korumaModulleri.Count}); kapı yanlış yere bakıyor.");

        List<string> eksikler = [];

        foreach (string modul in korumaModulleri)
        {
            string modulDosyasi = Path.Combine(
                SolutionLayout.SourceRoot, "Modules", modul, modul, $"{modul}Module.cs");

            if (!File.Exists(modulDosyasi))
            {
                eksikler.Add($"{modul}: `{modul}Module.cs` bulunamadı.");
                continue;
            }

            Regex konkretKayit = new(
                $@"AddSingleton\s*\(\s*InvariantGuardOptions\s*<\s*{Regex.Escape(modul)}DbContext\s*>\s*\.Default\s*\)",
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            if (!KodSatirlari(modulDosyasi).Any(satir => konkretKayit.IsMatch(satir)))
            {
                eksikler.Add(
                    $"{SolutionLayout.Relative(modulDosyasi)}: "
                    + $"`InvariantGuardOptions<{modul}DbContext>.Default` kaydedilmiyor.");
            }
        }

        Assert.True(
            eksikler.Count == 0,
            "Invariant korumasını çağıran modül, kilit seçeneğini KENDİ `DbContext`'iyle kaydeder; "
            + "başka modülün kaydına yaşayan bir bağımlılık bırakılmaz "
            + $"(**C9**, `0012b`'nin kaçan hatası):{Environment.NewLine}{string.Join(Environment.NewLine, eksikler)}");
    }

    /// <summary>`BuildingBlocks`'ta tanımlı, tip parametresi olmayan `*Options` kayıtları.</summary>
    private static IReadOnlyList<string> NonGenericSecenekTipleri()
    {
        Regex bildirim = new(
            @"record\s+(?<ad>\w+Options)\s*(?![<\w])",
            RegexOptions.None,
            TimeSpan.FromSeconds(2));

        return
        [
            .. SolutionLayout.SourceFiles()
                .Where(yol => SolutionLayout.Relative(yol).StartsWith("src/BuildingBlocks/", StringComparison.Ordinal))
                .SelectMany(yol => KodSatirlari(yol).SelectMany(satir => bildirim.Matches(satir)
                    .Select(eslesme => eslesme.Groups["ad"].Value)))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
        ];
    }

    /// <summary>Kaynağında <c>InvariantGuard.ExecuteAsync</c> çağrısı geçen modüllerin adları.</summary>
    private static IReadOnlyList<string> KorumayiCagiranModuller() =>
    [
        .. SolutionLayout.SourceFiles()
            .Where(yol => KodSatirlari(yol).Any(satir => satir.Contains(GuardCall, StringComparison.Ordinal)))
            .Select(SolutionLayout.Relative)
            .Where(yol => yol.StartsWith("src/Modules/", StringComparison.Ordinal))
            .Select(yol => yol.Split('/')[2])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
    ];

    /// <summary>
    /// Yorum satırları atlanır. Bu dosyanın ölçtüğü tuzağı `InvariantGuard.cs` ve üç modül
    /// YORUMDA anlatıyor; tarama yorumları okursa kapı, kendisini doğuran gerekçeyi ihlal
    /// sanar (`OutboxYazicisiTests`'in aynı gerekçesi).
    ///
    /// <para>Desen artık `SolutionLayout`'ta yaşıyor: aynı tuzak `0007`/AC-9'u da ısırdı
    /// (**D50**) ve ikinci bir kopya yazmak, iki kopyanın ayrı ayrı zayıflaması
    /// demekti — bu deponun iki kez ödediği ders.</para>
    /// </summary>
    private static IReadOnlyList<string> KodSatirlari(string yol) => SolutionLayout.CodeLines(yol);

    private static IReadOnlyList<string> ModulDosyalari() =>
    [
        .. SolutionLayout.SourceFiles()
            .Where(yol => Path.GetFileName(yol).EndsWith("Module.cs", StringComparison.Ordinal))
    ];
}
