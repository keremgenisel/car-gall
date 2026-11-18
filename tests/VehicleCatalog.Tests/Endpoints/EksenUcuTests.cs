using System.Net;
using System.Text.Json;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;

namespace CarGallery.Modules.VehicleCatalog.Tests.Endpoints;

/// <summary>`0015` C12 — vitrin ucunun yeni parametreleri, telden: bağlama, `200` ve şekil.</summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class EksenUcuTests(ShowcaseDatabase database)
{
    [Fact]
    public async Task Okur_BututunParametreleri_C12Adlariyla()
    {
        await database.ResetAsync();
        ShowcaseListing hedef = new ShowcaseListingBuilder().WithBodyType("suv")
            .WithSpecification("diesel", "automatic", "front-wheel-drive", "comfort")
            .WithModelYear(2020).WithMileage(40_000).WithPrice(800_000m, "TRY", 2).Build();
        await ShowcaseQueryHarness.SeedAsync(database, [hedef, new ShowcaseListingBuilder().WithModelYear(2010).Build()]);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        using JsonDocument belge = JsonDocument.Parse(await api.Client.GetStringAsync(
            "/api/catalog/showcase-listings?bodyType=suv&fuel=diesel&transmission=automatic&yearMin=2018&yearMax=2021&mileageMax=80000&priceMin=500000&priceMax=1500000&sort=price-asc"));

        Assert.Equal(1, belge.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(hedef.ListingId, belge.RootElement.GetProperty("items")[0].GetProperty("listingId").GetGuid());
    }

    [Theory(DisplayName = "0015/AC-12, AC-13, AC-15: Geçersiz aralık ve tanınmayan sort hata değildir; sunucu 200 döner")]
    [InlineData("?yearMin=2021&yearMax=2018")]
    [InlineData("?mileageMax=abc")]
    [InlineData("?mileageMax=-5")]
    [InlineData("?priceMax=500000.5")]
    [InlineData("?sort=cheapest")]
    [InlineData("?fuel=kerosene&transmission=MANUAL")]
    public async Task Doner200_GecersizGirdide(string sorgu)
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, [new ShowcaseListingBuilder().Build(), new ShowcaseListingBuilder().Build()]);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await api.Client.GetAsync($"/api/catalog/showcase-listings{sorgu}");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        using JsonDocument belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
        Assert.Equal(2, belge.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact(DisplayName = "0015/AC-29: 0001'in biçimindeki /tr/cars?bodyType=suv&page=2 aynı listeyi açmaya devam eder (uç yarısı: aynı sorgu aynı sonucu verir)")]
    public async Task Korur_0001Baglantisi()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(
            database,
            Enumerable.Range(0, 30).Select(index => new ShowcaseListingBuilder().WithBodyType(index % 2 == 0 ? "suv" : "sedan").Build()));
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        using JsonDocument belge = JsonDocument.Parse(await api.Client.GetStringAsync("/api/catalog/showcase-listings?bodyType=suv&page=2&pageSize=10"));

        Assert.Equal(15, belge.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, belge.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(5, belge.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact(DisplayName = "0015/AC-41: Gizli/kapalı alanlar ve şase, plaka, iç not liste yanıtında hiç bulunmaz; kart yanıtının şekli değişmez")]
    public async Task Degismez_KartSekli_EksenlerleBirlikte()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, [new ShowcaseListingBuilder().WithSpecification("diesel", "manual", "front-wheel-drive", "comfort").Build()]);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        string govde = await api.Client.GetStringAsync("/api/catalog/showcase-listings?fuel=diesel&transmission=manual&sort=year-desc");
        using JsonDocument belge = JsonDocument.Parse(govde);

        Assert.Equal(
            new[] { "branchName", "coverImageUrl", "isReserved", "listingId", "mileageKilometers", "modelName", "modelYear", "price", "slug" },
            belge.RootElement.GetProperty("items")[0].EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        foreach (string yasak in new[] { "vin", "chassis", "plate", "internalNote", "fuel", "transmission" })
        {
            Assert.DoesNotContain($"\"{yasak}", govde, StringComparison.OrdinalIgnoreCase);
        }
    }
}
