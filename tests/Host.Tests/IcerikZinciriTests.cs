using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Host.Localization;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0017` — `Content`'in gerçek boru hattı: rol politikası (D25), hata gövdesi (C5),
/// public okuma, kabuktaki eski slug yönlendirmesi (D13, D27) ve belge isteklerinin
/// durumu (D10). Üç rol gerçek personel olarak eklenir ve gerçek oturumla girer.
/// </summary>
public sealed class IcerikZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";

    private static readonly Guid About = Guid.Parse("0192c000-0000-7000-8000-0000c0a70001");
    private static readonly Guid Privacy = Guid.Parse("0192c000-0000-7000-8000-0000c0a70002");

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

        // Kurulum hesabı: zorunlu parola değişimi ve Yöneticide zorunlu ikinci adım (0013).
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

    [Fact(DisplayName = "0017/AC-2, AC-3 (uç yarısı): İçerik Editörü ve Yönetici serbest sayfa oluşturabilir")]
    public async Task Olusturur_EditorVeYonetici()
    {
        Assert.Equal(HttpStatusCode.Created, (await SayfaOlusturAsync(editor, "editor-sayfasi")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await SayfaOlusturAsync(yonetici, "yonetici-sayfasi")).StatusCode);
    }

    [Fact(DisplayName = "0017/AC-109: Satış Danışmanının içeriğin her yazma ucuna isteği 403 döner ve veri değişmez")]
    public async Task Reddeder_SatisDanismani()
    {
        int once = await SurumAsync(About);

        foreach ((HttpMethod yontem, string yol, object? govde) in YazmaUclari())
        {
            HttpResponseMessage yanit = await GonderAsync(danisman, yontem, yol, govde);
            Assert.True(yanit.StatusCode == HttpStatusCode.Forbidden, $"{yontem} {yol} → {(int)yanit.StatusCode}");
        }

        Assert.Equal(once, await SurumAsync(About));
        Assert.DoesNotContain("danisman-sayfasi", await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/content/pages"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0017/AC-110: Oturumsuz gönderilen aynı yazma istekleri 401 döner ve veri değişmez")]
    public async Task Reddeder_Oturumsuz()
    {
        int once = await SurumAsync(About);
        HttpClient oturumsuz = Istemci();

        foreach ((HttpMethod yontem, string yol, object? govde) in YazmaUclari())
        {
            HttpResponseMessage yanit = await GonderAsync(oturumsuz, yontem, yol, govde);
            Assert.True(yanit.StatusCode == HttpStatusCode.Unauthorized, $"{yontem} {yol} → {(int)yanit.StatusCode}");
        }

        Assert.Equal(once, await SurumAsync(About));
    }

    [Fact(DisplayName = "0017/AC-119, AC-120 (uç yarısı): iki alan hatası tek yanıtta; her hata yalnız alan adı ve errorCode taşır, kullanıcı cümlesi yok")]
    public async Task Doner_AlanBazindaKodlar()
    {
        HttpResponseMessage yanit = await editor.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/content/pages", new
        {
            locales = new[] { new { locale = "tr", title = new string('a', 121), body = "x", slug = "A" } },
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, yanit.StatusCode);
        JsonElement govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("content.validation_failed", govde.GetProperty("errorCode").GetString());
        Assert.Equal("tr.title,tr.slug", govde.GetProperty("fields").GetString());
        Assert.Equal("content.title_too_long", govde.GetProperty("tr.title").GetString());
        Assert.Equal("content.slug_invalid", govde.GetProperty("tr.slug").GetString());
        Assert.False(govde.TryGetProperty("title", out _));
        Assert.False(govde.TryGetProperty("detail", out _));
        Assert.False(govde.TryGetProperty("message", out _));
    }

    [Fact(DisplayName = "0017/AC-30 (uç yarısı): eski sürümle kaydetme isteği 409 döner")]
    public async Task Doner_409_EskiSurumde()
    {
        int surum = await SurumAsync(About);
        Assert.Equal(HttpStatusCode.OK, (await KaydetAsync(editor, About, surum, "tr", "Hakkımızda", "x", "hakkimizda-409")).StatusCode);

        HttpResponseMessage ikinci = await KaydetAsync(editor, About, surum, "tr", "Başka", "x", "hakkimizda-409");
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
    }

    /// <summary>
    /// `0004` **D7** ile bilerek değişti (DoD T11): AC-41'in "belge isteği 200" yarısı artık 404'tür
    /// ve onu `0004/AC-74` ölçer (`SeoBelgeTests`). Burada yalnız içerik ucunun 404'ü kaldı.
    /// </summary>
    [Fact(DisplayName = "0017/AC-35 (uç yarısı): olmayan slug için içerik ucu 404 döner")]
    public async Task Doner_404Uc_200Belge()
    {
        HttpClient ziyaretci = Istemci();

        Assert.Equal(HttpStatusCode.NotFound, (await ziyaretci.GetAsync("/api/content/pages/hic-olmayan?locale=tr")).StatusCode);

        HttpResponseMessage belge = await ziyaretci.GetAsync("/tr/hic-olmayan");
        Assert.Equal(HttpStatusCode.NotFound, belge.StatusCode);
        Assert.Null(belge.Headers.Location);
    }

    [Fact(DisplayName = "0017/AC-51, AC-52, AC-56: yayındaki slug değişince eski adres tek 301 ile güncel adrese gider; başka dilde gitmez; zincir yok")]
    public async Task Yonlendirir_EskiSlugu()
    {
        await KaydetVeYayinlaAsync(Privacy, "tr", "KVKK Aydınlatma Metni", "metin", "kvkk");
        Assert.Equal(HttpStatusCode.OK, (await KaydetAsync(yonetici, Privacy, await SurumAsync(Privacy), "tr", "KVKK Aydınlatma Metni", "metin", "kvkk-aydinlatma")).StatusCode);

        HttpClient ziyaretci = Istemci();
        HttpResponseMessage eski = await ziyaretci.GetAsync("/tr/kvkk");
        Assert.Equal(HttpStatusCode.MovedPermanently, eski.StatusCode);
        Assert.Equal("/tr/kvkk-aydinlatma", eski.Headers.Location?.OriginalString);

        // `0004` D7 ile bilerek değişti (DoD T11): en'de yayında olmayan adresin belgesi 200 değil 404.
        // Bu kriterin iddiası — başka dilde yönlendirme yok — aynen duruyor.
        HttpResponseMessage baskaDil = await ziyaretci.GetAsync("/en/kvkk");
        Assert.Equal(HttpStatusCode.NotFound, baskaDil.StatusCode);
        Assert.Null(baskaDil.Headers.Location);

        // D33 — a-1 → a-2 → a-3 sonrası a-1 de a-2 de DOĞRUDAN a-3'e.
        HttpResponseMessage olustur = await SayfaOlusturAsync(editor, "a-1");
        Guid g = JsonDocument.Parse(await olustur.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK, (await editor.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/content/pages/{g}/publish", new { version = await SurumAsync(g), locale = "tr" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await KaydetAsync(editor, g, await SurumAsync(g), "tr", "G", "x", "a-2")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await KaydetAsync(editor, g, await SurumAsync(g), "tr", "G", "x", "a-3")).StatusCode);

        foreach (string kaynak in new[] { "/tr/a-1", "/tr/a-2" })
        {
            HttpResponseMessage yanit = await ziyaretci.GetAsync(kaynak);
            Assert.Equal(HttpStatusCode.MovedPermanently, yanit.StatusCode);
            Assert.Equal("/tr/a-3", yanit.Headers.Location?.OriginalString);
        }
    }

    [Fact(DisplayName = "0017/AC-42: /tr/, /tr/cars, ilan detayı ve iki parçalı yol Content'e eski slug okuması yaptırmaz; yalnız tek parçalı ayrılmamış yol yaptırır")]
    public void Sormaz_DigerAdreslerde()
    {
        foreach (string yol in new[] { "/tr", "/tr/", "/tr/cars", "/tr/cars/toyota-corolla-2019-abc123", "/tr/foo/bar", "/tr/admin", "/tr/api", "/tr/assets" })
        {
            Assert.Null(LocalePrefixMiddleware.ContentSlugOf(new PathString(yol), "tr"));
        }

        Assert.Equal("kvkk", LocalePrefixMiddleware.ContentSlugOf(new PathString("/tr/kvkk"), "tr"));
    }

    [Fact(DisplayName = "0017/AC-50 (Host yarısı): vitrinin tanıdığı tek parçalı bölüm adları ayrılmış slug listesindedir")]
    public void Icerir_VitrinBolumleriniAyrilmisListede()
    {
        Assert.True(ContentReservedSlugs.Contains(LocalePrefixMiddleware.ListSegment));
        Assert.True(ContentReservedSlugs.Contains(LocalePrefixMiddleware.AdminPathPrefix.TrimStart('/')));
    }

    [Fact(DisplayName = "0017/AC-127: öneksiz /hakkimizda Accept-Language: ar ile 404'tür ve Location taşımaz")]
    public async Task Yonlendirmez_OneksizAdresi()
    {
        HttpRequestMessage istek = new(HttpMethod.Get, "/hakkimizda");
        istek.Headers.Add("Accept-Language", "ar");

        HttpResponseMessage yanit = await Istemci().SendAsync(istek);

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.Null(yanit.Headers.Location);
    }

    [Fact(DisplayName = "0017/AC-128: /tr/hakkimizda Accept-Language: en ile tr kabuğunu basar; /en/'e yönlendirme yok")]
    public async Task Basar_TrKabugu_AcceptLanguageEn()
    {
        HttpRequestMessage istek = new(HttpMethod.Get, "/tr/hakkimizda");
        istek.Headers.Add("Accept-Language", "en");

        HttpResponseMessage yanit = await Istemci().SendAsync(istek);

        // `0004` D7 ile bilerek değişti (Proje Yöneticisi kararı, 2026-11-26): yayında olmayan sayfanın
        // belgesi artık 404. Senaryo aynı; iddia: başka dile yönlendirme yok, tr kabuğu basılır.
        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.Null(yanit.Headers.Location);
        Assert.Contains("<html lang=\"tr\"", await yanit.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0017/AC-112 (uç yarısı): taslak ar dilinin oturumsuz okuması 404 döner")]
    public async Task Doner_404_TaslakDil()
    {
        await KaydetVeYayinlaAsync(About, "tr", "Hakkımızda", "x", "hakkimizda-112");
        Assert.Equal(HttpStatusCode.OK, (await KaydetAsync(yonetici, About, await SurumAsync(About), "ar", "مسودة سرية", "x", "man-nahnu-112")).StatusCode);

        HttpClient ziyaretci = Istemci();
        Assert.Equal(HttpStatusCode.NotFound, (await ziyaretci.GetAsync("/api/content/pages/man-nahnu-112?locale=ar")).StatusCode);
        Assert.DoesNotContain("مسودة سرية", await ziyaretci.GetStringAsync("/api/content/pages/hakkimizda-112?locale=tr"), StringComparison.Ordinal);
    }

    private IEnumerable<(HttpMethod, string, object?)> YazmaUclari()
    {
        string kok = $"{AdminSurface.RoutePrefix}/content";
        Guid faq = Guid.CreateVersion7();

        return
        [
            (HttpMethod.Post, $"{kok}/pages", new { locales = new[] { new { locale = "tr", title = "x", body = "x", slug = "danisman-sayfasi" } } }),
            (HttpMethod.Put, $"{kok}/pages/{About}", new { version = 1, locales = new[] { new { locale = "tr", title = "Ele geçirildi", body = "x", slug = "ele-gecirildi" } } }),
            (HttpMethod.Post, $"{kok}/pages/{About}/publish", new { version = 1, locale = "tr" }),
            (HttpMethod.Post, $"{kok}/pages/{About}/unpublish", new { version = 1, locale = "tr" }),
            (HttpMethod.Delete, $"{kok}/pages/{About}?version=1", null),
            (HttpMethod.Post, $"{kok}/faq", new { locales = new[] { new { locale = "tr", question = "q", answer = "a" } } }),
            (HttpMethod.Put, $"{kok}/faq/{faq}", new { version = 1, locales = Array.Empty<object>() }),
            (HttpMethod.Post, $"{kok}/faq/{faq}/publish", new { version = 1, locale = "tr" }),
            (HttpMethod.Post, $"{kok}/faq/{faq}/unpublish", new { version = 1, locale = "tr" }),
            (HttpMethod.Delete, $"{kok}/faq/{faq}?version=1", null),
            (HttpMethod.Post, $"{kok}/faq/{faq}/move", new { direction = "up" }),
        ];
    }

    private static Task<HttpResponseMessage> GonderAsync(HttpClient client, HttpMethod yontem, string yol, object? govde)
    {
        HttpRequestMessage istek = new(yontem, yol);

        if (govde is not null)
        {
            istek.Content = JsonContent.Create(govde);
        }

        return client.SendAsync(istek);
    }

    private Task<HttpResponseMessage> SayfaOlusturAsync(HttpClient client, string slug) =>
        client.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/content/pages", new
        {
            locales = new[] { new { locale = "tr", title = "Sayfa", body = "Gövde.", slug } },
        });

    private Task<HttpResponseMessage> KaydetAsync(HttpClient client, Guid id, int version, string locale, string title, string body, string slug) =>
        client.PutAsJsonAsync($"{AdminSurface.RoutePrefix}/content/pages/{id}", new
        {
            version,
            locales = new[] { new { locale, title, body, slug } },
        });

    private async Task KaydetVeYayinlaAsync(Guid id, string locale, string title, string body, string slug)
    {
        Assert.Equal(HttpStatusCode.OK, (await KaydetAsync(yonetici, id, await SurumAsync(id), locale, title, body, slug)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/content/pages/{id}/publish",
            new { version = await SurumAsync(id), locale })).StatusCode);
    }

    private async Task<int> SurumAsync(Guid id) =>
        JsonDocument.Parse(await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/content/pages/{id}"))
            .RootElement.GetProperty("version").GetInt32();

    /// <summary>Gerçek personel: Yönetici ekler, ilk girişte parola değişir, yeniden girilir.</summary>
    private async Task<HttpClient> PersonelAsync(string loginId, string role)
    {
        Assert.Equal(HttpStatusCode.Created, (await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/staff", new
        {
            displayName = "İçerik Personeli",
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
