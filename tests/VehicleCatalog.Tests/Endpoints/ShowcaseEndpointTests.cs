using System.Net;
using System.Text.Json;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;

namespace CarGallery.Modules.VehicleCatalog.Tests.Endpoints;

[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class ShowcaseEndpointTests(ShowcaseDatabase database)
{
    [Fact(DisplayName = "AC-15: Son sayfanın ötesinde bir sayfa istendiğinde API 200 ve ilk sayfanın içeriğini döner; 4xx dönmez")]
    public async Task Doner200VeIlkSayfa_SonSayfaOtesiIstendiginde()
    {
        await database.ResetAsync();
        await SeedAsync(30);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage ilkSayfa = await api.Client.GetAsync("/api/catalog/showcase-listings");
        HttpResponseMessage otesi = await api.Client.GetAsync("/api/catalog/showcase-listings?page=99");

        Assert.Equal(HttpStatusCode.OK, otesi.StatusCode);
        Assert.Equal(1, await ReadPageNumberAsync(otesi));
        Assert.Equal(await ReadIdsAsync(ilkSayfa), await ReadIdsAsync(otesi));
    }

    [Theory(DisplayName = "AC-16: Sayfa numarası olarak 0, negatif bir sayı veya sayı olmayan bir değer istendiğinde liste ilk sayfayı gösterir, API 200 döner ve ziyaretçiye hata metni gösterilmez")]
    [InlineData("0")]
    [InlineData("-3")]
    [InlineData("abc")]
    [InlineData("")]
    public async Task Doner200VeIlkSayfa_GecersizSayfaDegerinde(string page)
    {
        await database.ResetAsync();
        await SeedAsync(30);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage response = await api.Client.GetAsync($"/api/catalog/showcase-listings?page={page}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await ReadPageNumberAsync(response));

        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("error", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AC-17: ?bodyType=SUV&bodyType=sedan isteği yalnızca sedan ile daraltır")]
    public async Task YalnizTaninanDegerleDaraltir_KarisikKasaTipiIsteginde()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            .. Enumerable.Range(0, 4).Select(_ => new ShowcaseListingBuilder().WithBodyType("sedan").Build()),
            .. Enumerable.Range(0, 3).Select(_ => new ShowcaseListingBuilder().WithBodyType("suv").Build()),
        ]);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage response = await api.Client.GetAsync("/api/catalog/showcase-listings?bodyType=SUV&bodyType=sedan");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, await ReadTotalCountAsync(response));
    }

    [Fact(DisplayName = "AC-23: Liste yanıtı şase, plaka ve iç notlar alanlarını hiç içermez; bu alanlar boş değer olarak da bulunmaz")]
    public async Task Bulunmaz_SaseVePlakaVeIcNotlar_YanitAnahtarlarinda()
    {
        await database.ResetAsync();
        await SeedAsync(1);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        string body = await api.Client.GetStringAsync("/api/catalog/showcase-listings");

        foreach (string forbidden in new[] { "vin", "chassis", "plate", "internalNote", "sase", "plaka" })
        {
            Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(DisplayName = "AC-24: Gizli işaretli bir opsiyonel alan liste yanıtında hiç bulunmaz — boş değer olarak da bulunmaz — ve daraltma ekseni olarak da sunulmaz; gizleme VehicleCatalog'un sorgu tarafında uygulanır")]
    public async Task YanitSekliSabittir_YalnizKartAlanlari()
    {
        await database.ResetAsync();
        await SeedAsync(1);
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        using JsonDocument document = JsonDocument.Parse(await api.Client.GetStringAsync("/api/catalog/showcase-listings"));
        JsonElement item = document.RootElement.GetProperty("items")[0];

        // "slug" 0002/E5 ile eklendi: karttan detaya gidişin adresi sözleşmede taşınmalı.
        // Beklenti sessizce gevşetilmedi — kümeye AÇIKÇA eklendi ve 0001 R9'a not düşüldü.
        string[] expected =
        [
            "listingId", "slug", "isReserved", "coverImageUrl", "modelName",
            "modelYear", "mileageKilometers", "price", "branchName",
        ];

        Assert.Equal(
            expected.OrderBy(name => name, StringComparer.Ordinal),
            item.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
    }

    private Task SeedAsync(int count) => ShowcaseQueryHarness.SeedAsync(
        database,
        Enumerable.Range(0, count).Select(index => new ShowcaseListingBuilder()
            .WithSortWeight(count - index)
            .Build()));

    private static async Task<int> ReadPageNumberAsync(HttpResponseMessage response)
    {
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("page").GetInt32();
    }

    private static async Task<int> ReadTotalCountAsync(HttpResponseMessage response)
    {
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("totalCount").GetInt32();
    }

    private static async Task<List<string>> ReadIdsAsync(HttpResponseMessage response)
    {
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. document.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("listingId").GetString()!)];
    }
}
