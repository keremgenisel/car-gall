using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using AngleSharp.Html.Parser;
using CarGallery.Host.Seo;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0004` **D3** · **D7** — sitemap ve 404 GERÇEK zincirde: personel yayına alır, yayından
/// kaldırır, satar, rezerve eder, arşivler; ziyaretçi sitemap'i ve belgeyi okur. Olayla beslenen
/// bir depo olmadığı için uygulama açılmadan önce yayında olan ilan da ilk okumada vardır.
/// Kurulum `YayinZinciriTests`'inkiyle aynı (`0013` D30/D34 ikinci adım dâhil).
/// </summary>
public sealed class SitemapZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";

    private const string Kok = SeoOrtami.Kok;

    private static readonly XNamespace Ns = SeoEndpoints.SitemapNamespace;

    /// <summary>`SeedInitialDealership` migration'ının açtığı Kadıköy şubesi.</summary>
    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    /// <summary>`SeedInitialVehicleModels` migration'ının ilk satırı.</summary>
    private static readonly Guid SeedModel = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;

    private string ikinciAdimSirri = string.Empty;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());

        factory = YeniFabrika();
        HttpClient kurulum = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = IdentityTestSettings.SeedAdminPassword,
        })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await kurulum.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = IdentityTestSettings.SeedAdminPassword, newPassword = CalisanParola }))
            .StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = CalisanParola,
        })).StatusCode);

        ikinciAdimSirri = await IkinciAdim.KurAsync(kurulum);
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0004/AC-57: Bir ilan yayından kaldırıldıktan sonraki ilk `/sitemap-tr.xml` yanıtında o ilanın adresi yoktur.")]
    public async Task Yok_YayindanKaldirilinca()
    {
        HttpClient personel = await GirisYapmisIstemciAsync();
        string slug = await YayindaIlanAsync(personel, "SITEMAP000000057");
        Assert.Contains(Adres(slug), await SitemapAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await EylemAsync(personel, slug, "unpublish")).StatusCode);

        Assert.DoesNotContain(Adres(slug), await SitemapAsync());
    }

    [Fact(DisplayName = "0004/AC-58: Bir ilan satıldı olarak işaretlendikten sonraki ilk `/sitemap-tr.xml` yanıtında o ilanın adresi yoktur.")]
    public async Task Yok_Satilinca()
    {
        HttpClient personel = await GirisYapmisIstemciAsync();
        string slug = await YayindaIlanAsync(personel, "SITEMAP000000058");
        Assert.Contains(Adres(slug), await SitemapAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await EylemAsync(personel, slug, "sold")).StatusCode);

        Assert.DoesNotContain(Adres(slug), await SitemapAsync());
    }

    [Fact(DisplayName = "0004/AC-59: Rezerve bir ilanın adresi `/sitemap-tr.xml`'de vardır.")]
    public async Task Var_Rezerveyken()
    {
        HttpClient personel = await GirisYapmisIstemciAsync();
        string slug = await YayindaIlanAsync(personel, "SITEMAP000000059");

        Assert.Equal(HttpStatusCode.NoContent, (await EylemAsync(personel, slug, "reserve")).StatusCode);

        Assert.Contains(Adres(slug), await SitemapAsync());
    }

    [Fact(DisplayName = "0004/AC-60: Uygulama açılmadan önce veritabanında yayında olan bir ilan, uygulamanın ilk `/sitemap-tr.xml` yanıtında vardır.")]
    public async Task Var_UygulamaAcilmadanOnceYayindaysa()
    {
        string slug = await YayindaIlanAsync(await GirisYapmisIstemciAsync(), "SITEMAP000000060");

        // Uygulama kapanır ve aynı veritabanıyla yeniden açılır: ikinci uygulama yayın anını hiç görmedi.
        await factory!.DisposeAsync();
        factory = YeniFabrika();

        Assert.Contains(Adres(slug), await SitemapAsync());
    }

    [Fact(DisplayName = "0004/AC-73: Yayından kaldırılmış, satılmış ve arşivlenmiş bir ilanın `/tr/cars/{slug}` istekleri 404 döner. Gövdeleri, hiç var olmamış bir slug'ın gövdesiyle bayt bayt aynıdır.")]
    public async Task Ayni404_GizliVeOlmayanIlan()
    {
        HttpClient personel = await GirisYapmisIstemciAsync();
        string kaldirilan = await YayindaIlanAsync(personel, "SITEMAP000000731");
        string satilan = await YayindaIlanAsync(personel, "SITEMAP000000732");
        string arsivlenen = await YayindaIlanAsync(personel, "SITEMAP000000733");

        Assert.Equal(HttpStatusCode.NoContent, (await EylemAsync(personel, kaldirilan, "unpublish")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await EylemAsync(personel, satilan, "sold")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await EylemAsync(personel, arsivlenen, "unpublish")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await EylemAsync(personel, arsivlenen, "archive")).StatusCode);

        HttpClient ziyaretci = Istemci();
        HttpResponseMessage hicYok = await ziyaretci.GetAsync(new Uri("/tr/cars/hic-olmamis-ilan-2019-000000", UriKind.Relative));
        byte[] beklenen = await hicYok.Content.ReadAsByteArrayAsync();
        Assert.Equal(HttpStatusCode.NotFound, hicYok.StatusCode);

        foreach (string slug in new[] { kaldirilan, satilan, arsivlenen })
        {
            HttpResponseMessage yanit = await ziyaretci.GetAsync(new Uri($"/tr/cars/{slug}", UriKind.Relative));

            Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
            Assert.Equal(beklenen, await yanit.Content.ReadAsByteArrayAsync());
        }
    }

    [Fact(DisplayName = "0004/AC-51: Kapatılmış bir şube `department`'ta yer almaz.")]
    public async Task Yok_KapatilmisSube()
    {
        HttpClient ziyaretci = Istemci();
        Assert.Equal(["Ataşehir Şubesi", "Kadıköy Şubesi"], (await DepartmanlarAsync(ziyaretci)).Order(StringComparer.Ordinal));

        await using (NpgsqlConnection connection = new(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE dealership.branches SET status = 3 WHERE id = @id";
            command.Parameters.AddWithValue("id", SeedSube);
            await command.ExecuteNonQueryAsync();
        }

        Assert.Equal(["Ataşehir Şubesi"], await DepartmanlarAsync(ziyaretci));
    }

    private static async Task<IReadOnlyList<string>> DepartmanlarAsync(HttpClient ziyaretci)
    {
        string html = await ziyaretci.GetStringAsync(new Uri("/tr/", UriKind.Relative));
        string json = Assert.Single(new HtmlParser().ParseDocument(html).QuerySelectorAll("script[type='application/ld+json']")).TextContent;
        JsonElement dealer = JsonDocument.Parse(json).RootElement;

        return dealer.TryGetProperty("department", out JsonElement subeler)
            ? [.. subeler.EnumerateArray().Select(sube => sube.GetProperty("name").GetString()!)]
            : [];
    }

    private static string Adres(string slug) => $"{Kok}/tr/cars/{slug}";

    private async Task<IReadOnlyList<string>> SitemapAsync()
    {
        string xml = await Istemci().GetStringAsync(new Uri("/sitemap-tr.xml", UriKind.Relative));

        return [.. XDocument.Parse(xml).Root!.Elements(Ns + "url").Select(url => url.Element(Ns + "loc")!.Value)];
    }

    /// <summary>Araç oluşturur, hazır görsel bağlar ve yayına alır; ilanın slug'ını döner.</summary>
    private async Task<string> YayindaIlanAsync(HttpClient personel, string vin)
    {
        HttpResponseMessage olustur = await personel.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles",
            new
            {
                branchId = SeedSube,
                vehicleModelId = SeedModel,
                bodyType = "suv",
                fuelType = "diesel",
                transmissionType = "automatic",
                drivetrainType = "front-wheel-drive",
                trimLevel = "comfort",
                color = new Dictionary<string, string> { ["tr"] = "Beyaz" },
                modelYear = 2019,
                mileageKilometers = 45_000,
                priceAmount = 750_000m,
                priceOnRequest = false,
                vin,
            });
        Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);
        Guid arac = JsonDocument.Parse(await olustur.Content.ReadAsStringAsync()).RootElement.GetProperty("vehicleId").GetGuid();

        Guid varlik = await HazirMedyaEkleAsync();
        Assert.Equal(HttpStatusCode.NoContent, (await personel.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{arac}/media",
            new { mediaAssetIds = new[] { varlik } })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await personel.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/publish", UriKind.Relative), content: null)).StatusCode);

        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT slug FROM vehicle_catalog.listings WHERE vehicle_id = @id";
        command.Parameters.AddWithValue("id", arac);
        string slug = (string)(await command.ExecuteScalarAsync())!;

        araclar[slug] = arac;
        return slug;
    }

    private readonly Dictionary<string, Guid> araclar = [];

    private Task<HttpResponseMessage> EylemAsync(HttpClient personel, string slug, string eylem) =>
        personel.PostAsync(new Uri($"{AdminSurface.RoutePrefix}/vehicles/{araclar[slug]}/{eylem}", UriKind.Relative), content: null);

    private async Task<Guid> HazirMedyaEkleAsync()
    {
        Guid varlik = Guid.CreateVersion7();

        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO media.assets (
                id, status, content_type, byte_size, source_width, source_height,
                original_object_key, alt_text, attempt_count, uploaded_at, updated_at)
            VALUES (@id, 3, 'image/jpeg', 1024, 1200, 900, @key, '{}'::jsonb, 1, now(), now());
            INSERT INTO media.asset_derivatives (asset_id, variant_code, width, height, object_key)
            VALUES (@id, 'display', 800, 600, @derivative);
            """;

        command.Parameters.AddWithValue("id", varlik);
        command.Parameters.AddWithValue("key", $"private/originals/{varlik:N}.jpg");
        command.Parameters.AddWithValue("derivative", $"public/assets/{varlik:D}/display.webp");

        await command.ExecuteNonQueryAsync();

        return varlik;
    }

    private WebApplicationFactory<Program> YeniFabrika() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    {
        builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
        builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
        IdentityTestSettings.Apply(builder);
        NotificationsTestSettings.Apply(builder);
        MediaTestSettings.Apply(builder);
    });

    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    private async Task<HttpClient> GirisYapmisIstemciAsync()
    {
        HttpClient client = Istemci();

        await IkinciAdim.GirAsync(client, IdentityTestSettings.SeedAdminLoginId, CalisanParola, ikinciAdimSirri);

        return client;
    }
}
