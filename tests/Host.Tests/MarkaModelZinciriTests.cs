using System.Globalization;
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
/// `0021` — marka ve modelin gerçek boru hattı: Yönetici politikası (D4), oturumsuz referans
/// okumasının yalnız aktif kayıtları döndürmesi (D24), yeni ilanın adresi ve talep kopyasının
/// marka + model tam adı (C2, D17) ve panel listesindeki ad.
///
/// <para>Üç rol gerçek personel olarak eklenir ve gerçek oturumla girer (`0020`'nin deseni).
/// Testler sırayla koşar; her biri kendi markasını ekler.</para>
/// </summary>
public sealed class MarkaModelZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";
    private const string Kok = $"{AdminSurface.RoutePrefix}/reference";

    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");
    private static readonly Guid Corolla = Guid.Parse("0192c000-0000-7000-8000-00000000000a");
    private static readonly Guid Honda = Guid.Parse("0192c000-0000-7000-8000-0000000000b3");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;
    private HttpClient yonetici = null!;
    private HttpClient editor = null!;
    private HttpClient danisman = null!;
    private int sayac;

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
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0021/AC-53: Yönetici olmayan rolün marka ve model yazma uçlarına isteği `403` alır.")]
    public async Task Reddeder_YoneticiOlmayanRolu_YazmaUclarinda()
    {
        (Guid marka, Guid model) = await MarkaVeModelAsync();
        string once = await yonetici.GetStringAsync($"{Kok}/brands/{marka}");

        foreach (HttpClient rol in new[] { danisman, editor })
        {
            foreach ((HttpMethod yontem, string yol, object govde) in YazmaUclari(marka, model))
            {
                HttpResponseMessage yanit = await GonderAsync(rol, yontem, yol, govde);
                Assert.True(yanit.StatusCode == HttpStatusCode.Forbidden, $"{yontem} {yol} → {(int)yanit.StatusCode}");
            }
        }

        Assert.Equal(once, await yonetici.GetStringAsync($"{Kok}/brands/{marka}"));
    }

    [Fact]
    public async Task Doner401_OturumsuzYazma()
    {
        (Guid marka, Guid model) = await MarkaVeModelAsync();
        HttpClient oturumsuz = Istemci();

        foreach ((HttpMethod yontem, string yol, object govde) in YazmaUclari(marka, model))
        {
            HttpResponseMessage yanit = await GonderAsync(oturumsuz, yontem, yol, govde);
            Assert.True(yanit.StatusCode == HttpStatusCode.Unauthorized, $"{yontem} {yol} → {(int)yanit.StatusCode}");
        }
    }

    [Fact(DisplayName = "0021/AC-54: Satış Danışmanı araç formunda aktif markaları ve modelleri okuyabilir.")]
    public async Task Okur_DanismanAktifMarkaVeModelleri()
    {
        HttpResponseMessage markalar = await danisman.GetAsync("/api/reference/brands?locale=tr");
        HttpResponseMessage modeller = await danisman.GetAsync("/api/reference/models?locale=tr");

        Assert.Equal(HttpStatusCode.OK, markalar.StatusCode);
        Assert.Equal(HttpStatusCode.OK, modeller.StatusCode);
        JsonElement toyota = JsonDocument.Parse(await markalar.Content.ReadAsStringAsync()).RootElement
            .EnumerateArray().Single(item => item.GetProperty("slug").GetString() == "toyota");
        JsonElement corolla = JsonDocument.Parse(await modeller.Content.ReadAsStringAsync()).RootElement
            .EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == Corolla);
        Assert.Equal("Toyota", toyota.GetProperty("label").GetString());
        Assert.Equal("Corolla", corolla.GetProperty("label").GetString());
        Assert.Equal(toyota.GetProperty("id").GetGuid(), corolla.GetProperty("brandId").GetGuid());
    }

    [Fact(DisplayName = "0021/AC-55: Vitrinin ve araç formunun okuduğu referans uçları pasif marka ve modelleri döndürmez; pasifleri ve \"ana sayfada göster\" işaretini döndüren yönetim okuması Yönetici olmayan role `403` döner (D24).")]
    public async Task Dondurmez_Pasifleri_YonetimOkumasiYoneticiye()
    {
        (Guid pasifMarka, Guid pasifMarkaninModeli) = await MarkaVeModelAsync();
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync($"{Kok}/brands/{pasifMarka}/deactivate", new { version = 1 })).StatusCode);
        (Guid aktifMarka, Guid pasifModel) = await MarkaVeModelAsync();
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync($"{Kok}/models/{pasifModel}/deactivate", new { version = 1 })).StatusCode);

        HttpClient oturumsuz = Istemci();
        string markalar = await oturumsuz.GetStringAsync("/api/reference/brands?locale=tr");
        string modeller = await oturumsuz.GetStringAsync("/api/reference/models?locale=tr");

        Assert.DoesNotContain(pasifMarka.ToString(), markalar, StringComparison.Ordinal);
        Assert.Contains(aktifMarka.ToString(), markalar, StringComparison.Ordinal);
        Assert.DoesNotContain(pasifMarkaninModeli.ToString(), modeller, StringComparison.Ordinal);
        Assert.DoesNotContain(pasifModel.ToString(), modeller, StringComparison.Ordinal);
        Assert.DoesNotContain("showOnHome", markalar, StringComparison.Ordinal);
        Assert.DoesNotContain("isActive", markalar, StringComparison.Ordinal);

        foreach (HttpClient rol in new[] { danisman, editor })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await rol.GetAsync($"{Kok}/brands")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await rol.GetAsync($"{Kok}/brands/{aktifMarka}")).StatusCode);
        }

        string yonetim = await yonetici.GetStringAsync($"{Kok}/brands");
        Assert.Contains(pasifMarka.ToString(), yonetim, StringComparison.Ordinal);
        Assert.Contains("showOnHome", yonetim, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0021/AC-20: Modelin markası değiştirilemez: düzenleme ekranında marka seçimi sunulmaz ve uç markayı değiştiren isteği reddeder.")]
    public async Task Reddeder_MarkaDegistirenModelIstegi_422()
    {
        (_, Guid model) = await MarkaVeModelAsync();

        HttpResponseMessage yanit = await yonetici.PutAsJsonAsync(
            $"{Kok}/models/{model}",
            new { version = 1, brandId = Honda, canonicalName = "Model" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, yanit.StatusCode);
        JsonElement govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("vehicle_model.brand_immutable", govde.GetProperty("errorCode").GetString());
        Assert.Equal("brandId", govde.GetProperty("field").GetString());
    }

    [Fact]
    public async Task Doner_KararliKodlar_AdCakismasiVeEskiSurum()
    {
        HttpResponseMessage ayni = await yonetici.PostAsJsonAsync($"{Kok}/brands", new { canonicalName = "TOYOTA" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ayni.StatusCode);
        Assert.Equal("brand.name_taken", JsonDocument.Parse(await ayni.Content.ReadAsStringAsync()).RootElement.GetProperty("errorCode").GetString());

        (Guid marka, _) = await MarkaVeModelAsync();
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsJsonAsync($"{Kok}/brands/{marka}/show-on-home", new { version = 1, showOnHome = true })).StatusCode);
        HttpResponseMessage eski = await yonetici.PostAsJsonAsync($"{Kok}/brands/{marka}/deactivate", new { version = 1 });
        Assert.Equal(HttpStatusCode.Conflict, eski.StatusCode);
        Assert.Equal("concurrency.lost", JsonDocument.Parse(await eski.Content.ReadAsStringAsync()).RootElement.GetProperty("errorCode").GetString());
    }

    [Fact(DisplayName = "0021/AC-32: Yeni yayına alınan bir Fiat Egea ilanının adresi `fiat-egea-<yıl>-…` biçimindedir.")]
    public async Task Uretir_FiatEgeaAdresini()
    {
        Guid fiat = await MarkaEkleAsync("Fiat");
        Guid egea = await ModelEkleAsync(fiat, "Egea");

        Guid arac = await YayinlaAsync(egea, "FIATEGEA00000001");

        Assert.StartsWith("fiat-egea-2019-", await AdresAsync(arac), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0021/AC-33: Güncellemeden sonra Toyota Corolla için bırakılan talebin araç kopyası ve bildirim e-postası \"Toyota Corolla\" der (D17).")]
    public async Task Kopyalar_TamKanonikAdi_Talebe()
    {
        Guid arac = await YayinlaAsync(Corolla, "COROLLATALEP0001");
        string adres = (await AdresAsync(arac))!;

        HttpRequestMessage istek = new(HttpMethod.Post, "/api/leads")
        {
            Content = JsonContent.Create(new
            {
                listingSlug = adres,
                leadType = "information_request",
                locale = "ar",
                fullName = "Ayşe Yılmaz",
                phone = "+905321112233",
                email = "ziyaretci@ornek.com",
                message = (string?)null,
                consentGiven = true,
            }),
        };
        istek.Headers.Add("Idempotency-Key", "marka-talep-1");

        Assert.Equal(HttpStatusCode.Created, (await Istemci().SendAsync(istek)).StatusCode);

        // Bildirim e-postasının `modelAdi` alanı talebin bu kopyasından doldurulur
        // (`NotificationDispatcher`: `lead.CanonicalModelName`); kopya tam adsa e-posta da öyledir.
        Assert.Equal("Toyota Corolla", await TekDegerAsync("SELECT vehicle_canonical_model_name FROM leads.leads"));
    }

    [Fact(DisplayName = "0021/AC-34: Panelin araç listesi aracı marka + model olarak gösterir (D17).")]
    public async Task Gosterir_MarkaVeModeli_PanelListesinde()
    {
        Guid arac = await AracOlusturAsync(Corolla, "PANELLISTE000001");

        JsonElement liste = JsonDocument.Parse(await yonetici.GetStringAsync(
            $"{AdminSurface.RoutePrefix}/vehicles?locale=ar&page=1&pageSize=100&sort=recently-added")).RootElement;
        JsonElement satir = liste.GetProperty("items").EnumerateArray().Single(item => item.GetProperty("vehicleId").GetGuid() == arac);

        Assert.Equal("تويوتا كورولا", satir.GetProperty("modelName").GetString());

        JsonElement detay = JsonDocument.Parse(await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/vehicles/{arac}?locale=tr")).RootElement;
        Assert.Equal("Toyota", detay.GetProperty("model").GetProperty("brandLabel").GetString());
        Assert.Equal("Corolla", detay.GetProperty("model").GetProperty("modelLabel").GetString());
    }

    private IEnumerable<(HttpMethod, string, object)> YazmaUclari(Guid marka, Guid model) =>
    [
        (HttpMethod.Post, $"{Kok}/brands", new { canonicalName = $"Yetkisiz {Guid.NewGuid():N}" }),
        (HttpMethod.Put, $"{Kok}/brands/{marka}", new { version = 1, canonicalName = "Yetkisiz" }),
        (HttpMethod.Post, $"{Kok}/brands/{marka}/deactivate", new { version = 1 }),
        (HttpMethod.Post, $"{Kok}/brands/{marka}/activate", new { version = 1 }),
        (HttpMethod.Put, $"{Kok}/brands/{marka}/show-on-home", new { version = 1, showOnHome = true }),
        (HttpMethod.Post, $"{Kok}/brands/{marka}/models", new { canonicalName = "Yetkisiz" }),
        (HttpMethod.Put, $"{Kok}/models/{model}", new { version = 1, canonicalName = "Yetkisiz" }),
        (HttpMethod.Post, $"{Kok}/models/{model}/deactivate", new { version = 1 }),
        (HttpMethod.Post, $"{Kok}/models/{model}/activate", new { version = 1 }),
    ];

    private async Task<(Guid Marka, Guid Model)> MarkaVeModelAsync()
    {
        Guid marka = await MarkaEkleAsync($"Marka {Interlocked.Increment(ref sayac)} {Guid.NewGuid():N}"[..20]);
        return (marka, await ModelEkleAsync(marka, "Model"));
    }

    private async Task<Guid> MarkaEkleAsync(string ad)
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{Kok}/brands", new { canonicalName = ad });
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> ModelEkleAsync(Guid marka, string ad)
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{Kok}/brands/{marka}/models", new { canonicalName = ad });
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> YayinlaAsync(Guid model, string vin)
    {
        Guid arac = await AracOlusturAsync(model, vin);
        Guid varlik = await HazirMedyaEkleAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await yonetici.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{arac}/media",
            new { mediaAssetIds = new[] { varlik } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await yonetici.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/publish", UriKind.Relative),
            content: null)).StatusCode);

        return arac;
    }

    private async Task<Guid> AracOlusturAsync(Guid model, string vin)
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles", new
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

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement.GetProperty("vehicleId").GetGuid();
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

    private async Task<string?> AdresAsync(Guid arac) =>
        await TekDegerAsync($"SELECT slug FROM vehicle_catalog.listings WHERE vehicle_id = '{arac}'");

    private async Task<string?> TekDegerAsync(string sorgu)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = sorgu;

        return Convert.ToString(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private static Task<HttpResponseMessage> GonderAsync(HttpClient client, HttpMethod yontem, string yol, object govde) =>
        client.SendAsync(new HttpRequestMessage(yontem, yol) { Content = JsonContent.Create(govde) });

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
