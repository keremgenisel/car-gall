using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0015` D17 — yakıt ve vites eksenlerinin değerleri MEVCUT public uçlardan gelir
/// (`0010a`). Bu test o uçların ziyaretçi için gerçekten açık olduğunu telden ölçer.
/// </summary>
public sealed class VitrinEksenDegerleriTests(HostFactory factory) : IClassFixture<HostFactory>
{
    [Theory(DisplayName = "0015/AC-9: fuel-types ve transmission-types oturumsuz 200 döner ve türün tüm değerlerini etkin locale'in adıyla verir; değerin yanında sayı yoktur")]
    [InlineData("fuel-types", "tr", 4, "diesel", "Dizel")]
    [InlineData("fuel-types", "ar", 4, "diesel", "ديزل")]
    [InlineData("transmission-types", "en", 2, "manual", null)]
    public async Task Doner_TumDegerler_Oturumsuz(string uc, string locale, int adet, string slug, string? beklenenAd)
    {
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        HttpResponseMessage yanit = await client.GetAsync($"/api/reference/{uc}?locale={locale}");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        using JsonDocument belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
        Assert.Equal(adet, belge.RootElement.GetArrayLength());

        JsonElement deger = belge.RootElement.EnumerateArray().Single(item => item.GetProperty("slug").GetString() == slug);
        if (beklenenAd is not null)
        {
            Assert.Equal(beklenenAd, deger.GetProperty("label").GetString());
        }

        // X11 — sonuç sayısı (facet) taşınmaz.
        Assert.Equal(["kind", "label", "slug"], deger.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
    }
}
