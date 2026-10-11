using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Media.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.EventHandlers;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class OlayIsleyiciTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Simdi = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SubeAdiGuncellenir_BranchDetailsChangedGeldiginde()
    {
        await database.ResetAsync();
        Guid branchId = Guid.CreateVersion7();
        await SeedForBranchAsync(branchId, count: 3);

        await HandleAsync(new BranchDetailsChanged(
            Guid.CreateVersion7(),
            Simdi,
            branchId,
            [new BranchNameDto("tr", "Ataşehir Şubesi"), new BranchNameDto("ar", "فرع أتاشهير")]));

        PagedResult<ShowcaseListingDto> page = await ShowcaseQueryHarness.QueryAsync(database, locale: "tr");

        Assert.Equal(3, page.TotalCount);
        Assert.All(page.Items, item => Assert.Equal("Ataşehir Şubesi", item.BranchName));
    }

    [Fact]
    public async Task SonucDegismez_AyniOlayIkiKezGeldiginde()
    {
        await database.ResetAsync();
        Guid branchId = Guid.CreateVersion7();
        await SeedForBranchAsync(branchId, count: 2);

        // Teslimat en-az-bir-kezdir; handler idempotent olmak zorundadır
        // (docs/architecture.md § 2.C).
        BranchDetailsChanged notification = new(
            Guid.CreateVersion7(),
            Simdi,
            branchId,
            [new BranchNameDto("tr", "Bostancı Şubesi")]);

        await HandleAsync(notification);
        await HandleAsync(notification);

        PagedResult<ShowcaseListingDto> page = await ShowcaseQueryHarness.QueryAsync(database, locale: "tr");

        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, item => Assert.Equal("Bostancı Şubesi", item.BranchName));
    }

    [Fact(DisplayName = "0001/AC-3: \"lokasyon ataması bekliyor\" durumundaki ilanlar listede yer almaz")]
    public async Task VitrindenDuser_SubeKapatildiginda()
    {
        await database.ResetAsync();
        Guid kapatilan = Guid.CreateVersion7();
        Guid acikKalan = Guid.CreateVersion7();
        await SeedForBranchAsync(kapatilan, count: 4);
        await SeedForBranchAsync(acikKalan, count: 2);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            FixedClock saat = new(Simdi);
            BranchDeactivatedHandler handler = new(
                context, KatalogYazicilari.Birim(context, saat), saat);
            await handler.HandleAsync(
                new BranchDeactivated(Guid.CreateVersion7(), Simdi, kapatilan),
                CancellationToken.None);
        }

        PagedResult<ShowcaseListingDto> page = await ShowcaseQueryHarness.QueryAsync(database);

        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task KapakGorseliYazilir_MediaAssetReadyGeldiginde()
    {
        await database.ResetAsync();
        Guid kapakVarligi = Guid.CreateVersion7();
        ShowcaseListing listing = new ShowcaseListingBuilder().Build();
        listing.CoverAssetId = kapakVarligi;
        listing.CoverMediaUrl = null;
        await ShowcaseQueryHarness.SeedAsync(database, [listing]);

        await HandleMediaAsync(kapakVarligi, "https://cdn.example.test/kapak.webp");

        await using VehicleCatalogDbContext read = database.CreateContext();
        ShowcaseListing stored = await read.ShowcaseListings.AsNoTracking()
            .SingleAsync(row => row.ListingId == listing.ListingId);

        Assert.Equal("https://cdn.example.test/kapak.webp", stored.CoverMediaUrl);
    }

    [Fact]
    public async Task KapakDegismez_IkinciGorselinOlayiSonraGeldiginde()
    {
        await database.ResetAsync();

        Guid kapakVarligi = Guid.CreateVersion7();
        Guid ikinciVarlik = Guid.CreateVersion7();

        ShowcaseListing listing = new ShowcaseListingBuilder().Build();
        listing.CoverAssetId = kapakVarligi;
        listing.CoverMediaUrl = null;
        await ShowcaseQueryHarness.SeedAsync(database, [listing]);

        // Medya işleme süreleri farklıdır ve teslimat sıra garantisi vermez
        // (docs/architecture.md § 2.C): ikinci görsel önce hazır olabilir.
        await HandleMediaAsync(ikinciVarlik, "https://cdn.example.test/ikinci.webp");
        await HandleMediaAsync(kapakVarligi, "https://cdn.example.test/kapak.webp");
        await HandleMediaAsync(ikinciVarlik, "https://cdn.example.test/ikinci-yeniden.webp");

        await using VehicleCatalogDbContext read = database.CreateContext();
        ShowcaseListing stored = await read.ShowcaseListings.AsNoTracking()
            .SingleAsync(row => row.ListingId == listing.ListingId);

        // Kapak sıralı listenin ilk elemanıdır; en son hazır olan görsel değil.
        Assert.Equal("https://cdn.example.test/kapak.webp", stored.CoverMediaUrl);
    }

    /// <summary>
    /// `0011`/D2 — handler artık `vehicleId` parametresi ALMIYOR: kapak varlık kimliği tek
    /// başına ilanı belirliyor ve o değer olayın gövdesinde zaten var. Önceki imza
    /// dağıtıcının sözleşmesini uygulamıyordu ve handler kablolanamaz hâldeydi.
    ///
    /// <para><b>Bu iki test KRİTER testi değil, regresyon korumasıdır</b> (`[Fact]`,
    /// `DisplayName` yok). `0011`/AC-37 açıkça şunu istiyor: <i>elle handler çağıran bir
    /// test bunun yerine geçmez</i> — duyurunun dağıtıcı üzerinden ulaştığının kanıtı
    /// `tests/Host.Tests`'tedir. Buradaki ikisi kapak seçiminin DAVRANIŞINI koruyor.</para>
    /// </summary>
    private async Task HandleMediaAsync(Guid mediaAssetId, string url)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        MediaAssetReadyHandler handler = new(context, new FixedClock(Simdi));

        await handler.HandleAsync(
            new MediaAssetReady(
                Guid.CreateVersion7(),
                Simdi,
                mediaAssetId,
                [new MediaDerivativePayload("display", url, 800, 533)]),
            CancellationToken.None);
    }

    private async Task HandleAsync(BranchDetailsChanged notification)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        BranchDetailsChangedHandler handler = new(context, new FixedClock(Simdi));
        await handler.HandleAsync(notification, CancellationToken.None);
    }

    private Task SeedForBranchAsync(Guid branchId, int count)
    {
        List<ShowcaseListing> listings = [];
        for (int index = 0; index < count; index++)
        {
            ShowcaseListing listing = new ShowcaseListingBuilder().Build();
            listing.BranchId = branchId;
            listings.Add(listing);
        }

        return ShowcaseQueryHarness.SeedAsync(database, listings);
    }
}
