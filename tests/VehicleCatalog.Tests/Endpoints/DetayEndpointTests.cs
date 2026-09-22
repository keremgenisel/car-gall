using System.Net;
using System.Text.Json;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;

namespace CarGallery.Modules.VehicleCatalog.Tests.Endpoints;

[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class DetayEndpointTests(ShowcaseDatabase database)
{
    private const string Slug = "toyota-corolla-2019-abc123";

    [Fact(DisplayName = "0002/AC-1: Yayında durumundaki bir ilanın adresi istendiğinde o ilanın detayı döner")]
    public async Task Doner200_YayindakiIlanAdresinde()
    {
        await database.ResetAsync();
        await SeedAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage response = await api.Client.GetAsync($"/api/catalog/showcase-listings/{Slug}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory(DisplayName = "0002/AC-5, 0002/AC-6, 0002/AC-7: Var olmayan, Taslak, Arşiv, Satıldı veya lokasyon ataması bekleyen bir ilanın adresi istendiğinde sistem \"bulunamadı\" karşılığı verir")]
    [InlineData("hic-var-olmayan-ilan-999999")]
    [InlineData("taslak-ilan-000001")]
    [InlineData("arsiv-ilan-000002")]
    [InlineData("satildi-ilan-000003")]
    [InlineData("lokasyon-bekleyen-000004")]
    public async Task Doner404_VitrindeGorunmeyenAdresler(string slug)
    {
        await database.ResetAsync();
        await SeedAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage response = await api.Client.GetAsync($"/api/catalog/showcase-listings/{slug}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "0002/AC-8: Var olan ama gizlenen bir ilan ile hiç var olmayan bir ilan için dönen karşılık birbirinden ayırt edilemez: aynı hata kodu, aynı gövde")]
    public async Task AyirtEdilemez_GizlenenVeVarOlmayan()
    {
        await database.ResetAsync();
        await SeedAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage gizlenen = await api.Client.GetAsync("/api/catalog/showcase-listings/taslak-ilan-000001");
        HttpResponseMessage hicYok = await api.Client.GetAsync("/api/catalog/showcase-listings/bambaska-bir-adres-0001");

        Assert.Equal(gizlenen.StatusCode, hicYok.StatusCode);
        Assert.Equal(
            await gizlenen.Content.ReadAsStringAsync(),
            await hicYok.Content.ReadAsStringAsync());
        Assert.Equal(
            gizlenen.Content.Headers.ContentType?.ToString(),
            hicYok.Content.Headers.ContentType?.ToString());
    }

    [Fact(DisplayName = "0002/AC-14: Detay yanıtı şase, plaka ve iç notlar alanlarını hiç içermez; bu alanlar boş değer olarak da bulunmaz")]
    public async Task Bulunmaz_SaseVePlakaVeIcNotlar()
    {
        await database.ResetAsync();
        await SeedAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        string body = await api.Client.GetStringAsync($"/api/catalog/showcase-listings/{Slug}");

        foreach (string forbidden in new[] { "vin", "chassis", "plate", "internalNote", "sase", "plaka" })
        {
            Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(DisplayName = "0002/AC-12, 0002/AC-13: Kapalı veya Gizli işaretli bir opsiyonel alan detay yanıtında hiç bulunmaz; boş değer olarak da bulunmaz")]
    public async Task Bulunmaz_KapaliVeGizliAlanlar()
    {
        await database.ResetAsync();
        await SeedAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        using JsonDocument document = JsonDocument.Parse(
            await api.Client.GetStringAsync($"/api/catalog/showcase-listings/{Slug}"));

        string[] anahtarlar =
        [
            .. document.RootElement.GetProperty("attributes").EnumerateArray()
                .Select(item => item.GetProperty("key").GetString()!)
        ];

        Assert.DoesNotContain("damage_record_amount", anahtarlar);
    }

    [Fact(DisplayName = "0002/AC-2: Vitrin listesindeki bir ilan kartı, o ilanın detay adresine giden bir bağlantı taşır")]
    public async Task ListeYanitiAdresiTasir()
    {
        await database.ResetAsync();
        await SeedAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        using JsonDocument document = JsonDocument.Parse(
            await api.Client.GetStringAsync("/api/catalog/showcase-listings"));

        JsonElement item = document.RootElement.GetProperty("items")[0];
        Assert.Equal(Slug, item.GetProperty("slug").GetString());
    }

    private Task SeedAsync() => ShowcaseQueryHarness.SeedAsync(
        database,
        [
            new ShowcaseListingBuilder()
                .WithSlug(Slug)
                .WithModelYear(2019)
                .WithSpecification("diesel", "automatic", "front-wheel-drive", "comfort")
                .Build()
        ]);
}
