using System.Net;
using System.Text;
using CarGallery.BuildingBlocks;
using CarGallery.Host.Localization;
using CarGallery.Host.Theme;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0014` — adresler (R10, R11 · D9, D17, D21, D22). Sunucu yarısı: yönlendirme, durum
/// kodu ve sayfaya basılan etkin locale listesi. Tarayıcı yarısı (geri tuşu, görünen
/// sayfa) `frontend/e2e/ana-sayfa.spec.ts`'te.
/// </summary>
public sealed class AnaSayfaAdresTests(HostFactory factory) : IClassFixture<HostFactory>
{
    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    [Fact(DisplayName = "0014/AC-2: / istenince varsayılan locale'in ana sayfasına 301 ile yönlenir")]
    public async Task Yonlenir301_VarsayilanAnaSayfaya()
    {
        HttpResponseMessage yanit = await CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.MovedPermanently, yanit.StatusCode);
        Assert.Equal("/tr/", yanit.Headers.Location?.OriginalString);
    }

    [Fact(DisplayName = "0014/AC-3: Etkin olmayan bir önek (/xx/) 404 verir")]
    public async Task Doner404_EtkinOlmayanOnek()
    {
        HttpResponseMessage yanit = await CreateClient().GetAsync("/xx/");

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.Null(yanit.Headers.Location);
    }

    [Fact(DisplayName = "0014/AC-4: İstek Accept-Language: ar taşısa da / varsayılan locale'e yönlenir")]
    public async Task Yonlenir_Varsayilana_AcceptLanguageArIken()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "ar");

        HttpResponseMessage yanit = await client.GetAsync("/");

        Assert.Equal("/tr/", yanit.Headers.Location?.OriginalString);
    }

    [Theory(DisplayName = "0014/AC-5: /{locale}/cars araç listesini gösterir (sunucu yarısı: yönlendirme yok, sayfa o dilde basılır)")]
    [InlineData("/tr/cars", "tr")]
    [InlineData("/ar/cars?bodyType=suv&page=2", "ar")]
    public async Task Doner200_ListeAdresinde(string adres, string locale)
    {
        HttpResponseMessage yanit = await CreateClient().GetAsync(adres);

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Contains($"lang=\"{locale}\"", await yanit.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0014/AC-6: /tr/?bodyType=suv&page=2 açıldığında ziyaretçi /tr/cars?bodyType=suv&page=2 adresindeki listeyi görür (sunucu yarısı: 301 ve hedef)")]
    public async Task Yonlenir_EskiListeAdresi_AyniSorguyla()
    {
        HttpResponseMessage yanit = await CreateClient().GetAsync("/tr/?bodyType=suv&page=2");

        Assert.Equal(HttpStatusCode.MovedPermanently, yanit.StatusCode);
        Assert.Equal("/tr/cars?bodyType=suv&page=2", yanit.Headers.Location?.OriginalString);
    }

    [Theory(DisplayName = "0014/AC-8: /tr/ liste parametresi (bodyType, page, pageSize) taşımıyorsa yönlendirme olmaz; ana sayfa görünür. /tr/?utm_source=x de ana sayfayı gösterir")]
    [InlineData("/tr/")]
    [InlineData("/tr/?utm_source=x")]
    // QA BULGU-1 — anahtar eşleşmesi ORDİNAL: uç `bodyType`'ı harfe duyarlı okuyor; sunucu
    // `BodyType`'ı liste parametresi sayarsa ziyaretçi ana sayfa yerine filtresiz listeye
    // düşer ve 301 tarayıcıda kalıcı önbelleğe girer.
    [InlineData("/tr/?BodyType=suv")]
    [InlineData("/tr/?PAGE=2")]
    [InlineData("/tr/?PageSize=12")]
    public async Task Yonlendirmez_ListeParametresiYoksa(string adres)
    {
        HttpResponseMessage yanit = await CreateClient().GetAsync(adres);

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Null(yanit.Headers.Location);
    }

    [Fact(DisplayName = "0014/AC-69: /tr/?pageSize=12 açıldığında ziyaretçi /tr/cars?pageSize=12 adresine yönlenir")]
    public async Task Yonlenir_YalnizPageSizeIle()
    {
        HttpResponseMessage yanit = await CreateClient().GetAsync("/tr/?pageSize=12");

        Assert.Equal(HttpStatusCode.MovedPermanently, yanit.StatusCode);
        Assert.Equal("/tr/cars?pageSize=12", yanit.Headers.Location?.OriginalString);
    }

    [Fact(DisplayName = "0014/AC-70: /tr/?bodyType=suv&utm_source=x için yönlendirmenin hedefi /tr/cars?bodyType=suv&utm_source=x'tir: sorgu değiştirilmeden taşınır")]
    public async Task Tasir_SorguyuDegistirmeden()
    {
        HttpResponseMessage yanit = await CreateClient().GetAsync("/tr/?bodyType=suv&utm_source=x");

        Assert.Equal("/tr/cars?bodyType=suv&utm_source=x", yanit.Headers.Location?.OriginalString);
    }

    [Fact(DisplayName = "0014/AC-67: Kurulumda yalnız tr ve en etkinken altlıkta /ar/ bağlantısı yoktur (sunucu yarısı: sayfaya yalnız etkin locale'ler basılır)")]
    public async Task Basar_YalnizEtkinLocaleleri()
    {
        // Yapılandırma katmanında bir dizi elemanı silinemiyor (appsettings'teki üçüncü
        // eleman kalıyor); bu yüzden iki dilli kurulum renderer'ın kendi bağımlılığıyla
        // kuruluyor. Boru hattından geçen hâli bir alttaki test ölçüyor.
        IndexHtmlRenderer renderer = new(
            new SabitWebRoot(),
            ThemeOptions.Default,
            new LocalePrefixResolver("tr", ["tr", "en"]));

        DefaultHttpContext context = new();
        using MemoryStream govde = new();
        context.Response.Body = govde;

        await renderer.WriteAsync(context, Locale.Create("tr").Value);
        string html = Encoding.UTF8.GetString(govde.ToArray());

        Assert.Contains("<meta name=\"enabled-locales\" content=\"tr en\" />", html, StringComparison.Ordinal);
    }

    private sealed class SabitWebRoot : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "TestWebRoot");

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ApplicationName { get; set; } = "Host.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; } = "Test";
    }

    [Fact]
    public async Task Basar_UcEtkinLocale_VarsayilanKurulumda()
    {
        string html = await CreateClient().GetStringAsync("/en/");

        Assert.Contains("<meta name=\"enabled-locales\" content=\"tr ar en\" />", html, StringComparison.Ordinal);
    }
}
