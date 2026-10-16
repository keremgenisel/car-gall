using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// spec 0012b — yayın akışının GERÇEK boru hattında koştuğu tek yer.
///
/// <para><b>`docs/testing.md` § 3'ün ÜÇÜNCÜ kritik akışı ilk kez tamamlanıyor:</b>
/// *"personel giriş yapar → araç ekler → **yayına alır → vitrinde görünür**"*. Son iki
/// adım bu spec'e kadar hiçbir yoldan koşturulamıyordu — `VehicleCatalog`'un hiçbir yazma
/// ucu yoktu ve vitrin okuma modeline yazan tek üretim yolu geliştirme seed'iydi.</para>
///
/// <para>`0006`/C18'in rota TARAYAN testi yönetim kökü altındaki her ucun oturum
/// gerektirdiğini zaten doğruluyor; buradaki AC-22 onun bu dilimin altı ucu için somut
/// hâli. Taranan bir kural ile adlandırılmış bir kriter aynı şey değil.</para>
/// </summary>
public sealed class YayinZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";

    /// <summary>`SeedInitialDealership` migration'ının açtığı Kadıköy şubesi.</summary>
    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    /// <summary>`SeedInitialVehicleModels` migration'ının ilk satırı.</summary>
    private static readonly Guid SeedModel = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;

    /// <summary>
    /// `0013` D30/D34 — kurulum Yöneticisinin ikinci adım sırrı. Kurulum **bir kez**,
    /// `InitializeAsync`'te yapılıyor; testlerin her girişi kodu bundan hesaplıyor
    /// (`IkinciAdim`). D28'in e2e için verdiği kararın backend karşılığı.
    /// </summary>
    private string ikinciAdimSirri = string.Empty;

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
            new { currentPassword = IdentityTestSettings.SeedAdminPassword, newPassword = CalisanParola }))
            .StatusCode);

        // 0013 D30/D13 — Yöneticide ikinci adım ZORUNLU ve kurulumsuz hesap kapılı bir
        // oturum alıyor. Kurulum bir kez burada yapılır; aksi hâlde bu dosyadaki her
        // yönetim isteği `403 two_factor_setup_required` alır ve testler ikinci adımla
        // ilgisi olmayan bir sebeple kırmızı verirdi (D34).
        // Parola değişimi o personelin TÜM oturumlarını düşürdü (`0006` R11/AC-22), yani
        // eldeki çerez ölü. Yeniden giriş `two_factor_setup_required` döndürüyor ve
        // KAPILI bir oturum veriyor (D13) — kurulumun yapılabildiği tek hâl tam bu.
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

    [Theory(DisplayName = "0012b/AC-22: Oturumu olmayan biri hiçbir durum geçişini çağıramaz")]
    [InlineData("publish")]
    [InlineData("unpublish")]
    [InlineData("reserve")]
    [InlineData("unreserve")]
    [InlineData("sold")]
    [InlineData("archive")]
    public async Task Cagiramaz_OturumsuzKisi_HicbirGecisi(string eylem)
    {
        HttpClient client = Istemci();

        HttpResponseMessage yanit = await client.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{Guid.CreateVersion7()}/{eylem}", UriKind.Relative),
            content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    /// <summary>
    /// **D26** — reddin HTTP karşılığı SABİT ve panelin tepkisi ona bağlı: `409` "satırı
    /// tazele", `422` "eksikleri göster", `404` "bu araç yok".
    /// </summary>
    [Fact(DisplayName = "0012b/AC-8: Yayın reddi `422`, geçiş reddi `409`, bulunamayan araç `404` döner")]
    public async Task Doner_SabitKodlar_Reddedilen_Isteklerde()
    {
        HttpClient client = await GirisYapmisIstemciAsync();
        Guid aracId = await AracOlusturAsync(client, "YAYIN00000000001");

        // A2 reddi — hiç medya yok: `422` + kod LİSTESİ.
        HttpResponseMessage yayin = await EylemAsync(client, aracId, "publish");

        Assert.Equal(HttpStatusCode.UnprocessableContent, yayin.StatusCode);

        JsonElement govde = JsonDocument.Parse(await yayin.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("vehicle.cannot_publish", govde.GetProperty("errorCode").GetString());
        Assert.Contains(
            "vehicle.cannot_publish.no_media",
            govde.GetProperty("reasons").GetString(),
            StringComparison.Ordinal);

        // AC-9 — çevrilmiş metin YOK.
        Assert.False(govde.TryGetProperty("title", out _));
        Assert.False(govde.TryGetProperty("detail", out _));

        // A3 geçiş reddi — taslak bir ilan rezerve edilemez: `409`.
        HttpResponseMessage rezerve = await EylemAsync(client, aracId, "reserve");

        Assert.Equal(HttpStatusCode.Conflict, rezerve.StatusCode);
        Assert.Equal(
            "listing.invalid_status_transition",
            JsonDocument.Parse(await rezerve.Content.ReadAsStringAsync())
                .RootElement.GetProperty("errorCode").GetString());

        // Var olmayan araç: `404`.
        HttpResponseMessage yok = await EylemAsync(client, Guid.CreateVersion7(), "publish");

        Assert.Equal(HttpStatusCode.NotFound, yok.StatusCode);
    }

    /// <summary>
    /// Kritik akış #3'ün tamamı: araç ekle → fotoğrafı hazır say → yayına al → **ziyaretçi
    /// vitrininde gör**.
    ///
    /// <para>Medya varlığı doğrudan `media` şemasına yazılıyor: `Media`'nın işleme hattını
    /// koşturmak bu testin konusu değil ve `MedyaYuklemeZinciriTests` onu zaten ölçüyor.
    /// Ölçülen şey <b>yayın komutunun `Media`'nın okuma yüzeyinden gerçekten
    /// sorduğu</b> — sahte bir komşu bunu kanıtlamaz.</para>
    /// </summary>
    [Fact(DisplayName = "0012b/AC-41: Yayına alma vitrin okuma modeline satır yazar ve ilan ziyaretçiye görünür olur")]
    public async Task Gorunur_Vitrinde_YayinaAlindiginda()
    {
        HttpClient client = await GirisYapmisIstemciAsync();
        Guid aracId = await AracOlusturAsync(client, "VITRIN0000000002");

        Guid varlik = await HazirMedyaEkleAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{aracId}/media",
            new { mediaAssetIds = new[] { varlik } })).StatusCode);

        HttpResponseMessage yayin = await EylemAsync(client, aracId, "publish");

        Assert.Equal(HttpStatusCode.NoContent, yayin.StatusCode);

        // Ziyaretçi yüzeyi: oturum YOK ve ilan görünüyor. Vitrin yanıtı aracın kimliğini
        // taşımıyor — ilanın ADRESİ taşınıyor (0002 R2) ve kimlik üzerinden aramak
        // yanıtın şeklini yanlış varsaymak olurdu.
        string? adres = await AdresAsync(aracId);

        Assert.NotNull(adres);
        Assert.StartsWith("toyota-corolla-2019-", adres, StringComparison.Ordinal);

        HttpClient ziyaretci = Istemci();
        string vitrin = await ziyaretci.GetStringAsync(
            new Uri("/api/catalog/showcase-listings?locale=tr&pageSize=100", UriKind.Relative));

        Assert.Contains(adres, vitrin, StringComparison.Ordinal);
    }

    /// <summary>İlanın adresi — yazma modelinden, ham SQL ile (D13).</summary>
    private async Task<string?> AdresAsync(Guid aracId)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT slug FROM vehicle_catalog.listings WHERE vehicle_id = @id";
        command.Parameters.AddWithValue("id", aracId);

        return await command.ExecuteScalarAsync() as string;
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

    private static Task<HttpResponseMessage> EylemAsync(HttpClient client, Guid aracId, string eylem) =>
        client.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{aracId}/{eylem}", UriKind.Relative),
            content: null);

    private static async Task<Guid> AracOlusturAsync(HttpClient client, string vin)
    {
        HttpResponseMessage yanit = await client.PostAsJsonAsync(
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

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);

        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync())
            .RootElement.GetProperty("vehicleId").GetGuid();
    }

    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    private async Task<HttpClient> GirisYapmisIstemciAsync()
    {
        HttpClient client = Istemci();

        await IkinciAdim.GirAsync(
            client, IdentityTestSettings.SeedAdminLoginId, CalisanParola, ikinciAdimSirri);

        return client;
    }
}
