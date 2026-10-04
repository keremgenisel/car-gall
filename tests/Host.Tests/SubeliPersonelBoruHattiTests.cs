using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// AC-38/AC-39'un GERÇEK boru hattındaki karşılığı. Modül testleri bu kriterleri
/// `FakeBranchValidator` ile sınıyor: sözleşmeyi doğruluyorlar ama `Identity` →
/// `Dealership` atlamasını hiç geçmiyorlar. Sahte doğrulayıcı `dealership.branches`
/// tablosuna dokunmadığı için şemanın hiç uygulanmamış olması onlara görünmüyordu —
/// temiz bir kuruluma yapılan ilk şubeli personel ekleme `42P01: relation
/// "dealership.branches" does not exist` ile 500 veriyordu.
///
/// Spec'in kendi dersi: "kablolandığı yerde test edilmeyen kural yalnızca niyettir."
/// Bu test tam olarak o kablolamayı ölçer — sahte yok, gerçek Host, gerçek Postgres.
/// </summary>
public sealed class SubeliPersonelBoruHattiTests : IAsyncLifetime
{
    /// <summary>`SeedInitialDealership` migration'ının açtığı Kadıköy şubesi.</summary>
    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    private const string CalisanParola = "calisan-parolasi-1";

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
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
        });

        // Kurulum hesabı zorunlu parola değişimi bekleyerek doğar (D2); o hâldeyken
        // yönetim uçları 403 döner (R10) ve bu testler kriteri değil kendi eksiğini
        // ölçerdi. Adım bir kez, burada aşılıyor.
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

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0006/AC-38: Var olmayan şubeyle personel eklenemez — gerçek Dealership doğrulaması, sahte doğrulayıcı olmadan")]
    public async Task Reddedilir_VarOlmayanSubeyle_GercekBoruHattinda()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/staff",
            Personel("var-olmayan-sube@ornek.com", Guid.CreateVersion7()));

        // 500 DEĞİL: şema uygulanmamış olsaydı `dealership.branches` okunamaz ve istek
        // çökerdi. Bu satır, bulgunun kendisine karşı duran satırdır.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, yanit.StatusCode);

        JsonElement govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("identity.branch_not_found", govde.GetProperty("errorCode").GetString());
        Assert.Equal("branchId", govde.GetProperty("field").GetString());
    }

    [Fact(DisplayName = "0006/AC-38: Gerçekten var olan bir şubeyle personel eklenir — Host `dealership` şemasını uyguluyor")]
    public async Task Eklenir_SeedEdilmisSubeyle_GercekBoruHattinda()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/staff",
            Personel("subeli@ornek.com", SeedSube));

        // Şubenin VAR olduğunu görebilmek, şemanın uygulanmış OLMASINI gerektirir:
        // migration'ın seed ettiği satır ancak o zaman okunabilir.
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
    }

    [Fact(DisplayName = "0006/AC-39: Şube verilmeyen personel eklenebilir; doğrulama yalnız şube verildiğinde çalışır")]
    public async Task Eklenir_Subesiz_GercekBoruHattinda()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/staff",
            Personel("subesiz@ornek.com", branchId: null));

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
    }

    /// <summary>Rol tel üzerinde KOD olarak gider; enum adı değil (C7, AC-52).</summary>
    private static object Personel(string loginId, Guid? branchId) => new
    {
        displayName = "Şube Personeli",
        loginId,
        initialPassword = "ilk-parola-1234",
        role = "sales_advisor",
        preferredLocale = "tr",
        branchId,
    };

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

        HttpResponseMessage yanit = await client.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = CalisanParola,
        });

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        return client;
    }
}
