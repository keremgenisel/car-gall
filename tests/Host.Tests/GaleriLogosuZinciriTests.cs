using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0026` — galeri logosunun bağı ve vitrin belgesi, gerçek boru hattında: atama ve kaldırma uçları
/// (D9), sürüm (C9), Yönetici politikası (D4), Host'un logo çözümü (D3) ve araca bağlama reddi
/// (D16). Medya varlıkları SQL ile kurulur (hazır, türü ve türevleriyle): depo gerekmez. Yükleme
/// ucunun kendisi `LogoYuklemeUcuTests`'te (LocalStack).
/// </summary>
public sealed class GaleriLogosuZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";
    private const string Galeri = $"{AdminSurface.RoutePrefix}/gallery";
    private const string Logo = $"{AdminSurface.RoutePrefix}/gallery/logo";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;
    private HttpClient yonetici = null!;
    private HttpClient editor = null!;
    private HttpClient danisman = null!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);
        });

        HttpClient kurulum = Istemci();
        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = IdentityTestSettings.SeedAdminPassword,
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await kurulum.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = IdentityTestSettings.SeedAdminPassword, newPassword = CalisanParola })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = CalisanParola,
        })).StatusCode);
        string sir = await IkinciAdim.KurAsync(kurulum);

        yonetici = Istemci();
        await IkinciAdim.GirAsync(yonetici, IdentityTestSettings.SeedAdminLoginId, CalisanParola, sir);

        editor = await PersonelAsync("editor@ornek.com", "content_editor");
        danisman = await PersonelAsync("danisman@ornek.com", "sales_advisor");

        // Görünen ad zorunlu (0020 AC-5); belge kriterleri `AutoDealer` bloğuna bakar.
        Assert.Equal(HttpStatusCode.OK, (await ProfilKaydetAsync(await SurumAsync())).StatusCode);
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    // ----- Atama, değiştirme, kaldırma (R1, C9 · D2, D8, D9) -----

    [Fact(DisplayName = "0026/AC-11: Logo atandıktan sonra panelin profil okuması o kimliği taşır; sürüm bir artar ve yanıt yeni sürümü döner.")]
    public async Task Atar_Logo_SurumArtar()
    {
        Guid logo = await LogoVarligiAsync();
        int surum = await SurumAsync();

        HttpResponseMessage yanit = await yonetici.PutAsJsonAsync(Logo, new { mediaAssetId = logo, version = surum });

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal(surum + 1, (await yanit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32());
        JsonElement profil = await ProfilAsync();
        Assert.Equal(logo, profil.GetProperty("logoMediaAssetId").GetGuid());
        Assert.Equal(surum + 1, profil.GetProperty("version").GetInt32());
    }

    [Fact(DisplayName = "0026/AC-12: Eski sürümle logo ataması `409` döner; logo değişmez.")]
    public async Task Doner409_EskiSurum()
    {
        Guid eski = await AtaAsync(await LogoVarligiAsync());
        int bayat = await SurumAsync() - 1;

        HttpResponseMessage yanit = await yonetici.PutAsJsonAsync(Logo, new { mediaAssetId = await LogoVarligiAsync(), version = bayat });

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
        Assert.Equal(eski, (await ProfilAsync()).GetProperty("logoMediaAssetId").GetGuid());
    }

    [Fact(DisplayName = "0026/AC-13: Kaldırmak profil okumasında logo kimliğini null yapar ve sürümü artırır; kaldırılan varlığın Media okuması 200 dönmeye devam eder.")]
    public async Task Kaldirir_BagiSiler_VarlikDurur()
    {
        Guid logo = await AtaAsync(await LogoVarligiAsync());
        int surum = await SurumAsync();

        HttpResponseMessage yanit = await yonetici.DeleteAsync($"{Logo}?version={surum}");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        JsonElement profil = await ProfilAsync();
        Assert.Equal(JsonValueKind.Null, profil.GetProperty("logoMediaAssetId").ValueKind);
        Assert.Equal(surum + 1, profil.GetProperty("version").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await yonetici.GetAsync($"{AdminSurface.RoutePrefix}/media/assets/{logo}")).StatusCode);
    }

    [Fact(DisplayName = "0026/AC-14: Logolu galeriye başka bir logo atamak eskisinin yerine yenisini koyar.")]
    public async Task Degistirir_Logo()
    {
        await AtaAsync(await LogoVarligiAsync());
        Guid yeni = await AtaAsync(await LogoVarligiAsync());

        Assert.Equal(yeni, (await ProfilAsync()).GetProperty("logoMediaAssetId").GetGuid());
    }

    [Fact(DisplayName = "0026/AC-25, AC-73, AC-74: Aynı ekranda dönen sürümle profil kaydı, ardından logo ataması, ardından profil kaydı ve logo kaldırması `409` vermez.")]
    public async Task Surum_Benimsenir_AyniEkranda()
    {
        int surum = await SurumAsync();
        HttpResponseMessage profil = await ProfilKaydetAsync(surum);
        Assert.Equal(HttpStatusCode.OK, profil.StatusCode);
        surum = (await profil.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();

        HttpResponseMessage atama = await yonetici.PutAsJsonAsync(Logo, new { mediaAssetId = await LogoVarligiAsync(), version = surum });
        Assert.Equal(HttpStatusCode.OK, atama.StatusCode);
        surum = (await atama.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();

        HttpResponseMessage ikinci = await ProfilKaydetAsync(surum);
        Assert.Equal(HttpStatusCode.OK, ikinci.StatusCode);
        surum = (await ikinci.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("version").GetInt32();

        Assert.Equal(HttpStatusCode.OK, (await yonetici.DeleteAsync($"{Logo}?version={surum}")).StatusCode);
    }

    // ----- Yetki (R8 · D4) -----

    [Fact(DisplayName = "0026/AC-15, AC-16: Satış Danışmanı ve İçerik Editörü'nün logo ataması ve kaldırması `403`; oturumsuz `401`; logo değişmez.")]
    public async Task Yetki()
    {
        Guid logo = await AtaAsync(await LogoVarligiAsync());
        int surum = await SurumAsync();
        Guid baska = await LogoVarligiAsync();

        foreach (HttpClient rol in new[] { danisman, editor })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await rol.PutAsJsonAsync(Logo, new { mediaAssetId = baska, version = surum })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await rol.DeleteAsync($"{Logo}?version={surum}")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Istemci().PutAsJsonAsync(Logo, new { mediaAssetId = baska, version = surum })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Istemci().DeleteAsync($"{Logo}?version={surum}")).StatusCode);
        Assert.Equal(logo, (await ProfilAsync()).GetProperty("logoMediaAssetId").GetGuid());
        Assert.Equal(surum, await SurumAsync());
    }

    // ----- Araca bağlama (D16) -----

    [Fact(DisplayName = "0026/AC-69: Logo türündeki varlıkla araç medyası kaydetmesi telden `422` ve `vehicle.media_asset_wrong_kind` döner.")]
    public async Task Doner422_LogoAraca()
    {
        Guid logo = await LogoVarligiAsync();
        Guid arac = await AracAsync();

        HttpResponseMessage yanit = await yonetici.PutAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles/{arac}/media", new { mediaAssetIds = new[] { logo } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, yanit.StatusCode);
        Assert.Equal("vehicle.media_asset_wrong_kind", (await yanit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
    }

    // ----- Yardımcılar -----

    private async Task<Guid> AtaAsync(Guid logo)
    {
        HttpResponseMessage yanit = await yonetici.PutAsJsonAsync(Logo, new { mediaAssetId = logo, version = await SurumAsync() });
        Assert.True(yanit.StatusCode == HttpStatusCode.OK, $"{(int)yanit.StatusCode}: {await yanit.Content.ReadAsStringAsync()}");
        return logo;
    }

    private async Task<HttpResponseMessage> ProfilKaydetAsync(int surum) =>
        await yonetici.PutAsJsonAsync(Galeri, new
        {
            version = surum,
            commercialName = "Oto Galeri",
            displayNames = new Dictionary<string, string> { ["tr"] = "Oto Galeri Vitrin", ["ar"] = "معرض أوتو" },
            socialLinks = new Dictionary<string, string> { ["instagram"] = "https://www.instagram.com/otogaleri" },
        });

    private async Task<JsonElement> ProfilAsync() =>
        JsonDocument.Parse(await yonetici.GetStringAsync(Galeri)).RootElement;

    private async Task<int> SurumAsync() => (await ProfilAsync()).GetProperty("version").GetInt32();

    /// <summary>Hazır bir medya varlığı; tür ve türevleri gerçek biçimde (`logo-small` 320×80, `logo-large` 1200×300).</summary>
    internal async Task<Guid> LogoVarligiAsync(int durum = 3, short tur = 2)
    {
        Guid varlik = Guid.CreateVersion7();

        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO media.assets (
                id, status, content_type, byte_size, source_width, source_height,
                original_object_key, alt_text, attempt_count, uploaded_at, updated_at, kind)
            VALUES (@id, @durum, 'image/png', 1024, 2000, 500, @key, '{}'::jsonb, 1, now(), now(), @tur);
            """;
        command.Parameters.AddWithValue("id", varlik);
        command.Parameters.AddWithValue("durum", durum);
        command.Parameters.AddWithValue("tur", tur);
        command.Parameters.AddWithValue("key", $"private/originals/{varlik:N}.png");
        await command.ExecuteNonQueryAsync();

        if (durum == 3)
        {
            foreach ((string ad, int en, int boy) in tur == 2
                ? new[] { ("logo-small", 320, 80), ("logo-large", 1200, 300) }
                : new[] { ("display", 800, 200) })
            {
                await using NpgsqlCommand turev = connection.CreateCommand();
                turev.CommandText = "INSERT INTO media.asset_derivatives (asset_id, variant_code, width, height, object_key) VALUES (@id, @ad, @en, @boy, @key)";
                turev.Parameters.AddWithValue("id", varlik);
                turev.Parameters.AddWithValue("ad", ad);
                turev.Parameters.AddWithValue("en", en);
                turev.Parameters.AddWithValue("boy", boy);
                turev.Parameters.AddWithValue("key", $"public/assets/{varlik:D}/{ad}.webp");
                await turev.ExecuteNonQueryAsync();
            }
        }

        return varlik;
    }

    private async Task<Guid> AracAsync()
    {
        Guid sube = JsonDocument.Parse(await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/branches?locale=tr")).RootElement[0].GetProperty("branchId").GetGuid();
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles", new
        {
            branchId = sube,
            vehicleModelId = Guid.Parse("0192c000-0000-7000-8000-00000000000a"),
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
            vin = $"LOGO{Guid.NewGuid().ToString("N")[..13].ToUpperInvariant()}",
        });
        Assert.True(yanit.StatusCode == HttpStatusCode.Created, await yanit.Content.ReadAsStringAsync());
        return (await yanit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("vehicleId").GetGuid();
    }

    private async Task<HttpClient> PersonelAsync(string loginId, string role)
    {
        Assert.Equal(HttpStatusCode.Created, (await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/staff", new
        {
            displayName = "Galeri Personeli",
            loginId,
            initialPassword = "ilk-parola-1234",
            role,
            preferredLocale = "tr",
            branchId = (Guid?)null,
        })).StatusCode);

        HttpClient client = Istemci();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/identity/sessions", new { loginId, password = "ilk-parola-1234" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = "ilk-parola-1234", newPassword = PersonelParola })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/identity/sessions", new { loginId, password = PersonelParola })).StatusCode);

        return client;
    }

    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });
}
