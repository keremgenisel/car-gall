using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using CarGallery.Modules.VehicleCatalog.Tests.Application;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>`0014` R3 — ana sayfanın öne çıkan bölümünün okuması (D3, D4).</summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class OneCikanIlanSorgusuTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Yayin = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0014/AC-17: 8 öne çıkarılmış görünür ilan varken bölüm tam altı kart gösterir")]
    public async Task Doner_TamAlti_SekizOneCikarilmisVarken()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, OneCikarilmis(8));

        Assert.Equal(6, (await SorAsync()).Count);
    }

    [Fact(DisplayName = "0014/AC-18: AC-17'deki altı kart, öne çıkarılmış ilanlar arasında yayın tarihi en yeni altı ilandır ve yeniden eskiye sıralıdır")]
    public async Task Doner_EnYeniAlti_YenidenEskiye()
    {
        await database.ResetAsync();
        List<ShowcaseListing> ilanlar = OneCikarilmis(8);

        // Seed sırası karışık: sonuç seed sırasını değil yayın tarihini izlemeli.
        await ShowcaseQueryHarness.SeedAsync(database, [.. ilanlar.OrderBy(_ => Guid.NewGuid())]);

        IReadOnlyList<ShowcaseListingDto> kartlar = await SorAsync();

        Assert.Equal(
            ilanlar.OrderByDescending(ilan => ilan.PublishedAt).Take(6).Select(ilan => ilan.ListingId),
            kartlar.Select(kart => kart.ListingId));
    }

    [Fact(DisplayName = "0014/AC-19: Yayın tarihi eşit iki öne çıkarılmış ilandan ilan kimliği küçük olan önce gelir (vitrin listesiyle aynı sıra)")]
    public async Task Siralar_KucukKimlikOnce_EsitYayinTarihinde()
    {
        await database.ResetAsync();
        Guid kucuk = Guid.Parse("00000000-0000-7000-8000-000000000001");
        Guid buyuk = Guid.Parse("00000000-0000-7000-8000-000000000002");
        await ShowcaseQueryHarness.SeedAsync(
            database,
            [
                new ShowcaseListingBuilder().WithId(buyuk).WithSortWeight(Listing.FeaturedWeight).PublishedAt(Yayin).Build(),
                new ShowcaseListingBuilder().WithId(kucuk).WithSortWeight(Listing.FeaturedWeight).PublishedAt(Yayin).Build(),
            ]);

        IReadOnlyList<ShowcaseListingDto> kartlar = await SorAsync();
        PagedResult<ShowcaseListingDto> liste = await ShowcaseQueryHarness.QueryAsync(database);

        Assert.Equal([kucuk, buyuk], kartlar.Select(kart => kart.ListingId));
        Assert.Equal(liste.Items.Select(item => item.ListingId), kartlar.Select(kart => kart.ListingId));
    }

    [Fact(DisplayName = "0014/AC-20: 3 öne çıkarılmış ve 10 öne çıkarılmamış görünür ilan varken bölüm yalnız üç kart gösterir; öne çıkarılmamış hiçbir ilan bölümde yer almaz")]
    public async Task Doldurmaz_OneCikarilmamisla_UcVarken()
    {
        await database.ResetAsync();
        List<ShowcaseListing> oneCikan = OneCikarilmis(3);

        // Öne çıkarılmamışlar DAHA YENİ: boşluk doldurulsaydı ilk onlar girerdi.
        List<ShowcaseListing> sade =
        [
            .. Enumerable.Range(0, 10).Select(index => new ShowcaseListingBuilder()
                .WithSortWeight(Listing.NormalWeight)
                .PublishedAt(Yayin.AddDays(30 + index))
                .Build())
        ];
        await ShowcaseQueryHarness.SeedAsync(database, [.. oneCikan, .. sade]);

        IReadOnlyList<ShowcaseListingDto> kartlar = await SorAsync();

        Assert.Equal(3, kartlar.Count);
        Assert.All(kartlar, kart => Assert.Contains(oneCikan, ilan => ilan.ListingId == kart.ListingId));
    }

    [Fact(DisplayName = "0014/AC-21: Hiç öne çıkarılmış görünür ilan yokken (10 öne çıkarılmamış görünür ilan var) bölüm başlığıyla birlikte hiç görünmez; yerinde boş durum veya hata durumu da yoktur")]
    public async Task DonerBos_HicOneCikarilmisYokken()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(
            database,
            Enumerable.Range(0, 10).Select(_ => new ShowcaseListingBuilder().WithSortWeight(Listing.NormalWeight).Build()));

        // Sunucu yarısı: boş liste, hata değil. Bölümün görünmemesi uçta (FeaturedSection.test).
        Assert.Empty(await SorAsync());
    }

    [Fact(DisplayName = "0014/AC-22: Öne çıkarılmış bir Rezerve ilan bölümde Rezerve rozetiyle görünür")]
    public async Task Tasir_RezerveIsareti()
    {
        await database.ResetAsync();
        ShowcaseListing rezerve = new ShowcaseListingBuilder().WithSortWeight(Listing.FeaturedWeight).Reserved().Build();
        await ShowcaseQueryHarness.SeedAsync(database, [rezerve]);

        ShowcaseListingDto kart = Assert.Single(await SorAsync());

        Assert.True(kart.IsReserved);
    }

    [Fact(DisplayName = "0014/AC-23: Öne çıkarılmış Taslak, Satıldı ve Arşiv ilanların hiçbiri bölümde görünmez")]
    public async Task Gostermez_TaslakSatildiArsiv()
    {
        await database.ResetAsync();

        // Gerçek yazma yolu: agregat öne çıkarılıyor, durum değişiyor ve okuma modeli
        // yansıtıcıdan geçiyor. Satıldı ve Arşiv önce VİTRİNDE görünür kılınıyor ki
        // satırın gerçekten kaldırıldığı ölçülsün, hiç yazılmadığı değil.
        await IlanAsync(ilan => Result.Success(), yayinla: false);
        await IlanAsync(ilan => ilan.MarkSold(), yayinla: true);
        await IlanAsync(ilan => ilan.Archive(), yayinla: true);

        Assert.Empty(await SorAsync());
    }

    [Fact(DisplayName = "0014/AC-24: \"Fiyat sorunuz\" ilanın kartı fiyat yerine \"fiyat sorunuz\" metnini gösterir; 0 göstermez")]
    public async Task Tasir_FiyatSorunuz_SifirDegil()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(
            database,
            [new ShowcaseListingBuilder().WithSortWeight(Listing.FeaturedWeight).PriceOnRequest().Build()]);

        ShowcasePriceDto fiyat = Assert.Single(await SorAsync()).Price;

        Assert.True(fiyat.OnRequest);
        Assert.Null(fiyat.Amount);
    }

    [Fact(DisplayName = "0014/AC-25: Para birimi KWD olan kurulumda 12345.500 tutarlı ilanın kartı tutarı üç ondalıkla gösterir")]
    public async Task Tasir_UcOndalik_KWD()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(
            database,
            [new ShowcaseListingBuilder().WithSortWeight(Listing.FeaturedWeight).WithPrice(12345.500m, "KWD", 3).Build()]);

        ShowcasePriceDto fiyat = Assert.Single(await SorAsync()).Price;

        // Sunucu yarısı: tutar ve üs kaybolmadan taşınıyor. Üç ondalıklı BİÇİM uçta
        // (FeaturedSection.test).
        Assert.Equal(12345.500m, fiyat.Amount);
        Assert.Equal("KWD", fiyat.CurrencyCode);
        Assert.Equal(3, fiyat.MinorUnitExponent);
    }

    private async Task<IReadOnlyList<ShowcaseListingDto>> SorAsync()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await new FeaturedListingQueries(context, new FakeModelNameResolver(), ShowcaseQueryHarness.Locales)
            .GetAsync("tr", CancellationToken.None);
    }

    private static List<ShowcaseListing> OneCikarilmis(int adet) =>
    [
        .. Enumerable.Range(0, adet).Select(index => new ShowcaseListingBuilder()
            .WithSortWeight(Listing.FeaturedWeight)
            .PublishedAt(Yayin.AddDays(index))
            .Build())
    ];

    private async Task IlanAsync(Func<Listing, Result> gecis, bool yayinla)
    {
        FakeMediaAssetQueries medya = new();
        Vehicle arac = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"ANASAY{Interlocked.Increment(ref sayac):D11}")
            .Build();
        arac.AttachMediaAsset(medya.Ekle(MediaProcessingStatus.Ready));

        Listing ilan = Listing.CreateDraft(arac.Id, Listing.NormalWeight);
        Assert.True(ilan.Feature().IsSuccess);

        if (yayinla)
        {
            Assert.True(ilan.Publish(
                ListingSlug.Create("Toyota Corolla", arac.ModelYear, ilan.Id).Value,
                new FixedClock(Yayin)).IsSuccess);
        }

        await using (VehicleCatalogDbContext kayit = database.CreateContext())
        {
            kayit.Vehicles.Add(arac);
            kayit.Listings.Add(ilan);
            await kayit.SaveChangesAsync();
        }

        if (yayinla)
        {
            await YansitAsync(arac.Id, medya, _ => Result.Success());
            Assert.Contains(await SorAsync(), kart => kart.ListingId == ilan.Id.Value);
        }

        await YansitAsync(arac.Id, medya, gecis);
    }

    private async Task YansitAsync(VehicleId aracId, FakeMediaAssetQueries medya, Func<Listing, Result> gecis)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle arac = await context.Vehicles.SingleAsync(item => item.Id == aracId);
        Listing ilan = await context.Listings.SingleAsync(item => item.VehicleId == aracId);

        Assert.True(gecis(ilan).IsSuccess);

        await KatalogYazicilari.Yansitma(context, medya).ProjectAsync(arac, ilan, read: null, CancellationToken.None);
        await context.SaveChangesAsync();
    }
}
