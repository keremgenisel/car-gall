using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0022` — ana sayfa metinlerinin gerçek boru hattı: rol politikası (R9), hata gövdesi,
/// eşzamanlılık (R8) ve metnin vitrin belgesine BASILMASI — kodlama (C5, D16), önbellek
/// (D20) ve kaydetmeden hemen sonraki ilk istek (R5). Üç rol gerçek personel olarak eklenir.
/// </summary>
public sealed class AnaSayfaMetinleriZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";
    private const string Kok = $"{AdminSurface.RoutePrefix}/content/home";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;
    private HttpClient yonetici = null!;
    private HttpClient editor = null!;
    private HttpClient danisman = null!;

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

    [Fact(DisplayName = "0022/AC-3: Türkçe giriş başlığı \"Kadıköy'ün güvenilir galerisi\" olarak kaydedildikten **hemen sonraki ilk** Türkçe ana sayfa isteği bu başlığı gösterir (D20).")]
    public async Task Basar_KaydedilenBasligi_IlkIstekte()
    {
        HttpClient ziyaretci = Istemci();
        Assert.DoesNotContain("home-text-intro-title", await ziyaretci.GetStringAsync("/tr/"), StringComparison.Ordinal);

        await KaydetAsync(editor, "tr", introTitle: "Kadıköy'ün güvenilir galerisi");

        string html = await ziyaretci.GetStringAsync("/tr/");
        Assert.Contains(
            "<meta name=\"home-text-intro-title\" content=\"Kadıköy&#39;ün güvenilir galerisi\" />",
            html,
            StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0022/AC-4: Türkçe giriş başlığı kaydedildiğinde İngilizce ve Arapça ana sayfa kendi varsayılan başlıklarını göstermeye devam eder.")]
    public async Task Basmaz_DigerDillerde()
    {
        await KaydetAsync(editor, "tr", introTitle: "Kadıköy'ün güvenilir galerisi");

        HttpClient ziyaretci = Istemci();
        Assert.DoesNotContain("home-text-intro-title", await ziyaretci.GetStringAsync("/en/"), StringComparison.Ordinal);
        Assert.DoesNotContain("home-text-intro-title", await ziyaretci.GetStringAsync("/ar/"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0022/AC-10: Yayında hiç ilan yokken vitrin listesi aynı kaydedilmiş başlığı ve açıklamayı gösterir.")]
    public async Task Basar_BosStokMesajiniListede_DetaydaBasmaz()
    {
        await KaydetAsync(editor, "tr", emptyTitle: "Yeni araçlar yolda", emptyBody: "Bir\nİki");

        HttpClient ziyaretci = Istemci();
        string liste = await ziyaretci.GetStringAsync("/tr/cars");
        Assert.Contains("<meta name=\"home-text-empty-title\" content=\"Yeni araçlar yolda\" />", liste, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"home-text-empty-body\" content=\"Bir&#10;İki\" />", liste, StringComparison.Ordinal);

        // D21 — okuma yalnız ana sayfa ve listede: detay ve içerik adresi bu okumayı ödemez.
        Assert.DoesNotContain("home-text-", await ziyaretci.GetStringAsync("/tr/cars/olmayan-ilan-abc123"), StringComparison.Ordinal);
        Assert.DoesNotContain("home-text-", await ziyaretci.GetStringAsync("/tr/hakkimizda"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0022/AC-12: `<b>Kalın</b> & \"tırnak\"` olarak kaydedilen giriş başlığı ana sayfada bu karakterlerle metin olarak görünür; kalın yazılmaz ve sayfada yeni bir öğe oluşmaz.")]
    public async Task Kodlar_IsaretlemeKarakterlerini()
    {
        await KaydetAsync(editor, "tr", introTitle: "<b>Kalın</b> & \"tırnak\"");

        string html = await Istemci().GetStringAsync("/tr/");

        Assert.Contains(
            "<meta name=\"home-text-intro-title\" content=\"&lt;b&gt;Kalın&lt;/b&gt; &amp; &quot;tırnak&quot;\" />",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("<b>", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0022/AC-13: `</script></head><script>alert(1)</script>` içeren bir açıklama kaydedildiğinde sunucunun ana sayfa yanıtında bu dizi kodlanmış durur, sayfanın `<head>` yapısı bozulmaz ve tarayıcıda hiçbir betik çalışmaz (D16).")]
    public async Task Kodlar_ScriptVeHeadKapanislarini()
    {
        const string Saldiri = "</script></head><script>alert(1)</script>";
        await KaydetAsync(editor, "tr", introBody: $"Önce {Saldiri} sonra", emptyBody: Saldiri);

        HttpClient ziyaretci = Istemci();

        foreach (string adres in new[] { "/tr/", "/tr/cars" })
        {
            string html = await ziyaretci.GetStringAsync(adres);

            Assert.DoesNotContain(Saldiri, html, StringComparison.Ordinal);
            Assert.DoesNotContain("<script>alert", html, StringComparison.Ordinal);
            Assert.Contains("&lt;/script&gt;&lt;/head&gt;&lt;script&gt;alert(1)&lt;/script&gt;", html, StringComparison.Ordinal);

            // `<head>` yapısı: tek açılış, tek kapanış, ve bütün etiketler kapanıştan ÖNCE —
            // `IndexHtmlRenderer`'ın `Replace("</head>", …)`'ı metindeki bir `</head>`'i de değiştirirdi.
            Assert.Single(Regex.Matches(html, "<head>"));
            Assert.Single(Regex.Matches(html, "</head>"));
            int kapanis = html.IndexOf("</head>", StringComparison.Ordinal);
            Assert.All(
                Regex.Matches(html, "<meta name=\"(home-text-[a-z-]+|enabled-locales)\"").Select(eslesme => eslesme.Index),
                konum => Assert.True(konum < kapanis));
            Assert.Equal(html.IndexOf("<body>", StringComparison.Ordinal), html.LastIndexOf("<body>", StringComparison.Ordinal));
            Assert.True(html.IndexOf("<body>", StringComparison.Ordinal) > kapanis);
        }
    }

    [Fact(DisplayName = "0022/AC-14: Giriş açıklamasına yazılan satır sonu ana sayfada yeni satır olarak görünür.")]
    public async Task Korur_SatirSonunu()
    {
        await KaydetAsync(editor, "tr", introBody: "Birinci satır\nİkinci satır");

        Assert.Contains(
            "<meta name=\"home-text-intro-body\" content=\"Birinci satır&#10;İkinci satır\" />",
            await Istemci().GetStringAsync("/tr/"),
            StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0022/AC-16: 121 karakterlik bir başlık ve 501 karakterlik bir açıklama alan hatasıyla reddedilir; kayıtlı metin değişmez.")]
    public async Task Doner_422_AlanKodlariyla()
    {
        HttpResponseMessage yanit = await GonderAsync(editor, await SurumAsync(), "tr", introTitle: new string('a', 121), introBody: new string('b', 501));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, yanit.StatusCode);
        JsonElement govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("content.validation_failed", govde.GetProperty("errorCode").GetString());
        Assert.Equal("content.title_too_long", govde.GetProperty("tr.introTitle").GetString());
        Assert.Equal("content.description_too_long", govde.GetProperty("tr.introBody").GetString());
        Assert.False(govde.TryGetProperty("detail", out _));
        Assert.Equal(1, await SurumAsync());
    }

    [Fact(DisplayName = "0022/AC-23: Ana sayfanın HTML yanıtı önbelleklenmeyi engelleyen bir başlık taşır ya da kayıt önbelleği geçersizler (D20).")]
    public async Task Tasir_NoStore()
    {
        HttpClient ziyaretci = Istemci();

        foreach (string adres in new[] { "/tr/", "/ar/", "/tr/cars" })
        {
            HttpResponseMessage yanit = await ziyaretci.GetAsync(adres);
            Assert.True(yanit.Headers.CacheControl?.NoStore, $"{adres} Cache-Control: {yanit.Headers.CacheControl}");
        }
    }

    [Fact(DisplayName = "0022/AC-25: Okunduktan sonra başka bir kaydetmeyle değişmiş sürüm üzerinden kaydetmek `409` ve bir hata koduyla reddedilir; kayıtlı metin değişmez.")]
    public async Task Doner_409_EskiSurumde()
    {
        int okunan = await SurumAsync();
        await KaydetAsync(yonetici, "tr", introTitle: "Birinci");

        HttpResponseMessage yanit = await GonderAsync(editor, okunan, "tr", introTitle: "İkinci");

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
        Assert.Equal("concurrency.lost", JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement.GetProperty("errorCode").GetString());
        Assert.Contains("content=\"Birinci\"", await Istemci().GetStringAsync("/tr/"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0022/AC-27: Yönetici de ekranı açar ve kaydeder.")]
    public async Task AcarVeKaydeder_Yonetici()
    {
        HttpResponseMessage okuma = await yonetici.GetAsync(Kok);
        Assert.Equal(HttpStatusCode.OK, okuma.StatusCode);
        JsonElement govde = JsonDocument.Parse(await okuma.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(["tr", "ar", "en"], govde.GetProperty("locales").EnumerateArray().Select(l => l.GetProperty("locale").GetString()));

        await KaydetAsync(yonetici, "ar", introTitle: "معرض موثوق");
        Assert.Contains("content=\"معرض موثوق\"", await Istemci().GetStringAsync("/ar/"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0022/AC-29: Satış Danışmanı'nın yazma ucuna isteği `403` alır ve metin değişmez.")]
    public async Task Reddeder_SatisDanismani()
    {
        HttpResponseMessage yanit = await GonderAsync(danisman, 1, "tr", introTitle: "Ele geçirildi");

        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await danisman.GetAsync(Kok)).StatusCode);
        Assert.Equal(1, await SurumAsync());
        Assert.DoesNotContain("Ele geçirildi", await Istemci().GetStringAsync("/tr/"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0022/AC-30: Oturumsuz yazma isteği `401` alır.")]
    public async Task Reddeder_Oturumsuz()
    {
        HttpResponseMessage yanit = await GonderAsync(Istemci(), 1, "tr", introTitle: "Oturumsuz");

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
        Assert.Equal(1, await SurumAsync());
    }

    private async Task KaydetAsync(
        HttpClient client,
        string locale,
        string? introTitle = null,
        string? introBody = null,
        string? emptyTitle = null,
        string? emptyBody = null)
    {
        HttpResponseMessage yanit = await GonderAsync(client, await SurumAsync(), locale, introTitle, introBody, emptyTitle, emptyBody);
        Assert.True(yanit.StatusCode == HttpStatusCode.OK, $"PUT {Kok} → {(int)yanit.StatusCode} {await yanit.Content.ReadAsStringAsync()}");
    }

    private static Task<HttpResponseMessage> GonderAsync(
        HttpClient client,
        int version,
        string locale,
        string? introTitle = null,
        string? introBody = null,
        string? emptyTitle = null,
        string? emptyBody = null) =>
        client.PutAsJsonAsync(Kok, new
        {
            version,
            locales = new[] { new { locale, introTitle, introBody, formTitle = (string?)null, featuredTitle = (string?)null, emptyTitle, emptyBody } },
        });

    private async Task<int> SurumAsync() =>
        JsonDocument.Parse(await yonetici.GetStringAsync(Kok)).RootElement.GetProperty("version").GetInt32();

    /// <summary>Gerçek personel: Yönetici ekler, ilk girişte parola değişir, yeniden girilir.</summary>
    private async Task<HttpClient> PersonelAsync(string loginId, string role)
    {
        Assert.Equal(HttpStatusCode.Created, (await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/staff", new
        {
            displayName = "Ana Sayfa Personeli",
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
