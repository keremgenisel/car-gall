using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.EventHandlers;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// **A8**'in yazma tarafı (`0012b` **D15**, **D32**, **D33**), gerçek PostgreSQL üzerinde.
///
/// <para>Bu spec'e kadar işleyici YALNIZ vitrin satırını siliyordu: ilan `Yayında`
/// kalıyor, aracın bayrağını kimse yazmıyordu ve biri yeniden yayınladığında araç
/// <b>ölü bir şubeyle</b> vitrine dönüyordu — sessizce.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class SubeKapanisiTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Kapanis = new(2026, 10, 11, 16, 0, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0012b/AC-26: Şube kapatıldığında o şubedeki `Yayında` ilanlar yayından kalkar ve aracın \"lokasyon ataması bekliyor\" bayrağı **aynı işlemde** yazılır")]
    public async Task Kalkar_YayindanVeBayrak_AyniIslemde()
    {
        Guid sube = Guid.CreateVersion7();
        Guid aracId = await AracEkleAsync(sube, ListingStatus.Published);

        await KapatAsync(sube);

        (ListingStatus durum, bool bekliyor) = await HalAsync(aracId);

        Assert.Equal(ListingStatus.Draft, durum);
        Assert.True(bekliyor);
    }

    /// <summary>
    /// **D32** — `Rezerve` ilanlar da kapsanıyor. Önceki hâli onları çıkışı olmayan bir
    /// hâlde bırakıyordu: vitrinden bayrakla düşüyor, durumu `Rezerve` kalıyor ve oradan
    /// tek çıkış `Satıldı` oluyordu.
    /// </summary>
    [Fact(DisplayName = "0012b/AC-27: Aynı yol `Rezerve` ilanları da kapsar")]
    public async Task Kapsar_RezerveIlanlari()
    {
        Guid sube = Guid.CreateVersion7();
        Guid aracId = await AracEkleAsync(sube, ListingStatus.Reserved);

        await KapatAsync(sube);

        (ListingStatus durum, bool bekliyor) = await HalAsync(aracId);

        Assert.Equal(ListingStatus.Draft, durum);
        Assert.True(bekliyor);
    }

    [Theory(DisplayName = "0012b/AC-28: `Satıldı` ve `Arşiv` kayıtlara dokunulmaz")]
    [InlineData(nameof(ListingStatus.Sold))]
    [InlineData(nameof(ListingStatus.Archived))]
    public async Task Dokunulmaz_SatildiVeArsiv(string durumAdi)
    {
        ListingStatus baslangic = Enum.Parse<ListingStatus>(durumAdi);
        Guid sube = Guid.CreateVersion7();
        Guid aracId = await AracEkleAsync(sube, baslangic);

        await KapatAsync(sube);

        (ListingStatus durum, bool bekliyor) = await HalAsync(aracId);

        // Satılmış bir aracı "lokasyon bekliyor" yapmak, kapanan şubenin satış geçmişini
        // bozardı; arşiv ise zaten emekliye ayrılmış bir kayıt.
        Assert.Equal(baslangic, durum);
        Assert.False(bekliyor);
    }

    [Fact(DisplayName = "0012b/AC-39: Şube kapanışı yolundaki geçişler de duyurulur")]
    public async Task Duyurulur_SubeKapanisiYolundakiGecisler()
    {
        Guid sube = Guid.CreateVersion7();
        await AracEkleAsync(sube, ListingStatus.Published);
        await AracEkleAsync(sube, ListingStatus.Reserved);

        await KapatAsync(sube);

        await using VehicleCatalogDbContext context = database.CreateContext();
        List<string> tipler = await context.OutboxMessages.AsNoTracking()
            .Select(message => message.Type)
            .ToListAsync();

        // D33 — duyuru personelin EYLEMİNE değil, olgunun kendisine ait.
        Assert.Equal(2, tipler.Count(tip => tip.EndsWith("VehicleUnpublished", StringComparison.Ordinal)));
    }

    [Fact(DisplayName = "0012b/AC-29: Bayraklı araç yayına alınamaz")]
    public async Task Alinamaz_Yayina_BayrakliArac()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid sube = Guid.CreateVersion7();
        Guid aracId = await AracEkleAsync(sube, ListingStatus.Published, varlik);

        await KapatAsync(sube);

        // Şube kapandı, ilan taslağa döndü ve bayrak yazıldı. Yeniden yayınlamak,
        // aracı ÖLÜ bir şubeyle vitrine çıkarırdı — A8'in bütün gerekçesi bu.
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(Kapanis);

        ListingTransitionCommands komut = new(
            context,
            KatalogYazicilari.Birim(context, saat),
            KatalogYazicilari.Yansitma(context, medya),
            medya,
            new FakeModelNameResolver(),
            InvariantGuardOptions.Default,
            saat);

        Result sonuc = await komut.ExecuteAsync(aracId, ListingAction.Publish, CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Contains(
            "vehicle.cannot_publish.branch_unassigned",
            sonuc.Error.Parameters!["reasons"],
            StringComparison.Ordinal);
    }

    /// <summary>
    /// **D33** — eylem şubedeki **bütün** araçlara uygulanır ve seçim üst sınırına tabi
    /// değildir. Kısmi başarı bir PERSONEL kararının karşılığıdır; sistemin kendi
    /// tepkisinde yarım uygulanmış bir şube kapanışı A8'i deler.
    /// </summary>
    [Fact(DisplayName = "0012c/AC-41: Şube kapanışı yolu seçim üst sınırına **tabi değildir**")]
    public async Task Uygulanir_ButunAraclara_SecimSinirindanBagimsiz()
    {
        Guid sube = Guid.CreateVersion7();

        // Araç sayısı seçim üst sınırının BİR FAZLASI. Önceki hâli 12 araçla koşuyordu ve
        // sınır 100'dü: "sınıra tabi değil" cümlesini sınırın altında kalarak ölçüyordu,
        // yani hiç ölçmüyordu. Sayı dağıtım varsayılanından geliyor (`0010b` C26) ve
        // sınırın yapılandırmadan okunduğunu `0012c`/AC-7 ayrıca sınıyor.
        const int secimSiniri = 100;

        for (int index = 0; index < secimSiniri + 1; index++)
        {
            await AracEkleAsync(sube, ListingStatus.Published);
        }

        await KapatAsync(sube);

        await using VehicleCatalogDbContext context = database.CreateContext();
        int bekleyen = await context.Vehicles.AsNoTracking()
            .CountAsync(vehicle => vehicle.BranchId == sube && vehicle.AwaitingBranchAssignment);

        // Kısmi başarı bir PERSONEL kararının karşılığıdır; sistemin kendi tepkisinde
        // yarım uygulanmış bir şube kapanışı A8'i deler (**D33**).
        Assert.Equal(secimSiniri + 1, bekleyen);
    }

    private async Task<Guid> AracEkleAsync(Guid sube, ListingStatus durum, Guid? medya = null)
    {
        Vehicle arac = new VehicleBuilder()
            .WithoutMedia()
            .WithBranch(sube)
            .WithVin($"SUBE{Interlocked.Increment(ref sayac):D13}")
            .Build();

        if (medya is not null)
        {
            arac.AttachMediaAsset(medya.Value);
        }

        Listing listing = Listing.CreateDraft(arac.Id, showcaseSortWeight: 0);
        FixedClock saat = new(Kapanis);

        if (durum is not ListingStatus.Draft)
        {
            listing.Publish(ListingSlug.Create("Toyota Corolla", 2019, listing.Id).Value, saat);

            switch (durum)
            {
                case ListingStatus.Reserved:
                    listing.Reserve();
                    break;
                case ListingStatus.Sold:
                    listing.MarkSold();
                    break;
                case ListingStatus.Archived:
                    listing.Archive();
                    arac.MarkArchived();
                    break;
                case ListingStatus.Draft:
                case ListingStatus.Published:
                default:
                    break;
            }
        }

        listing.ClearDomainEvents();

        await using VehicleCatalogDbContext context = database.CreateContext();
        await context.OutboxMessages.ExecuteDeleteAsync();
        context.Vehicles.Add(arac);
        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        return arac.Id.Value;
    }

    private async Task KapatAsync(Guid sube)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(Kapanis);
        BranchDeactivatedHandler handler = new(context, KatalogYazicilari.Birim(context, saat), saat);

        await handler.HandleAsync(
            new BranchDeactivated(Guid.CreateVersion7(), Kapanis, sube),
            CancellationToken.None);
    }

    private async Task<(ListingStatus Durum, bool Bekliyor)> HalAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Vehicle arac = await context.Vehicles.AsNoTracking()
            .SingleAsync(item => item.Id == new VehicleId(aracId));

        ListingStatus durum = await context.Listings.AsNoTracking()
            .Where(listing => listing.VehicleId == arac.Id)
            .Select(listing => listing.Status)
            .SingleAsync();

        return (durum, arac.AwaitingBranchAssignment);
    }
}
