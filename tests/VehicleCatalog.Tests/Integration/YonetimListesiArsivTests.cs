using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Application;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012b` **D19**/**D35** — panel listesinin arşiv koşulu ve **R13**'ün bayrak rozeti.
///
/// <para>Arşiv bu projenin "silme"sidir (`0010b` X19); varsayılan listede kalırsa liste
/// sonsuz büyür ve eylem hiçbir şey yapmamış görünür. Aynı koşul <b>toplam sayıya ve özet
/// sayaçlarına</b> da uygulanıyor — iki sayı ayrışırsa "12 araç" yazan bir özetin altında
/// dokuz satır görünür.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class YonetimListesiArsivTests(ShowcaseDatabase database)
{
    private static readonly Locale Tr = Locale.Create("tr").Value;

    private static readonly Locale Ar = Locale.Create("ar").Value;

    private static readonly Locale En = Locale.Create("en").Value;

    private static int sayac;

    [Fact(DisplayName = "0012b/AC-50: Arşivlenen araç panel listesinin varsayılan görünümünde yer almaz")]
    public async Task Yoktur_Arsiv_VarsayilanGorunumde()
    {
        Guid duran = await AracEkleAsync(arsiv: false);
        Guid arsivlenen = await AracEkleAsync(arsiv: true);

        IReadOnlyList<Guid> kimlikler = await ListeAsync(includeArchived: false);

        Assert.Contains(duran, kimlikler);
        Assert.DoesNotContain(arsivlenen, kimlikler);
    }

    [Fact(DisplayName = "0012b/AC-51: Açık bir filtreyle arşivlenen araçlar görülebilir")]
    public async Task Gorulur_Arsiv_AcikFiltreyle()
    {
        Guid arsivlenen = await AracEkleAsync(arsiv: true);

        Assert.Contains(arsivlenen, await ListeAsync(includeArchived: true));
    }

    [Fact(DisplayName = "0012b/AC-52: Arşiv koşulu listenin **toplam sayısına ve özet sayaçlarına** da uygulanır")]
    public async Task Uygulanir_ArsivKosulu_SayilaraDa()
    {
        // DELTA ölçülüyor, mutlak sayı değil: koleksiyon tek bir veritabanını paylaşıyor
        // ve `ResetAsync` yalnız okuma modelini temizliyor. Paylaşılan duruma bağımlı test
        // yazılmaz (`docs/testing.md` § 4) — ilk yazımda mutlak saydım ve test başka
        // sınıfların bıraktığı satırlar yüzünden kırmızı verdi.
        int oncekiToplam = (await Sorgu().ListAsync(Filtre(includeArchived: false), CancellationToken.None)).TotalCount;
        VehicleAdminSummaryDto oncekiOzet = await Sorgu().GetSummaryAsync(CancellationToken.None);

        await AracEkleAsync(arsiv: false);
        await AracEkleAsync(arsiv: true);
        await AracEkleAsync(arsiv: true);

        PagedResult<VehicleAdminListItemDto> sayfa =
            await Sorgu().ListAsync(Filtre(includeArchived: false), CancellationToken.None);

        VehicleAdminSummaryDto ozet = await Sorgu().GetSummaryAsync(CancellationToken.None);

        // Üç araç eklendi, ikisi arşiv: hem liste sayısı hem özet YALNIZ biri kadar arttı.
        Assert.Equal(oncekiToplam + 1, sayfa.TotalCount);
        Assert.Equal(oncekiOzet.TotalCount + 1, ozet.TotalCount);

        // Çeviri eksiği sayacı da aynı evreni sayıyor: arşivlenmiş iki aracın metni de
        // eksik ama ikisi de sayılmıyor.
        Assert.Equal(oncekiOzet.TranslationIncompleteCount + 1, ozet.TranslationIncompleteCount);
    }

    [Fact(DisplayName = "0012b/AC-53: Lokasyon ataması bekleyen araç panel listesinde ayırt edilir")]
    public async Task Ayirt_Edilir_LokasyonAtamasiBekleyen()
    {
        Guid normal = await AracEkleAsync(arsiv: false);
        Guid bekleyen = await AracEkleAsync(arsiv: false, lokasyonBekliyor: true);

        // `RecentlyAdded` sıralaması: iki araç da az önce eklendi ve ilk sayfada.
        PagedResult<VehicleAdminListItemDto> sayfa = await Sorgu().ListAsync(
            new VehicleAdminListFilter("tr", 1, 100, VehicleAdminListSort.RecentlyAdded, false),
            CancellationToken.None);

        Assert.True(sayfa.Items.Single(item => item.VehicleId == bekleyen).AwaitingBranchAssignment);
        Assert.False(sayfa.Items.Single(item => item.VehicleId == normal).AwaitingBranchAssignment);
    }

    private async Task<Guid> AracEkleAsync(bool arsiv, bool lokasyonBekliyor = false)
    {
        Vehicle arac = new VehicleBuilder()
            .WithoutMedia()
            // Guid v7'nin BAŞ baytları zaman damgasıdır: aynı milisaniyede üretilen iki
            // kimlik aynı öneki paylaşır ve kısmi tekil indeks haklı olarak reddeder.
            // Ölçüldü — ilk yazımda `ux_vehicles_vin` düştü.
            .WithVin($"ARSIV{Interlocked.Increment(ref sayac):D12}")
            .Build();

        if (arsiv)
        {
            arac.MarkArchived();
        }

        if (lokasyonBekliyor)
        {
            arac.MarkAwaitingBranchAssignment();
        }

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(arac);
        context.Listings.Add(Listing.CreateDraft(arac.Id, showcaseSortWeight: 0));
        await context.SaveChangesAsync();

        return arac.Id.Value;
    }

    private async Task<IReadOnlyList<Guid>> ListeAsync(bool includeArchived)
    {
        PagedResult<VehicleAdminListItemDto> sayfa = await Sorgu().ListAsync(
            new VehicleAdminListFilter("tr", 1, 100, VehicleAdminListSort.RecentlyAdded, includeArchived),
            CancellationToken.None);

        return [.. sayfa.Items.Select(item => item.VehicleId)];
    }

    private VehicleAdminQueries Sorgu() => new(
        database.CreateContext(),
        new SahteModelSorgusu(),
        new ShowcaseLocaleOptions(Tr, [Tr, En, Ar]),
        new CatalogAdminPagingOptions(25, 100));

    private static VehicleAdminListFilter Filtre(bool includeArchived) =>
        new("tr", 1, 100, VehicleAdminListSort.LastUpdated, includeArchived);
}
