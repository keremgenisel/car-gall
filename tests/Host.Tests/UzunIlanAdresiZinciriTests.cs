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
/// `0002` E9 · R2 — kaçan hata (2026-11-26): en uzun kanonik adlı ilanın adresi. Marka (≤ 80) ve
/// model (≤ 120) yönetim ucundan GERÇEK `VehicleReference`'a girer; ilan gerçek komutla yayına
/// alınır; ziyaretçi detay belgesini ve sitemap'i okur. Kurulum `SitemapZinciriTests`'inkiyle aynı.
/// </summary>
public sealed class UzunIlanAdresiZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";

    private const string Kok = SeoOrtami.Kok;

    private static readonly XNamespace Ns = SeoEndpoints.SitemapNamespace;

    /// <summary>`SeedInitialDealership` migration'ının açtığı Kadıköy şubesi.</summary>
    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    private const string ReferansKoku = $"{AdminSurface.RoutePrefix}/reference";

    /// <summary>`ReferenceNames`'in sınırları: marka 80, model 120 karakter. Her test kendi markasını açar (tekil ad).</summary>
    private static string EnUzunMarka(char harf) => "Marka" + new string(harf, 75);

    private static readonly string EnUzunModel = "Model " + new string('m', 114);

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

    [Fact(DisplayName = "0002 E9 (kaçan hata): En uzun marka (80) ve model (120) adlı ilan TEKİL uçtan yayına alınır (204); adresi en çok 160 karakter, yıl ve kimlik sonekiyle biter; detay belgesi 200 ve adres sitemap'te")]
    public async Task Yayinlanir_EnUzunKanonikAdli_TekilUc()
    {
        HttpClient personel = await GirisYapmisIstemciAsync();
        Guid arac = await TaslakAsync(personel, await EnUzunModelAsync(personel, 'b'), "UZUNADRES00000001");

        Assert.Equal(HttpStatusCode.NoContent, (await YayinlaAsync(personel, arac)).StatusCode);

        await YayindaVeOkunurAsync(arac, "markabbbbb");
    }

    [Fact(DisplayName = "0002 E9 (kaçan hata): En uzun marka ve model adlı ilan TOPLU yayından (0012c) geçer: satır uygulandı, adres en çok 160 karakter, detay 200 ve sitemap'te")]
    public async Task Yayinlanir_EnUzunKanonikAdli_TopluUc()
    {
        HttpClient personel = await GirisYapmisIstemciAsync();
        Guid arac = await TaslakAsync(personel, await EnUzunModelAsync(personel, 'c'), "UZUNADRES00000002");

        HttpResponseMessage toplu = await personel.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/bulk/publish", new { vehicleIds = new[] { arac } });
        Assert.Equal(HttpStatusCode.OK, toplu.StatusCode);
        JsonElement satir = JsonDocument.Parse(await toplu.Content.ReadAsStringAsync()).RootElement
            .GetProperty("results").EnumerateArray().Single();
        Assert.True(satir.GetProperty("applied").GetBoolean(), satir.ToString());

        await YayindaVeOkunurAsync(arac, "markacccccc");
    }

    [Fact(DisplayName = "0002 E9 (kaçan hata): En uzun adlı ilan satılıp satışı geri alınınca (0023) vitrine AYNI adresle döner: geri alma 204, adres değişmez, detay 200 ve sitemap'te")]
    public async Task AyniAdres_SatisGeriAlininca()
    {
        HttpClient personel = await GirisYapmisIstemciAsync();
        Guid arac = await TaslakAsync(personel, await EnUzunModelAsync(personel, 'd'), "UZUNADRES00000003");
        Assert.Equal(HttpStatusCode.NoContent, (await YayinlaAsync(personel, arac)).StatusCode);
        string once = await SlugAsync(arac);

        Assert.Equal(HttpStatusCode.NoContent, (await personel.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/sold", UriKind.Relative), content: null)).StatusCode);
        Assert.DoesNotContain(Adres(once), await SitemapAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await personel.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{arac}/revert-sale",
            new { reason = "Yanlış araca tıklandı, satış başka araçtaydı.", expectedStatus = "Published" })).StatusCode);

        Assert.Equal(once, await SlugAsync(arac));
        await YayindaVeOkunurAsync(arac, "markadddddd");
    }

    /// <summary>Yayındaki ilanın adresi: en çok 160 karakter, yıl ve kimlik soneki; detay 200; sitemap'te.</summary>
    private async Task YayindaVeOkunurAsync(Guid arac, string bas)
    {
        string slug = await SlugAsync(arac);
        Assert.InRange(slug.Length, 1, 160);
        Assert.StartsWith(bas, slug, StringComparison.Ordinal);
        Assert.Matches("-2019-[0-9a-f]{6}$", slug);
        Assert.EndsWith(await IlanSonekiAsync(arac), slug, StringComparison.Ordinal);

        HttpResponseMessage detay = await Istemci().GetAsync(new Uri($"/tr/cars/{slug}", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, detay.StatusCode);
        Assert.Contains(Adres(slug), await SitemapAsync());
    }

    private async Task<Guid> EnUzunModelAsync(HttpClient yonetici, char harf)
    {
        Assert.Equal(80, EnUzunMarka(harf).Length);
        Assert.Equal(120, EnUzunModel.Length);
        return await ModelEkleAsync(yonetici, await MarkaEkleAsync(yonetici, EnUzunMarka(harf)), EnUzunModel);
    }

    private static string Adres(string slug) => $"{Kok}/tr/cars/{slug}";

    private async Task<IReadOnlyList<string>> SitemapAsync()
    {
        string xml = await Istemci().GetStringAsync(new Uri("/sitemap-tr.xml", UriKind.Relative));

        return [.. XDocument.Parse(xml).Root!.Elements(Ns + "url").Select(url => url.Element(Ns + "loc")!.Value)];
    }

    private async Task<Guid> MarkaEkleAsync(HttpClient yonetici, string ad)
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{ReferansKoku}/brands", new { canonicalName = ad });
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> ModelEkleAsync(HttpClient yonetici, Guid marka, string ad)
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{ReferansKoku}/brands/{marka}/models", new { canonicalName = ad });
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private async Task<string> SlugAsync(Guid arac)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT slug FROM vehicle_catalog.listings WHERE vehicle_id = @id";
        command.Parameters.AddWithValue("id", arac);
        return (string)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>`ListingSlug`'ın soneki: ilan kimliğinin SON altı hex karakteri.</summary>
    private async Task<string> IlanSonekiAsync(Guid arac)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM vehicle_catalog.listings WHERE vehicle_id = @id";
        command.Parameters.AddWithValue("id", arac);
        return ((Guid)(await command.ExecuteScalarAsync())!).ToString("N")[^6..];
    }

    private static Task<HttpResponseMessage> YayinlaAsync(HttpClient personel, Guid arac) =>
        personel.PostAsync(new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/publish", UriKind.Relative), content: null);

    /// <summary>Araç oluşturur ve hazır görsel bağlar; yayına hazır taslağın kimliğini döner.</summary>
    private async Task<Guid> TaslakAsync(HttpClient personel, Guid model, string vin)
    {
        HttpResponseMessage olustur = await personel.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles",
            new
            {
                branchId = SeedSube,
                vehicleModelId = model,
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

        return arac;
    }

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
