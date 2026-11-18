using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>`0015` — ziyaretçi seçimli sıralamalar (R5, R6, R7 · D1, D7, D8).</summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class VitrinSiralamaTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Gun = new(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0015/AC-18: ?sort=newest ilanları yayın tarihi yeniden eskiye sıralar")]
    public async Task Siralar_EnYeni()
    {
        await database.ResetAsync();
        ShowcaseListing eski = new ShowcaseListingBuilder().PublishedAt(Gun.AddDays(-3)).Build();
        ShowcaseListing orta = new ShowcaseListingBuilder().PublishedAt(Gun.AddDays(-2)).Build();
        ShowcaseListing yeni = new ShowcaseListingBuilder().PublishedAt(Gun).Build();
        await ShowcaseQueryHarness.SeedAsync(database, [orta, eski, yeni]);

        Assert.Equal([yeni.ListingId, orta.ListingId, eski.ListingId], await SiraAsync("newest"));
    }

    [Fact(DisplayName = "0015/AC-19: ?sort=price-asc fiyatı artan sıralar; \"fiyat sorunuz\" ilanlar sonda durur")]
    public async Task Siralar_FiyatArtan_SorunuzSonda()
    {
        await database.ResetAsync();
        ShowcaseListing sorunuz = new ShowcaseListingBuilder().PriceOnRequest().PublishedAt(Gun).Build();
        ShowcaseListing ucuz = Fiyat(100_000m);
        ShowcaseListing pahali = Fiyat(900_000m);
        await ShowcaseQueryHarness.SeedAsync(database, [pahali, sorunuz, ucuz]);

        Assert.Equal([ucuz.ListingId, pahali.ListingId, sorunuz.ListingId], await SiraAsync("price-asc"));
    }

    [Fact(DisplayName = "0015/AC-20: ?sort=price-desc fiyatı azalan sıralar; \"fiyat sorunuz\" ilanlar yine sonda durur")]
    public async Task Siralar_FiyatAzalan_SorunuzSonda()
    {
        await database.ResetAsync();
        ShowcaseListing sorunuz = new ShowcaseListingBuilder().PriceOnRequest().PublishedAt(Gun).Build();
        ShowcaseListing ucuz = Fiyat(100_000m);
        ShowcaseListing pahali = Fiyat(900_000m);
        await ShowcaseQueryHarness.SeedAsync(database, [ucuz, sorunuz, pahali]);

        Assert.Equal([pahali.ListingId, ucuz.ListingId, sorunuz.ListingId], await SiraAsync("price-desc"));
    }

    [Fact(DisplayName = "0015/AC-21: ?sort=mileage-asc kilometreyi artan sıralar")]
    public async Task Siralar_KilometreArtan()
    {
        await database.ResetAsync();
        ShowcaseListing az = new ShowcaseListingBuilder().WithMileage(5_000).Build();
        ShowcaseListing cok = new ShowcaseListingBuilder().WithMileage(200_000).Build();
        ShowcaseListing orta = new ShowcaseListingBuilder().WithMileage(60_000).Build();
        await ShowcaseQueryHarness.SeedAsync(database, [cok, az, orta]);

        Assert.Equal([az.ListingId, orta.ListingId, cok.ListingId], await SiraAsync("mileage-asc"));
    }

    [Fact(DisplayName = "0015/AC-22: ?sort=year-desc model yılını azalan, ?sort=year-asc artan sıralar")]
    public async Task Siralar_YilAzalanVeArtan()
    {
        await database.ResetAsync();
        ShowcaseListing y2016 = new ShowcaseListingBuilder().WithModelYear(2016).Build();
        ShowcaseListing y2023 = new ShowcaseListingBuilder().WithModelYear(2023).Build();
        ShowcaseListing y2019 = new ShowcaseListingBuilder().WithModelYear(2019).Build();
        await ShowcaseQueryHarness.SeedAsync(database, [y2019, y2016, y2023]);

        Assert.Equal([y2023.ListingId, y2019.ListingId, y2016.ListingId], await SiraAsync("year-desc"));
        Assert.Equal([y2016.ListingId, y2019.ListingId, y2023.ListingId], await SiraAsync("year-asc"));
    }

    [Fact(DisplayName = "0015/AC-23: Öne çıkarılmış ve en pahalı ilan, ?sort=price-asc ile sonda, ?sort=newest ile yayın tarihine göre yerinde durur; öne çıkarma ağırlığı seçimli sıralamada yok sayılır")]
    public async Task YokSayar_OneCikarma_SecimliSiralamada()
    {
        await database.ResetAsync();
        ShowcaseListing oneCikan = new ShowcaseListingBuilder()
            .WithSortWeight(Listing.FeaturedWeight).WithPrice(5_000_000m, "TRY", 2).PublishedAt(Gun.AddDays(-10)).Build();
        ShowcaseListing a = new ShowcaseListingBuilder().WithPrice(100_000m, "TRY", 2).PublishedAt(Gun).Build();
        ShowcaseListing b = new ShowcaseListingBuilder().WithPrice(200_000m, "TRY", 2).PublishedAt(Gun.AddDays(-1)).Build();
        await ShowcaseQueryHarness.SeedAsync(database, [oneCikan, a, b]);

        Assert.Equal([a.ListingId, b.ListingId, oneCikan.ListingId], await SiraAsync("price-asc"));
        Assert.Equal([a.ListingId, b.ListingId, oneCikan.ListingId], await SiraAsync("newest"));

        // Karşı kanıt: varsayılan sıralamada öne çıkan BAŞTA (0001 AC-4, D7).
        Assert.Equal(oneCikan.ListingId, (await SiraAsync(null))[0]);
    }

    [Fact(DisplayName = "0015/AC-24: Eşitlik kırıcı: aynı fiyatlı iki ilandan yayın tarihi yeni olan önce gelir; fiyat ve yayın tarihi de eşitse ilan kimliği küçük olan önce gelir")]
    public async Task KirarEsitligi_YayinTarihiSonraKimlik()
    {
        await database.ResetAsync();
        Guid kucuk = Guid.Parse("00000000-0000-7000-8000-000000000001");
        Guid buyuk = Guid.Parse("00000000-0000-7000-8000-000000000002");
        ShowcaseListing yeni = new ShowcaseListingBuilder().WithPrice(300_000m, "TRY", 2).PublishedAt(Gun).Build();
        ShowcaseListing eskiBuyuk = new ShowcaseListingBuilder().WithId(buyuk).WithPrice(300_000m, "TRY", 2).PublishedAt(Gun.AddDays(-1)).Build();
        ShowcaseListing eskiKucuk = new ShowcaseListingBuilder().WithId(kucuk).WithPrice(300_000m, "TRY", 2).PublishedAt(Gun.AddDays(-1)).Build();
        await ShowcaseQueryHarness.SeedAsync(database, [eskiBuyuk, yeni, eskiKucuk]);

        Assert.Equal([yeni.ListingId, kucuk, buyuk], await SiraAsync("price-asc"));
        Assert.Equal([yeni.ListingId, kucuk, buyuk], await SiraAsync("price-desc"));
    }

    [Fact(DisplayName = "0015/AC-25: Fiyatı ve yayın tarihi aynı 30 ilan varken ?sort=price-asc ile birinci ve ikinci sayfa birlikte 30 farklı ilan içerir")]
    public async Task TekrarEtmez_IkiSayfada30Farkli()
    {
        await database.ResetAsync();
        List<ShowcaseListing> ilanlar = [.. Enumerable.Range(0, 30).Select(_ => new ShowcaseListingBuilder().WithPrice(250_000m, "TRY", 2).PublishedAt(Gun).Build())];
        await ShowcaseQueryHarness.SeedAsync(database, ilanlar);

        PagedResult<ShowcaseListingDto> bir = await ShowcaseQueryHarness.QueryAsync(database, new ShowcaseListingFilter([], 1, "tr") { Sort = "price-asc" });
        PagedResult<ShowcaseListingDto> iki = await ShowcaseQueryHarness.QueryAsync(database, new ShowcaseListingFilter([], 2, "tr") { Sort = "price-asc" });

        HashSet<Guid> hepsi = [.. bir.Items.Select(item => item.ListingId), .. iki.Items.Select(item => item.ListingId)];
        Assert.Equal(30, bir.Items.Count + iki.Items.Count);
        Assert.Equal(30, hepsi.Count);
    }

    [Fact(DisplayName = "0015/AC-26: sort verilmediğinde sıralama 0001 AC-4'teki gibidir (öne çıkarma ağırlığı, yayın tarihi, ilan kimliği)")]
    public async Task Korur_VarsayilanSiralama()
    {
        await database.ResetAsync();
        Guid kucuk = Guid.Parse("00000000-0000-7000-8000-000000000001");
        Guid buyuk = Guid.Parse("00000000-0000-7000-8000-000000000002");
        ShowcaseListing agirlikli = new ShowcaseListingBuilder().WithSortWeight(Listing.FeaturedWeight).PublishedAt(Gun.AddDays(-30)).Build();
        ShowcaseListing yeni = new ShowcaseListingBuilder().PublishedAt(Gun).Build();
        ShowcaseListing esitBuyuk = new ShowcaseListingBuilder().WithId(buyuk).PublishedAt(Gun.AddDays(-1)).Build();
        ShowcaseListing esitKucuk = new ShowcaseListingBuilder().WithId(kucuk).PublishedAt(Gun.AddDays(-1)).Build();
        await ShowcaseQueryHarness.SeedAsync(database, [esitBuyuk, yeni, agirlikli, esitKucuk]);

        Assert.Equal([agirlikli.ListingId, yeni.ListingId, kucuk, buyuk], await SiraAsync(null));
        // AC-15'in sunucu yarısı: tanınmayan sort varsayılana düşer.
        Assert.Equal([agirlikli.ListingId, yeni.ListingId, kucuk, buyuk], await SiraAsync("cheapest"));
    }

    private async Task<List<Guid>> SiraAsync(string? siralama) =>
        [.. (await ShowcaseQueryHarness.QueryAsync(database, new ShowcaseListingFilter([], 1, "tr") { Sort = siralama }))
            .Items.Select(item => item.ListingId)];

    private static ShowcaseListing Fiyat(decimal tutar) =>
        new ShowcaseListingBuilder().WithPrice(tutar, "TRY", 2).PublishedAt(Gun.AddDays(-5)).Build();
}
