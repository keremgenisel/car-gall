using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using SkiaSharp;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0026` D1 · D4 — yükleme ucunun <c>kind</c> alanı ve yükleme yetkisi, gerçek boru hattında ve
/// gerçek depoyla (orijinal İSTEK İÇİNDE yazılıyor, `0011` D9). Tür değeri TEK kapıdan: alan yoksa
/// `photo`, varsa birebir `photo` ya da `logo`; aksi her değer `422` `media.kind_unknown` ve varlık
/// oluşmaz.
/// </summary>
public sealed class LogoYuklemeUcuTests : IAsyncLifetime
{
    private const string Bucket = "cargallery-media-logo";
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";
    private const string Yukleme = $"{AdminSurface.RoutePrefix}/media/assets";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private readonly LocalStackContainer depo = new LocalStackBuilder("localstack/localstack:3.8.1").Build();

    private WebApplicationFactory<Program>? factory;
    private HttpClient yonetici = null!;
    private HttpClient danisman = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(container.StartAsync(), depo.StartAsync());
        await TestSchema.ApplyAsync(container.GetConnectionString());

        using AmazonS3Client s3 = new("test", "test", new AmazonS3Config
        {
            ServiceURL = depo.GetConnectionString(),
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
        });
        await s3.PutBucketAsync(new PutBucketRequest { BucketName = Bucket });

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);
            builder.UseSetting("Media:Storage:ServiceUrl", depo.GetConnectionString());
            builder.UseSetting("Media:Storage:Bucket", Bucket);
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
        danisman = await PersonelAsync("danisman@ornek.com", "sales_advisor");
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
        await depo.DisposeAsync();
    }

    [Theory(DisplayName = "0026/AC-72: `banner`, boş, harf büyüklüğü farklı, NUL ya da satır sonu içeren ve çift verilen tür değeri `422` `media.kind_unknown` döner; varlık oluşmaz.")]
    [InlineData("banner")]
    [InlineData("")]
    [InlineData("Logo")]
    [InlineData("logo\0")]
    [InlineData("logo\n")]
    [InlineData(" logo")]
    [InlineData("logo|logo")]
    public async Task Doner422_BilinmeyenTur(string tur)
    {
        long once = await VarlikSayisiAsync();

        HttpResponseMessage yanit = await yonetici.PostAsync(Yukleme, Govde(Png(), tur.Split('|')));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, yanit.StatusCode);
        Assert.Equal("media.kind_unknown", (await yanit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errorCode").GetString());
        Assert.Equal(once, await VarlikSayisiAsync());
    }

    [Fact(DisplayName = "0026/AC-7: Logo türüyle yüklenen SVG `415` ile reddedilir ve varlık oluşmaz.")]
    public async Task Doner415_SvgLogo()
    {
        long once = await VarlikSayisiAsync();
        byte[] svg = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"></svg>"u8.ToArray();

        HttpResponseMessage yanit = await yonetici.PostAsync(Yukleme, Govde(svg, ["logo"]));

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, yanit.StatusCode);
        Assert.Equal(once, await VarlikSayisiAsync());
    }

    [Fact(DisplayName = "0026/AC-9, AC-18: Satış Danışmanı logo türünde yükler (`202`, varlığın türü `logo`) ama varlık galeriye atanmaz; tür verilmeden yükleme `photo`.")]
    public async Task Yukler_Danisman_AmaAtanmaz()
    {
        HttpResponseMessage yanit = await danisman.PostAsync(Yukleme, Govde(Png(), ["logo"]));

        Assert.Equal(HttpStatusCode.Accepted, yanit.StatusCode);
        Guid id = (await yanit.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal("logo", (await yonetici.GetFromJsonAsync<JsonElement>($"{Yukleme}/{id}")).GetProperty("kind").GetString());
        Assert.Equal(JsonValueKind.Null, (await yonetici.GetFromJsonAsync<JsonElement>($"{AdminSurface.RoutePrefix}/gallery")).GetProperty("logoMediaAssetId").ValueKind);

        // Atama ise yalnız Yönetici (D4): Satış Danışmanı `403`.
        Assert.Equal(HttpStatusCode.Forbidden, (await danisman.PutAsJsonAsync($"{AdminSurface.RoutePrefix}/gallery/logo", new { mediaAssetId = id, version = 1 })).StatusCode);

        HttpResponseMessage fotograf = await yonetici.PostAsync(Yukleme, Govde(Png(), []));
        Guid fotografId = (await fotograf.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal("photo", (await yonetici.GetFromJsonAsync<JsonElement>($"{Yukleme}/{fotografId}")).GetProperty("kind").GetString());
    }

    private static MultipartFormDataContent Govde(byte[] icerik, string[] turler)
    {
        ByteArrayContent dosya = new(icerik);
        dosya.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        MultipartFormDataContent govde = new();

        foreach (string tur in turler)
        {
            govde.Add(new StringContent(tur), "kind");
        }

        govde.Add(dosya, "file", "logo.png");
        return govde;
    }

    private static byte[] Png()
    {
        using SKBitmap bitmap = new(200, 50);
        bitmap.Erase(new SKColor(40, 70, 120));
        using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    private async Task<long> VarlikSayisiAsync()
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM media.assets";
        return (long)(await command.ExecuteScalarAsync())!;
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
