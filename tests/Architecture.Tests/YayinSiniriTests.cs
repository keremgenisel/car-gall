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
}
