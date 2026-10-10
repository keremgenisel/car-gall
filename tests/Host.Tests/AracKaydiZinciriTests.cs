using CarGallery.BuildingBlocks;
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

        // 0010b — liste yanıtı artık `PagedResult` zarfında (AC-25). `0010a`'nın kriteri
        // değişmedi: kaydın listede görünmesi. Değişen tek şey zarfın adı.
        PagedResult<VehicleAdminListItemDto>? liste = await client
            .GetFromJsonAsync<PagedResult<VehicleAdminListItemDto>>($"{AdminSurface.RoutePrefix}/vehicles?locale=tr");

        Assert.NotNull(liste);
        Assert.Contains(liste.Items, item => item.Vin == "ZINCIR0000000001");
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

    [Fact(DisplayName = "0010b/AC-1: Oturumu olmayan biri araç düzenleyemez; istek reddedilir")]
    public async Task Duzenleyemez_OturumsuzKisi()
    {
        HttpClient client = Istemci();

        HttpResponseMessage yanit = await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{Guid.CreateVersion7()}",
            Duzenleme("OTURUMSUZDUZ0001"));

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact(DisplayName = "0010b/AC-4: Var olmayan bir araç kimliğiyle gelen düzenleme isteği `404` ve kararlı bir hata koduyla reddedilir")]
    public async Task Reddedilir_VarOlmayanAracKimligi()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{Guid.CreateVersion7()}",
            Duzenleme("YOKARAC000000001"));

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);

        using JsonDocument govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());

        Assert.Equal("vehicle.not_found", govde.RootElement.GetProperty("errorCode").GetString());
    }

    /// <summary>
    /// AC-5 — eksik gelen alan "dokunulmadı" DEĞİL geçersiz sayılır (C25). Ölçüm TEL
    /// ÜZERİNDE yapılıyor: gövdeden alan çıkarıldığında bağlayıcı varsayılan değer
    /// üretiyor ve kuralın orada da tuttuğunu yalnız bu seviye gösterebilir.
    /// </summary>
    [Fact(DisplayName = "0010b/AC-5: Çekirdek alanlardan birini hiç göndermeyen düzenleme isteği reddedilir; eksik alan \"dokunulmadı\" sayılmaz (C25)")]
    public async Task Reddedilir_CekirdekAlaniHicGondermeyenIstek()
    {
        HttpClient client = await GirisYapmisIstemciAsync();
        Guid id = await AracOlusturAsync(client, "EKSIKALAN0000001");

        // `bodyType` gövdede HİÇ YOK.
        HttpResponseMessage yanit = await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{id}",
            new
            {
                branchId = SeedSube,
                vehicleModelId = SeedModel,
                fuelType = "diesel",
                transmissionType = "automatic",
                drivetrainType = "front-wheel-drive",
                trimLevel = "comfort",
                color = new Dictionary<string, string> { ["tr"] = "Beyaz" },
                modelYear = 2019,
                mileageKilometers = 45_000,
                priceAmount = 750_000m,
                priceOnRequest = false,
                vin = "EKSIKALAN0000001",
            });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, yanit.StatusCode);

        using JsonDocument govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());

        Assert.Equal("vehicle.core_field_missing", govde.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("bodyType", govde.RootElement.GetProperty("field").GetString());
    }

    [Fact(DisplayName = "0010b/AC-2: Kaydedilmiş bir araç düzenlenir ve değişiklik listede görünür")]
    public async Task Duzenlenir_VeDegisiklikListedeGorunur()
    {
        HttpClient client = await GirisYapmisIstemciAsync();
        Guid id = await AracOlusturAsync(client, "ZINCIRDUZ0000001");

        HttpResponseMessage yanit = await client.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{id}",
            Duzenleme("ZINCIRDUZ0000001", kilometre: 88_000));

        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);

        PagedResult<VehicleAdminListItemDto>? liste = await client
            .GetFromJsonAsync<PagedResult<VehicleAdminListItemDto>>($"{AdminSurface.RoutePrefix}/vehicles?locale=tr");

        Assert.Equal(88_000, liste!.Items.Single(item => item.VehicleId == id).MileageKilometers);
    }

    [Fact(DisplayName = "0010b/AC-25: Liste yanıtı `PagedResult<T>` biçimindedir ve sayfa, sayfa boyutu ile toplam kayıt sayısını taşır")]
    public async Task Tasir_SayfalamaZarfini()
    {
        HttpClient client = await GirisYapmisIstemciAsync();
        await AracOlusturAsync(client, "ZARFZINCIR000001");

        using JsonDocument govde = JsonDocument.Parse(
            await client.GetStringAsync(new Uri($"{AdminSurface.RoutePrefix}/vehicles?locale=tr", UriKind.Relative)));

        foreach (string alan in new[] { "items", "page", "pageSize", "totalCount" })
        {
            Assert.True(govde.RootElement.TryGetProperty(alan, out _), alan);
        }
    }

    [Fact(DisplayName = "0010b/AC-26: Üst sınırı aşan bir `pageSize` isteği üst sınıra kırpılır ve hata dönmez (C26)")]
    public async Task Kirpilir_UstSiniriAsanSayfaBoyutu()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        PagedResult<VehicleAdminListItemDto>? liste = await client
            .GetFromJsonAsync<PagedResult<VehicleAdminListItemDto>>(
                $"{AdminSurface.RoutePrefix}/vehicles?locale=tr&pageSize=5000");

        Assert.Equal(100, liste!.PageSize);
    }

    private async Task<Guid> AracOlusturAsync(HttpClient client, string vin)
    {
        HttpResponseMessage yanit = await client.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles", Arac(vin));

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);

        using JsonDocument govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());

        return govde.RootElement.GetProperty("vehicleId").GetGuid();
    }

    private static object Duzenleme(string vin, int kilometre = 45_000) => new
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
        mileageKilometers = kilometre,
        priceAmount = 750_000m,
        priceOnRequest = false,
        vin,
        listingTitle = new Dictionary<string, string> { ["tr"] = "Temiz Corolla" },
        listingDescription = new Dictionary<string, string> { ["tr"] = "Bakımları yapıldı" },
        optionalValues = Array.Empty<object>(),
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
