using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class DaraltmaTests(ShowcaseDatabase database)
{
    [Fact(DisplayName = "AC-6: Tek bir kasa tipi seçildiğinde liste yalnızca o kasa tipindeki yayında ilanları gösterir")]
    public async Task Daraltilir_TekKasaTipiSecildiginde()
    {
        await database.ResetAsync();
        await SeedKarisikAsync();

        PagedResult<ShowcaseListingDto> page = await ShowcaseQueryHarness.QueryAsync(database, bodyTypes: ["suv"]);

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(3, page.Items.Count);
    }

    [Fact(DisplayName = "AC-7: İki kasa tipi seçildiğinde liste, bu iki kasa tipinden herhangi birine uyan yayında ilanları gösterir")]
    public async Task Daraltilir_IkiKasaTipininHerhangiBirineUyanlarla()
    {
        await database.ResetAsync();
        await SeedKarisikAsync();

        PagedResult<ShowcaseListingDto> page = await ShowcaseQueryHarness.QueryAsync(database, bodyTypes: ["suv", "hatchback"]);

        Assert.Equal(5, page.TotalCount);
    }

    [Fact(DisplayName = "AC-9: Seçim temizlendiğinde liste, daraltma öncesindeki tüm yayında ilan kümesine döner")]
    public async Task Daraltmasiz_TumYayindaIlanlar_SecimTemizlendiginde()
    {
        await database.ResetAsync();
        await SeedKarisikAsync();

        PagedResult<ShowcaseListingDto> daraltilmis = await ShowcaseQueryHarness.QueryAsync(database, bodyTypes: ["suv"]);
        PagedResult<ShowcaseListingDto> temizlenmis = await ShowcaseQueryHarness.QueryAsync(database, bodyTypes: []);

        Assert.Equal(3, daraltilmis.TotalCount);
        Assert.Equal(9, temizlenmis.TotalCount);
    }

    [Fact(DisplayName = "AC-17: Tanınmayan bir kasa tipi değeri istendiğinde o değer yok sayılır; tanınan değerler varsa onlarla daraltma yapılır, hiç tanınan değer yoksa daraltmasız liste gösterilir")]
    public async Task YokSayar_TaninmayanKasaTipiDegerini()
    {
        await database.ResetAsync();
        await SeedKarisikAsync();

        PagedResult<ShowcaseListingDto> yalnizTaninmayan = await ShowcaseQueryHarness.QueryAsync(database, bodyTypes: ["SUV"]);
        PagedResult<ShowcaseListingDto> karisik = await ShowcaseQueryHarness.QueryAsync(database, bodyTypes: ["SUV", "sedan"]);

        Assert.Equal(9, yalnizTaninmayan.TotalCount);
        Assert.Equal(4, karisik.TotalCount);
    }

    [Fact(DisplayName = "AC-17: Eşleştirme harfe duyarlıdır: ?bodyType=SUV tanınmayan değerdir")]
    public async Task HarfeDuyarlidir_KasaTipiEslestirmesi()
    {
        await database.ResetAsync();
        await SeedKarisikAsync();

        PagedResult<ShowcaseListingDto> buyukHarfli = await ShowcaseQueryHarness.QueryAsync(database, bodyTypes: ["Suv"]);
        PagedResult<ShowcaseListingDto> kucukHarfli = await ShowcaseQueryHarness.QueryAsync(database, bodyTypes: ["suv"]);

        Assert.Equal(9, buyukHarfli.TotalCount);
        Assert.Equal(3, kucukHarfli.TotalCount);
    }

    /// <summary>4 sedan · 3 suv · 2 hatchback = 9 ilan.</summary>
    private Task SeedKarisikAsync()
    {
        List<ShowcaseListing> listings = [];
        listings.AddRange(Enumerable.Range(0, 4).Select(_ => new ShowcaseListingBuilder().WithBodyType("sedan").Build()));
        listings.AddRange(Enumerable.Range(0, 3).Select(_ => new ShowcaseListingBuilder().WithBodyType("suv").Build()));
        listings.AddRange(Enumerable.Range(0, 2).Select(_ => new ShowcaseListingBuilder().WithBodyType("hatchback").Build()));

        return ShowcaseQueryHarness.SeedAsync(database, listings);
    }
}
