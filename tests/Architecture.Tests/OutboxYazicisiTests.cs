using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// Yasak Listesi 16 — bir modül başka modülün outbox tablosuna YAZMAZ.
///
/// <para><b>Bu kapı bir kaçan hatanın bedeliydi</b> (`0006` kaçan hata #2, 2026-10-11).
/// `IOutboxWriter` non-generic bir arayüzdü ve `Dealership`, `Leads`, `Identity` onu kendi
/// `DbContext`'iyle kaydediyordu. DI'da son kayıt kazanır; `Program.cs`'te `AddLeads` en
/// sonda çağrılıyordu. Sonuç: `Identity`'nin duyurusu `OutboxWriter&lt;LeadsDbContext&gt;`
/// ile yazılıyor, satır `Leads`'in değişiklik izleyicisine düşüyor, modül kendi
/// `DbContext`'ini kaydediyor ve duyuru <b>hiç kalıcı olmuyordu</b> — `0006`/AC-48'in
/// ("değişiklik kaydedildiyse duyurusu da kaydedilmiştir") tam tersi.
///
/// <para><b>Birincil koruma bu kapı DEĞİL,</b> non-generic arayüzün yokluğudur: olmayan bir
/// tuzak kurulamaz. Kapı onun geri gelmesini yasaklar ve üçüncü iddiasıyla asıl kuralı
/// ölçer — outbox tutan her modül yazıcısını KENDİ `DbContext`'iyle, konkret olarak
/// kaydeder.</para>
///
/// <para><b>Niye `0011`/AC-35'in yerine geçmiyor:</b> o kriter arşivlenmiş bir kartta ve
/// yalnız `Media`'yı sorar; kapanmış kart düzeltilmez. Bu kapı dört modülü kapsadığı için
/// bir kritere değil, Yasak Listesi 16'ya dayanır.</para>
/// </summary>
public sealed class OutboxYazicisiTests
{
    /// <summary>Paylaşılan bir soyutlama altında yazıcı kaydı: `AddScoped&lt;IX, OutboxWriter&lt;…&gt;&gt;`.</summary>
    private static readonly Regex PaylasilanKayit = new(
        @"Add\w*<\s*[A-Z]\w*\s*,\s*OutboxWriter\s*<",
        RegexOptions.None,
        TimeSpan.FromSeconds(2));

    /// <summary>Non-generic bir outbox yazıcı soyutlaması: tip parametresi olmayan `interface IOutboxWriter`.</summary>
    private static readonly Regex NonGenericSoyutlama = new(
        @"interface\s+I\w*OutboxWriter\s*(?![<\w])",
        RegexOptions.None,
        TimeSpan.FromSeconds(2));

    [Fact(DisplayName = "Mimari test: hiçbir modül outbox yazıcısını paylaşılan bir soyutlama altında kaydetmez")]
    public void Kaydetmez_HicbirModul_PaylasilanSoyutlamaAltinda()
    {
        List<string> ihlaller =
        [
            .. ModulDosyalari().SelectMany(yol => KodSatirlari(yol)
                .Where(satir => PaylasilanKayit.IsMatch(satir))
                .Select(satir => $"{SolutionLayout.Relative(yol)}: {satir.Trim()}"))
        ];

        Assert.True(
            ihlaller.Count == 0,
            "Outbox yazıcısı paylaşılan bir soyutlama altında kaydedilmez: son kayıt kazanır ve "
            + "başka modülün duyurusu bu modülün şemasına yazılır — ya da hiç yazılmaz "
            + $"(Yasak Listesi 16):{Environment.NewLine}{string.Join(Environment.NewLine, ihlaller)}");
    }

    [Fact(DisplayName = "Mimari test: outbox yazıcısının non-generic bir soyutlaması yoktur")]
    public void Yoktur_NonGenericSoyutlama()
    {
        List<string> ihlaller =
        [
            .. SolutionLayout.SourceFiles().SelectMany(yol => KodSatirlari(yol)
                .Where(satir => NonGenericSoyutlama.IsMatch(satir))
                .Select(satir => $"{SolutionLayout.Relative(yol)}: {satir.Trim()}"))
        ];

        Assert.True(
            ihlaller.Count == 0,
            "Outbox yazıcısının non-generic bir soyutlaması, hangi şemaya yazdığını imzasında "
            + "TAŞIMAZ ve iki modülün kaydı birbirinin yerine geçebilir hâle gelir. Tuzağı "
            + "yasaklayan bir kapı, tuzağı ifade edilemez kılmaktan zayıftır "
            + $"(`0006` kaçan hata #2):{Environment.NewLine}{string.Join(Environment.NewLine, ihlaller)}");
    }

    [Fact(DisplayName = "Mimari test: outbox tutan her modül yazıcısını kendi DbContext'iyle konkret olarak kaydeder")]
    public void Kaydeder_HerOutboxModulu_KendiDbContextiyle()
    {
        IReadOnlyList<string> outboxModulleri = OutboxTutanModuller();

        // Kapının kendisi de ölçülür: modül listesi boşsa test hiçbir şey sormadan yeşil
        // kalır ve bunu sessizce yapardı.
        Assert.True(
            outboxModulleri.Count >= 4,
            $"Outbox tutan modül sayısı beklenenden az ({outboxModulleri.Count}); kapı yanlış yere bakıyor.");

        List<string> eksikler = [];

        foreach (string modul in outboxModulleri)
        {
            string modulDosyasi = Path.Combine(SolutionLayout.SourceRoot, "Modules", modul, modul, $"{modul}Module.cs");

            if (!File.Exists(modulDosyasi))
            {
                eksikler.Add($"{modul}: `{modul}Module.cs` bulunamadı.");
                continue;
            }

            Regex konkretKayit = new(
                $@"Add\w*<\s*OutboxWriter\s*<\s*{Regex.Escape(modul)}DbContext\s*>\s*>",
                RegexOptions.None,
                TimeSpan.FromSeconds(2));

            if (!KodSatirlari(modulDosyasi).Any(satir => konkretKayit.IsMatch(satir)))
            {
                eksikler.Add(
                    $"{SolutionLayout.Relative(modulDosyasi)}: `OutboxWriter<{modul}DbContext>` "
                    + "konkret olarak kaydedilmiyor.");
            }
        }

        Assert.True(
            eksikler.Count == 0,
            "Outbox tutan modül, yazıcısını KENDİ `DbContext`'iyle ve konkret tip olarak kaydeder; "
            + "konkret tip hangi şemaya yazdığını imzasında taşır "
            + $"(Yasak Listesi 16):{Environment.NewLine}{string.Join(Environment.NewLine, eksikler)}");
    }

    /// <summary>`IOutboxDbContext`'i uygulayan bir `DbContext` taşıyan modüllerin adları.</summary>
    private static IReadOnlyList<string> OutboxTutanModuller() =>
    [
        .. SolutionLayout.SourceFiles()
            .Where(yol => Path.GetFileName(yol).EndsWith("DbContext.cs", StringComparison.Ordinal))
            .Where(yol => KodSatirlari(yol).Any(satir => satir.Contains("IOutboxDbContext", StringComparison.Ordinal)))
            .Select(SolutionLayout.Relative)
            .Where(yol => yol.StartsWith("src/Modules/", StringComparison.Ordinal))
            .Select(yol => yol.Split('/')[2])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
    ];

    private static IReadOnlyList<string> ModulDosyalari() =>
    [
        .. SolutionLayout.SourceFiles()
            .Where(yol => Path.GetFileName(yol).EndsWith("Module.cs", StringComparison.Ordinal))
    ];

    /// <summary>
    /// Yorum satırları atlanır. `MediaModule.cs` ve `OutboxWriter.cs` bu kusuru YORUMDA
    /// anlatıyor; tarama yorumları okursa kapı, kendisini doğuran gerekçeyi ihlal sanar.
    /// </summary>
    private static IReadOnlyList<string> KodSatirlari(string yol) =>
    [
        .. File.ReadLines(yol).Where(satir =>
        {
            string kirpilmis = satir.TrimStart();

            return !kirpilmis.StartsWith("//", StringComparison.Ordinal)
                && !kirpilmis.StartsWith("///", StringComparison.Ordinal)
                && !kirpilmis.StartsWith("*", StringComparison.Ordinal);
        })
    ];
}
