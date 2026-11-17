using System.Net;
using System.Text.Json;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;

namespace CarGallery.Modules.VehicleCatalog.Tests.Endpoints;

/// <summary>
/// `0014` **D27** — öne çıkan ucunun kablosu: rota, modül kaydı ve şekil. Sorgu
/// davranışı `OneCikanIlanSorgusuTests`'te. Kriter öneki taşımaz: buradaki iddia bir
/// kriter değil, uç ile kaydın varlığı (DI kaydı iki kez sessizce eksik kaldı).
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class OneCikanEndpointTests(ShowcaseDatabase database)
{
    [Fact]
    public async Task Doner200_KartSekliyle_OneCikanUcu()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(
            database,
            [new ShowcaseListingBuilder().WithSortWeight(Listing.FeaturedWeight).Build(), new ShowcaseListingBuilder().Build()]);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await api.Client.GetAsync("/api/catalog/featured-listings?locale=tr");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        using JsonDocument belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
        Assert.Equal(1, belge.RootElement.GetArrayLength());
        Assert.Equal(
            new[] { "branchName", "coverImageUrl", "isReserved", "listingId", "mileageKilometers", "modelName", "modelYear", "price", "slug" },
            belge.RootElement[0].EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
    }
}
