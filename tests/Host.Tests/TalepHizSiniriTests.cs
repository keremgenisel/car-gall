using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Leads;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// C17'nin hız sınırı GERÇEK BORU HATTINDA sınanır. Modülün kendi endpoint testinde
/// sınamak yetmez: politika modülde tanımlı ama uygulaması Host'un `UseRateLimiter()`
/// satırındadır ve o satır silinirse sınır SESSİZCE kaybolur.
///
/// Bu, 0003'ün dersinin aynısı: test edilmiş bir alıcı, yayıncısı olmadan hiçbir şey
/// kanıtlamaz. Burada test edilen şey politikanın varlığı değil, KABLOLANMIŞ olmasıdır.
///
/// Eşik varsayılan bırakılır — yani spec'in kendi sayısı (10 dakikada 5). Kanıt üretimi
/// için yükseltilebilir olması bu testi etkilemez.
/// </summary>
public sealed class TalepHizSiniriTests : IAsyncLifetime
{
    private static readonly Guid SubeId = Guid.Parse("00000000-0000-7000-8000-00000000c001");
    private static readonly Guid ModelId = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private const string Slug = "toyota-corolla-2019-aaa111";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        // Kurulum adımı uygulamadan ÖNCE koşar: uygulama artık şema UYGULAMAZ, yalnız
        // güncel mi diye bakar ve eksikse başlamayı reddeder (docs/decisions/0002).
        await TestSchema.ApplyAsync(container.GetConnectionString());

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());

            // 0006 başlangıç sözleşmesi: parola olmadan uygulama hiç kurulmaz.
            IdentityTestSettings.Apply(builder);
        });

        await factory.Services.SeedVehicleReferenceForDevelopmentAsync(ModelId);
        await factory.Services.SeedVehicleCatalogForDevelopmentAsync(0, ModelId);

        await SeedShowcaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    /// <summary>
    /// BULGU-1'in regresyon testi. Eskiden bütçeyi boru hattındaki istek sınırı
    /// tüketiyordu ve İSTEK sayıyordu: sunucu doğrulamasına düşen beş deneme bütçeyi
    /// bitiriyor, ardından gelen TAMAMEN GEÇERLİ talep 429 alıp kayboluyordu. Hiç talep
    /// kaydedilmemiş olmasına rağmen.
    ///
    /// Spec'in Intent'i tam olarak bu kaybı önlemek için yazılmış; AC-32'nin
    /// "kaydedildikten sonra" ifadesi de bu yüzden bilinçli.
    /// </summary>
    [Fact(DisplayName = "0005/AC-32: Aynı IP'den 10 dakika içinde 5 talep kaydedildikten sonra altıncı istek reddedilir ve bu istek için kayıt oluşmaz")]
    public async Task Kabul_GecerliTalep_BesReddedilenIstektenSonra()
    {
        HttpClient client = factory!.CreateClient();

        for (int sira = 1; sira <= 5; sira++)
        {
            HttpResponseMessage reddedilen = await GonderAsync(client, $"rl-gecersiz-{sira}", ad: null);

            Assert.Equal(HttpStatusCode.UnprocessableContent, reddedilen.StatusCode);
        }

        Assert.Equal(0, await TalepSayisiAsync());

        HttpResponseMessage gecerli = await GonderAsync(client, "rl-gecerli-1");

        Assert.Equal(HttpStatusCode.Created, gecerli.StatusCode);
        Assert.Equal(1, await TalepSayisiAsync());
    }

    /// <summary>
    /// Sayacın bölüm anahtarı bağlantının GERÇEK adresidir. `X-Forwarded-For`
    /// yapılandırılmamış bir dağıtımda okunmaz: okunsaydı istemci her istekte başka bir
    /// değer göndererek sınırı kendisi kapatırdı.
    /// </summary>
    [Fact(DisplayName = "0005/AC-32: Aynı IP'den 10 dakika içinde 5 talep kaydedildikten sonra altıncı istek reddedilir ve bu istek için kayıt oluşmaz")]
    public async Task Asilamaz_XForwardedForBasligiyla()
    {
        HttpClient client = factory!.CreateClient();

        for (int sira = 1; sira <= 5; sira++)
        {
            Assert.Equal(HttpStatusCode.Created, (await GonderAsync(client, $"xff-{sira}")).StatusCode);
        }

        HttpRequestMessage istek = Istek("xff-6");
        istek.Headers.Add("X-Forwarded-For", "198.51.100.77");

        HttpResponseMessage altinci = await client.SendAsync(istek);

        Assert.Equal(HttpStatusCode.TooManyRequests, altinci.StatusCode);
        Assert.Equal(5, await TalepSayisiAsync());
    }

    [Fact(DisplayName = "0005/AC-32: Aynı IP'den 10 dakika içinde 5 talep kaydedildikten sonra altıncı istek reddedilir ve bu istek için kayıt oluşmaz")]
    public async Task Reddedilir_AltinciIstek_AyniIpten()
    {
        HttpClient client = factory!.CreateClient();

        for (int sira = 1; sira <= 5; sira++)
        {
            HttpResponseMessage kabul = await GonderAsync(client, $"anahtar-{sira}");
            Assert.Equal(HttpStatusCode.Created, kabul.StatusCode);
        }

        HttpResponseMessage altinci = await GonderAsync(client, "anahtar-6");

        Assert.Equal(HttpStatusCode.TooManyRequests, altinci.StatusCode);

        // Reddedilen istek için KAYIT OLUŞMAZ: sınır handler'dan önce durdurur.
        Assert.Equal(5, await TalepSayisiAsync());
    }

    [Fact(DisplayName = "0005/AC-33: Hız sınırına takılan istek `429` durum kodu ve `lead.rate_limited` hata kodu ile reddedilir")]
    public async Task Doner429_Ve_RateLimitedKodu()
    {
        HttpClient client = factory!.CreateClient();

        for (int sira = 1; sira <= 5; sira++)
        {
            await GonderAsync(client, $"kod-anahtar-{sira}");
        }

        HttpResponseMessage altinci = await GonderAsync(client, "kod-anahtar-6");
        JsonElement govde = JsonDocument.Parse(await altinci.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.TooManyRequests, altinci.StatusCode);
        Assert.Equal("lead.rate_limited", govde.GetProperty("errorCode").GetString());
    }

    [Fact(DisplayName = "0005/AC-34: Reddedilen istekte ziyaretçiye teknik ayrıntı gösterilmez")]
    public async Task IcermezTeknikAyrinti_HizSinirYaniti()
    {
        HttpClient client = factory!.CreateClient();

        for (int sira = 1; sira <= 6; sira++)
        {
            await GonderAsync(client, $"gizlilik-anahtar-{sira}");
        }

        HttpResponseMessage altinci = await GonderAsync(client, "gizlilik-anahtar-7");
        string govde = await altinci.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.TooManyRequests, altinci.StatusCode);

        foreach (string isaret in new[] { "Exception", "   at ", "Npgsql", "/api/leads", "RateLimit" })
        {
            Assert.DoesNotContain(isaret, govde, StringComparison.OrdinalIgnoreCase);
        }

        // IP KİŞİSEL VERİDİR: yanıtta da bulunmaz (C17, docs/conventions.md § 12).
        Assert.DoesNotContain("127.0.0.1", govde, StringComparison.Ordinal);
        Assert.DoesNotContain("::1", govde, StringComparison.Ordinal);
    }

    private static async Task<HttpResponseMessage> GonderAsync(
        HttpClient client,
        string anahtar,
        string? ad = "Ayşe Yılmaz") =>
        await client.SendAsync(Istek(anahtar, ad));

    private static HttpRequestMessage Istek(string anahtar, string? ad = "Ayşe Yılmaz")
    {
        HttpRequestMessage istek = new(HttpMethod.Post, "/api/leads")
        {
            Content = JsonContent.Create(new
            {
                listingSlug = Slug,
                leadType = "information_request",
                locale = "tr",
                fullName = ad,
                phone = "+905321112233",
                email = (string?)null,
                message = (string?)null,
                consentGiven = true,
            }),
        };

        istek.Headers.Add("Idempotency-Key", anahtar);

        return istek;
    }

    private async Task<int> TalepSayisiAsync()
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM leads.leads";

        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task SeedShowcaseAsync()
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO vehicle_catalog.showcase_listings (
                listing_id, vehicle_id, is_reserved, showcase_sort_weight, published_at, slug,
                body_type_slug, fuel_type_slug, transmission_type_slug, drivetrain_type_slug,
                trim_level_slug, color_names, vehicle_model_id, model_year, mileage_kilometers,
                price_amount, price_currency_code, price_minor_unit_exponent, price_on_request,
                branch_id, branch_names, cover_asset_id, cover_media_url)
            VALUES (
                gen_random_uuid(), gen_random_uuid(), false, 0, now(), @slug,
                'sedan', 'diesel', 'automatic', 'front-wheel-drive',
                'comfort', '{"tr":"Beyaz"}'::jsonb, @modelId, 2019, 125000,
                1250000, 'TRY', 2, false,
                @branchId, '{"tr":"Merkez Şube"}'::jsonb, NULL, NULL)
            """;

        command.Parameters.AddWithValue("slug", Slug);
        command.Parameters.AddWithValue("modelId", ModelId);
        command.Parameters.AddWithValue("branchId", SubeId);

        await command.ExecuteNonQueryAsync();
    }
}
