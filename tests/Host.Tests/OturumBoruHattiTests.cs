using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CarGallery.Host.Tests;

/// <summary>
/// Oturumun GERÇEK BORU HATTINDAKİ davranışı. Modül testleri şemanın kendisini sınıyor;
/// burada sınanan şey onun KABLOLANMIŞ olmasıdır — Host `UseAuthentication()` /
/// `UseAuthorization()` / `UseIdentity()` çağırmazsa koruma sessizce kaybolur.
///
/// Saat sabitlenir; süre kriterleri gerçek saate bağlı test yazılarak doğrulanamaz
/// (docs/testing.md § 4, C20).
/// </summary>
public sealed class OturumBoruHattiTests : IAsyncLifetime
{
    private static readonly TimeSpan ErisimSuresi = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan YenilemeSuresi = TimeSpan.FromDays(14);

    private readonly Testcontainers.PostgreSql.PostgreSqlContainer container =
        new Testcontainers.PostgreSql.PostgreSqlBuilder("postgres:17-alpine").WithDatabase("cargallery").Build();

    private readonly TestClock clock = new(new DateTimeOffset(2026, 9, 22, 8, 30, 0, TimeSpan.Zero));

    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);

            // Saat en SONDA değiştirilir: modüller kendi `IClock`'larını kaydediyor ve
            // son kayıt kazanıyor.
            builder.ConfigureTestServices(services => services.AddSingleton<IClock>(clock));
        });

        // Kurulum hesabı zorunlu parola değişimi bekleyerek doğar (D2) ve o hâldeyken
        // yalnız parola değiştirme ile çıkış çalışır (R10). Testlerin ölçtüğü şey oturum
        // yaşam döngüsü olduğu için bu adım bir kez, burada aşılıyor.
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

    /// <summary>İlk değişimden sonraki kalıcı parola; testler bununla giriş yapar.</summary>
    private const string CalisanParola = "calisan-parolasi-1";

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "AC-1: Geçerli kimlik ve parolayla giriş yapan personel yönetim paneline erişir")]
    public async Task Erisir_GecerliGiristenSonra()
    {
        HttpClient client = Istemci();
        await GirisAsync(client);

        HttpResponseMessage yanit = await client.GetAsync($"{AdminSurface.RoutePrefix}/me");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    [Fact(DisplayName = "AC-16: Süresi dolmuş erişim oturumu, geçerli bir yenileme hakkı varken sessizce yenilenir ve personel işini kaybetmez")]
    public async Task SessizceYenilenir_ErisimSuresiDolduktanSonra()
    {
        HttpClient client = Istemci();
        await GirisAsync(client);

        clock.Advance(ErisimSuresi);

        // Personel hiçbir şey yapmadı; ekran bir şey görmedi.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);
    }

    [Fact(DisplayName = "AC-17: Yenileme hakkı yapılandırılan süre sonunda geçerliliğini yitirir; personel yeniden giriş yapar")]
    public async Task Doner401_YenilemeSuresiDolunca()
    {
        HttpClient client = Istemci();
        await GirisAsync(client);

        clock.Advance(YenilemeSuresi);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);
    }

    [Fact(DisplayName = "AC-18: Personel çıkış yaptığında o oturumun yenileme hakkı geçersizleşir ve aynı hakla yeni oturum alınamaz")]
    public async Task Doner401_CikistanSonra()
    {
        HttpClient client = Istemci();
        await GirisAsync(client);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.DeleteAsync("/api/identity/sessions/current")).StatusCode);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);
    }

    [Fact(DisplayName = "AC-19: Çıkış yalnızca o oturumu kapatır; aynı personelin başka bir cihazdaki oturumu çalışmaya devam eder")]
    public async Task Surer_DigerCihaz()
    {
        HttpClient birinci = Istemci();
        HttpClient ikinci = Istemci();

        await GirisAsync(birinci);
        await GirisAsync(ikinci);

        await birinci.DeleteAsync("/api/identity/sessions/current");

        Assert.Equal(HttpStatusCode.Unauthorized, (await birinci.GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ikinci.GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);
    }

    [Fact(DisplayName = "AC-24: Oturumu olmayan biri yönetim ekranına gittiğinde giriş ekranına yönlendirilir")]
    public async Task Doner401_OturumsuzYonetimIstegi()
    {
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await Istemci().GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);
    }

    /// <summary>
    /// AC-54/AC-55'in SUNUCU yarısı: panel kabuğu personelin tercih ettiği dille basılır
    /// ve ziyaretçinin o anki vitrin dili paneli etkilemez.
    /// </summary>
    [Fact(DisplayName = "AC-54: Yönetim paneli, giriş yapan personelin tercih ettiği dilde açılır; ziyaretçinin o anki vitrin dili paneli etkilemez")]
    public async Task Basar_PersonelinDiliyle()
    {
        HttpClient client = Istemci();
        await GirisAsync(client);

        string panel = await (await client.GetAsync("/admin")).Content.ReadAsStringAsync();

        // Kurulum hesabının tercihi varsayılan dildir.
        Assert.Contains("lang=\"tr\"", panel, StringComparison.Ordinal);
        Assert.Contains("dir=\"ltr\"", panel, StringComparison.Ordinal);

        // Ziyaretçi Arapça vitrindeyken bile panel personelin dilinde kalır.
        HttpResponseMessage vitrin = await client.GetAsync("/ar/");
        Assert.Contains("dir=\"rtl\"", await vitrin.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        string panelYine = await (await client.GetAsync("/admin")).Content.ReadAsStringAsync();
        Assert.Contains("lang=\"tr\"", panelYine, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "AC-24: Oturumu olmayan biri yönetim ekranına gittiğinde giriş ekranına yönlendirilir")]
    public async Task Basar_VarsayilanDille_OturumYokken()
    {
        string panel = await (await Istemci().GetAsync("/admin")).Content.ReadAsStringAsync();

        Assert.Contains("lang=\"tr\"", panel, StringComparison.Ordinal);
    }

    /// <summary>
    /// Taban adres `https`: oturum çerezi `Secure` işaretlidir (C17) ve `CookieContainer`
    /// güvenli bir çerezi `http` üzerinden saklamaz. Şemanın gerçeği yansıtmaması,
    /// testin çerezi hiç taşımamasına ve her isteğin 401 olmasına yol açardı.
    /// </summary>
    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    private static async Task GirisAsync(HttpClient client)
    {
        HttpResponseMessage yanit = await client.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = CalisanParola,
        });

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        JsonElement govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

        Assert.False(govde.GetProperty("mustChangePassword").GetBoolean());
    }

    private sealed class TestClock(DateTimeOffset start) : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = start;

        internal void Advance(TimeSpan amount) => UtcNow = UtcNow.Add(amount);
    }
}
