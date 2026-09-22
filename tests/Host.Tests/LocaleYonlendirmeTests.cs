using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

public sealed class LocaleYonlendirmeTests : IClassFixture<HostFactory>
{
    private readonly HostFactory factory;

    public LocaleYonlendirmeTests(HostFactory factory) => this.factory = factory;

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    [Fact(DisplayName = "0001/AC-33: Öneksiz / isteği kurulum varsayılan locale'ine 301 ile yönlendirilir")]
    public async Task Yonlendirir301_OneksizIstekte()
    {
        HttpResponseMessage response = await CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/tr/", response.Headers.Location?.ToString());
    }

    [Fact(DisplayName = "0001/AC-33: Etkin olmayan bir locale öneki (/de/) 404 döner ve varsayılan locale'e yönlendirilmez")]
    public async Task Doner404_EtkinOlmayanLocaleOnekinde()
    {
        HttpResponseMessage response = await CreateClient().GetAsync("/de/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("tr", "ltr")]
    [InlineData("en", "ltr")]
    [InlineData("ar", "rtl")]
    public async Task YonSunucudanBasilir_EtkinLocaleOnekinde(string locale, string beklenenYon)
    {
        HttpResponseMessage response = await CreateClient().GetAsync($"/{locale}/");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"lang=\"{locale}\"", html, StringComparison.Ordinal);
        Assert.Contains($"dir=\"{beklenenYon}\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YonlendirilmezVeDokunulmaz_ApiRotalari()
    {
        HttpResponseMessage response = await CreateClient().GetAsync("/api/catalog/does-not-exist");

        Assert.NotEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
    }

    [Fact]
    public async Task AcceptLanguageYonlendirmeyiEtkilemez()
    {
        using HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("Accept-Language", "ar-KW,ar;q=0.9");

        HttpResponseMessage response = await client.GetAsync("/");

        // Yönlendirme yapılandırmadan gelir, Accept-Language'dan değil (Yasak Listesi 28).
        Assert.Equal("/tr/", response.Headers.Location?.ToString());
    }
}
