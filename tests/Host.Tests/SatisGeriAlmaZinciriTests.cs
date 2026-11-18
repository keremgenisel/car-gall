using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0023` — satışı geri almanın GERÇEK boru hattı: rol politikası (**R12**), hata gövdesi,
/// kayıt yüzeyinin salt okunurluğu (**AC-21**), toplu yüzeyin yokluğu (**AC-30**) ve
/// ziyaretçi vitrinine dönüş (**AC-1**). Üç rol gerçek personel olarak eklenir ve gerçek
/// oturumla girer (`IcerikZinciriTests`'in deseni).
/// </summary>
public sealed class SatisGeriAlmaZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";
    private const string Gerekce = "Yanlış araca tıklandı, satış başka araçtaydı.";

    /// <summary>`SeedInitialDealership` migration'ının açtığı Kadıköy şubesi.</summary>
    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    /// <summary>`SeedInitialVehicleModels` migration'ının ilk satırı.</summary>
    private static readonly Guid SeedModel = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private static int sayac;

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

        // Kurulum hesabı: zorunlu parola değişimi ve Yöneticide zorunlu ikinci adım (0013).
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
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0023/AC-1: Yayındayken satıldı işaretlenen ilanın satışı Yönetici tarafından geri alındığında ilan **Yayında** olur ve vitrinde yeniden görünür")]
    public async Task Gorunur_ZiyaretciVitrininde_GeriAlindiginda()
    {
        (Guid arac, string adres) = await SatilmisAracAsync();
        HttpClient ziyaretci = Istemci();

        Assert.DoesNotContain(adres, await VitrinAsync(ziyaretci), StringComparison.Ordinal);

        HttpResponseMessage yanit = await GeriAlAsync(yonetici, arac, Gerekce, "Published");

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);
        Assert.Equal("Published", await DurumAsync(arac));
        Assert.Contains(adres, await VitrinAsync(ziyaretci), StringComparison.Ordinal);
    }

    /// <summary>
    /// **D16** — `409` gövdesi güncel hedefi taşır; yönetici onu görür. Hata gövdesi çevrilmiş
    /// metin taşımaz (`title`/`detail` yok). Şube durumu GERÇEK `Dealership` yüzeyinden
    /// okunuyor; `VehicleCatalog.Tests` aynı kriteri sahte bir şube yüzeyiyle ölçüyor.
    /// </summary>
    [Fact(DisplayName = "0023/AC-7: Onay penceresi açıldıktan sonra aracın şubesi kalıcı kapatılırsa, pencerenin gösterdiği \"Yayında\" hedefiyle gönderilen geri alma `409` ile reddedilir, ilan Satıldı kalır ve kayıt yazılmaz (D16)")]
    public async Task Doner409_HedefDegistiyse()
    {
        (Guid arac, _) = await SatilmisAracAsync();

        HttpResponseMessage pencere = await yonetici.GetAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/sale-reversals", UriKind.Relative));

        Assert.Equal("Published", (await GovdeAsync(pencere)).GetProperty("targetStatus").GetString());

        // Pencere açıkken şube KALICI kapatılıyor — `Dealership`'in kendi tablosunda, gerçek
        // okuma yüzeyi (`IBranchQueries`) bunu görecek. Duyuru yazılmıyor: A8'in işleyicisi
        // satılmış ilanı zaten atlıyor ve ölçülen şey geri almanın şubeyi KAYNAKTAN
        // okuması (D12).
        await using (NpgsqlConnection connection = new(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE dealership.branches SET status = 3 WHERE id = @id";
            command.Parameters.AddWithValue("id", SeedSube);
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }

        HttpResponseMessage yanit = await GeriAlAsync(yonetici, arac, Gerekce, "Published");
        JsonElement govde = await GovdeAsync(yanit);

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
        Assert.Equal("listing.sale_reversal.target_changed", govde.GetProperty("errorCode").GetString());
        Assert.Equal("Draft", govde.GetProperty("targetStatus").GetString());
        Assert.False(govde.TryGetProperty("title", out _));
        Assert.False(govde.TryGetProperty("detail", out _));
        Assert.Equal("Sold", await DurumAsync(arac));
        Assert.Equal(0, await KayitSayisiAsync(arac));
    }

    [Fact(DisplayName = "0023/AC-14: 10 karakterden kısa gerekçe sunucuda reddedilir ve ilan Satıldı kalır")]
    public async Task Doner422_KisaGerekce()
    {
        (Guid arac, _) = await SatilmisAracAsync();

        HttpResponseMessage yanit = await GeriAlAsync(yonetici, arac, "kısa", "Published");

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);
        Assert.Equal("listing.sale_reversal.reason_too_short", (await GovdeAsync(yanit)).GetProperty("errorCode").GetString());
        Assert.Equal("Sold", await DurumAsync(arac));
    }

    [Fact(DisplayName = "0023/AC-20: Yönetici ilanın panel ekranında geri alma kayıtlarını en yenisi üstte, personel adı, an, gerekçe ve dönülen durumla görür")]
    public async Task Doner_Kayitlar_PanelUcundan()
    {
        (Guid arac, _) = await SatilmisAracAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await GeriAlAsync(yonetici, arac, Gerekce, "Published")).StatusCode);

        HttpResponseMessage yanit = await yonetici.GetAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/sale-reversals", UriKind.Relative));
        JsonElement govde = await GovdeAsync(yanit);
        JsonElement kayit = Assert.Single(govde.GetProperty("records").EnumerateArray());

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal(Gerekce, kayit.GetProperty("reason").GetString());
        Assert.Equal("Published", kayit.GetProperty("restoredStatus").GetString());
        Assert.False(string.IsNullOrWhiteSpace(kayit.GetProperty("staffDisplayName").GetString()));
        Assert.True(kayit.TryGetProperty("revertedAt", out _));
    }

    /// <summary>
    /// Rotalar TARANIYOR (`0006`/C18'in deseni): uçları elle saymak, bir sonraki spec'in
    /// eklediği bir silme ucunu görmezdi.
    /// </summary>
    [Fact(DisplayName = "0023/AC-21: Geri alma kaydını düzenleyen ya da silen bir uç yoktur")]
    public void Yoktur_KaydiDuzenleyenYaDaSilenUc()
    {
        EndpointDataSource kaynak = factory!.Services.GetRequiredService<EndpointDataSource>();

        List<string> kayitUclari = [];

        foreach (Endpoint endpoint in kaynak.Endpoints)
        {
            if (endpoint is not RouteEndpoint route
                || route.RoutePattern.RawText?.Contains("sale-revers", StringComparison.OrdinalIgnoreCase) != true)
            {
                continue;
            }

            foreach (string method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["*"])
            {
                kayitUclari.Add($"{method} {route.RoutePattern.RawText}");
            }
        }

        // Tarama boş geçmemeli: hiç uç bulamayan bir tarama da "silme ucu yok" der.
        Assert.Equal(["GET /api/admin/vehicles/{vehicleId:guid}/sale-reversals"], kayitUclari);
    }

    [Theory(DisplayName = "0023/AC-29: Yönetici olmayan rolün geri alma ucuna isteği `403` alır ve ilan değişmez")]
    [InlineData("danisman")]
    [InlineData("editor")]
    public async Task Doner403_YoneticiOlmayanRol(string rol)
    {
        HttpClient client = rol == "danisman" ? danisman : editor;
        (Guid arac, _) = await SatilmisAracAsync();

        HttpResponseMessage geriAl = await GeriAlAsync(client, arac, Gerekce, "Published");
        HttpResponseMessage kayitlar = await client.GetAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/sale-reversals", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Forbidden, geriAl.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, kayitlar.StatusCode);
        Assert.Equal("Sold", await DurumAsync(arac));
        Assert.Equal(0, await KayitSayisiAsync(arac));
    }

    /// <summary>
    /// Toplu yüzeyin rotası HİÇ tanımlanmadı; yokluğun HTTP karşılığı `404` (`0012c` **D18**'in
    /// rezerve için verdiği kararın aynısı). Ekran yarısı `BulkActionBar.test.tsx`'te.
    /// </summary>
    [Fact(DisplayName = "0023/AC-30: Toplu eylem çubuğunda \"Satışı geri al\" seçeneği yoktur")]
    public async Task Yoktur_TopluGeriAlmaRotasi()
    {
        (Guid arac, _) = await SatilmisAracAsync();

        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/bulk/revert-sale",
            new { vehicleIds = new[] { arac }, reason = Gerekce, expectedStatus = "Published" });

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.Equal("Sold", await DurumAsync(arac));
    }

    private static Task<HttpResponseMessage> GeriAlAsync(HttpClient client, Guid arac, string gerekce, string beklenen) =>
        client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{arac}/revert-sale",
            new { reason = gerekce, expectedStatus = beklenen });

    private static async Task<JsonElement> GovdeAsync(HttpResponseMessage yanit) =>
        JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

    private static Task<string> VitrinAsync(HttpClient ziyaretci) =>
        ziyaretci.GetStringAsync(new Uri("/api/catalog/showcase-listings?locale=tr&pageSize=100", UriKind.Relative));

    private async Task<(Guid Arac, string Adres)> SatilmisAracAsync()
    {
        HttpResponseMessage olustur = await yonetici.PostAsJsonAsync(
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
                vin = $"GERIAL{Interlocked.Increment(ref sayac):D11}",
            });

        Assert.Equal(HttpStatusCode.Created, olustur.StatusCode);
        Guid arac = (await GovdeAsync(olustur)).GetProperty("vehicleId").GetGuid();

        Assert.Equal(HttpStatusCode.NoContent, (await yonetici.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{arac}/media",
            new { mediaAssetIds = new[] { await HazirMedyaEkleAsync() } })).StatusCode);

        foreach (string eylem in new[] { "publish", "sold" })
        {
            Assert.Equal(HttpStatusCode.NoContent, (await yonetici.PostAsync(
                new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/{eylem}", UriKind.Relative),
                content: null)).StatusCode);
        }

        string? adres = await TekDegerAsync<string>("SELECT slug FROM vehicle_catalog.listings WHERE vehicle_id = @id", arac);

        Assert.NotNull(adres);

        return (arac, adres);
    }

    private async Task<string> DurumAsync(Guid arac)
    {
        int durum = await TekDegerAsync<int>("SELECT status FROM vehicle_catalog.listings WHERE vehicle_id = @id", arac);

        return durum switch
        {
            0 => "Draft",
            1 => "Published",
            2 => "Reserved",
            3 => "Sold",
            _ => "Archived",
        };
    }

    private async Task<int> KayitSayisiAsync(Guid arac) =>
        (int)await TekDegerAsync<long>("SELECT count(*) FROM vehicle_catalog.listing_sale_reversals WHERE vehicle_id = @id", arac);

    private async Task<T> TekDegerAsync<T>(string sql, Guid arac)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("id", arac);

        return (T)(await command.ExecuteScalarAsync())!;
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
