using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SkiaSharp;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// Medya yüklemesinin GERÇEK boru hattında koştuğu tek yer. Modül testleri komutun doğru
/// çalıştığını gösteriyor; bu testler onun <b>kablolanmış</b> olduğunu gösteriyor:
/// gerçek kimlik doğrulama, gerçek rota kökü, gerçek depolama.
///
/// <para>`0006`/C18'in rota TARAYAN testi yönetim kökü altındaki her ucun oturum
/// gerektirdiğini zaten doğruluyor; buradaki AC-2 onun medya ucu için somut hâli.</para>
/// </summary>
public sealed class MedyaYuklemeZinciriTests : IAsyncLifetime
{
    private const string Bucket = "cargallery-media-zincir";

    private const string WatermarkKey = "private/brand/watermark.png";

    private const string CalisanParola = "calisan-parolasi-1";

    /// <summary>
    /// Yapılandırılan boyut sınırı. 640×480 JPEG bunun üstünde, 64×48 JPEG altında —
    /// ikisi de aynı uçtan geçiyor ve fark yalnızca boyut.
    /// </summary>
    private const long ByteSinir = 8 * 1024;

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    // D9 — orijinal İSTEK İÇİNDE depoya yazılıyor; depo olmadan AC-1 kanıtlanamaz.
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

            // Gerçek depo: `MediaTestSettings`'in yer tutucu adresinin üzerine yazıyor.
            builder.UseSetting("Media:Storage:ServiceUrl", depo.GetConnectionString());
            builder.UseSetting("Media:Storage:Bucket", Bucket);
            builder.UseSetting("Media:PublicBaseUrl", $"https://cdn.ornek.test/{Bucket}/");
            builder.UseSetting("Media:WatermarkObjectKey", WatermarkKey);

            // AC-5'in HTTP yolunu ölçebilmek için sınır KÜÇÜK tutuluyor; kriter
            // yapılandırılan değeri doğruluyor, koda gömülü bir sabiti değil.
            builder.UseSetting("Media:MaximumByteSize", ByteSinir.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
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

    [Fact(DisplayName = "0011/AC-1: Oturum açmış personel bir görsel yükler; yanıt varlığın kimliğini ve işleme durumunu taşır")]
    public async Task Yukler_Personel_Gorsel()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await client.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/media/assets", UriKind.Relative),
            Govde(JpegUret()));

        Assert.Equal(HttpStatusCode.Accepted, yanit.StatusCode);

        JsonElement govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

        Assert.NotEqual(Guid.Empty, govde.GetProperty("id").GetGuid());

        // Durum bir KOD, çevrilmiş ad değil (Yasak Listesi 22/23).
        Assert.Equal("uploaded", govde.GetProperty("status").GetString());

        // Ve kayıt gerçekten okunabiliyor: uç yalnız kimlik üretmiyor, varlık duruyor.
        HttpResponseMessage okuma = await client.GetAsync(new Uri(
            $"{AdminSurface.RoutePrefix}/media/assets/{govde.GetProperty("id").GetGuid()}",
            UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, okuma.StatusCode);
    }

    [Theory(DisplayName = "0011/AC-2: Oturumu olmayan biri görsel yükleyemez; istek reddedilir")]
    [InlineData("POST")]
    [InlineData("GET")]
    public async Task Yukleyemez_OturumsuzKisi(string yontem)
    {
        HttpClient client = Istemci();

        HttpResponseMessage yanit = yontem == "POST"
            ? await client.PostAsync(
                new Uri($"{AdminSurface.RoutePrefix}/media/assets", UriKind.Relative),
                Govde(JpegUret()))
            : await client.GetAsync(new Uri(
                $"{AdminSurface.RoutePrefix}/media/assets/{Guid.CreateVersion7()}",
                UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact(DisplayName = "0011/AC-7: Reddedilen istek çevrilmiş metin değil `errorCode` döner")]
    public async Task Doner_ErrorCode_ReddedilenIstekte()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        // `image/jpeg` adıyla gönderilen SVG (AC-4): tür İÇERİKTEN belirleniyor.
        HttpResponseMessage yanit = await client.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/media/assets", UriKind.Relative),
            Govde(
                """<?xml version="1.0"?><svg xmlns="http://www.w3.org/2000/svg"></svg>"""u8.ToArray(),
                "arac.jpg"));

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);

        string govde = await yanit.Content.ReadAsStringAsync();
        JsonElement kok = JsonDocument.Parse(govde).RootElement;

        Assert.Equal("media.unsupported_type", kok.GetProperty("errorCode").GetString());
        Assert.False(kok.TryGetProperty("title", out _));
        Assert.False(kok.TryGetProperty("detail", out _));
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

    /// <summary>
    /// Sıkışmayan bir görsel. İlk denemede 1200×900 düz renk kullandım ve JPEG onu
    /// **6700 bayta** sıkıştırdı — sınırın altında kaldı ve test kendi iddiasıyla kırmızı
    /// verdi. Gürültü, boyutun piksel sayısıyla birlikte büyümesini garanti ediyor.
    /// </summary>
    private static byte[] BuyukJpegUret()
    {
        using SKBitmap bitmap = new(400, 400);
        Random rastgele = new(Seed: 11);

        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)rastgele.Next(256), (byte)rastgele.Next(256), (byte)rastgele.Next(256)));
            }
        }

        using SKData jpeg = bitmap.Encode(SKEncodedImageFormat.Jpeg, 95);

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

    [Fact(DisplayName = "0011/AC-5: Yapılandırılan boyut sınırını aşan dosya reddedilir ve varlık kaydı oluşmaz")]
    public async Task Reddedilir_SinirdanBuyukDosya_UcUzerinden()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        // Sınırın BELİRGİN biçimde üstünde: boru hattının `InvalidDataException`'ı bu
        // yolda tetiklenir ve yakalanmazsa `500` döner (ölçüldü).
        byte[] buyuk = BuyukJpegUret();

        Assert.True(buyuk.Length > ByteSinir, $"Test görseli sınırın altında: {buyuk.Length}");

        HttpResponseMessage yanit = await client.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/media/assets", UriKind.Relative),
            Govde(buyuk));

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);

        JsonElement govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("media.too_large", govde.GetProperty("errorCode").GetString());

        // AC-7 — gövdede uçta yanlışlıkla basılabilecek İngilizce bir metin YOK. Genel
        // `UseExceptionHandler` tam olarak bunu koyuyordu.
        Assert.False(govde.TryGetProperty("title", out _));
        Assert.False(govde.TryGetProperty("detail", out _));
        Assert.False(govde.TryGetProperty("traceId", out _));
    }

    /// <summary>
    /// Reddin sebebinin BOYUT olduğunun kanıtı: aynı uç, aynı oturum, sınırın altındaki
    /// gövde kabul ediliyor. Bu olmadan test "uç her şeyi reddediyor" hâlinde de yeşil
    /// kalırdı.
    /// </summary>
    [Fact]
    public async Task Kabul_Edilir_SinirinAltindakiDosya_AyniUctan()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        byte[] kucuk = JpegUret();

        Assert.True(kucuk.Length < ByteSinir, $"Test görseli sınırın üstünde: {kucuk.Length}");

        HttpResponseMessage yanit = await client.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/media/assets", UriKind.Relative),
            Govde(kucuk));

        Assert.Equal(HttpStatusCode.Accepted, yanit.StatusCode);
    }

    /// <summary>
    /// Taban adres `https`: oturum çerezi `Secure` işaretlidir ve `CookieContainer`
    /// güvenli bir çerezi `http` üzerinden saklamaz — `0010a`'nın zincir testi aynı
    /// bedeli ödedi.
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
