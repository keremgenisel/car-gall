using System.Net;
using System.Xml.Linq;
using CarGallery.Host.Seo;
using CarGallery.Modules.Content.Contracts.Pages;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0004` **R7** · **R8** — sitemap ve robots.txt'in yapısı. Sitemap iddiaları YAPISALDIR (D22 b):
/// belge `System.Xml` ile iyi biçimli ayrıştırılır; kök öğenin adı ve ad alanı, yalnız izinli alt
/// öğeler. Okumalar sahte; yayından kaldırma, satış ve açılış öncesi ilan gerçek zincirde
/// (<see cref="SitemapZinciriTests"/>).
/// </summary>
public sealed class SitemapTests(HostFactory factory) : IClassFixture<HostFactory>, IDisposable
{
    public void Dispose() => ortam.Dispose();

    private const string K = SeoOrtami.Kok;

    private static readonly XNamespace Ns = SeoEndpoints.SitemapNamespace;

    private readonly SeoOrtami ortam = Ortam();

    private static SeoOrtami Ortam()
    {
        SeoOrtami ortam = new();
        ortam.Ilanlar[("audi-a3-2020-aaa111", "*")] = SeoOrtami.Ilan("audi-a3-2020-aaa111", reserved: true);
        ortam.Ilanlar[("toyota-corolla-2019-bbb222", "*")] = SeoOrtami.Ilan("toyota-corolla-2019-bbb222");
        ortam.Sayfalar[("tr", "kvkk")] = SeoOrtami.Sayfa("KVKK", "x", new ContentPageAlternateDto("en", "privacy"));
        ortam.Sayfalar[("en", "privacy")] = SeoOrtami.Sayfa("Privacy", "x", new ContentPageAlternateDto("tr", "kvkk"));
        return ortam;
    }

    private async Task<(HttpResponseMessage Yanit, XDocument? Belge)> GetAsync(string adres)
    {
        HttpResponseMessage yanit = await ortam.Istemci(factory).GetAsync(adres);
        string metin = await yanit.Content.ReadAsStringAsync();

        return (yanit, yanit.IsSuccessStatusCode ? XDocument.Parse(metin) : null);
    }

    private static IReadOnlyList<string> Adresler(XDocument belge) =>
        [.. belge.Root!.Elements(Ns + "url").Select(url => Assert.Single(url.Elements()).Value)];

    [Fact(DisplayName = "0004/AC-55: `/sitemap.xml` 200 döner, `Content-Type`'ı XML'dir ve iyi biçimli XML'dir. Kök öğesi `http://www.sitemaps.org/schemas/sitemap/0.9` ad alanında `sitemapindex`'tir, alt öğeleri yalnız `<sitemap><loc>`'tur. Etkin her dil için tam olarak bir `{kök}/sitemap-{dil}.xml` girdisi taşır.")]
    public async Task Dizin_Yapisi()
    {
        (HttpResponseMessage yanit, XDocument? belge) = await GetAsync("/sitemap.xml");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal("application/xml", yanit.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Ns + "sitemapindex", belge!.Root!.Name);
        Assert.All(belge.Root.Elements(), sitemap =>
        {
            Assert.Equal(Ns + "sitemap", sitemap.Name);
            Assert.Equal(Ns + "loc", Assert.Single(sitemap.Elements()).Name);
        });
        Assert.Equal(
            [$"{K}/sitemap-tr.xml", $"{K}/sitemap-ar.xml", $"{K}/sitemap-en.xml"],
            belge.Root.Elements().Select(sitemap => sitemap.Element(Ns + "loc")!.Value));
    }

    [Fact(DisplayName = "0004/AC-56, 0028/AC-11: `/sitemap-tr.xml` iyi biçimli XML'dir. Kök öğesi aynı ad alanında `urlset`'tir, alt öğeleri yalnız `<url><loc>`'tur. Adres kümesi tam olarak şudur: `{kök}/tr/`, `{kök}/tr/cars`, `{kök}/tr/contact`, görünür her ilanın `{kök}/tr/cars/{slug}`'ı ve tr'de görünen (`FindBySlugAsync`'in döndüreceği) her içerik sayfasının `{kök}/tr/{slug}`'ı.")]
    public async Task DilSitemap_Yapisi()
    {
        (_, XDocument? belge) = await GetAsync("/sitemap-tr.xml");

        Assert.Equal(Ns + "urlset", belge!.Root!.Name);
        Assert.All(belge.Root.Elements(), url =>
        {
            Assert.Equal(Ns + "url", url.Name);
            Assert.Equal(Ns + "loc", Assert.Single(url.Elements()).Name);
        });
        Assert.Equal(
            // `0028` D12 — iletişim sayfası bilerek eklendi (0028 Context tablosu).
            new HashSet<string>([$"{K}/tr/", $"{K}/tr/cars", $"{K}/tr/contact", $"{K}/tr/cars/audi-a3-2020-aaa111", $"{K}/tr/cars/toyota-corolla-2019-bbb222", $"{K}/tr/kvkk"]),
            new HashSet<string>(Adresler(belge)));
    }

    [Fact(DisplayName = "0004/AC-61: ar'da yayında olmayan bir içerik sayfası `/sitemap-ar.xml`'de yoktur. tr'de yayındaysa `/sitemap-tr.xml`'de tr slug'ıyla vardır.")]
    public async Task IcerikSayfasi_DilBasina()
    {
        Assert.DoesNotContain(Adresler((await GetAsync("/sitemap-ar.xml")).Belge!), adres => adres.EndsWith("/kvkk", StringComparison.Ordinal) || adres.EndsWith("/privacy", StringComparison.Ordinal));
        Assert.Contains($"{K}/tr/kvkk", Adresler((await GetAsync("/sitemap-tr.xml")).Belge!));
        Assert.Contains($"{K}/en/privacy", Adresler((await GetAsync("/sitemap-en.xml")).Belge!));
    }

    [Fact(DisplayName = "0004/AC-62: Hiçbir sitemap girdisi `/admin`, `/newsletter/` ya da `?` içermez.")]
    public async Task Girdiler_PanelBultenSorguYok()
    {
        foreach (string dil in new[] { "tr", "ar", "en" })
        {
            Assert.All(Adresler((await GetAsync($"/sitemap-{dil}.xml")).Belge!), adres =>
            {
                Assert.DoesNotContain("/admin", adres, StringComparison.Ordinal);
                Assert.DoesNotContain("/newsletter/", adres, StringComparison.Ordinal);
                Assert.DoesNotContain("?", adres, StringComparison.Ordinal);
            });
        }
    }

    [Fact(DisplayName = "0004/AC-63: Etkin olmayan bir dilin sitemap'i (`/sitemap-de.xml`) 404 döner.")]
    public async Task EtkinOlmayanDil_404() =>
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync("/sitemap-de.xml")).Yanit.StatusCode);

    [Fact(DisplayName = "0004/AC-64: Hiçbir sitemap girdisi `lastmod` öğesi taşımaz.")]
    public async Task Lastmod_Yok()
    {
        foreach (string adres in new[] { "/sitemap.xml", "/sitemap-tr.xml", "/sitemap-ar.xml", "/sitemap-en.xml" })
        {
            Assert.DoesNotContain((await GetAsync(adres)).Belge!.Descendants(), oge => oge.Name.LocalName == "lastmod");
        }
    }

    [Fact(DisplayName = "0004/AC-89: tr'de yayına alınmış ama hiç girdisi olmayan bir SSS sayfası `/sitemap-tr.xml`'de yoktur.")]
    public async Task GirdisizSss_Yok()
    {
        // Sitemap'in içerik adresleri `ListVisibleSlugsAsync`'ten: girdisiz SSS orada yok
        // (`Content.Tests/SitemapSayfalariTests`, gerçek veritabanı). Burada Host'un o listeyi
        // olduğu gibi kullandığı: listede olmayan `sss` sitemap'te de yok.
        Assert.DoesNotContain($"{K}/tr/sss", Adresler((await GetAsync("/sitemap-tr.xml")).Belge!));
    }

    [Fact(DisplayName = "0004/AC-65: `/robots.txt` 200 döner, `Content-Type`'ı `text/plain`'dir ve `Sitemap: {kök}/sitemap.xml` satırını taşır.")]
    public async Task Robots_Yapisi()
    {
        HttpResponseMessage yanit = await ortam.Istemci(factory).GetAsync("/robots.txt");
        string[] satirlar = (await yanit.Content.ReadAsStringAsync()).Split('\n');

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal("text/plain", yanit.Content.Headers.ContentType?.MediaType);
        Assert.Contains($"Sitemap: {K}/sitemap.xml", satirlar);
    }

    [Fact(DisplayName = "0004/AC-66: robots.txt `/api/` için `Disallow` taşır. `/admin` ya da `/newsletter` için `Disallow` taşımaz. Başka bir yol kapatılmaz.")]
    public async Task Robots_YalnizApiKapali()
    {
        string metin = await ortam.Istemci(factory).GetStringAsync("/robots.txt");

        Assert.Equal(["Disallow: /api/"], metin.Split('\n').Where(satir => satir.StartsWith("Disallow", StringComparison.Ordinal)));
    }
}
