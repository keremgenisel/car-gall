using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// D13'ün kapısı GERÇEK boru hattında. Kapı bir ara katmandır ve `UseIdentity()`
/// çağrılmazsa sessizce kaybolur — `PasswordChangeGate` için `Host.Tests`'in var olma
/// sebebi bu, ikinci adım için de aynı.
///
/// Dosya ayrıca giriş yanıtının **şeklini** ölçüyor (D15/D43): bekleyen dalın kimlik
/// taşımaması bir uç davranışıdır, modül testinde görünmez.
/// </summary>
public sealed class IkinciAdimKapisiTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string DanismanParola = "danisman-parolasi-1";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

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
            MediaTestSettings.Apply(builder);
        });

        // Kurulum hesabı zorunlu parola değişimi bekleyerek doğar (`0006` D2). Bu sınıfın
        // konusu İKİNCİ kapı olduğu için ilk kapı burada, bir kez aşılıyor.
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

    [Fact(DisplayName = "0013/AC-29: Zorunlu rolde kurulumu olmayan personel giriş yaptığında yanıt kurulum gerektiğini söyler ve ona **kapılı** bir oturum verilir")]
    public async Task Verir_KapiliOturum_KurulumsuzYoneticiye()
    {
        (HttpClient client, HttpResponseMessage giris) = await GirisAsync();

        JsonElement govde = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("two_factor_setup_required", govde.GetProperty("status").GetString());

        // Oturum çerezi YAZILDI (D13): kurulumu olmayan hesapta parola bugün zaten tek
        // faktör ve R7'nin "önce kurar"ı başka hiçbir yoldan yapılamıyor.
        Assert.Contains(
            giris.Headers.GetValues("Set-Cookie"),
            cerez => cerez.StartsWith(AdminSurface.SessionCookieName, StringComparison.Ordinal));

        // Gövde KİMLİK TAŞIMIYOR (C13): bekleyen dalla aynı suskunluk.
        Assert.False(govde.TryGetProperty("session", out JsonElement oturum) && oturum.ValueKind is JsonValueKind.Object);
    }

    [Theory(DisplayName = "0013/AC-30: Kapılı oturumda izin verilen **üç yol** çalışır: kendi parolasını değiştirme, kendi ikinci adım kurulumu ve çıkış")]
    [InlineData("me/password")]
    [InlineData("me/two-factor")]
    [InlineData("cikis")]
    public async Task Calisir_IzinVerilenUcYol(string yol)
    {
        (HttpClient client, _) = await GirisAsync();

        HttpResponseMessage yanit = yol switch
        {
            "me/password" => await client.PostAsJsonAsync(
                $"{AdminSurface.RoutePrefix}/me/password",
                new { currentPassword = CalisanParola, newPassword = "yeni-parola-1234" }),

            "me/two-factor" => await client.PostAsJsonAsync(
                $"{AdminSurface.RoutePrefix}/me/two-factor", new { }),

            // Çıkış kapının İZNİNDEN değil, KAPSAMINDAN geçiyor: yolu yönetim kökünün
            // dışında ve kapı o kökle sınırlı. Bir gün kökün altına taşınırsa bu satır
            // kırmızıya döner ve muafiyet açıkça yazılmak zorunda kalır.
            _ => await client.DeleteAsync("/api/identity/sessions/current"),
        };

        Assert.True(
            yanit.StatusCode is HttpStatusCode.OK or HttpStatusCode.NoContent,
            $"{yol} → {(int)yanit.StatusCode}");
    }

    [Theory(DisplayName = "0013/AC-31: Kapılı oturumda izin verilenler dışındaki her yönetim isteği `403` ve `identity.two_factor_setup_required` ile reddedilir; oturumsuz çalışan uçların **adıyla sayıldığı** rota kapısı yeşil kalır")]
    [InlineData("me")]
    [InlineData("staff")]
    [InlineData("vehicles")]
    public async Task Doner403_DorduncuIstek(string yol)
    {
        (HttpClient client, _) = await GirisAsync();

        HttpResponseMessage yanit = await client.GetAsync($"{AdminSurface.RoutePrefix}/{yol}");

        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
        Assert.Equal("identity.two_factor_setup_required", await HataKoduAsync(yanit));
    }

    [Fact(DisplayName = "0013/AC-32: İlk girişte sıra **parola değişimi, sonra kurulumdur**: ikisini birden bekleyen personel önce parola kapısını görür")]
    public async Task Once_ParolaKapisi_SonraKurulum()
    {
        // Yeni bir Yönetici: hem parola değişimi hem kurulum bekliyor.
        (HttpClient yonetici, _) = await KurulumuTamamlanmisYoneticiAsync();

        string kimlik = $"ikinci-yonetici-{Guid.CreateVersion7()}@ornek.com";

        Assert.Equal(HttpStatusCode.Created, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/staff",
            new
            {
                displayName = "İkinci Yönetici",
                loginId = kimlik,
                initialPassword = "ilk-parola-1234",
                role = "manager",
                preferredLocale = "tr",
            })).StatusCode);

        HttpClient yeni = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await yeni.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = kimlik, password = "ilk-parola-1234" })).StatusCode);

        // PAROLA kapısı önce: kurulum ucu bile reddediliyor.
        HttpResponseMessage kurulumDenemesi = await yeni.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/two-factor", new { });

        Assert.Equal(HttpStatusCode.Forbidden, kurulumDenemesi.StatusCode);
        Assert.Equal("identity.password_change_required", await HataKoduAsync(kurulumDenemesi));

        Assert.Equal(HttpStatusCode.NoContent, (await yeni.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = "ilk-parola-1234", newPassword = "yeni-parola-1234" })).StatusCode);

        // Parola değişimi oturumu düşürdü; yeniden giriş ve şimdi KURULUM kapısı kapalı.
        HttpClient ikinciOturum = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await ikinciOturum.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = kimlik, password = "yeni-parola-1234" })).StatusCode);

        HttpResponseMessage ikinciKapi = await ikinciOturum.GetAsync($"{AdminSurface.RoutePrefix}/me");

        Assert.Equal(HttpStatusCode.Forbidden, ikinciKapi.StatusCode);
        Assert.Equal("identity.two_factor_setup_required", await HataKoduAsync(ikinciKapi));
    }

    [Fact(DisplayName = "0013/AC-33: Opsiyonel rolde kurulumu olmayan personel yönetim yüzeyinde **kısıtsız** çalışır")]
    public async Task Kisitsiz_OpsiyonelRolde()
    {
        (HttpClient yonetici, _) = await KurulumuTamamlanmisYoneticiAsync();

        string kimlik = $"danisman-{Guid.CreateVersion7()}@ornek.com";

        Assert.Equal(HttpStatusCode.Created, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/staff",
            new
            {
                displayName = "Satış Danışmanı",
                loginId = kimlik,
                initialPassword = "ilk-parola-1234",
                role = "sales_advisor",
                preferredLocale = "tr",
            })).StatusCode);

        HttpClient danisman = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await danisman.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = kimlik, password = "ilk-parola-1234" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await danisman.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = "ilk-parola-1234", newPassword = DanismanParola })).StatusCode);

        HttpClient calisan = Istemci();
        HttpResponseMessage giris = await calisan.PostAsJsonAsync(
            "/api/identity/sessions", new { loginId = kimlik, password = DanismanParola });

        JsonElement govde = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement;

        // Opsiyonel rolde dal TAMAMLANMIŞ: ne ikinci adım ne kurulum isteniyor (D30).
        Assert.Equal("completed", govde.GetProperty("status").GetString());
        Assert.False(govde.GetProperty("session").GetProperty("twoFactorEnrolled").GetBoolean());

        Assert.Equal(HttpStatusCode.OK, (await calisan.GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);
    }

    [Fact(DisplayName = "0013/AC-53: İkinci adım durumu her istekte **kaynaktan** okunur: kurulumu biten personel **aynı oturumda** doğru durumu görür")]
    public async Task Okur_Kaynaktan_AyniOturumda()
    {
        (HttpClient client, _) = await GirisAsync();

        // Kurulumdan ÖNCE: kapı kapalı, yani durum "kurulmamış" olarak okunuyor.
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);

        await IkinciAdim.KurAsync(client);

        // Kurulumdan SONRA, AYNI oturum çereziyle: kapı açıldı ve alan `true`. Girişte
        // pişirilmiş bir claim burada hâlâ "kurulmamış" derdi (D26/D35).
        HttpResponseMessage ben = await client.GetAsync($"{AdminSurface.RoutePrefix}/me");

        Assert.Equal(HttpStatusCode.OK, ben.StatusCode);

        JsonElement govde = JsonDocument.Parse(await ben.Content.ReadAsStringAsync()).RootElement;

        Assert.True(govde.GetProperty("twoFactorEnrolled").GetBoolean());
    }

    [Fact(DisplayName = "0013/AC-12: Bekleyen giriş yanıtı `200` döner, yalnız hangi adımın geldiğini söyler ve personelin adını, rolünü, dilini ve kimliğini **taşımaz**")]
    public async Task TasimazKimlik_BekleyenYanit()
    {
        (HttpClient kurulumYapan, _) = await GirisAsync();
        await IkinciAdim.KurAsync(kurulumYapan);

        HttpClient client = Istemci();
        HttpResponseMessage giris = await client.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = IdentityTestSettings.SeedAdminLoginId, password = CalisanParola });

        Assert.Equal(HttpStatusCode.OK, giris.StatusCode);

        string govde = await giris.Content.ReadAsStringAsync();
        JsonElement ayristirilmis = JsonDocument.Parse(govde).RootElement;

        Assert.Equal("two_factor_required", ayristirilmis.GetProperty("status").GetString());

        foreach (string alan in new[] { "staffId", "displayName", "role", "preferredLocale" })
        {
            Assert.DoesNotContain(alan, govde, StringComparison.OrdinalIgnoreCase);
        }

        Assert.DoesNotContain("Kurulum Yöneticisi", govde, StringComparison.Ordinal);

        // D4/AC-11 — oturum çerezi YAZILMADI; yalnız ara durum çerezi var ve `HttpOnly`.
        List<string> cerezler = [.. giris.Headers.GetValues("Set-Cookie")];

        Assert.DoesNotContain(
            cerezler,
            cerez => cerez.StartsWith($"{AdminSurface.SessionCookieName}=", StringComparison.Ordinal));

        string araDurum = Assert.Single(
            cerezler,
            cerez => cerez.StartsWith("cargallery.pending_login=", StringComparison.Ordinal));

        Assert.Contains("httponly", araDurum, StringComparison.OrdinalIgnoreCase);

        // AC-16 — ara durum bir OTURUM DEĞİL: hiçbir yönetim isteği onunla çalışmaz.
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync($"{AdminSurface.RoutePrefix}/me")).StatusCode);
    }

    [Fact(DisplayName = "0013/AC-16: Ara durum oturum değildir: ara durum çerezini taşıyan hiçbir yönetim isteği çalışmaz, `401` döner")]
    public async Task Doner401_AraDurumlaYonetimIstegi()
    {
        (HttpClient kurulumYapan, _) = await GirisAsync();
        await IkinciAdim.KurAsync(kurulumYapan);

        HttpClient client = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = IdentityTestSettings.SeedAdminLoginId, password = CalisanParola })).StatusCode);

        foreach (string yol in new[] { "me", "staff", "me/two-factor" })
        {
            Assert.Equal(
                HttpStatusCode.Unauthorized,
                (await client.GetAsync($"{AdminSurface.RoutePrefix}/{yol}")).StatusCode);
        }
    }

    [Fact(DisplayName = "0013/AC-17: *\"Kullanıcı değiştir\"* ara durumu **siler**; silinmiş ara durumla yapılan doğrulama reddedilir")]
    public async Task Siler_KullaniciDegistirde()
    {
        (HttpClient kurulumYapan, _) = await GirisAsync();
        string sir = await IkinciAdim.KurAsync(kurulumYapan);

        HttpClient client = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = IdentityTestSettings.SeedAdminLoginId, password = CalisanParola })).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.DeleteAsync("/api/identity/sessions/pending")).StatusCode);

        HttpResponseMessage dogrulama = await client.PostAsJsonAsync(
            "/api/identity/sessions/two-factor",
            new { code = IkinciAdim.Kod(sir, IkinciAdim.AdimSimdi()) });

        Assert.Equal(HttpStatusCode.Unauthorized, dogrulama.StatusCode);
        Assert.Equal("identity.two_factor_pending_expired", await HataKoduAsync(dogrulama));
    }

    [Fact(DisplayName = "0013/AC-46: Yönetici olmayan rolün sıfırlama isteği `403` ile reddedilir")]
    public async Task Doner403_YoneticiOlmayanSifirlama()
    {
        (HttpClient yonetici, _) = await KurulumuTamamlanmisYoneticiAsync();

        string kimlik = $"editor-{Guid.CreateVersion7()}@ornek.com";

        Assert.Equal(HttpStatusCode.Created, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/staff",
            new
            {
                displayName = "İçerik Editörü",
                loginId = kimlik,
                initialPassword = "ilk-parola-1234",
                role = "content_editor",
                preferredLocale = "tr",
            })).StatusCode);

        HttpClient editor = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await editor.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = kimlik, password = "ilk-parola-1234" })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await editor.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = "ilk-parola-1234", newPassword = "editor-parolasi-1" })).StatusCode);

        // Parola değişimi o personelin TÜM oturumlarını düşürdü (`0006` R11): yeniden
        // giriş olmadan gelen red `401` olurdu ve testin ölçtüğü şey YETKİ reddi değil,
        // oturumsuzluk olurdu.
        HttpClient calisan = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await calisan.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = kimlik, password = "editor-parolasi-1" })).StatusCode);

        HttpResponseMessage sifirlama = await calisan.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/staff/{Guid.CreateVersion7()}/two-factor-reset", new { });

        Assert.Equal(HttpStatusCode.Forbidden, sifirlama.StatusCode);
    }

    /// <summary>Kapılı oturum: parolası değişmiş ama kurulumu olmayan Yönetici.</summary>
    private async Task<(HttpClient Client, HttpResponseMessage Giris)> GirisAsync()
    {
        HttpClient client = Istemci();

        HttpResponseMessage giris = await client.PostAsJsonAsync(
            "/api/identity/sessions",
            new { loginId = IdentityTestSettings.SeedAdminLoginId, password = CalisanParola });

        Assert.Equal(HttpStatusCode.OK, giris.StatusCode);

        return (client, giris);
    }

    /// <summary>Kurulumu da tamamlanmış, kapısı açık Yönetici oturumu.</summary>
    private async Task<(HttpClient Client, string Sir)> KurulumuTamamlanmisYoneticiAsync()
    {
        (HttpClient client, _) = await GirisAsync();
        string sir = await IkinciAdim.KurAsync(client);

        return (client, sir);
    }

    private static async Task<string?> HataKoduAsync(HttpResponseMessage yanit) =>
        JsonDocument.Parse(await yanit.Content.ReadAsStringAsync())
            .RootElement.GetProperty("errorCode").GetString();

    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });
}
