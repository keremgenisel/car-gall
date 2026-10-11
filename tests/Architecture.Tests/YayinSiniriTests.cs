using System.Globalization;
using System.Text.RegularExpressions;

namespace CarGallery.Architecture.Tests;

/// <summary>
/// spec 0012b — AC-45, AC-58.
///
/// <para>İkisi de bir KURALIN tek yerde kaldığını söylüyor ve ikisi de kod gözden
/// geçirmesiyle korunamaz: durum ataması bir komuta sızdığı gün agregat bir veri taşıyıcı
/// olur, görünürlük kararı ikinci bir yerde tekrarlandığı gün vitrin iki farklı cevap
/// verir.</para>
/// </summary>
public sealed partial class YayinSiniriTests
{
    /// <summary>
    /// **C2**/Yasak Listesi 20 — durum geçişi mantığı agregatta kalır. `Listing.Status`
    /// özel bir kurucuya sahip ve `private set`; dışarıdan atanamaz. Kapı, komut ve uç
    /// katmanında bir durum ATAMASI aramıyor — derleyici onu zaten engelliyor — **enum
    /// dallanması** arıyor: `ListingStatus.X` karşılaştırması yapan bir komut, geçişin
    /// kararını agregattan almış olmaz.
    ///
    /// <para><b>İki istisna adıyla yazılı:</b> yayın komutu `Draft` kontrolü yapıyor
    /// (geçiş reddi A2'nin önüne geçsin diye, **D26**) ve şube kapanışı işleyicisi
    /// `Sold`/`Archived`'a dokunmuyor (**AC-28**). İkisi de bir geçiş YÜRÜTMÜYOR; hangi
    /// kaydın işleneceğini seçiyor.</para>
    /// </summary>
    [Fact(DisplayName = "0012b/AC-58: Durum geçişi mantığı agregatta kalır; komut ve uç katmanı durum ataması yapmaz")]
    public void Kalir_GecisMantigi_Agregatta()
    {
        List<string> violations = [];

        foreach (string file in ModuleSourceFiles("VehicleCatalog"))
        {
            string relative = SolutionLayout.Relative(file);

            bool komutVeyaUc =
                relative.Contains("/VehicleCatalog/Application/", StringComparison.Ordinal)
                || relative.Contains("/VehicleCatalog/Endpoints/", StringComparison.Ordinal);

            if (!komutVeyaUc || Istisna(relative))
            {
                continue;
            }

            violations.AddRange(CodeLines(file, StatusAssignment()));
        }

        Assert.True(
            violations.Count == 0,
            "Durum ataması yalnız agregatta olur (C2, Yasak Listesi 20):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    /// <summary>
    /// **C12**/**D34** — görünürlük kararını veren tek yer yansıtmadır. Kapı, okuma
    /// modeline satır EKLEYEN ya da SİLEN kodu arıyor; kopyalanmış kolonları tazeleyen
    /// `ExecuteUpdate` çağrıları dışarıda ve bu ayrım kararın kendisi.
    ///
    /// <para><b>`BranchDeactivatedHandler`'ın silmesi adıyla istisna:</b> o, görünürlük
    /// kararı vermiyor — kapanan bir şubenin satırlarını topluca temizliyor ve aynı
    /// işlemde aracın bayrağını yazıyor; bayraklı araç yansıtmada zaten `null` dönüyor.
    /// Küme halinde silmek, araç başına yansıtma çağırmaktan ölçülebilir biçimde ucuz.</para>
    /// </summary>
    [Fact(DisplayName = "0012b/AC-45: Görünürlük kararını yalnız yansıtma verir; kopyalanmış kolonları besleyen olay işleyicileri bu kararı vermez")]
    public void Verir_GorunurlukKararini_YalnizYansitma()
    {
        List<string> violations = [];

        foreach (string file in ModuleSourceFiles("VehicleCatalog"))
        {
            string relative = SolutionLayout.Relative(file);

            if (relative.EndsWith("ShowcaseProjectionWriter.cs", StringComparison.Ordinal)
                || relative.EndsWith("ShowcaseSeeder.cs", StringComparison.Ordinal)
                || relative.EndsWith("BranchEventHandlers.cs", StringComparison.Ordinal))
            {
                continue;
            }

            violations.AddRange(CodeLines(file, ShowcaseRowWrite()));
        }

        Assert.True(
            violations.Count == 0,
            "Vitrin satırını ekleyen/silen tek yer yansıtmadır (C12, D34):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");

        // Tarama BOŞ geçmemeli: yansıtmanın kendisi gerçekten yazıyor.
        string yazici = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot,
            "Modules/VehicleCatalog/VehicleCatalog/Application/Projection/ShowcaseProjectionWriter.cs"));

        Assert.Contains("ShowcaseListings.Add", yazici, StringComparison.Ordinal);
        Assert.Contains("ShowcaseListings.Remove", yazici, StringComparison.Ordinal);
    }

    /// <summary>
    /// **D40** — `Dealership.Contracts`'a eklenen okumanın **eklemeli** olduğu: mevcut iki
    /// imza yerinde duruyor. Kapanmış bir spec'in sözleşmesine dokunurken ölçülmesi
    /// gereken şey "ne eklendi" değil, "ne kırıldı".
    /// </summary>
    /// <remarks>Kritere bağlı değil: DoD'un **D40** maddesinin kapısı.</remarks>
    [Fact]
    public void Eklemelidir_SubeOkumasi()
    {
        string sozlesme = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot, "Modules/Dealership/Dealership.Contracts/Events/BranchEvents.cs"));

        Assert.Contains(
            "Task<BranchSummaryDto?> FindAsync(Guid branchId, string locale, CancellationToken cancellationToken);",
            sozlesme,
            StringComparison.Ordinal);
        Assert.Contains(
            "Task<IReadOnlyList<BranchSummaryDto>> ListAsync(string locale, CancellationToken cancellationToken);",
            sozlesme,
            StringComparison.Ordinal);
        Assert.Contains("FindNamesAsync", sozlesme, StringComparison.Ordinal);
    }

    /// <summary>
    /// **C11** — `OutboxYazicisiTests` bunu zaten JENERİK olarak tarıyor ve `VehicleCatalog`
    /// outbox'ı kurduğu an kapsıyor (ölçüldü). Bu test onun yerine geçmiyor, onu `0012b`'nin
    /// kriterine BAĞLIYOR: etiketsiz bir jenerik test, kriter → test eşlemesinde görünmez
    /// (`0012a`'nın AC-46'sıyla aynı gerekçe).
    /// </summary>
    [Fact(DisplayName = "0012b/AC-37: Outbox yazıcısı **konkret** `OutboxWriter<VehicleCatalogDbContext>` tipiyle kaydedilir")]
    public void Kaydedilir_Yazici_KonkretTiple()
    {
        string modul = File.ReadAllText(Path.Combine(
            SolutionLayout.SourceRoot, "Modules/VehicleCatalog/VehicleCatalog/VehicleCatalogModule.cs"));

        Assert.Contains("AddScoped<OutboxWriter<VehicleCatalogDbContext>>()", modul, StringComparison.Ordinal);

        // Paylaşılan bir soyutlama altında kayıt YOK: `IOutboxWriter` 2026-10-11'de
        // silindi (`7e8bdef`, #52) çünkü DI'da son kayıt kazanıyordu ve bir modülün
        // duyurusu HİÇ kalıcı olmuyordu.
        Assert.DoesNotContain("IOutboxWriter", modul, StringComparison.Ordinal);
    }

    /// <summary>
    /// **Kriter → test eşlemesinin kendisi bir kapı.** Her `0012b/AC-n` etiketinin METNİ,
    /// spec'teki aynı numaralı kriterin metniyle birebir aynı olmalı
    /// (`docs/testing.md` § 1: *"metin spec'ten KOPYALANIR"*).
    ///
    /// <para><b>Bu test bir dış bulgunun bedeliydi.</b> Yedi etiketin numarası kaymıştı ve
    /// bir PERMÜTASYON kuruyordu: 2→6, 6→4, 4→3, 3→2. Dört davranışın dördü de test
    /// ediliyordu — hiçbir kriter kapısız değildi — ama dördünün de ADRESİ yanlıştı.
    /// Etiket SAYAN bir ölçüm bunu göremez: permütasyon her numarayı bir kez kullanıyor ve
    /// 58/58 çıkıyor. PR'ın ölçümü tam olarak bu yüzden kaçırdı.</para>
    ///
    /// <para><b>Kapı neden yalnız `0012b`'yi tarıyor:</b> ölçtüm — aynı karşılaştırma
    /// depo genelinde 571 etiketin 299'unda uyuşmazlık veriyor. Çoğu eski spec'lerin
    /// kısaltılmış ya da birden çok kriteri birleştiren etiketleri; hepsini düzeltmek bu
    /// triyajın kapsamı dışında ve kendi işini hak ediyor. Susturulmuş bir kapı yerine
    /// <b>dar ama gerçek</b> bir kapı: `0012b` için bugün yeşil ve yarın kayarsa kırmızı.</para>
    /// </summary>
    [Fact]
    public void Ayni_EtiketMetni_SpectekiKriterle()
    {
        Dictionary<int, string> kriterler = SpectekiKriterler();

        Assert.Equal(58, kriterler.Count);

        List<string> violations = [];
        int sayilan = 0;

        foreach (string file in TestSourceFiles())
        {
            string[] lines = File.ReadAllLines(file);

            for (int index = 0; index < lines.Length; index++)
            {
                foreach (Match match in EtiketliDisplayName().Matches(lines[index]))
                {
                    int numara = int.Parse(match.Groups["no"].Value, CultureInfo.InvariantCulture);
                    string metin = Sadelestir(match.Groups["metin"].Value);

                    sayilan++;

                    if (!kriterler.TryGetValue(numara, out string? beklenen))
                    {
                        violations.Add($"{SolutionLayout.Relative(file)}:{index + 1}  AC-{numara} spec'te yok");
                        continue;
                    }

                    if (!string.Equals(metin, beklenen, StringComparison.Ordinal))
                    {
                        violations.Add(
                            $"{SolutionLayout.Relative(file)}:{index + 1}  AC-{numara} etiketi başka bir kriterin metnini taşıyor");
                    }
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Etiketin metni kendi kriterinin metni olmalı (`docs/testing.md` § 1):"
            + $"{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");

        // Tarama BOŞ geçmemeli: hiç etiket bulamayan bir tarama da "hepsi doğru" der.
        Assert.True(sayilan > 20, $"Yalnız {sayilan} etiket tarandı; tarama anlamsız.");
    }

    private static Dictionary<int, string> SpectekiKriterler()
    {
        string spec = File.ReadAllText(SpecYolu());

        Dictionary<int, string> kriterler = [];

        foreach (Match match in KriterSatiri().Matches(spec))
        {
            kriterler[int.Parse(match.Groups["no"].Value, CultureInfo.InvariantCulture)] =
                Sadelestir(match.Groups["metin"].Value);
        }

        return kriterler;
    }

    /// <summary>
    /// Kartın yolu, iş bittiğinde <c>specs/</c> altından <c>specs/done/</c> altına taşınır
    /// (DoD <c>T13</c>) — bu yüzden kapı <b>iki yere de</b> bakıyor.
    ///
    /// <para><b>Bu metot bir kapanış hatasının bedeliydi.</b> İlk hâli depo kökünü
    /// <c>specs/0012b-….md</c>'nin VARLIĞIYLA arıyordu: dosya arşive taşındığı an yukarı
    /// yürüyüş kökü hiç bulamıyor, <c>dizin</c> null oluyor ve kapı tam <b>kartı kapatan
    /// commit'te</b> kırılıyordu. Kök artık <see cref="SolutionLayout.Root"/> üzerinden
    /// geliyor; o, spec'in yerinden bağımsız. <b>Ders:</b> bir kapı, denetlediği dosyanın
    /// yolunu kökü bulmak için kullanmamalı.</para>
    /// </summary>
    private static string SpecYolu()
    {
        const string ad = "0012b-yayin-ve-durum-eylemleri.md";

        string[] adaylar =
        [
            Path.Combine(SolutionLayout.Root, "specs", ad),
            Path.Combine(SolutionLayout.Root, "specs", "done", ad),
        ];

        string? bulunan = Array.Find(adaylar, File.Exists);

        Assert.True(
            bulunan is not null,
            $"`{ad}` ne `specs/` ne `specs/done/` altında bulundu; kapı okuyacak kriter bulamadı.");

        return bulunan!;
    }

    /// <summary>
    /// Karşılaştırma biçimden arındırılıyor: kaçış karakterleri çözülüyor, boşluklar
    /// tekleniyor, sondaki nokta düşüyor. Vurgu işaretleri (`**`, backtick) KALIYOR —
    /// `docs/testing.md` metnin kopyalanmasını istiyor ve vurgu da metnin parçası.
    /// </summary>
    private static string Sadelestir(string value) =>
        string.Join(' ', value.Replace("\\\"", "\"", StringComparison.Ordinal).Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');

    private static IEnumerable<string> TestSourceFiles() =>
        Directory.EnumerateFiles(Path.Combine(SolutionLayout.Root, "tests"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !SolutionLayout.Relative(file).Contains("/obj/", StringComparison.Ordinal))
            .Where(file => !SolutionLayout.Relative(file).Contains("/bin/", StringComparison.Ordinal));

    private static bool Istisna(string relative) =>
        // Yayın komutu: geçiş reddi A2'nin ÖNÜNE geçiyor (D26) — geçiş yürütmüyor, hangi
        // reddin döneceğini seçiyor.
        relative.EndsWith("ListingTransitionCommands.cs", StringComparison.Ordinal)
        // Şube kapanışı: `Satıldı`/`Arşiv` kayda DOKUNMUYOR (AC-28) — geçiş yürütmüyor,
        // hangi kaydın işleneceğini seçiyor.
        || relative.EndsWith("BranchEventHandlers.cs", StringComparison.Ordinal)
        // Okuma tarafı: durumu KOD olarak yanıta koyuyor (Yasak Listesi 22).
        || relative.EndsWith("VehicleAdminQueries.cs", StringComparison.Ordinal)
        || relative.EndsWith("ShowcaseListingProjector.cs", StringComparison.Ordinal)
        || relative.EndsWith("ShowcaseProjectionWriter.cs", StringComparison.Ordinal)
        || relative.EndsWith("ShowcaseSeeder.cs", StringComparison.Ordinal);

    private static IEnumerable<string> ModuleSourceFiles(string module) =>
        SolutionLayout.SourceFiles().Where(file => SolutionLayout.ModuleNameOf(file) == module);

    private static IEnumerable<string> CodeLines(string file, Regex pattern)
    {
        string relative = SolutionLayout.Relative(file);
        string[] lines = File.ReadAllLines(file);

        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].TrimStart();

            if (trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith("///", StringComparison.Ordinal)
                || trimmed.StartsWith("*", StringComparison.Ordinal))
            {
                continue;
            }

            if (pattern.IsMatch(lines[index]))
            {
                yield return $"{relative}:{index + 1}  {trimmed.Trim()}";
            }
        }
    }

    [GeneratedRegex(@"\bListingStatus\.\w+")]
    private static partial Regex StatusAssignment();

    [GeneratedRegex(@"ShowcaseListings\.(Add|Remove|RemoveRange|AddRange)|ShowcaseListings[\s\S]{0,40}ExecuteDelete")]
    private static partial Regex ShowcaseRowWrite();

    [GeneratedRegex(@"DisplayName = ""0012b/AC-(?<no>\d+): (?<metin>(?:[^""\\]|\\.)*)""")]
    private static partial Regex EtiketliDisplayName();

    [GeneratedRegex(@"^- \*\*AC-(?<no>\d+)\.\*\* (?<metin>.+)$", RegexOptions.Multiline)]
    private static partial Regex KriterSatiri();
}
