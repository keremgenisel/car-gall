using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class VitrinListesiTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Yayin = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0001/AC-1: Vitrinde 30 adet Yayında ilan varken liste ilk sayfada 24 ilan gösterir")]
    public async Task IlkSayfa24IlanDoner_30YayindaIlanVarken()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, Listings(30));

        PagedResult<ShowcaseListingDto> page = await ShowcaseQueryHarness.QueryAsync(database);

        Assert.Equal(24, page.Items.Count);
        Assert.Equal(30, page.TotalCount);
        Assert.Equal(1, page.Page);
    }

    [Fact(DisplayName = "0001/AC-4: Liste, vitrin sıralama ağırlığı azalan sırada gelir; ağırlığı eşit iki ilandan yayın tarihi daha yeni olan önce gelir; ağırlığı ve yayın tarihi de eşit iki ilandan ilan kimliği küçük olan önce gelir")]
    public async Task Siralanir_AgirlikSonraYayinTarihiSonraIlanKimligi()
    {
        await database.ResetAsync();

        Guid dusukKimlik = Guid.Parse("00000000-0000-7000-8000-000000000001");
        Guid yuksekKimlik = Guid.Parse("00000000-0000-7000-8000-000000000002");

        ShowcaseListing agirlikliOlan = new ShowcaseListingBuilder().WithSortWeight(10).PublishedAt(Yayin.AddDays(-30)).Build();
        ShowcaseListing yeniYayinlanan = new ShowcaseListingBuilder().WithSortWeight(5).PublishedAt(Yayin).Build();
        ShowcaseListing eskiYayinlanan = new ShowcaseListingBuilder().WithSortWeight(5).PublishedAt(Yayin.AddDays(-1)).WithId(yuksekKimlik).Build();
        ShowcaseListing esitOlanKucukKimlik = new ShowcaseListingBuilder().WithSortWeight(5).PublishedAt(Yayin.AddDays(-1)).WithId(dusukKimlik).Build();

        await ShowcaseQueryHarness.SeedAsync(database, [eskiYayinlanan, yeniYayinlanan, agirlikliOlan, esitOlanKucukKimlik]);

        PagedResult<ShowcaseListingDto> page = await ShowcaseQueryHarness.QueryAsync(database);

        Assert.Equal(agirlikliOlan.ListingId, page.Items[0].ListingId);
        Assert.Equal(yeniYayinlanan.ListingId, page.Items[1].ListingId);
        Assert.Equal(dusukKimlik, page.Items[2].ListingId);
        Assert.Equal(yuksekKimlik, page.Items[3].ListingId);
    }

    [Fact(DisplayName = "0001/AC-5: Vitrin sıralama ağırlığı ve yayın tarihi aynı olan 30 ilan varken birinci ve ikinci sayfa birlikte 30 farklı ilan içerir; tekrar eden veya kaybolan ilan yoktur")]
    public async Task TekrarEtmez_EsitAgirlikVeTarihte_IkiSayfada30Farkli()
    {
        await database.ResetAsync();
        List<ShowcaseListing> listings =
        [
            .. Enumerable.Range(0, 30).Select(_ => new ShowcaseListingBuilder()
                .WithSortWeight(7)
                .PublishedAt(Yayin)
                .Build())
        ];
        await ShowcaseQueryHarness.SeedAsync(database, listings);

        PagedResult<ShowcaseListingDto> birinci = await ShowcaseQueryHarness.QueryAsync(database, page: 1);
        PagedResult<ShowcaseListingDto> ikinci = await ShowcaseQueryHarness.QueryAsync(database, page: 2);

        List<Guid> hepsi = [.. birinci.Items.Select(item => item.ListingId), .. ikinci.Items.Select(item => item.ListingId)];

        Assert.Equal(30, hepsi.Count);
        Assert.Equal(30, hepsi.Distinct().Count());
        Assert.Equal(listings.Select(listing => listing.ListingId).OrderBy(id => id), hepsi.OrderBy(id => id));
    }

    [Fact(DisplayName = "0001/AC-13: Vitrinde 49 yayında ilan varken liste 3 sayfa üretir ve üçüncü sayfa 1 ilan gösterir")]
    public async Task UcSayfaUretilir_49IlandaUcuncuSayfada1Ilan()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, Listings(49));

        PagedResult<ShowcaseListingDto> ucuncu = await ShowcaseQueryHarness.QueryAsync(database, page: 3);

        Assert.Equal(3, ucuncu.TotalPages);
        Assert.Single(ucuncu.Items);
        Assert.Equal(49, ucuncu.TotalCount);
    }

    private static IEnumerable<ShowcaseListing> Listings(int count) =>
        Enumerable.Range(0, count).Select(index => new ShowcaseListingBuilder()
            .WithSortWeight(count - index)
            .PublishedAt(Yayin.AddMinutes(-index))
            .Build());
}
