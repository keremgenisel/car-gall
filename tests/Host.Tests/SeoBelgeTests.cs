using System.Net;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CarGallery.Host.Localization;
using CarGallery.Host.Seo;
using CarGallery.Modules.Content.Contracts.Home;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0004` — belge etiketleri ve durum kodu, sunucunun yanıtının kendisi ayrıştırılarak (C13, D22).
/// Sitemap ve robots.txt <see cref="SitemapTests"/>'te.
/// </summary>
public sealed class SeoBelgeTests(HostFactory factory) : IClassFixture<HostFactory>, IDisposable
{
    public void Dispose() => ortam.Dispose();

    private const string Slug = "toyota-corolla-2019-abc123";

    private const string K = SeoOrtami.Kok;

    private readonly SeoOrtami ortam = Ortam();

    private static SeoOrtami Ortam()
    {
        SeoOrtami ortam = new();
        ortam.Ilanlar[(Slug, "*")] = SeoOrtami.Ilan(Slug);
        ortam.Ilanlar[(Slug, "ar")] = SeoOrtami.Ilan(Slug, modelName: "تويوتا كورولا");
        ortam.Sayfalar[("tr", "kvkk")] = SeoOrtami.Sayfa("KVKK Aydınlatma Metni", "Kişisel verileriniz korunur.", new ContentPageAlternateDto("en", "privacy"));
        ortam.Sayfalar[("en", "privacy")] = SeoOrtami.Sayfa("Privacy notice", "Your data.", new ContentPageAlternateDto("tr", "kvkk"));
        ortam.Sayfalar[("en", "only-english")] = SeoOrtami.Sayfa("Only English", "Text.");
        return ortam;
    }

    private Task<(HttpResponseMessage Yanit, IDocument Belge, string Html)> GetAsync(string adres, Action<HttpRequestMessage>? ayarla = null) =>
        SeoOrtami.GetAsync(ortam.Istemci(factory), adres, ayarla);

    // ── Başlık ve açıklama ─────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-1: `/tr/` belgesinde tam olarak bir `<title>` vardır ve metni galerinin tr görünen adıdır.")]
    public async Task Baslik_AnaSayfa_GorunenAd() =>
        Assert.Equal("Oto Galeri", (await GetAsync("/tr/")).Belge.Baslik());

    [Fact(DisplayName = "0004/AC-2: Ana sayfa metinlerinde giriş açıklaması ar için kaydedilmişken `/ar/` belgesinin `<meta name=\"description\">` değeri o kayıtlı metindir.")]
    public async Task Aciklama_AnaSayfa_KayitliMetin()
    {
        ortam.AnaSayfaMetinleri["ar"] = new HomeTextsDto(null, "نص المقدمة المحفوظ", null, null, null, null);

        Assert.Equal("نص المقدمة المحفوظ", (await GetAsync("/ar/")).Belge.Meta("description"));
    }

    [Fact(DisplayName = "0004/AC-3: Giriş açıklaması en için kaydedilmemişken `/en/` belgesinin açıklaması en vitrin kataloğundaki `home.intro.body` anahtarının metnidir.")]
    public async Task Aciklama_AnaSayfa_KatalogVarsayilani() =>
        Assert.Equal(Katalog("en", "home.intro.body"), (await GetAsync("/en/")).Belge.Meta("description"));

    [Fact(DisplayName = "0004/AC-4: `/tr/cars` belgesinin başlığı `{katalogdaki tr liste başlığı} · {görünen ad}`'dır ve açıklaması katalogdaki tr liste açıklamasıdır.")]
    public async Task Baslik_Liste()
    {
        IDocument belge = (await GetAsync("/tr/cars")).Belge;

        Assert.Equal($"{Katalog("tr", "seo.list.title")} · Oto Galeri", belge.Baslik());
        Assert.Equal(Katalog("tr", "seo.list.description"), belge.Meta("description"));
    }

    [Fact(DisplayName = "0004/AC-5: Görünür bir ilanın `/tr/cars/{slug}` belgesinin başlığı `{tr model adı} {yıl} · {görünen ad}`'dır.")]
    public async Task Baslik_Detay() =>
        Assert.Equal("Toyota Corolla 2019 · Oto Galeri", (await GetAsync($"/tr/cars/{Slug}")).Belge.Baslik());

    [Fact(DisplayName = "0004/AC-6: Modelin ar karşılığı kayıtlıyken `/ar/cars/{slug}` belgesinin başlığı ar model adını taşır. Karşılık kayıtlı değilse kanonik Latin adı taşır.")]
    public async Task Baslik_Detay_ArModelAdi()
    {
        Assert.Equal("تويوتا كورولا 2019 · معرض السيارات", (await GetAsync($"/ar/cars/{Slug}")).Belge.Baslik());

        // Karşılık yoksa okuma kanonik Latin adı döner (`0021` D24); belge onu olduğu gibi basar.
        ortam.Ilanlar.Remove((Slug, "ar"));
        Assert.Equal("Toyota Corolla 2019 · معرض السيارات", (await GetAsync($"/ar/cars/{Slug}")).Belge.Baslik());
    }

    [Theory(DisplayName = "0004/AC-7: Detay açıklaması yılı, kilometreyi, fiyatı ve şube adını taşır. Kilometre ve fiyatın yazımı, üç dilde de `frontend/src/shared/i18n/format.fixtures.json`'daki beklenen değerlerle aynıdır (`0018` **D24**/**D32**'nin fikstürü).")]
    [InlineData("tr")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task Aciklama_Detay_FiksturBicimi(string dil)
    {
        using JsonDocument fikstur = JsonDocument.Parse(File.ReadAllText(Path.Combine(CozumKoku(), "frontend", "src", "shared", "i18n", "format.fixtures.json")));
        string km = fikstur.RootElement.GetProperty("mileage").EnumerateArray().Single(item => item.GetProperty("value").GetInt32() == 125_000).GetProperty(dil).GetString()!;
        string fiyat = fikstur.RootElement.GetProperty("money").EnumerateArray()
            .Single(item => item.GetProperty("amount").GetString() == "1250000" && item.GetProperty("currencyCode").GetString() == "TRY")
            .GetProperty(dil).GetString()!;

        string aciklama = (await GetAsync($"/{dil}/cars/{Slug}")).Belge.Meta("description")!;

        Assert.Contains("2019", aciklama, StringComparison.Ordinal);
        Assert.Contains(km, aciklama, StringComparison.Ordinal);
        Assert.Contains(fiyat, aciklama, StringComparison.Ordinal);
        Assert.Contains("Kadıköy", aciklama, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0004/AC-8: \"Fiyat sorunuz\" ilanının detay açıklaması vitrin kataloğundaki `detail.priceOnRequest` metnini taşır ve hiçbir fiyat rakamı (`0` dâhil) içermez.")]
    public async Task Aciklama_FiyatSorunuz()
    {
        ortam.Ilanlar[("bmw-320-2018-ask001", "*")] = SeoOrtami.Ilan("bmw-320-2018-ask001", price: ShowcasePriceDto.OnRequestPrice());

        string aciklama = (await GetAsync("/tr/cars/bmw-320-2018-ask001")).Belge.Meta("description")!;

        Assert.Contains(Katalog("tr", "detail.priceOnRequest"), aciklama, StringComparison.Ordinal);
        Assert.DoesNotContain("₺", aciklama, StringComparison.Ordinal);
        Assert.DoesNotContain(" 0", aciklama.Replace("125.000", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.Equal("2019 model, 125.000 km, Fiyat sorunuz. Kadıköy şubesinde.", aciklama);
    }

    [Fact(DisplayName = "0004/AC-9: Para birimi KWD (minor unit 3) olan bir kurulumda `1250.500` KWD'lik ilanın detay açıklaması fiyatı üç ondalıkla yazar.")]
    public async Task Aciklama_Kwd_UcOndalik()
    {
        ortam.Ilanlar[("lexus-es-2022-kwd001", "*")] = SeoOrtami.Ilan("lexus-es-2022-kwd001", price: new ShowcasePriceDto(false, 1250.500m, "KWD", 3));

        // `Intl` biçimi (fikstür): kod ile sayı arasında bölünmez boşluk (U+00A0).
        Assert.Contains("KWD\u00A01.250,500", (await GetAsync("/tr/cars/lexus-es-2022-kwd001")).Belge.Meta("description"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0004/AC-10: Yayındaki bir içerik sayfasının `/tr/{slug}` belgesinin başlığı `{sayfa başlığı} · {görünen ad}`'dır.")]
    public async Task Baslik_Icerik() =>
        Assert.Equal("KVKK Aydınlatma Metni · Oto Galeri", (await GetAsync("/tr/kvkk")).Belge.Baslik());

    [Fact(DisplayName = "0004/AC-11: Gövdesi `## Başlık`, `**kalın**` ve `[bağlantı](https://ornek.test)` ile başlayan bir içerik sayfasının açıklamasında `#`, `**`, `[`, `](` dizileri yoktur, bağlantının metni vardır.")]
    public async Task Aciklama_Icerik_IsaretsizMetin()
    {
        ortam.Sayfalar[("tr", "bicimli")] = SeoOrtami.Sayfa("Biçimli", "## Başlık\n\n**kalın** metin ve [bağlantı](https://ornek.test) burada.\n\n- madde");

        string aciklama = (await GetAsync("/tr/bicimli")).Belge.Meta("description")!;

        foreach (string isaret in new[] { "#", "**", "[", "](" })
        {
            Assert.DoesNotContain(isaret, aciklama, StringComparison.Ordinal);
        }

        Assert.Contains("bağlantı", aciklama, StringComparison.Ordinal);
        Assert.Equal("Başlık kalın metin ve bağlantı burada. madde", aciklama);
    }

    [Fact(DisplayName = "0004/AC-12: 400 karakterlik gövdeli bir içerik sayfasının açıklaması en çok 160 karakterdir ve gövdedeki bir kelimenin ortasında bitmez.")]
    public async Task Aciklama_Icerik_160Karakter_KelimeSinirinda()
    {
        string govde = string.Join(' ', Enumerable.Range(0, 80).Select(index => $"ke{index % 10}lm")).PadRight(400, 'x')[..400];
        ortam.Sayfalar[("tr", "uzun")] = SeoOrtami.Sayfa("Uzun", govde);

        string aciklama = (await GetAsync("/tr/uzun")).Belge.Meta("description")!;

        Assert.InRange(aciklama.Length, 1, SeoHeadBuilder.DescriptionLimit);
        Assert.True(govde.Length == aciklama.Length || govde[aciklama.Length] == ' ', $"Kelime ortasında kesildi: …{aciklama[^5..]}|{govde[aciklama.Length]}");
        Assert.StartsWith(aciklama, govde, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0004/AC-13: Görünen ad sorgusu hata verdiğinde `/tr/cars` belgesi 200 döner ve başlığı yalnız katalogdaki liste başlığıdır (` · ` eki yoktur).")]
    public async Task Baslik_Liste_AdOkunamazken()
    {
        ortam.AdOkunamaz = true;

        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync("/tr/cars");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal(Katalog("tr", "seo.list.title"), belge.Baslik());
    }

    [Fact(DisplayName = "0004/AC-14: Görünen ad sorgusu hata verdiğinde `/tr/` belgesinin başlığı katalogdaki tr ana sayfa başlığıdır.")]
    public async Task Baslik_AnaSayfa_AdOkunamazken()
    {
        ortam.AdOkunamaz = true;

        Assert.Equal(Katalog("tr", "seo.home.fallbackTitle"), (await GetAsync("/tr/")).Belge.Baslik());
    }

    [Fact(DisplayName = "0004/AC-15: Ana sayfa, liste, detay, içerik, 404, bülten teyit/çıkış ve panel belgelerinin hiçbirinin `<title>`'ında ya da `og:site_name`'inde `CarGallery` dizesi geçmez (görünen ad okunamadığı durum dâhil). Hiçbir belgede birden çok `<title>` yoktur.")]
    public async Task Basmaz_YazilimAdini()
    {
        string[] adresler = ["/tr/", "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk", "/tr/yok-boyle", "/tr/newsletter/confirm?token=x", "/tr/newsletter/unsubscribe?token=x", "/admin"];

        foreach (bool okunamaz in new[] { false, true })
        {
            ortam.AdOkunamaz = okunamaz;

            foreach (string adres in adresler)
            {
                IDocument belge = (await GetAsync(adres)).Belge;
                string baslik = belge.Baslik();

                Assert.DoesNotContain("CarGallery", baslik, StringComparison.Ordinal);
                Assert.DoesNotContain("CarGallery", belge.Og("og:site_name") ?? string.Empty, StringComparison.Ordinal);
            }
        }
    }

    [Fact(DisplayName = "0004/AC-16: Görünen adı `A <b>&\"B\"` olan galerinin `/tr/cars` belgesi ayrıştırıldığında `<title>`'ın metin değeri `{liste başlığı} · A <b>&\"B\"`'dir ve belgede `<b>` öğesi oluşmaz.")]
    public async Task Kodlanir_OzelKarakterliAd()
    {
        ortam.Adlar["tr"] = "A <b>&\"B\"";

        IDocument belge = (await GetAsync("/tr/cars")).Belge;

        Assert.Equal($"{Katalog("tr", "seo.list.title")} · A <b>&\"B\"", belge.Baslik());
        Assert.Empty(belge.QuerySelectorAll("b"));
    }

    // ── Kanonik ───────────────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-17: Ana sayfa, liste, görünür ilan detayı ve yayındaki içerik sayfası belgelerinin her biri tam olarak bir `<link rel=\"canonical\">` taşır ve `href`'i sırasıyla `{kök}/tr/`, `{kök}/tr/cars`, `{kök}/tr/cars/{slug}`, `{kök}/tr/{slug}`'dır.")]
    public async Task Kanonik_DortSayfa()
    {
        Assert.Equal([$"{K}/tr/"], (await GetAsync("/tr/")).Belge.Kanonikler());
        Assert.Equal([$"{K}/tr/cars"], (await GetAsync("/tr/cars")).Belge.Kanonikler());
        Assert.Equal([$"{K}/tr/cars/{Slug}"], (await GetAsync($"/tr/cars/{Slug}")).Belge.Kanonikler());
        Assert.Equal([$"{K}/tr/kvkk"], (await GetAsync("/tr/kvkk")).Belge.Kanonikler());
    }

    [Fact(DisplayName = "0004/AC-18: `/ar/cars/{slug}` belgesinin kanoniği `{kök}/ar/cars/{slug}`'dır.")]
    public async Task Kanonik_ArDetay() =>
        Assert.Equal([$"{K}/ar/cars/{Slug}"], (await GetAsync($"/ar/cars/{Slug}")).Belge.Kanonikler());

    [Fact(DisplayName = "0004/AC-19: `/tr/cars/{slug}?utm_source=x` belgesinin kanoniği sorgu dizesi taşımaz.")]
    public async Task Kanonik_SorguDizesiz() =>
        Assert.Equal([$"{K}/tr/cars/{Slug}"], (await GetAsync($"/tr/cars/{Slug}?utm_source=x")).Belge.Kanonikler());

    [Fact(DisplayName = "0004/AC-20: `/tr` ve `/tr/` isteklerinin belgeleri aynı kanoniği (`{kök}/tr/`) taşır.")]
    public async Task Kanonik_EgikCizgisiz()
    {
        Assert.Equal([$"{K}/tr/"], (await GetAsync("/tr")).Belge.Kanonikler());
        Assert.Equal([$"{K}/tr/"], (await GetAsync("/tr/")).Belge.Kanonikler());
    }

    [Fact(DisplayName = "0004/AC-21: `Host: kotu.test` ve `X-Forwarded-Host: kotu.test` başlıklarıyla yapılan istekte kanonik, dil alternatifleri ve `og:url` yine `{kök}` ile başlar ve `kotu.test` belgede geçmez.")]
    public async Task Kok_IstektenTuretilmez()
    {
        foreach (string adres in new[] { "/tr/", "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk" })
        {
            (_, IDocument belge, string html) = await GetAsync(adres, istek =>
            {
                istek.Headers.Host = "kotu.test";
                istek.Headers.Add("X-Forwarded-Host", "kotu.test");
            });

            Assert.All(belge.Kanonikler(), href => Assert.StartsWith(K + "/", href, StringComparison.Ordinal));
            Assert.All(belge.Alternatifler().Values, href => Assert.StartsWith(K + "/", href, StringComparison.Ordinal));
            Assert.StartsWith(K + "/", belge.Og("og:url"), StringComparison.Ordinal);
            Assert.DoesNotContain("kotu.test", html, StringComparison.Ordinal);
        }
    }

    [Theory(DisplayName = "0004/AC-86: Kök `https://x.test/` ve `https://x.test` olarak yapılandırılmış iki kurulumda `/tr/cars` belgesinin kanoniği aynıdır: `https://x.test/tr/cars`.")]
    [InlineData("https://x.test/")]
    [InlineData("https://x.test")]
    public async Task Kanonik_SondakiEgikCizgiNormalize(string kok)
    {
        HttpClient istemci = ortam.Istemci(factory, builder => builder.UseSetting(SiteRoot.ConfigurationKey, kok));

        Assert.Equal(["https://x.test/tr/cars"], (await SeoOrtami.GetAsync(istemci, "/tr/cars")).Belge.Kanonikler());
    }

    // ── Dil alternatifleri ─────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-23: Ana sayfa, liste ve detay belgelerinin her biri, etkin her dil için bir `<link rel=\"alternate\" hreflang=\"{dil}\">` taşır. `href`'i o dilin mutlak adresidir ve küme belgenin kendi dilini de içerir.")]
    public async Task Alternatifler_HerEtkinDil()
    {
        foreach ((string adres, Func<string, string> yol) in new (string, Func<string, string>)[]
        {
            ("/ar/", dil => $"/{dil}/"),
            ("/ar/cars", dil => $"/{dil}/cars"),
            ($"/ar/cars/{Slug}", dil => $"/{dil}/cars/{Slug}"),
        })
        {
            IReadOnlyDictionary<string, string> alternatifler = (await GetAsync(adres)).Belge.Alternatifler();

            foreach (string dil in new[] { "tr", "ar", "en" })
            {
                Assert.Equal(K + yol(dil), alternatifler[dil]);
            }
        }
    }

    [Fact(DisplayName = "0004/AC-24: Ana sayfa, liste ve detay belgelerinin `hreflang=\"x-default\"` alternatifi varsayılan dilin (tr) aynı sayfasıdır.")]
    public async Task XDefault_VarsayilanDil()
    {
        Assert.Equal($"{K}/tr/", (await GetAsync("/en/")).Belge.Alternatifler()["x-default"]);
        Assert.Equal($"{K}/tr/cars", (await GetAsync("/en/cars")).Belge.Alternatifler()["x-default"]);
        Assert.Equal($"{K}/tr/cars/{Slug}", (await GetAsync($"/en/cars/{Slug}")).Belge.Alternatifler()["x-default"]);
    }

    [Fact(DisplayName = "0004/AC-25: Etkin diller `tr, en` iken `/tr/cars/{slug}` belgesinde `hreflang=\"ar\"` alternatifi yoktur.")]
    public async Task Alternatif_Yok_EtkinOlmayanDil()
    {
        // Yapılandırma katmanında dizi elemanı silinemiyor (`0014` AC-67'nin notu); iki dilli kurulum
        // bileşimin kendi bağımlılığıyla kuruluyor. Okuma yine sahte port, çıktı gerçek bileşim.
        LocalePrefixResolver diller = new("tr", ["tr", "en"]);
        SeoHeadBuilder seo = new(SiteRoot.Parse(K), SeoCatalog.Load(AppContext.BaseDirectory, diller.Enabled), diller, NullLogger<SeoHeadBuilder>.Instance);
        DefaultHttpContext context = new()
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IShowcaseListingDetailQueries>(new SabitDetay(SeoOrtami.Ilan(Slug)))
                .BuildServiceProvider(),
        };
        context.Request.Path = $"/tr/cars/{Slug}";

        SeoHead head = await seo.BuildAsync(context, CarGallery.BuildingBlocks.Locale.Create("tr").Value, "Oto Galeri", null);
        IReadOnlyDictionary<string, string> alternatifler = new HtmlParser().ParseDocument($"<head>{head.Tags}</head>").Alternatifler();

        Assert.Equal(["en", "tr", "x-default"], alternatifler.Keys.Order(StringComparer.Ordinal));
        Assert.False(alternatifler.ContainsKey("ar"));
    }

    private sealed class SabitDetay(ShowcaseListingDetailDto detay) : IShowcaseListingDetailQueries
    {
        public Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale, CancellationToken cancellationToken) =>
            Task.FromResult<ShowcaseListingDetailDto?>(detay);

        public Task<ShowcaseImageDto?> FindFirstImageAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult<ShowcaseImageDto?>(detay.Images.FirstOrDefault());
    }

    [Fact(DisplayName = "0004/AC-26: tr'de `kvkk`, en'de `privacy` slug'ıyla yayında, ar'da yayında olmayan bir içerik sayfasının `/tr/kvkk` belgesi tam olarak iki dil alternatifi (`{kök}/tr/kvkk`, `{kök}/en/privacy`) ve `x-default` = `{kök}/tr/kvkk` taşır.")]
    public async Task Alternatifler_IcerikYayindakiDiller() =>
        Assert.Equal(
            new Dictionary<string, string> { ["tr"] = $"{K}/tr/kvkk", ["en"] = $"{K}/en/privacy", ["x-default"] = $"{K}/tr/kvkk" },
            (await GetAsync("/tr/kvkk")).Belge.Alternatifler());

    [Fact(DisplayName = "0004/AC-27: Varsayılan dil tr'de yayında olmayan, en'de yayında olan bir içerik sayfasının `/en/{slug}` belgesi `x-default` alternatifi taşımaz.")]
    public async Task XDefault_Yok_VarsayilanDildeYayindaDegilken() =>
        Assert.False((await GetAsync("/en/only-english")).Belge.Alternatifler().ContainsKey("x-default"));

    [Fact(DisplayName = "0004/AC-28: `hreflang` değerleri yalnız dil kodudur (`tr`, `ar`, `en`, `x-default`). Bölge eki yoktur.")]
    public async Task Hreflang_YalnizDilKodu()
    {
        foreach (string adres in new[] { "/tr/", "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk" })
        {
            Assert.All((await GetAsync(adres)).Belge.Alternatifler().Keys, dil => Assert.Contains(dil, new[] { "tr", "ar", "en", "x-default" }));
        }
    }

    // ── Open Graph ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-29: İndekslenebilir her belge türünde `og:title` başlıkla, `og:description` açıklamayla aynı metni taşır.")]
    public async Task Og_BaslikVeAciklama()
    {
        foreach (string adres in new[] { "/tr/", "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk" })
        {
            IDocument belge = (await GetAsync(adres)).Belge;
            Assert.Equal(belge.Baslik(), belge.Og("og:title"));
            Assert.Equal(belge.Meta("description"), belge.Og("og:description"));
        }
    }

    [Fact(DisplayName = "0004/AC-30: İndekslenebilir her belge türünde `og:url` kanonikle aynıdır ve `og:type` `website`'tır.")]
    public async Task Og_UrlVeTip()
    {
        foreach (string adres in new[] { "/tr/", "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk" })
        {
            IDocument belge = (await GetAsync(adres)).Belge;
            Assert.Equal(Assert.Single(belge.Kanonikler()), belge.Og("og:url"));
            Assert.Equal("website", belge.Og("og:type"));
        }
    }

    [Fact(DisplayName = "0004/AC-31: Görselli bir ilanın detay belgesindeki `og:image`, ilk görselin büyük türevinin mutlak adresidir. Medya adresi göreliyse `{kök}` ile başlar.")]
    public async Task OgImage_IlkGorselinBuyukTurevi_Mutlak() =>
        Assert.Equal($"{K}/seed-media/{Slug}-0-large.webp", (await GetAsync($"/tr/cars/{Slug}")).Belge.Og("og:image"));

    [Fact(DisplayName = "0004/AC-32, 0026/AC-47, AC-48: Logosuz kurulumda görselsiz bir ilanın detay belgesinde `og:image` yoktur; hazır logolu kurulumda `logo-large`'ın adresidir.")]
    public async Task OgImage_Yok_GorselsizIlan()
    {
        ortam.Ilanlar[("fiat-egea-2021-img000", "*")] = SeoOrtami.Ilan("fiat-egea-2021-img000", images: 0);

        Assert.Null((await GetAsync("/tr/cars/fiat-egea-2021-img000")).Belge.Og("og:image"));

        // `0026` D6 — aynı ilan, hazır logolu kurulumda (yeni istemci: belge her istekte kurulur).
        Guid logo = Guid.CreateVersion7();
        ortam.LogoKimligi = logo;
        ortam.LogoVarligi = SeoOrtami.LogoDto(logo);
        Assert.Equal(
            $"https://cdn.galeri.test/media/public/assets/{logo:D}/logo-large.webp",
            (await GetAsync("/tr/cars/fiat-egea-2021-img000")).Belge.Og("og:image"));
    }

    [Fact(DisplayName = "0004/AC-33, 0026/AC-44, AC-45, AC-48: Logosuz kurulumda ana sayfa, liste ve içerik belgelerinde `og:image` yoktur; hazır logolu kurulumda (süzgeçli liste dâhil) `logo-large`'ın adresidir.")]
    public async Task OgImage_Yok_DigerSayfalarda()
    {
        foreach (string adres in new[] { "/tr/", "/tr/cars", "/tr/kvkk" })
        {
            Assert.Null((await GetAsync(adres)).Belge.Og("og:image"));
        }

        // `0026` D6 — hazır logolu kurulum.
        Guid logo = Guid.CreateVersion7();
        ortam.LogoKimligi = logo;
        ortam.LogoVarligi = SeoOrtami.LogoDto(logo);

        foreach (string adres in new[] { "/tr/", "/tr/cars", "/tr/cars?bodyType=suv", "/tr/kvkk" })
        {
            Assert.Equal($"https://cdn.galeri.test/media/public/assets/{logo:D}/logo-large.webp", (await GetAsync(adres)).Belge.Og("og:image"));
        }
    }

    [Fact(DisplayName = "0004/AC-34: `og:site_name` görünen addır. Görünen ad okunamadığında `og:site_name` basılmaz.")]
    public async Task OgSiteName()
    {
        Assert.Equal("Oto Galeri", (await GetAsync("/tr/cars")).Belge.Og("og:site_name"));

        ortam.AdOkunamaz = true;
        Assert.Null((await GetAsync("/tr/cars")).Belge.Og("og:site_name"));
    }

    [Fact(DisplayName = "0004/AC-35: Hiçbir belgede `og:locale` yoktur.")]
    public async Task OgLocale_Yok()
    {
        foreach (string adres in new[] { "/tr/", "/ar/cars", $"/en/cars/{Slug}", "/tr/kvkk", "/tr/yok-boyle", "/admin" })
        {
            Assert.Null((await GetAsync(adres)).Belge.Og("og:locale"));
        }
    }

    // ── Car + Offer ────────────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-36: Görünür bir ilanın detay belgesi tam olarak bir `<script type=\"application/ld+json\">` taşır. İçeriği geçerli JSON'dur ve `@type`'ı `Car`'dır.")]
    public async Task JsonLd_TekCar() =>
        Assert.Equal("Car", Assert.Single((await GetAsync($"/tr/cars/{Slug}")).Belge.JsonLd()).GetProperty("@type").GetString());

    [Fact(DisplayName = "0004/AC-37: `Car` nesnesinin anahtarları **D14**'ün listesinin alt kümesidir (`@context`, `@type`, `name`, `vehicleModelDate`, `mileageFromOdometer`, `image`, `url`, `offers`). `offers`'ın anahtarları da `@type`, `price`, `priceCurrency`, `availability`, `url`'nin alt kümesidir. Başka anahtar yoktur.")]
    public async Task JsonLd_KapaliAnahtarListesi()
    {
        JsonElement car = Assert.Single((await GetAsync($"/tr/cars/{Slug}")).Belge.JsonLd());

        Assert.Subset(
            new HashSet<string>(["@context", "@type", "name", "vehicleModelDate", "mileageFromOdometer", "image", "url", "offers"]),
            new HashSet<string>(car.Anahtarlar()));
        Assert.Subset(
            new HashSet<string>(["@type", "price", "priceCurrency", "availability", "url"]),
            new HashSet<string>(car.GetProperty("offers").Anahtarlar()));
    }

    [Fact(DisplayName = "0004/AC-38: `vehicleModelDate` ilanın yılıdır. `mileageFromOdometer`, `value`'su ilanın kilometresi ve `unitCode`'u `KMT` olan bir `QuantitativeValue`'dur.")]
    public async Task JsonLd_YilVeKilometre()
    {
        JsonElement car = Assert.Single((await GetAsync($"/tr/cars/{Slug}")).Belge.JsonLd());

        Assert.Equal("2019", car.GetProperty("vehicleModelDate").GetString());
        JsonElement km = car.GetProperty("mileageFromOdometer");
        Assert.Equal("QuantitativeValue", km.GetProperty("@type").GetString());
        Assert.Equal(125_000, km.GetProperty("value").GetInt32());
        Assert.Equal("KMT", km.GetProperty("unitCode").GetString());
    }

    [Fact(DisplayName = "0004/AC-39: `image`, ilanın görsellerinin büyük türevlerinin mutlak adresleridir, ilandaki sırayla. `url` kanoniktir.")]
    public async Task JsonLd_GorsellerVeUrl()
    {
        JsonElement car = Assert.Single((await GetAsync($"/tr/cars/{Slug}")).Belge.JsonLd());

        Assert.Equal(
            [$"{K}/seed-media/{Slug}-0-large.webp", $"https://cdn.galeri.test/{Slug}-1-large.webp"],
            car.GetProperty("image").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal($"{K}/tr/cars/{Slug}", car.GetProperty("url").GetString());
    }

    [Fact(DisplayName = "0004/AC-40: TRY kurulumunda `1.250.000,00` TL'lik ilanın `offers.price`'ı `\"1250000.00\"`, `priceCurrency`'si `TRY`'dir.")]
    public async Task JsonLd_FiyatTry()
    {
        JsonElement teklif = Assert.Single((await GetAsync($"/tr/cars/{Slug}")).Belge.JsonLd()).GetProperty("offers");

        Assert.Equal("1250000.00", teklif.GetProperty("price").GetString());
        Assert.Equal("TRY", teklif.GetProperty("priceCurrency").GetString());
    }

    [Fact(DisplayName = "0004/AC-41: KWD (minor unit 3) kurulumunda `1250.5` KWD'lik ilanın `offers.price`'ı `\"1250.500\"`, `priceCurrency`'si `KWD`'dir.")]
    public async Task JsonLd_FiyatKwd()
    {
        ortam.Ilanlar[("lexus-es-2022-kwd002", "*")] = SeoOrtami.Ilan("lexus-es-2022-kwd002", price: new ShowcasePriceDto(false, 1250.5m, "KWD", 3));

        JsonElement teklif = Assert.Single((await GetAsync("/tr/cars/lexus-es-2022-kwd002")).Belge.JsonLd()).GetProperty("offers");

        Assert.Equal("1250.500", teklif.GetProperty("price").GetString());
        Assert.Equal("KWD", teklif.GetProperty("priceCurrency").GetString());
    }

    [Fact(DisplayName = "0004/AC-42: `/ar/cars/{slug}` belgesindeki `offers.price` Latin rakam ve nokta ondalık taşır. Arapça rakam ya da binlik ayracı yoktur.")]
    public async Task JsonLd_FiyatAr_LatinRakam() =>
        Assert.Matches(@"^\d+\.\d{2}$", Assert.Single((await GetAsync($"/ar/cars/{Slug}")).Belge.JsonLd()).GetProperty("offers").GetProperty("price").GetString());

    [Fact(DisplayName = "0004/AC-43: Yayındaki ilanın `availability`'si `https://schema.org/InStock`, rezerve ilanınki `https://schema.org/Reserved`'dır.")]
    public async Task JsonLd_Bulunabilirlik()
    {
        ortam.Ilanlar[("audi-a3-2020-rez001", "*")] = SeoOrtami.Ilan("audi-a3-2020-rez001", reserved: true);

        Assert.Equal("https://schema.org/InStock", Assert.Single((await GetAsync($"/tr/cars/{Slug}")).Belge.JsonLd()).GetProperty("offers").GetProperty("availability").GetString());
        Assert.Equal("https://schema.org/Reserved", Assert.Single((await GetAsync("/tr/cars/audi-a3-2020-rez001")).Belge.JsonLd()).GetProperty("offers").GetProperty("availability").GetString());
    }

    [Fact(DisplayName = "0004/AC-44: \"Fiyat sorunuz\" ilanının `Car` nesnesinde `offers` anahtarı yoktur.")]
    public async Task JsonLd_FiyatSorunuz_OffersYok()
    {
        ortam.Ilanlar[("bmw-320-2018-ask002", "*")] = SeoOrtami.Ilan("bmw-320-2018-ask002", price: ShowcasePriceDto.OnRequestPrice());

        Assert.DoesNotContain("offers", Assert.Single((await GetAsync("/tr/cars/bmw-320-2018-ask002")).Belge.JsonLd()).Anahtarlar());
    }

    [Fact(DisplayName = "0004/AC-45: `/ar/cars/{slug}` belgesindeki `Car.name`, AC-6'daki ar model adıdır.")]
    public async Task JsonLd_ArModelAdi() =>
        Assert.Equal("تويوتا كورولا", Assert.Single((await GetAsync($"/ar/cars/{Slug}")).Belge.JsonLd()).GetProperty("name").GetString());

    [Fact(DisplayName = "0004/AC-46: Model adı `X</script><script>alert(1)</script>` olan ilanın detay belgesi ayrıştırıldığında tek bir JSON-LD bloğu vardır, içeriği geçerli JSON'dur ve `name`'i bu dizedir. Belgedeki `<script>` öğesi sayısı, aynı belgenin bu kartın etiketleri olmadan basılmış hâlinden tam bir fazladır ve `alert(1)` hiçbir `<script>` öğesinin metni değildir.")]
    public async Task JsonLd_ScriptKapatilamaz()
    {
        const string Ad = "X</script><script>alert(1)</script>";
        ortam.Ilanlar[("kotu-ad-2020-xss001", "*")] = SeoOrtami.Ilan("kotu-ad-2020-xss001", modelName: Ad);

        IDocument belge = (await GetAsync("/tr/cars/kotu-ad-2020-xss001")).Belge;

        // "Bu kartın etiketleri olmadan" = kabuğun kendisi: Host'un servis ettiği index.html.
        int kabuk = new HtmlParser().ParseDocument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestWebRoot", "index.html"))).QuerySelectorAll("script").Length;

        Assert.Equal(Ad, Assert.Single(belge.JsonLd()).GetProperty("name").GetString());
        Assert.Equal(kabuk + 1, belge.QuerySelectorAll("script").Length);
        Assert.DoesNotContain(belge.QuerySelectorAll("script"), script => script.TextContent == "alert(1)");
    }

    [Fact(DisplayName = "0004/AC-47: Kurulumda gizlenmiş bir opsiyonel alanın (**F3**) değeri, detay belgesinin hiçbir etiketinde ve JSON-LD'sinde geçmez.")]
    public async Task Sizmaz_GizliAlan()
    {
        // Gizli alan okumadan zaten gelmez (F3, modülün testleri); Host'un kapalı listesi (D14) ise
        // okumada GELEN bir özniteliği bile basmaz — değer burada okumaya kasten konuyor.
        ortam.Ilanlar[("gizli-alan-2020-f3f3f3", "*")] = SeoOrtami.Ilan("gizli-alan-2020-f3f3f3", attributes: [new ShowcaseAttributeDto("vin", "Şasi", "GIZLI-SASI-777")]);

        IDocument belge = (await GetAsync("/tr/cars/gizli-alan-2020-f3f3f3")).Belge;

        Assert.DoesNotContain("GIZLI-SASI-777", belge.Head!.OuterHtml, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0004/AC-48: Detay belgesi dışındaki belgelerde `@type: Car` taşıyan JSON-LD yoktur.")]
    public async Task JsonLd_CarYalnizDetayda()
    {
        foreach (string adres in new[] { "/tr/", "/tr/cars", "/tr/kvkk", "/tr/yok-boyle", "/admin" })
        {
            Assert.DoesNotContain((await GetAsync(adres)).Belge.JsonLd(), json => json.GetProperty("@type").GetString() == "Car");
        }
    }

    // ── AutoDealer ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-49: `/tr/` belgesi tam olarak bir JSON-LD bloğu taşır. `@type`'ı `AutoDealer`, `name`'i tr görünen ad, `url`'i `{kök}/tr/`'dir.")]
    public async Task AutoDealer_AnaSayfa()
    {
        JsonElement dealer = Assert.Single((await GetAsync("/tr/")).Belge.JsonLd());

        Assert.Equal("AutoDealer", dealer.GetProperty("@type").GetString());
        Assert.Equal("Oto Galeri", dealer.GetProperty("name").GetString());
        Assert.Equal($"{K}/tr/", dealer.GetProperty("url").GetString());
    }

    [Fact(DisplayName = "0004/AC-50: Vitrinde görünen iki şubesi olan kurulumda `department` iki öğe taşır. Her öğe `@type: AutoDealer`, şubenin tr adı, tr adresi ve telefonudur.")]
    public async Task AutoDealer_IkiSube()
    {
        ortam.Galeri = new PublicGalleryDto("Oto Galeri",
        [
            new PublicBranchDto("Kadıköy Şubesi", "Bağdat Cad. 1, İstanbul", "+902160000001"),
            new PublicBranchDto("Ataşehir Şubesi", "Barbaros Mah. 7, İstanbul", "+902160000002"),
        ],
        []);

        JsonElement[] subeler = [.. Assert.Single((await GetAsync("/tr/")).Belge.JsonLd()).GetProperty("department").EnumerateArray()];

        Assert.Equal(2, subeler.Length);
        Assert.All(subeler, sube => Assert.Equal("AutoDealer", sube.GetProperty("@type").GetString()));
        Assert.Equal(("Kadıköy Şubesi", "Bağdat Cad. 1, İstanbul", "+902160000001"), (subeler[0].GetProperty("name").GetString(), subeler[0].GetProperty("address").GetString(), subeler[0].GetProperty("telephone").GetString()));
        Assert.Equal(("Ataşehir Şubesi", "Barbaros Mah. 7, İstanbul", "+902160000002"), (subeler[1].GetProperty("name").GetString(), subeler[1].GetProperty("address").GetString(), subeler[1].GetProperty("telephone").GetString()));
    }

    [Fact(DisplayName = "0004/AC-52, 0024/AC-45, 0025/AC-55, 0026/AC-52: `AutoDealer` bloğunda (kökte ve `department` öğelerinde) `geo` anahtarı yoktur (`0024` D8: `sameAs`, `0025` D9: `openingHoursSpecification`, `0026` D7: `logo` listeden çıktı).")]
    public async Task AutoDealer_KapsamDisiAnahtarlarYok()
    {
        // `0024`, `0025`, `0026` — hesaplı, programlı ve logolu kurulumda ölçülür: `sameAs`, şube
        // öğesindeki `openingHoursSpecification` ve kökteki `logo` artık beklenen anahtarlar.
        Guid logo = Guid.CreateVersion7();
        ortam.LogoKimligi = logo;
        ortam.LogoVarligi = SeoOrtami.LogoDto(logo);
        ortam.Galeri = ortam.Galeri! with
        {
            SocialLinks = [new PublicSocialLinkDto("instagram", "https://www.instagram.com/otogaleri")],
            Branches = [ortam.Galeri.Branches[0] with { OpeningHours = CalismaSaatleriBelgeTests.Program(("monday", [("09:00", "18:00")])) }],
        };
        JsonElement dealer = Assert.Single((await GetAsync("/tr/")).Belge.JsonLd());
        IEnumerable<string> butunAnahtarlar = dealer.Anahtarlar()
            .Concat(dealer.GetProperty("department").EnumerateArray().SelectMany(sube => sube.Anahtarlar()));

        Assert.Contains("logo", dealer.Anahtarlar());

        foreach (string yasak in new[] { "geo" })
        {
            Assert.DoesNotContain(yasak, butunAnahtarlar);
        }
    }

    [Fact(DisplayName = "0004/AC-53: Görünen ad okunamadığında `/tr/` belgesi 200 döner ve `AutoDealer` bloğu taşımaz.")]
    public async Task AutoDealer_Yok_AdOkunamazken()
    {
        ortam.AdOkunamaz = true;

        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync("/tr/");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Empty(belge.JsonLd());
    }

    [Fact(DisplayName = "0004/AC-54: Liste, detay ve içerik belgelerinde `@type: AutoDealer` taşıyan JSON-LD yoktur.")]
    public async Task AutoDealer_YalnizAnaSayfada()
    {
        foreach (string adres in new[] { "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk" })
        {
            Assert.DoesNotContain((await GetAsync(adres)).Belge.JsonLd(), json => json.GetProperty("@type").GetString() == "AutoDealer");
        }
    }

    // ── İndekslenmeme ──────────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-67: `/admin`, `/admin/vehicles` ve `/admin/content/home` belgelerinin her biri `<meta name=\"robots\" content=\"noindex\">` taşır.")]
    public async Task Noindex_Panel()
    {
        foreach (string adres in new[] { "/admin", "/admin/vehicles", "/admin/content/home" })
        {
            Assert.Equal("noindex", (await GetAsync(adres)).Belge.Meta("robots"));
        }
    }

    [Fact(DisplayName = "0004/AC-92: `/admin` ve `/tr/newsletter/confirm` belgelerinin `<title>`'ı görünen addır. Görünen ad okunamadığında katalogdaki yedek başlıktır.")]
    public async Task Baslik_PanelVeBulten()
    {
        Assert.Equal("Oto Galeri", (await GetAsync("/admin")).Belge.Baslik());
        Assert.Equal("Oto Galeri", (await GetAsync("/tr/newsletter/confirm?token=x")).Belge.Baslik());

        ortam.AdOkunamaz = true;
        Assert.Equal(Katalog("tr", "seo.document.fallbackTitle"), (await GetAsync("/admin")).Belge.Baslik());
        Assert.Equal(Katalog("tr", "seo.document.fallbackTitle"), (await GetAsync("/tr/newsletter/confirm?token=x")).Belge.Baslik());
    }

    [Theory(DisplayName = "0004/AC-68: `/tr/cars?bodyType=suv`, `/tr/cars?page=2`, `/tr/cars?page=1`, `/tr/cars?sort=newest` ve `/tr/cars?fuel=diesel` belgelerinin her biri `<meta name=\"robots\" content=\"noindex, follow\">` taşır. Kanonik, dil alternatifi ve `og:url` taşımaz (**D23**).")]
    [InlineData("/tr/cars?bodyType=suv")]
    [InlineData("/tr/cars?page=2")]
    [InlineData("/tr/cars?page=1")]
    [InlineData("/tr/cars?sort=newest")]
    [InlineData("/tr/cars?fuel=diesel")]
    public async Task NoindexFollow_DaraltilmisListe(string adres)
    {
        IDocument belge = (await GetAsync(adres)).Belge;

        Assert.Equal("noindex, follow", belge.Meta("robots"));
        Assert.Empty(belge.Kanonikler());
        Assert.Empty(belge.Alternatifler());
        Assert.Null(belge.Og("og:url"));
    }

    [Fact(DisplayName = "0004/AC-69: `/tr/cars?utm_source=x` belgesi robots etiketi taşımaz ve kanoniği `{kök}/tr/cars`'tır.")]
    public async Task Indekslenir_IzlemeParametresiyle()
    {
        IDocument belge = (await GetAsync("/tr/cars?utm_source=x")).Belge;

        Assert.Null(belge.Meta("robots"));
        Assert.Equal([$"{K}/tr/cars"], belge.Kanonikler());
    }

    [Fact(DisplayName = "0004/AC-70: `/tr/newsletter/confirm?token=…` belgesi bugünkü `noindex` etiketini ve `Referrer-Policy: no-referrer` başlığını taşır. Kanonik, dil alternatifi ve JSON-LD taşımaz.")]
    public async Task Bulten_NoindexVeReferrer()
    {
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync("/tr/newsletter/confirm?token=abc");

        Assert.Equal("noindex", belge.Meta("robots"));
        Assert.Equal("no-referrer", Assert.Single(yanit.Headers.GetValues("Referrer-Policy")));
        Assert.Empty(belge.Kanonikler());
        Assert.Empty(belge.Alternatifler());
        Assert.Empty(belge.JsonLd());
    }

    [Fact(DisplayName = "0004/AC-71: Ana sayfa, parametresiz liste, görünür ilan detayı ve yayındaki içerik sayfası belgelerinde robots etiketi yoktur.")]
    public async Task RobotsYok_IndekslenebilirSayfalarda()
    {
        foreach (string adres in new[] { "/tr/", "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk" })
        {
            Assert.Empty((await GetAsync(adres)).Belge.QuerySelectorAll("meta[name='robots']"));
        }
    }

    // ── Durum kodu ─────────────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-72: Hiç var olmamış bir slug'ın `/tr/cars/{slug}` isteği 404 döner. Gövde kabuktur ve uç \"bulunamadı\" ekranını basar.")]
    public async Task Doner404_OlmayanIlan()
    {
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync("/tr/cars/hic-olmamis-ilan-000000");

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.NotNull(belge.QuerySelector("#root"));
    }

    [Fact(DisplayName = "0004/AC-87: Detay belgesinin 404 kararı, vitrin okuma modelinin yolundan (`FindAsync` / `FindFirstImageAsync`) verilir: gizli bir ilanın isteği, hiç var olmamış bir slug'ın isteğinden fazla okuma yapmaz (`0002` **C13**/**E7**). Kanıt bir mimari test ya da sahte port testidir.")]
    public async Task AyniOkuma_GizliVeOlmayanIlan()
    {
        // Okuma modeli gizli ilanın satırını taşımaz (E7): port için gizli ilan = yok. Host'un iki
        // istekte de yaptığı okuma sayısı aynı olmalı — gizli ilan için ek bir "var mı" okuması yok.
        await GetAsync("/tr/cars/gizlenmis-ilan-2019-hid001");
        (int gizliDetay, int gizliIlk) = (ortam.DetayOkumasi, ortam.IlkGorselOkumasi);

        await GetAsync("/tr/cars/hic-olmamis-ilan-000000");

        Assert.Equal((1, 0), (gizliDetay, gizliIlk));
        Assert.Equal((2, 0), (ortam.DetayOkumasi, ortam.IlkGorselOkumasi));
    }

    [Fact(DisplayName = "0004/AC-74: ar'da yayında olmayan bir içerik sayfasının ar slug'ıyla yapılan istek ve hiç var olmamış bir tek parçalı yol (`/tr/yok-boyle-sayfa`) 404 döner.")]
    public async Task Doner404_YayindaOlmayanIcerik()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync("/ar/kvkk")).Yanit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync("/tr/yok-boyle-sayfa")).Yanit.StatusCode);
    }

    [Fact(DisplayName = "0004/AC-75: Eski bir içerik slug'ının isteği bugünkü gibi güncel adrese 301 döner (`0017` **D13**).")]
    public async Task Yonlendirir_EskiSlug()
    {
        HttpResponseMessage yanit = (await GetAsync("/tr/eski-kvkk")).Yanit;

        Assert.Equal(HttpStatusCode.MovedPermanently, yanit.StatusCode);
        Assert.Equal("/tr/kvkk", yanit.Headers.Location?.OriginalString);
    }

    [Theory(DisplayName = "0004/AC-76: `/tr/foo/bar`, `/tr/cars/{görünür slug}/x`, `/tr/admin`, `/tr/newsletter` ve `/tr/newsletter/foo` istekleri 404 döner ve uç liste ya da detay değil \"bulunamadı\" ekranını basar (tr ve ar için görsel kanıt).")]
    [InlineData("/tr/foo/bar")]
    [InlineData("/tr/cars/" + Slug + "/x")]
    [InlineData("/tr/admin")]
    [InlineData("/tr/newsletter")]
    [InlineData("/tr/newsletter/foo")]
    public async Task Doner404_TaninmayanYollar(string adres) =>
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync(adres)).Yanit.StatusCode);

    [Fact(DisplayName = "0004/AC-90: `/tr/newsletter/confirm` ve `/tr/newsletter/unsubscribe` belgeleri 200 döner ve `noindex` taşır (AC-70).")]
    public async Task Doner200_BultenSayfalari()
    {
        foreach (string adres in new[] { "/tr/newsletter/confirm", "/tr/newsletter/unsubscribe" })
        {
            (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync(adres);
            Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
            Assert.Equal("noindex", belge.Meta("robots"));
        }
    }

    [Fact(DisplayName = "0004/AC-77: 404 belgeleri `<meta name=\"robots\" content=\"noindex\">` taşır. Kanonik, dil alternatifi, `og:` etiketi ve JSON-LD taşımaz.")]
    public async Task Belge404_Etiketleri()
    {
        foreach (string adres in new[] { "/tr/cars/hic-olmamis-ilan-000000", "/tr/yok-boyle-sayfa", "/tr/foo/bar" })
        {
            IDocument belge = (await GetAsync(adres)).Belge;
            Assert.Equal("noindex", belge.Meta("robots"));
            Assert.Empty(belge.Kanonikler());
            Assert.Empty(belge.Alternatifler());
            Assert.Empty(belge.QuerySelectorAll("meta[property^='og:']"));
            Assert.Empty(belge.JsonLd());
        }
    }

    [Fact(DisplayName = "0004/AC-78: Etkin olmayan önekin (`/de/`) isteği bugünkü gibi boş gövdeli 404 döner.")]
    public async Task Doner404_EtkinOlmayanOnek()
    {
        (HttpResponseMessage yanit, _, string html) = await GetAsync("/de/");

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
        Assert.Equal(string.Empty, html);
    }

    [Fact(DisplayName = "0004/AC-91: Hiç var olmamış bir ilanın, gizli bir ilanın ve hiç var olmamış bir içerik sayfasının 404 belgelerinin `<title>`'ı aynıdır: `{tr content.notFound.title} · {görünen ad}`.")]
    public async Task Baslik404_Tek()
    {
        string beklenen = $"{Katalog("tr", "content.notFound.title")} · Oto Galeri";

        Assert.Equal(beklenen, (await GetAsync("/tr/cars/hic-olmamis-ilan-000000")).Belge.Baslik());
        Assert.Equal(beklenen, (await GetAsync("/tr/cars/gizlenmis-ilan-2019-hid001")).Belge.Baslik());
        Assert.Equal(beklenen, (await GetAsync("/tr/yok-boyle-sayfa")).Belge.Baslik());
    }

    [Fact(DisplayName = "0004/AC-88: tr'de yayına alınmış ama hiç girdisi olmayan bir SSS sayfasının `/tr/{slug}` isteği 404 döner.")]
    public async Task Doner404_GirdisizSss()
    {
        // Girdisiz SSS'yi `FindBySlugAsync` döndürmez (`0017` R20; `Content.Tests`/`SitemapSayfalariTests`).
        // Belge o okumanın sonucunu izler: okuma yoksa 404.
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync("/tr/sss")).Yanit.StatusCode);
    }

    [Fact(DisplayName = "0004/AC-79: `/tr/`, `/tr/cars`, görünür bir ilanın detayı ve yayındaki bir içerik sayfası 200 döner.")]
    public async Task Doner200_GorunurSayfalar()
    {
        foreach (string adres in new[] { "/tr/", "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk" })
        {
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(adres)).Yanit.StatusCode);
        }
    }

    /// <summary>
    /// QA 2 BULGU-2 — sunucu yolu ÇÖZÜLMÜŞ hâliyle sınıflandırır (<c>PathString</c>); uç da artık
    /// bölüm adını çözerek karşılaştırıyor (`route.test.ts`). İki taraf aynı rotayı söyler.
    /// </summary>
    [Theory]
    [InlineData("/tr/c%61rs", "List", null)]
    [InlineData("/tr/c%61rs/" + Slug, "Detail", Slug)]
    [InlineData("/tr/%61dmin", "NotFound", null)]
    public void Siniflandirir_CozulmusYolla(string hamYol, string tur, string? slug) =>
        Assert.Equal(new SeoPage(Enum.Parse<SeoPageKind>(tur), slug), SeoPage.Of(PathString.FromUriComponent(hamYol), "tr"));

    [Fact]
    public async Task Ayni_KodlanmisBolumAdi_Detay()
    {
        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync($"/tr/c%61rs/{Slug}");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal([$"{K}/tr/cars/{Slug}"], belge.Kanonikler());
    }

    // ── Korunan davranış ───────────────────────────────────────────────────

    [Fact(DisplayName = "0004/AC-80: Görselli bir ilanın detay belgesindeki ön yükleme bağlantısı (`rel=\"preload\"`, `href`, `imagesrcset`, `imagesizes`, `fetchpriority=\"high\"`) `0016b`'nin kriterleriyle aynıdır ve belgede bu kartın eklediği tüm etiketlerden **önce** gelir.")]
    public async Task OnYukleme_KartinEtiketlerindenOnce()
    {
        IDocument belge = (await GetAsync($"/tr/cars/{Slug}")).Belge;
        IElement onYukleme = Assert.Single(belge.QuerySelectorAll("link[rel='preload']"));

        Assert.Equal($"/seed-media/{Slug}-0-display.webp", onYukleme.GetAttribute("href"));
        Assert.Equal($"/seed-media/{Slug}-0-thumbnail.webp 400w, /seed-media/{Slug}-0-display.webp 800w, /seed-media/{Slug}-0-large.webp 1200w", onYukleme.GetAttribute("imagesrcset"));
        Assert.Equal("(min-width: 1232px) 1200px, 100vw", onYukleme.GetAttribute("imagesizes"));
        Assert.Equal("high", onYukleme.GetAttribute("fetchpriority"));

        List<IElement> head = [.. belge.Head!.Children];
        IEnumerable<IElement> kartinEtiketleri = belge.Head.QuerySelectorAll(
            "title, meta[name='description'], link[rel='canonical'], link[rel='alternate'], meta[property^='og:'], script[type='application/ld+json']");

        Assert.NotEmpty(kartinEtiketleri);
        Assert.All(kartinEtiketleri, etiket => Assert.True(head.IndexOf(onYukleme) < head.IndexOf(etiket), etiket.OuterHtml));
    }

    [Fact(DisplayName = "0004/AC-81: `enabled-locales`, `gallery-display-name` ve `home-text-*` etiketleri bugünkü belgelerde bugünkü değerleriyle bulunur.")]
    public async Task Korunur_OncekiEtiketler()
    {
        ortam.AnaSayfaMetinleri["tr"] = new HomeTextsDto("Giriş başlığı", null, null, null, null, null);

        IDocument belge = (await GetAsync("/tr/")).Belge;

        Assert.Equal("tr ar en", belge.Meta("enabled-locales"));
        Assert.Equal("Oto Galeri", belge.Meta("gallery-display-name"));
        Assert.Equal("Giriş başlığı", belge.Meta("home-text-intro-title"));
    }

    [Fact(DisplayName = "0004/AC-82: 200 ve 404 vitrin belgelerinin her biri `Cache-Control: no-store` taşır.")]
    public async Task NoStore_200Ve404()
    {
        foreach (string adres in new[] { "/tr/", $"/tr/cars/{Slug}", "/tr/cars/hic-olmamis-ilan-000000", "/tr/foo/bar" })
        {
            Assert.True((await GetAsync(adres)).Yanit.Headers.CacheControl?.NoStore, adres);
        }
    }

    private static string Katalog(string dil, string anahtar)
    {
        string dosya = anahtar.StartsWith("seo.", StringComparison.Ordinal)
            ? Path.Combine(CozumKoku(), "frontend", "src", "shared", "i18n", "locales", "seo", $"{dil}.json")
            : Path.Combine(CozumKoku(), "frontend", "src", "shared", "i18n", "locales", $"{dil}.json");

        using JsonDocument katalog = JsonDocument.Parse(File.ReadAllText(dosya));
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
