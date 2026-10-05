using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// Araç kaydının GERÇEK boru hattında koştuğu tek yer. Modül testleri komutun doğru
/// çalıştığını gösteriyor; bu testler onun **kablolanmış** olduğunu gösteriyor.
///
/// <para>Şube, kasa tipi, referans terimleri ve model — dördü de MIGRATION'ın seed
/// ettiği gerçek satırlar. Sahte bir komşuyla koşsaydı, `0006`'da yaşanan "şema üretim
/// yolunda hiç uygulanmamış" hatası burada da görünmezdi.</para>
/// </summary>
public sealed class AracKaydiZinciriTests : IAsyncLifetime
{
    /// <summary>`SeedInitialDealership` migration'ının açtığı Kadıköy şubesi.</summary>
    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    /// <summary>`SeedInitialVehicleModels` migration'ının ilk satırı.</summary>
    private static readonly Guid SeedModel = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private const string CalisanParola = "calisan-parolasi-1";

    private WebApplicationFactory<Program>? factory;

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

    /// <summary>
    /// Taban adres `https`: oturum çerezi `Secure` işaretlidir ve `CookieContainer`
    /// güvenli bir çerezi `http` üzerinden SAKLAMAZ — ilk denememde bütün istekler
    /// oturumsuz gitti ve `401` aldı.
    /// </summary>
    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0010a/AC-1: Oturum açmış personel, çekirdek alanları doldurarak bir araç kaydı oluşturur ve kayıt listede görünür")]
    public async Task Olusturur_Personel_AracKaydi()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles",
            Arac("ZINCIR0000000001"));

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);

        IReadOnlyList<VehicleAdminListItemDto>? liste = await client
            .GetFromJsonAsync<IReadOnlyList<VehicleAdminListItemDto>>($"{AdminSurface.RoutePrefix}/vehicles?locale=tr");

        Assert.NotNull(liste);
        Assert.Contains(liste, item => item.Vin == "ZINCIR0000000001");
    }

    [Theory(DisplayName = "0010a/AC-2: Oturumu olmayan biri araç kaydı oluşturamaz; istek reddedilir")]
    [InlineData("POST")]
    [InlineData("GET")]
    public async Task Olusturamaz_OturumsuzKisi(string yontem)
    {
        HttpClient client = Istemci();

        HttpResponseMessage yanit = yontem == "POST"
            ? await client.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles", Arac("OTURUMSUZ0000001"))
            : await client.GetAsync(new Uri($"{AdminSurface.RoutePrefix}/vehicles", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact(DisplayName = "0010a/AC-19: Şase numarası yönetim yanıtında bulunur ve o yanıt oturum gerektirir (C20)")]
    public async Task Bulunur_Sase_YonetimYanitinda()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        await client.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles", Arac("YONETIM000000001"));

        string govde = await client.GetStringAsync(new Uri($"{AdminSurface.RoutePrefix}/vehicles?locale=tr", UriKind.Relative));

        Assert.Contains("YONETIM000000001", govde, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0010a/AC-18: Şase numarası hiçbir vitrin yanıtında bulunmaz; boş değer olarak da bulunmaz")]
    public async Task Bulunmaz_Sase_VitrinYanitinda()
    {
        HttpClient client = await GirisYapmisIstemciAsync();
        await client.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles", Arac("VITRIN0000000001"));

        // Taslak ilan vitrinde görünmez (AC-11); yine de yanıtın şase ALANI taşımadığını
        // ölçüyoruz — "boş değer olarak da bulunmaz" bunu gerektiriyor.
        string liste = await client.GetStringAsync(new Uri("/api/catalog/showcase-listings?locale=tr", UriKind.Relative));

        Assert.DoesNotContain("VITRIN0000000001", liste, StringComparison.Ordinal);
        Assert.DoesNotContain("vin", liste, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "0010a/AC-11: Taslak durumundaki ilan vitrin listesinde ve ilan detayında görünmez")]
    public async Task Gorunmez_TaslakIlan_Vitrinde()
    {
        HttpClient client = await GirisYapmisIstemciAsync();
        await client.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles", Arac("TASLAK0000000001"));

        JsonElement vitrin = JsonDocument
            .Parse(await client.GetStringAsync(new Uri("/api/catalog/showcase-listings?locale=tr", UriKind.Relative)))
            .RootElement;

        // Vitrinde hiç ilan yok: tek kayıt TASLAK ve taslak vitrine çıkmaz (A3).
        Assert.Equal(0, vitrin.GetProperty("totalCount").GetInt32());
    }

    private static object Arac(string vin) => new
    {
        branchId = SeedSube,
        vehicleModelId = SeedModel,
        bodyType = "suv",
        fuelType = "diesel",
        transmissionType = "automatic",
        drivetrainType = "front-wheel-drive",
        trimLevel = "comfort",
        color = new Dictionary<string, string> { ["tr"] = "Beyaz", ["en"] = "White" },
        modelYear = 2019,
        mileageKilometers = 45_000,
        priceAmount = 750_000m,
        priceOnRequest = false,
        vin,
    };

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
