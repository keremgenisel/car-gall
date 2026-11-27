using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AngleSharp.Dom;
using CarGallery.Host.Localization;
using CarGallery.Host.Seo;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0028` **D2** · **D12** · **D13** — iletişim sayfasının BELGESİ: gerçek HTTP boru hattı, modüllerin
/// okumaları sahte (<see cref="SeoOrtami"/>). Ziyaretçi okumasının kendisi (adresler, anahtar, izin
/// listesi) gerçek zincirde ölçülür (`IletisimZinciriTests`).
/// </summary>
public sealed class IletisimBelgeTests(HostFactory factory) : IClassFixture<HostFactory>, IDisposable
{
    private const string K = SeoOrtami.Kok;

    private const string KadikoyLink = "https://www.google.com/maps/search/?api=1&query=40.98765%2C29.0275";

    private const string BostanciLink = "https://www.google.com/maps/search/?api=1&query=Ba%C4%9Fdat%20Cad.%20No%3A5";

    private readonly SeoOrtami ortam = Ortam();

    public void Dispose() => ortam.Dispose();

    /// <summary>
    /// Spec'in örnek şubeleri: programlı, koordinatlı, e-postalı Kadıköy (anahtarlı: gömme adresi de var);
    /// programsız, koordinatsız, e-postasız Bostancı.
    /// </summary>
    private static SeoOrtami Ortam()
    {
        SeoOrtami ortam = new();
        ortam.Galeri = new PublicGalleryDto(
            "Oto Galeri",
            [Kadikoy(), Bostanci()],
            [new PublicSocialLinkDto("instagram", "https://www.instagram.com/otogaleri")]);
        ortam.Koordinatlar.AddRange([new PublicGeoDto(40.98765m, 29.0275m), null]);
        return ortam;
    }

    private static PublicBranchDto Kadikoy(string adres = "Bağdat Cad. 1, İstanbul") => new(
        "Kadıköy Şubesi",
        adres,
        "+902160000001",
        CalismaSaatleriBelgeTests.Program(("monday", [("09:00", "18:00")]), ("saturday", [("10:00", "14:00")])),
        new PublicOpenNowDto(true, "2026-12-07", new PublicNextChangeDto("2026-12-07", "18:00"), 3600),
        "kadikoy@otogaleri.com.tr",
        new PublicBranchMapDto(KadikoyLink) { EmbedUrl = "https://www.google.com/maps/embed/v1/place?key=test-anahtar&q=40.98765%2C29.0275&language=tr" })
    {
        SpecialHours = [new PublicSpecialDayDto("2026-12-31", "2026-12-31", [])],
    };

    private static PublicBranchDto Bostanci() => new(
        "Bostancı Şubesi",
        "Bağdat Cad. No:5",
        "+902160000002",
        Map: new PublicBranchMapDto(BostanciLink));

    private Task<(HttpResponseMessage Yanit, IDocument Belge, string Html)> GetAsync(string adres) =>
        SeoOrtami.GetAsync(ortam.Istemci(factory), adres);

    private async Task<JsonElement> DealerAsync(string adres = "/tr/contact") =>
        Assert.Single((await GetAsync(adres)).Belge.JsonLd());

    private async Task<JsonElement> DepartmentAsync(string ad, string adres = "/tr/contact") =>
        (await DealerAsync(adres)).GetProperty("department").EnumerateArray().Single(oge => oge.GetProperty("name").GetString() == ad);

    // ── Adres ve belge (R1, R8 · D2, D12) ──────────────────────────────

    [Theory(DisplayName = "0028/AC-1, AC-9: `/{l}/contact` 200 döner; `Cache-Control: no-store`, `Referrer-Policy` yok, `robots` yok")]
    [InlineData("/tr/contact")]
    [InlineData("/ar/contact")]
    [InlineData("/en/contact")]
    public async Task Belge_200(string adres)
    {
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync(adres);

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal("no-store", yanit.Headers.CacheControl?.ToString());
        Assert.False(yanit.Headers.Contains("Referrer-Policy"));
        Assert.Null(belge.Meta("robots"));
    }

    [Theory(DisplayName = "0028/AC-2: `/tr/contact/x` ve `/tr/contact/kadikoy` 404 ve `noindex`")]
    [InlineData("/tr/contact/x")]
    [InlineData("/tr/contact/kadikoy")]
    public async Task AltYol_404(string adres)
    {
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync(adres);

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.Equal("noindex", belge.Meta("robots"));
    }

    [Fact(DisplayName = "0028/AC-4: `<title>` = `{seo.contact.title} · {görünen ad}`; ad okunamazsa yalnız katalog başlığı")]
    public async Task Baslik()
    {
        Assert.Equal($"{Katalog("tr", "seo.contact.title")} · Oto Galeri", (await GetAsync("/tr/contact")).Belge.Baslik());

        ortam.AdOkunamaz = true;
        Assert.Equal(Katalog("tr", "seo.contact.title"), (await GetAsync("/tr/contact")).Belge.Baslik());
    }

    [Fact(DisplayName = "0028/AC-5, AC-8: ar açıklaması katalogdan; `og:title`/`og:description`/`og:url` başlık, açıklama ve kanonikle aynı, `og:type website`")]
    public async Task Aciklama_VeOpenGraph()
    {
        IDocument belge = (await GetAsync("/ar/contact")).Belge;

        Assert.Equal(Katalog("ar", "seo.contact.description"), belge.Meta("description"));
        Assert.Equal(belge.Baslik(), belge.Og("og:title"));
        Assert.Equal(belge.Meta("description"), belge.Og("og:description"));
        Assert.Equal(Assert.Single(belge.Kanonikler()), belge.Og("og:url"));
        Assert.Equal("website", belge.Og("og:type"));
    }

    [Fact(DisplayName = "0028/AC-6, AC-7: Kanonik sorgu dizesi taşımaz; etkin her dil için `hreflang` ve `x-default` = tr")]
    public async Task Kanonik_VeAlternatifler()
    {
        IDocument belge = (await GetAsync("/tr/contact?utm_source=x")).Belge;

        Assert.Equal([$"{K}/tr/contact"], belge.Kanonikler());
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["tr"] = $"{K}/tr/contact",
                ["ar"] = $"{K}/ar/contact",
                ["en"] = $"{K}/en/contact",
                ["x-default"] = $"{K}/tr/contact",
            },
            belge.Alternatifler());
    }

    [Fact(DisplayName = "0028/AC-7 (iki dilli kurulum): Etkin diller `tr, en` iken `ar` alternatifi yoktur")]
    public async Task Alternatif_Yok_EtkinOlmayanDil()
    {
        SeoHead head = await KurAsync(new LocalePrefixResolver("tr", ["tr", "en"]), "/tr/contact", "tr", ortam.Galeri!);

        Assert.Contains("hreflang=\"en\"", head.Tags, StringComparison.Ordinal);
        Assert.DoesNotContain("hreflang=\"ar\"", head.Tags, StringComparison.Ordinal);
        Assert.Contains($"hreflang=\"x-default\" href=\"{K}/tr/contact\"", head.Tags, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0028/AC-10, 0004/AC-15: İletişim belgesinin `<title>`'ında ve `og:site_name`'inde `CarGallery` geçmez (ad okunamazken de)")]
    public async Task YazilimAdiYok()
    {
        foreach (bool okunamaz in new[] { false, true })
        {
            ortam.AdOkunamaz = okunamaz;
            IDocument belge = (await GetAsync("/tr/contact")).Belge;

            Assert.DoesNotContain("CarGallery", belge.Baslik(), StringComparison.Ordinal);
            Assert.DoesNotContain("CarGallery", belge.Og("og:site_name") ?? string.Empty, StringComparison.Ordinal);
        }
    }

    [Theory(DisplayName = "0028/AC-11: Üç dilin sitemap'i `{kök}/{l}/contact`'ı tam bir kez taşır")]
    [InlineData("tr")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task Sitemap(string dil)
    {
        Assert.Single(await SitemapAsync(dil), adres => adres == $"{K}/{dil}/contact");
    }

    [Fact(DisplayName = "0028/AC-11 (robots): `robots.txt` yalnız `/api/`'yi kapatır, iletişim sayfasına dokunmaz")]
    public async Task Robots_Degismez()
    {
        string robots = await ortam.Istemci(factory).GetStringAsync("/robots.txt");

        Assert.DoesNotContain("contact", robots, StringComparison.Ordinal);
    }

    // ── Yapısal veri (R9 · C3 · D13) ───────────────────────────────────

    [Fact(DisplayName = "0028/AC-12, AC-19: Tam bir JSON-LD bloğu: `AutoDealer`, tr ad, `url` = `{kök}/tr/`; `sameAs` yok (hesaplı kurulumda da)")]
    public async Task Kok()
    {
        IDocument belge = (await GetAsync("/tr/contact")).Belge;
        JsonElement dealer = Assert.Single(belge.JsonLd());

        Assert.Single(belge.QuerySelectorAll("script[type='application/ld+json']"));
        Assert.Equal("AutoDealer", dealer.GetProperty("@type").GetString());
        Assert.Equal("Oto Galeri", dealer.GetProperty("name").GetString());
        Assert.Equal($"{K}/tr/", dealer.GetProperty("url").GetString());
        Assert.DoesNotContain("sameAs", dealer.Anahtarlar());
        Assert.Contains("sameAs", (await DealerAsync("/tr/")).Anahtarlar());
    }

    [Fact(DisplayName = "0028/AC-13, AC-14, AC-15: Kadıköy'ün öğesi ad, adres, telefon, `email`, `hasMap` = `linkUrl`, saatler, özel günler ve `geo` (JSON sayısı) taşır")]
    public async Task Kadikoy_Ogesi()
    {
        JsonElement oge = await DepartmentAsync("Kadıköy Şubesi");

        Assert.Equal(
            ["@type", "address", "email", "geo", "hasMap", "name", "openingHoursSpecification", "specialOpeningHoursSpecification", "telephone"],
            oge.Anahtarlar().Order(StringComparer.Ordinal));
        Assert.Equal("AutoDealer", oge.GetProperty("@type").GetString());
        Assert.Equal("Bağdat Cad. 1, İstanbul", oge.GetProperty("address").GetString());
        Assert.Equal("+902160000001", oge.GetProperty("telephone").GetString());
        Assert.Equal("kadikoy@otogaleri.com.tr", oge.GetProperty("email").GetString());
        Assert.Equal(KadikoyLink, oge.GetProperty("hasMap").GetString());
        Assert.Equal(
            """[{"@type":"OpeningHoursSpecification","validFrom":"2026-12-31","validThrough":"2026-12-31","opens":"00:00","closes":"00:00"}]""",
            oge.GetProperty("specialOpeningHoursSpecification").GetRawText());
        Assert.Equal("""{"@type":"GeoCoordinates","latitude":40.98765,"longitude":29.0275}""", oge.GetProperty("geo").GetRawText());
        Assert.Equal(JsonValueKind.Number, oge.GetProperty("geo").GetProperty("latitude").ValueKind);
    }

    [Fact(DisplayName = "0028/AC-16: Bostancı (programsız, koordinatsız, e-postasız) `geo`, `email` ve saat anahtarları taşımaz, `hasMap` taşır")]
    public async Task Bostanci_Ogesi()
    {
        JsonElement oge = await DepartmentAsync("Bostancı Şubesi");

        Assert.Equal(["@type", "address", "hasMap", "name", "telephone"], oge.Anahtarlar().Order(StringComparer.Ordinal));
        Assert.Equal(BostanciLink, oge.GetProperty("hasMap").GetString());
    }

    [Fact(DisplayName = "0028/AC-17, 0004/AC-52: `/tr/` belgesinin her `department` öğesi tam `@type, address, name, telephone` (programlı, özel günlü, e-postalı Kadıköy'de de); `geo` yok")]
    public async Task AnaSayfa_DortAnahtar()
    {
        JsonElement dealer = await DealerAsync("/tr/");

        Assert.All(
            dealer.GetProperty("department").EnumerateArray(),
            oge => Assert.Equal(["@type", "address", "name", "telephone"], oge.Anahtarlar().Order(StringComparer.Ordinal)));
    }

    [Fact(DisplayName = "0028/AC-18 (CLARIFY B3): 300 karakterlik ASCII dışı adres ve 254 B e-posta `/ar/contact`'ı `A` adresli e-postasız hâlinden en çok 2816 B büyütür")]
    public async Task Butce_UzunAdres()
    {
        string adres = new('ğ', 300);
        string eposta = new string('e', 254 - "@galeri.com".Length) + "@galeri.com";
        string link = "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(adres);

        PublicGalleryDto Tek(string a, string? e, string l) => new(
            new string('ع', 120),
            [new PublicBranchDto(new string('ف', 120), a, "+96550000001", Email: e, Map: new PublicBranchMapDto(l))],
            []);

        LocalePrefixResolver diller = new("tr", ["tr", "ar", "en"]);
        int kisa = Encoding.UTF8.GetByteCount((await KurAsync(diller, "/ar/contact", "ar", Tek("A", null, "https://www.google.com/maps/search/?api=1&query=A"))).Tags);
        int uzun = Encoding.UTF8.GetByteCount((await KurAsync(diller, "/ar/contact", "ar", Tek(adres, eposta, link))).Tags);

        Assert.InRange(uzun - kisa, 2_400, 2_816);
    }

    [Fact(DisplayName = "0028/AC-20: Belge \"şu an\" metnini, `openNow` değerini, `embedUrl`'i ve `test-anahtar`'ı taşımaz")]
    public async Task SuAn_VeAnahtarYok()
    {
        string html = (await GetAsync("/tr/contact")).Html;

        foreach (string yasak in new[] { "Şu an", "openNow", "refreshInSeconds", "maps/embed", "embedUrl", "test-anahtar" })
        {
            Assert.DoesNotContain(yasak, html, StringComparison.Ordinal);
        }
    }

    [Fact(DisplayName = "0028/AC-92 (belge yarısı): `map`'i null olan şubenin öğesi `hasMap` taşımaz, adres metin olarak vardır")]
    public async Task HaritasizSube()
    {
        ortam.Galeri = ortam.Galeri! with { Branches = [Bostanci() with { Map = null }] };

        JsonElement oge = await DepartmentAsync("Bostancı Şubesi");

        Assert.DoesNotContain("hasMap", oge.Anahtarlar());
        Assert.Equal("Bağdat Cad. No:5", oge.GetProperty("address").GetString());
    }

    // ── Ayrılmış ad ve sitemap (CLARIFY B4) ────────────────────────────

    [Fact(DisplayName = "0028/AC-93: en slug'ı `contact` olan görünür bir içerik sayfası varken `sitemap-en.xml`'de `{kök}/en/contact` tam bir kez; belgenin başlığı `seo.contact.title`")]
    public async Task AyrilmisAdliIcerik_Contact()
    {
        ortam.Sayfalar[("en", "contact")] = SeoOrtami.Sayfa("Gölge Sayfa", "x");

        Assert.Single(await SitemapAsync("en"), adres => adres == $"{K}/en/contact");
        Assert.Equal($"{Katalog("en", "seo.contact.title")} · Auto Gallery", (await GetAsync("/en/contact")).Belge.Baslik());
    }

    [Fact(DisplayName = "0028/AC-94 (önceden var olan kusur): tr slug'ı `favorites` ve `compare` olan görünür içerik sayfaları `sitemap-tr.xml`'de yok; ayrılmamış slug'lı sayfa var")]
    public async Task AyrilmisAdliIcerik_FavoriKarsilastirma()
    {
        ortam.Sayfalar[("tr", "favorites")] = SeoOrtami.Sayfa("Gölge 1", "x");
        ortam.Sayfalar[("tr", "compare")] = SeoOrtami.Sayfa("Gölge 2", "x");
        ortam.Sayfalar[("tr", "kvkk")] = SeoOrtami.Sayfa("KVKK", "x");

        IReadOnlyList<string> adresler = await SitemapAsync("tr");

        Assert.DoesNotContain($"{K}/tr/favorites", adresler);
        Assert.DoesNotContain($"{K}/tr/compare", adresler);
        Assert.Contains($"{K}/tr/kvkk", adresler);
    }

    // ── İkizler (CLARIFY B10) ──────────────────────────────────────────

    [Fact(DisplayName = "0028/AC-99, 0024/AC-49: Adresi `A & B` olan şubenin ham JSON-LD'si `A \\u0026 B` taşır; ayrıştırılan adres kayıtlı adrese eşit")]
    public async Task Ampersand()
    {
        ortam.Galeri = ortam.Galeri! with { Branches = [Kadikoy("A & B")] };
        string html = (await GetAsync("/tr/contact")).Html;

        Assert.Contains("A \\u0026 B", html, StringComparison.Ordinal);
        Assert.Equal("A & B", (await DepartmentAsync("Kadıköy Şubesi")).GetProperty("address").GetString());
    }

    [Fact(DisplayName = "0028/AC-100, 0024/AC-50, 0004/AC-53: Görünen ad okunamazken `/tr/contact` 200 döner ve `AutoDealer` bloğu taşımaz")]
    public async Task AdOkunamazken()
    {
        ortam.AdOkunamaz = true;
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync("/tr/contact");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Empty(belge.JsonLd());
    }

    [Fact(DisplayName = "0028/AC-101, 0026/AC-51: Logolu kurulumda kök `logo` taşır, `department` öğelerinin hiçbiri taşımaz")]
    public async Task Logo()
    {
        Guid logo = Guid.CreateVersion7();
        ortam.LogoKimligi = logo;
        ortam.LogoVarligi = SeoOrtami.LogoDto(logo);

        JsonElement dealer = await DealerAsync();

        Assert.Contains("logo", dealer.Anahtarlar());
        Assert.All(dealer.GetProperty("department").EnumerateArray(), oge => Assert.DoesNotContain("logo", oge.Anahtarlar()));
    }

    [Fact(DisplayName = "0028/AC-102, 0004/AC-46: Adresi `X</script><script>alert(1)</script>` olan şubenin belgesinde tam bir JSON-LD bloğu; içerik geçerli JSON, `address` bu dize")]
    public async Task ScriptKapatilamaz()
    {
        const string adres = "X</script><script>alert(1)</script>";
        ortam.Galeri = ortam.Galeri! with { Branches = [Kadikoy(adres)] };
        IDocument belge = (await GetAsync("/tr/contact")).Belge;

        IElement script = Assert.Single(belge.QuerySelectorAll("script"), oge => oge.GetAttribute("type") == "application/ld+json");
        Assert.Equal(adres, JsonDocument.Parse(script.TextContent).RootElement.GetProperty("department")[0].GetProperty("address").GetString());
    }

    [Theory(DisplayName = "0028/AC-103: `/{l}/` ve `/{l}/contact` belgelerinin kök `@id`'si aynı: `{kök}/{l}/#dealer`")]
    [InlineData("tr")]
    [InlineData("ar")]
    public async Task AyniKimlik(string dil)
    {
        string beklenen = $"{K}/{dil}/#dealer";

        Assert.Equal(beklenen, (await DealerAsync($"/{dil}/")).GetProperty("@id").GetString());
        Assert.Equal(beklenen, (await DealerAsync($"/{dil}/contact")).GetProperty("@id").GetString());
    }

    // ── Yardımcılar ────────────────────────────────────────────────────

    private async Task<IReadOnlyList<string>> SitemapAsync(string dil)
    {
        XDocument belge = XDocument.Parse(await ortam.Istemci(factory).GetStringAsync($"/sitemap-{dil}.xml"));
        XNamespace ns = SeoEndpoints.SitemapNamespace;
        return [.. belge.Root!.Elements(ns + "url").Select(url => Assert.Single(url.Elements()).Value)];
    }

    /// <summary>Bileşimi doğrudan kurar: dil kümesi yapılandırmada daraltılamıyor (`SeoBelgeTests` AC-25'in notu).</summary>
    private static async Task<SeoHead> KurAsync(LocalePrefixResolver diller, string yol, string dil, PublicGalleryDto galeri)
    {
        SeoHeadBuilder seo = new(SiteRoot.Parse(K), SeoCatalog.Load(AppContext.BaseDirectory, diller.Enabled), diller, NullLogger<SeoHeadBuilder>.Instance);
        DefaultHttpContext context = new()
        {
            RequestServices = new ServiceCollection().AddSingleton<IPublicGalleryQueries>(new Sabit(galeri)).BuildServiceProvider(),
        };
        context.Request.Path = yol;

        return await seo.BuildAsync(context, CarGallery.BuildingBlocks.Locale.Create(dil).Value, galeri.DisplayName, null);
    }

    private sealed class Sabit(PublicGalleryDto galeri) : IPublicGalleryQueries
    {
        public Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken) => Task.FromResult<PublicGalleryDto?>(galeri);
    }

    private static string Katalog(string dil, string anahtar)
    {
        DirectoryInfo dizin = new(AppContext.BaseDirectory);

        while (!Directory.Exists(Path.Combine(dizin.FullName, "frontend")))
        {
            dizin = dizin.Parent!;
        }

        using JsonDocument katalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(dizin.FullName, "frontend", "src", "shared", "i18n", "locales", "seo", $"{dil}.json")));
        return katalog.RootElement.GetProperty(anahtar).GetString()!;
    }
}
