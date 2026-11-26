using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AngleSharp.Dom;
using CarGallery.Host.Seo;
using CarGallery.Modules.Content.Contracts.Pages;

namespace CarGallery.Host.Tests;

/// <summary>
/// `frontend-003` **D1** · **D2** · **D3** — favoriler ve karşılaştırma ekranlarının belgesi: iki
/// ayrılmış ad, iki sayfa türü, `noindex`, `no-referrer`, sitemap dışı. Okumalar sahte (<see cref="SeoOrtami"/>);
/// ekranın kendisi uçta (`frontend/e2e/favoriler.spec.ts`).
/// </summary>
public sealed partial class FavoriKarsilastirmaBelgeTests(HostFactory factory) : IClassFixture<HostFactory>, IDisposable
{
    public void Dispose() => ortam.Dispose();

    private readonly SeoOrtami ortam = new();

    private Task<(HttpResponseMessage Yanit, IDocument Belge, string Html)> GetAsync(string adres) =>
        SeoOrtami.GetAsync(ortam.Istemci(factory), adres);

    public static TheoryData<string> IkiSayfa() => new()
    {
        "/tr/favorites", "/ar/favorites", "/en/favorites",
        "/tr/compare?listing=a-2020-aaaaaa&listing=b-2021-bbbbbb",
    };

    [Theory(DisplayName = "frontend-003/AC-1: `/tr|ar|en/favorites` ve iki görünmeyen slug taşıyan `/tr/compare?listing=…` 200 döner ve kabuğu basar")]
    [MemberData(nameof(IkiSayfa))]
    public async Task Doner200_IkiSayfa(string adres)
    {
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync(adres);

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.NotNull(belge.QuerySelector("#root"));
    }

    [Theory(DisplayName = "frontend-003/AC-2: `/tr/favorites/x`, `/tr/compare/x` ve `/tr/compare/x/y` 404 döner ve `noindex` taşır")]
    [InlineData("/tr/favorites/x")]
    [InlineData("/tr/compare/x")]
    [InlineData("/tr/compare/x/y")]
    [InlineData("/ar/favorites/x")]
    public async Task Doner404_AltYollar(string adres)
    {
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync(adres);

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.Equal("noindex", belge.Meta("robots"));
    }

    [Theory(DisplayName = "frontend-003/AC-4, AC-6: İki sayfanın belgesi `noindex` taşır; kanonik, `hreflang`, `og:` ve JSON-LD taşımaz; yanıt `Referrer-Policy: no-referrer`")]
    [MemberData(nameof(IkiSayfa))]
    public async Task Noindex_KanoniksizVeReferrersiz(string adres)
    {
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync(adres);

        Assert.Equal("noindex", Assert.Single(belge.QuerySelectorAll("meta[name='robots']")).GetAttribute("content"));
        Assert.Empty(belge.Kanonikler());
        Assert.Empty(belge.Alternatifler());
        Assert.Empty(belge.QuerySelectorAll("meta[property^='og:']"));
        Assert.Empty(belge.JsonLd());
        Assert.Null(belge.Meta("description"));
        Assert.Equal("no-referrer", Assert.Single(yanit.Headers.GetValues("Referrer-Policy")));
    }

    [Theory(DisplayName = "frontend-003/AC-5: İki sayfanın `<title>`'ı SEO kümesindeki başlık · galeri adıdır, üç dilde çevrilidir")]
    [InlineData("tr", "favorites", "seo.favorites.title")]
    [InlineData("ar", "favorites", "seo.favorites.title")]
    [InlineData("en", "favorites", "seo.favorites.title")]
    [InlineData("tr", "compare", "seo.compare.title")]
    [InlineData("ar", "compare", "seo.compare.title")]
    [InlineData("en", "compare", "seo.compare.title")]
    public async Task Baslik_SeoKumesindenVeAddan(string dil, string bolum, string anahtar)
    {
        string baslik = (await GetAsync($"/{dil}/{bolum}")).Belge.Baslik();

        // Galeri adı dilin görünen adıdır (sahte ortamda dile göre değişir); başlık `0004` D11 biçimi.
        Assert.StartsWith($"{SeoKatalogu(dil, anahtar)} · ", baslik, StringComparison.Ordinal);
        Assert.True(baslik.Length > $"{SeoKatalogu(dil, anahtar)} · ".Length, baslik);

        if (dil != "tr")
        {
            Assert.NotEqual(SeoKatalogu("tr", anahtar), SeoKatalogu(dil, anahtar));
        }
    }

    [Fact(DisplayName = "frontend-003/AC-7: Üç dilin sitemap'i favoriler ve karşılaştırma adresini içermez; robots.txt bu adları anmaz ve yalnız `/api/`'yi kapatır")]
    public async Task SitemapDisi_RobotsDegismez()
    {
        HttpClient istemci = ortam.Istemci(factory);

        foreach (string dil in new[] { "tr", "ar", "en" })
        {
            XDocument belge = XDocument.Parse(await istemci.GetStringAsync($"/sitemap-{dil}.xml"));
            List<string> adresler = [.. belge.Descendants().Where(e => e.Name.LocalName == "loc").Select(e => e.Value)];

            Assert.NotEmpty(adresler);
            Assert.DoesNotContain(adresler, adres => adres.Contains("/favorites", StringComparison.Ordinal) || adres.Contains("/compare", StringComparison.Ordinal));
        }

        string robots = await istemci.GetStringAsync("/robots.txt");
        Assert.DoesNotContain("favorites", robots, StringComparison.Ordinal);
        Assert.DoesNotContain("compare", robots, StringComparison.Ordinal);
        Assert.Equal(["/api/"], [.. robots.Split('\n').Where(satir => satir.StartsWith("Disallow:", StringComparison.Ordinal)).Select(satir => satir["Disallow:".Length..].Trim())]);
    }

    [Fact(DisplayName = "frontend-003/AC-11: Liste belgesinin `noindex, follow` kuralı değişmez: `listing` liste parametresi değildir ve `/tr/cars?listing=x` belgesi kanonik taşır, robots etiketi taşımaz")]
    public async Task ListeKurali_Degismez()
    {
        IDocument belge = (await GetAsync("/tr/cars?listing=x")).Belge;

        Assert.Equal($"{SeoOrtami.Kok}/tr/cars", Assert.Single(belge.Kanonikler()));
        Assert.Empty(belge.QuerySelectorAll("meta[name='robots']"));

        using JsonDocument parametreler = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(CozumKoku(), "frontend", "src", "features", "catalog", "listQueryParams.json")));
        Assert.DoesNotContain("listing", parametreler.RootElement.ToString(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "frontend-003/AC-3 (ayna): Uçtaki `RESERVED_SEGMENTS` (route.ts) ile `ContentReservedSlugs.All` aynı kümedir ve ikisi de `favorites` ile `compare`'i taşır")]
    public void AynaListe_AyniKume()
    {
        string route = File.ReadAllText(Path.Combine(CozumKoku(), "frontend", "src", "shared", "lib", "route.ts"));
        string govde = AynaListe().Match(route).Groups["govde"].Value;
        Dictionary<string, string> sabitler = SabitTanimi().Matches(route)
            .ToDictionary(eslesme => eslesme.Groups["ad"].Value, eslesme => eslesme.Groups["deger"].Value);

        List<string> uc = [.. govde.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(oge => oge.StartsWith('\'') ? oge.Trim('\'') : sabitler[oge])];

        Assert.NotEmpty(uc);
        Assert.Equal(ContentReservedSlugs.All.Order(StringComparer.Ordinal), uc.Order(StringComparer.Ordinal));
        Assert.Contains("favorites", uc);
        Assert.Contains("compare", uc);
    }

    [GeneratedRegex(@"const RESERVED_SEGMENTS[^=]*=\s*new Set\(\[(?<govde>[^\]]*)\]\)", RegexOptions.Singleline)]
    private static partial Regex AynaListe();

    [GeneratedRegex(@"export const (?<ad>[A-Z_]+) = '(?<deger>[a-z-]+)'")]
    private static partial Regex SabitTanimi();

    private static string SeoKatalogu(string dil, string anahtar)
    {
        using JsonDocument katalog = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(CozumKoku(), "frontend", "src", "shared", "i18n", "locales", "seo", $"{dil}.json")));
        return katalog.RootElement.GetProperty(anahtar).GetString()!;
    }

    private static string CozumKoku()
    {
        DirectoryInfo? dizin = new(AppContext.BaseDirectory);

        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "CarGallery.slnx")))
        {
            dizin = dizin.Parent;
        }

        return dizin?.FullName ?? throw new InvalidOperationException("CarGallery.slnx bulunamadı.");
    }
}
