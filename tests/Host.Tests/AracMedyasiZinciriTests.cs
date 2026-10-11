using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SkiaSharp;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// spec 0012a — aracın medya yüzeyinin GERÇEK boru hattında koştuğu tek yer.
///
/// <para>Modül testleri komutun ve sorgunun doğru çalıştığını gösteriyor; buradaki
/// zincir onların <b>kablolanmış</b> olduğunu gösteriyor — ve bu dilimde kablo
/// <b>iki modülü</b> birbirine bağlıyor: yükleme `Media`'nın ucundan geçiyor, bağlama
/// `VehicleCatalog`'un ucundan, okuma ikisini sunucuda birleştiriyor (**D10**). Hiçbir
/// sahte komşu yok: `IMediaAssetQueries`'in gerçek uygulaması, gerçek depo, gerçek
/// oturum.</para>
///
/// <para>`0006`/C18'in rota TARAYAN testi yönetim kökü altındaki her ucun oturum
/// gerektirdiğini zaten doğruluyor; buradaki AC-10 ve AC-15 onun bu dilimin üç ucu için
/// somut hâli. Taranan bir kural ile adlandırılmış bir kriter aynı şey değil: tarama
/// yarın eklenen bir ucu da kapsar, kriter ise <b>bu</b> ucun kapalı olduğunu söyler.</para>
/// </summary>
public sealed class AracMedyasiZinciriTests : IAsyncLifetime
{
    private const string Bucket = "cargallery-arac-medyasi";

    private const string WatermarkKey = "private/brand/watermark.png";

    private const string CalisanParola = "calisan-parolasi-1";

    /// <summary>`SeedInitialDealership` migration'ının açtığı Kadıköy şubesi.</summary>
    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    /// <summary>`SeedInitialVehicleModels` migration'ının ilk satırı.</summary>
    private static readonly Guid SeedModel = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private readonly LocalStackContainer depo =
        new LocalStackBuilder("localstack/localstack:3.8.1").Build();

    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(postgres.StartAsync(), depo.StartAsync());
        await TestSchema.ApplyAsync(postgres.GetConnectionString());

        using AmazonS3Client s3 = new("test", "test", new AmazonS3Config
        {
            ServiceURL = depo.GetConnectionString(),
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
        });

        await s3.PutBucketAsync(new PutBucketRequest { BucketName = Bucket });

        using MemoryStream filigran = new(FiligranPng(), writable: false);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = WatermarkKey,
            InputStream = filigran,
            ContentType = "image/png",
        });

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", postgres.GetConnectionString());
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);

            builder.UseSetting("Media:Storage:ServiceUrl", depo.GetConnectionString());
            builder.UseSetting("Media:Storage:Bucket", Bucket);
            builder.UseSetting("Media:PublicBaseUrl", $"https://cdn.ornek.test/{Bucket}/");
            builder.UseSetting("Media:WatermarkObjectKey", WatermarkKey);
        });

        // Kurulum hesabı ilk girişte parolasını DEĞİŞTİRMEK zorunda (0006 R10); aksi hâlde
        // yönetim uçları `403 password_change_required` döner ve bu testler yanlış sebeple
        // kırmızı verir.
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
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await postgres.DisposeAsync();
        await depo.DisposeAsync();
    }

    [Theory(DisplayName = "0012a/AC-10: Oturumu olmayan biri bağlama, sıralama veya çıkarma yapamaz")]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task Yapamaz_OturumsuzKisi_BaglamaSiralamaCikarma(string yontem)
    {
        HttpClient client = Istemci();
        Uri yol = new($"{AdminSurface.RoutePrefix}/vehicles/{Guid.CreateVersion7()}/media", UriKind.Relative);

        HttpResponseMessage yanit = yontem == "GET"
            ? await client.GetAsync(yol)
            : await client.PutAsJsonAsync(yol, new { mediaAssetIds = new[] { Guid.CreateVersion7() } });

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact(DisplayName = "0012a/AC-15: Oturumu olmayan biri `alt` metni yazamaz")]
    public async Task Yazamaz_OturumsuzKisi_AltMetni()
    {
        HttpClient client = Istemci();

        HttpResponseMessage yanit = await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/media/assets/{Guid.CreateVersion7()}/alt-text",
            new { altText = new Dictionary<string, string> { ["tr"] = "önden" } });

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    /// <summary>
    /// Zincirin tamamı, tek testte ve tek konteynerde: yükleme → bağlama → birleşik
    /// okuma → `alt` metni → çıkarma.
    ///
    /// <para><b>Neden tek test:</b> adımlar birbirinin ÖN KOŞULU. Her adımı ayrı teste
    /// bölmek, her birinde yeniden yükleme ve yeniden bağlama demekti — aynı zinciri beş
    /// kez koşan beş test, bir zinciri koşan bir testten daha az şey kanıtlar ve beş kat
    /// uzun sürer. `0011`'in C11 sapması aynı gerekçeyi tersten yazıyordu.</para>
    ///
    /// <para>Ölçülen şey kablo: <c>IMediaAssetQueries</c>'in GERÇEK uygulaması çözülüyor
    /// mu, birleştirme sunucuda mı oluyor, `alt` metni `Media`'ya mı yazılıyor ve
    /// çıkarılan varlık `Media`'da duruyor mu.</para>
    /// </summary>
    [Fact]
    public async Task Kosar_Zincir_YuklemeBaglamaOkumaAltMetniCikarma()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        Guid aracId = await AracOlusturAsync(client, "MEDYAZINCIR00001");

        // 1 — iki fotoğraf YÜKLENİYOR (`Media`'nın ucu).
        Guid birinci = await YukleAsync(client);
        Guid ikinci = await YukleAsync(client);

        Assert.NotEqual(birinci, ikinci);

        // Bağlamadan ÖNCE liste boş: zincirin başlangıç noktası ölçülüyor, varsayılmıyor.
        VehicleMediaDto? baslangic = await MedyaOkuAsync(client, aracId);

        Assert.NotNull(baslangic);
        Assert.Empty(baslangic.Items);
        Assert.Null(baslangic.CoverMediaAssetId);

        // 2 — BAĞLAMA (`VehicleCatalog`'un ucu, D12: tam liste).
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{aracId}/media",
            new { mediaAssetIds = new[] { birinci, ikinci } })).StatusCode);

        // 3 — BİRLEŞİK OKUMA: sıra `VehicleCatalog`'un, durum ve türev `Media`'nın.
        VehicleMediaDto? bagli = await MedyaOkuAsync(client, aracId);

        Assert.NotNull(bagli);
        Assert.Equal([birinci, ikinci], bagli.Items.Select(oge => oge.MediaAssetId));
        Assert.Equal(birinci, bagli.CoverMediaAssetId);

        // Durum bir KOD, çevrilmiş ad değil (Yasak Listesi 22/23).
        Assert.All(bagli.Items, oge => Assert.Contains(
            oge.Status,
            new[] { "uploaded", "processing", "ready" },
            StringComparer.Ordinal));

        // 4 — `alt` metni `Media`'nın ucuna yazılıyor ve BİRLEŞİK okumada görünüyor.
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/media/assets/{birinci}/alt-text",
            new { altText = new Dictionary<string, string> { ["tr"] = "beyaz araç, önden" } }))
            .StatusCode);

        VehicleMediaDto? metinli = await MedyaOkuAsync(client, aracId);

        Assert.NotNull(metinli);
        Assert.Equal(
            "beyaz araç, önden",
            metinli.Items.Single(oge => oge.MediaAssetId == birinci).AltText["tr"]);

        // 5 — SIRA DEĞİŞİYOR ve kapak onunla birlikte değişiyor (D2).
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{aracId}/media",
            new { mediaAssetIds = new[] { ikinci, birinci } })).StatusCode);

        Assert.Equal(ikinci, (await MedyaOkuAsync(client, aracId))!.CoverMediaAssetId);

        // 6 — ÇIKARMA: listeden düşüyor ama varlık `Media`'da DURUYOR (R3, F1).
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{aracId}/media",
            new { mediaAssetIds = new[] { ikinci } })).StatusCode);

        VehicleMediaDto? kalan = await MedyaOkuAsync(client, aracId);

        Assert.NotNull(kalan);
        Assert.Equal([ikinci], kalan.Items.Select(oge => oge.MediaAssetId));

        HttpResponseMessage cikarilan = await client.GetAsync(new Uri(
            $"{AdminSurface.RoutePrefix}/media/assets/{birinci}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, cikarilan.StatusCode);

        // Ve yazdığımız `alt` metni çıkarıldıktan sonra da `Media`'da: metin aracın değil,
        // VARLIĞIN verisi (C5).
        JsonElement govde = JsonDocument.Parse(await cikarilan.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(
            "beyaz araç, önden",
            govde.GetProperty("altText").GetProperty("tr").GetString());
    }

    private async Task<Guid> AracOlusturAsync(HttpClient client, string vin)
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

    private static async Task<Guid> YukleAsync(HttpClient client)
    {
        HttpResponseMessage yanit = await client.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/media/assets", UriKind.Relative),
            Govde(JpegUret()));

        Assert.Equal(HttpStatusCode.Accepted, yanit.StatusCode);

        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<VehicleMediaDto?> MedyaOkuAsync(HttpClient client, Guid aracId)
    {
        HttpResponseMessage yanit = await client.GetAsync(new Uri(
            $"{AdminSurface.RoutePrefix}/vehicles/{aracId}/media", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        return await yanit.Content.ReadFromJsonAsync<VehicleMediaDto>();
    }

    private static MultipartFormDataContent Govde(byte[] icerik, string dosyaAdi = "arac.jpg")
    {
        ByteArrayContent dosya = new(icerik);
        dosya.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        MultipartFormDataContent govde = new();
        govde.Add(dosya, "file", dosyaAdi);

        return govde;
    }

    private static byte[] JpegUret(int genislik = 64, int yukseklik = 48)
    {
        using SKBitmap bitmap = new(genislik, yukseklik);

        using (SKCanvas tuval = new(bitmap))
        {
            tuval.Clear(new SKColor(40, 70, 120));
        }

        using SKData jpeg = bitmap.Encode(SKEncodedImageFormat.Jpeg, 90);

        return jpeg.ToArray();
    }

    private static byte[] FiligranPng()
    {
        using SKBitmap bitmap = new(240, 80);

        using (SKCanvas tuval = new(bitmap))
        {
            tuval.Clear(new SKColor(255, 255, 255, 220));
        }

        using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);

        return png.ToArray();
    }

    /// <summary>
    /// Taban adres `https`: oturum çerezi `Secure` işaretlidir ve `CookieContainer`
    /// güvenli bir çerezi `http` üzerinden saklamaz.
    /// </summary>
    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    private async Task<HttpClient> GirisYapmisIstemciAsync()
    {
        HttpClient client = Istemci();

        HttpResponseMessage giris = await client.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = IdentityTestSettings.SeedAdminLoginId, password = CalisanParola });

        Assert.Equal(HttpStatusCode.OK, giris.StatusCode);

        return client;
    }
}
