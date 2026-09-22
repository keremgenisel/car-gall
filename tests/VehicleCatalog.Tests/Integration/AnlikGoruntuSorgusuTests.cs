using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleCatalog.Infrastructure;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// 0005'in `VehicleCatalog.Contracts` genişlemesi. Burada sınanan şey Catalog'un
/// sorumluluğudur: hangi ilanın talebe kaynak OLABİLECEĞİ ve bu bilginin ne kadarını
/// verdiği. Talebin kabul/ret kararı `Leads`'indir ve orada sınanır.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class AnlikGoruntuSorgusuTests(ShowcaseDatabase database)
{
    private const string Slug = "toyota-corolla-2019-abc123";

    [Fact(DisplayName = "0005/AC-17: Talep, ilgilenilen aracın o andaki model adını ve fiyatını kendi içinde taşır")]
    public async Task Doner_KimlikFiyatVeSube_GorunurIlanIcin()
    {
        Guid modelId = Guid.CreateVersion7();
        Guid subeId = Guid.CreateVersion7();

        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder()
                .WithSlug(Slug)
                .WithVehicleModelId(modelId)
                .WithBranchId(subeId)
                .WithPrice(1_250_000m, "TRY", 2)
                .Build()
        ]);

        ListingSnapshotDto? anlikGoruntu = await FindAsync(Slug);

        Assert.NotNull(anlikGoruntu);
        Assert.Equal(modelId, anlikGoruntu.VehicleModelId);
        Assert.Equal(subeId, anlikGoruntu.BranchId);
        Assert.False(anlikGoruntu.IsReserved);
        Assert.False(anlikGoruntu.Price.OnRequest);
        Assert.Equal(1_250_000m, anlikGoruntu.Price.Amount);
        Assert.Equal("TRY", anlikGoruntu.Price.CurrencyCode);
        Assert.Equal(2, anlikGoruntu.Price.MinorUnitExponent);
    }

    [Fact(DisplayName = "0005/AC-21: Fiyatı \"fiyat sorunuz\" olan bir araç için bırakılan talebin anlık görüntüsü \"fiyat sorunuz\" durumunu taşır; tutar `0` olarak kopyalanmaz")]
    public async Task Doner_FiyatSorunuzDurumu_TutarSifirDegil()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder().WithSlug(Slug).PriceOnRequest().Build()
        ]);

        ListingSnapshotDto? anlikGoruntu = await FindAsync(Slug);

        Assert.NotNull(anlikGoruntu);
        Assert.True(anlikGoruntu.Price.OnRequest);
        Assert.Null(anlikGoruntu.Price.Amount);
    }

    [Fact(DisplayName = "0005/AC-22: Kurulum para birimi KWD iken `1250,500` tutarlı bir araç için bırakılan talebin anlık görüntüsü tutarı üç ondalıkla saklar; yuvarlama veya kesme yapılmaz")]
    public async Task Doner_UcOndalikliKwdTutarini_Kesmeden()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder().WithSlug(Slug).WithPrice(1250.505m, "KWD", 3).Build()
        ]);

        ListingSnapshotDto? anlikGoruntu = await FindAsync(Slug);

        Assert.NotNull(anlikGoruntu);
        Assert.Equal(1250.505m, anlikGoruntu.Price.Amount);
        Assert.Equal(3, anlikGoruntu.Price.MinorUnitExponent);
    }

    [Fact(DisplayName = "0005/AC-12: `Rezerve` durumundaki bir ilan için gönderilen talep sunucuda reddedilir ve kayıt oluşmaz")]
    public async Task Bildirir_RezerveOldugunu()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder().WithSlug(Slug).Reserved().Build()
        ]);

        ListingSnapshotDto? anlikGoruntu = await FindAsync(Slug);

        Assert.NotNull(anlikGoruntu);
        Assert.True(anlikGoruntu.IsReserved);
    }

    [Fact(DisplayName = "0005/AC-14: Hiç var olmayan bir ilan için gönderilen talep ile görünür olmayan bir ilan için gönderilen talep aynı karşılığı alır: aynı hata kodu, aynı gövde")]
    public async Task AyniSonuc_VarOlmayanVeGorunmeyenIlan()
    {
        await database.ResetAsync();

        // Vitrin okuma modeli yalnız görünür ilanları taşır (0001): Taslak/Arşiv/Satıldı
        // ilanın satırı HİÇ yoktur. Bu yüzden "görünmüyor" ile "hiç yok" aynı kod yolundan
        // geçer ve aralarında ayrım yapılabilecek bir dal bulunmaz.
        ListingSnapshotDto? gorunmeyen = await FindAsync(Slug);
        ListingSnapshotDto? hicYok = await FindAsync("hic-var-olmayan-ilan-999999");

        Assert.Null(gorunmeyen);
        Assert.Null(hicYok);
    }

    private async Task<ListingSnapshotDto?> FindAsync(string slug)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        ListingSnapshotQueries queries = new(context);

        return await queries.FindBySlugAsync(slug, CancellationToken.None);
    }
}
