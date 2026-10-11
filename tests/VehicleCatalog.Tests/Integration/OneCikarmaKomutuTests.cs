using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Tests.Application;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012c` **R5** — öne çıkarmanın kablosu: vitrin sırası, duyurunun **yokluğu**, zaman
/// damgası ve panelin gördüğü alan.
///
/// <para>Agregatın kuralları `OneCikarmaTests`'te; burada ölçülen şey yazma yolunun
/// gerçekten yansıttığı ve gerçekten **duyurmadığı**.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class OneCikarmaKomutuTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Kayit = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Eylem = new(2026, 10, 12, 16, 45, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0012c/AC-24: Öne çıkarılan ilan vitrin sıralamasının **başında** görünür")]
    public async Task Gorunur_Basta_OneCikarilan()
    {
        FakeMediaAssetQueries medya = new();
        Guid once = await YayindaAracAsync(medya);
        Guid sonra = await YayindaAracAsync(medya);

        // İkinci araç öne çıkarılıyor; yayın tarihleri eşit olduğundan sırayı yalnız
        // ağırlık belirliyor (X2 — kendi aralarında sıralama `0015`'in işi).
        Assert.True((await UygulaAsync(sonra, featured: true, medya)).IsSuccess);

        await using VehicleCatalogDbContext context = database.CreateContext();

        List<Guid> sirali = await context.ShowcaseListings.AsNoTracking()
            .Where(row => row.VehicleId == once || row.VehicleId == sonra)
            .OrderByDescending(row => row.ShowcaseSortWeight)
            .ThenByDescending(row => row.PublishedAt)
            .Select(row => row.VehicleId)
            .ToListAsync();

        Assert.Equal(sonra, sirali[0]);
    }

    [Fact(DisplayName = "0012c/AC-25: Öne çıkarma **duyuru üretmez**: outbox'a satır yazılmaz")]
    public async Task Uretmez_Duyuru_OneCikarma()
    {
        FakeMediaAssetQueries medya = new();
        Guid arac = await YayindaAracAsync(medya);

        await using (VehicleCatalogDbContext temizlik = database.CreateContext())
        {
            await temizlik.OutboxMessages.ExecuteDeleteAsync();
        }

        Assert.True((await UygulaAsync(arac, featured: true, medya)).IsSuccess);
        Assert.True((await UygulaAsync(arac, featured: false, medya)).IsSuccess);

        // TABLO sayılıyor, kod okunmuyor: `IOutboxWriter` tuzağının dersi — duyuru yanlış
        // tabloya değil, HİÇ yazılmıyordu ve bunu yalnız sayı gösterdi. Burada tersi
        // isteniyor: iki başarılı eylem, SIFIR satır.
        await using VehicleCatalogDbContext context = database.CreateContext();

        Assert.Equal(0, await context.OutboxMessages.CountAsync());
    }

    [Fact(DisplayName = "0012c/AC-30: Öne çıkarma aracın **son güncellenme** zamanını ilerletir")]
    public async Task Ilerletir_SonGuncellenmeyi()
    {
        FakeMediaAssetQueries medya = new();
        Guid arac = await YayindaAracAsync(medya);

        Assert.True((await UygulaAsync(arac, featured: true, medya)).IsSuccess);

        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle okunan = await context.Vehicles.AsNoTracking()
            .SingleAsync(item => item.Id == new VehicleId(arac));

        Assert.Equal(Eylem, okunan.UpdatedAt);
    }

    [Fact(DisplayName = "0012c/AC-32: Panel listesi **hangi aracın öne çıkarıldığını** gösterir")]
    public async Task Gosterir_Panel_OneCikarilaniAyirt()
    {
        FakeMediaAssetQueries medya = new();
        Guid sade = await YayindaAracAsync(medya);
        Guid oneCikan = await YayindaAracAsync(medya);

        Assert.True((await UygulaAsync(oneCikan, featured: true, medya)).IsSuccess);

        await using VehicleCatalogDbContext context = database.CreateContext();

        VehicleAdminQueries sorgu = new(
            context,
            new SahteModelSorgusu(),
            new ShowcaseLocaleOptions(Locale.Create("tr").Value, [Locale.Create("tr").Value]),
            new CatalogAdminPagingOptions(25, 100));

        PagedResult<VehicleAdminListItemDto> sayfa = await sorgu.ListAsync(
            new VehicleAdminListFilter("tr", 1, 100, VehicleAdminListSort.RecentlyAdded, false),
            CancellationToken.None);

        Assert.True(sayfa.Items.Single(item => item.VehicleId == oneCikan).IsFeatured);
        Assert.False(sayfa.Items.Single(item => item.VehicleId == sade).IsFeatured);
    }

    private async Task<Guid> YayindaAracAsync(FakeMediaAssetQueries medya)
    {
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);

        Vehicle arac = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"ONECIK{Interlocked.Increment(ref sayac):D11}")
            .CreatedAt(Kayit)
            .Build();

        arac.AttachMediaAsset(varlik);

        Listing ilan = Listing.CreateDraft(arac.Id, Listing.NormalWeight);

        Assert.True(ilan.Publish(
            ListingSlug.Create("Toyota Corolla", arac.ModelYear, ilan.Id).Value,
            new FixedClock(Kayit)).IsSuccess);

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(arac);
        context.Listings.Add(ilan);
        await context.SaveChangesAsync();

        await using VehicleCatalogDbContext yansitma = database.CreateContext();
        Vehicle okunan = await yansitma.Vehicles.SingleAsync(item => item.Id == arac.Id);
        Listing okunanIlan = await yansitma.Listings.SingleAsync(item => item.VehicleId == arac.Id);

        await KatalogYazicilari.Yansitma(yansitma, medya)
            .ProjectAsync(okunan, okunanIlan, read: null, CancellationToken.None);

        await yansitma.SaveChangesAsync();

        return arac.Id.Value;
    }

    private async Task<Result> UygulaAsync(Guid aracId, bool featured, FakeMediaAssetQueries medya)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(Eylem);

        ListingFeatureCommands komut = new(
            context,
            KatalogYazicilari.Birim(context, saat),
            KatalogYazicilari.Yansitma(context, medya),
            InvariantGuardOptions.Default,
            saat);

        return await komut.ExecuteAsync(aracId, featured, CancellationToken.None);
    }
}
