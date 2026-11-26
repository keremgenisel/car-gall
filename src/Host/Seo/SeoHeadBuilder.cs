using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Formatting;
using CarGallery.Host.Localization;
using CarGallery.Modules.Content.Contracts.Home;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;

namespace CarGallery.Host.Seo;

/// <summary>Belgenin durum kodu ve <c>&lt;/head&gt;</c>'den önce basılacak etiketleri.</summary>
internal sealed record SeoHead(int StatusCode, string Tags);

/// <summary>
/// `0004` **P6** — belgenin sayfa türü, durum kodu ve bu kartın etiketleri TEK yerde: başlık,
/// açıklama, kanonik, dil alternatifleri, Open Graph, JSON-LD ve robots. Tam SSR yok (C1):
/// <see cref="IndexHtmlRenderer"/> kabuğu yazarken buradan alır. Host iş mantığı yazmaz (C3):
/// veriler modüllerin sözleşmelerinden, metinler <see cref="SeoCatalog"/>'tan gelir.
///
/// <para><b>Ön yükleme önce gelir</b> (`0016b` C13, AC-80): detayın ilk görselinin
/// <c>&lt;link rel="preload"&gt;</c>'i bu kartın bütün etiketlerinden önce basılır; LCP öğesi
/// odur ve keşfi bayt sırasına bağlıdır (D17).</para>
///
/// <para><b>Detay TEK okuma yapar</b> (**P5**): <c>FindAsync</c> hem 404 kararını, hem etiketleri,
/// hem ön yüklemeyi verir (<c>Images[0]</c>, <c>FindFirstImageAsync</c> ile aynı kural). Gizli ve
/// olmayan ilan aynı yoldan <c>null</c> döner (`0002` C13 · E7, AC-87). Okuma hata verirse belge
/// 200 ile basılır, ön yükleme <c>FindFirstImageAsync</c>'ten gelir ve detay etiketleri basılmaz
/// (`0020` R5a deseni); hata loglanır.</para>
/// </summary>
internal sealed class SeoHeadBuilder(
    SiteRoot root,
    SeoCatalog catalog,
    LocalePrefixResolver locales,
    ILogger<SeoHeadBuilder> logger)
{
    /// <summary>`0004` **D11**/**D20** — başlıktaki ayraç.</summary>
    internal const string TitleSeparator = " · ";

    /// <summary>
    /// `0004` **D17** · **P10** — yapısal verinin <c>image</c> dizisinin üst sınırı. Ölçüldü: ilk
    /// görsel +298 B, sonraki her görsel +126 B. Ürün bugün ilan başına görsel sayısını
    /// sınırlamıyor, yani sınır olmadan bütçe hiçbir tavanla tutmazdı. AC-83'ün en kötü durumu
    /// üç görselle 5647 B (bütçe 6144 B, AP-08 revizyonu). Sıra ilandaki sıradır.
    /// </summary>
    internal const int MaxStructuredImages = 3;

    /// <summary>`0004` **D11** — içerik açıklamasının üst sınırı.</summary>
    internal const int DescriptionLimit = 160;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Arapça ve Türkçe harfler kaçırılmaz (bayt bütçesi, D17); `<`, `>`, `&` aşağıda ayrıca
        // kaçırılır (D16) — bu üçü JSON'da yalnız dize içinde geçebilir.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    internal async Task<SeoHead> BuildAsync(HttpContext context, Locale locale, string? displayName, HomeTextsDto? homeTexts)
    {
        SeoPage page = SeoPage.Of(context.Request.Path, locale.Code);
        string? ad = string.IsNullOrWhiteSpace(displayName) ? null : displayName;

        return page.Kind switch
        {
            SeoPageKind.Home => Home(locale.Code, ad, homeTexts, await GalleryAsync(context, locale.Code, ad)),
            SeoPageKind.List => List(context, locale.Code, ad),
            SeoPageKind.Detail => await DetailAsync(context, locale.Code, ad, page.Slug!),
            SeoPageKind.Content => await ContentAsync(context, locale.Code, ad, page.Slug!),
            SeoPageKind.Newsletter => new SeoHead(StatusCodes.Status200OK, Title(ad ?? Text(locale.Code, SeoCatalog.DocumentFallbackTitle))),
            SeoPageKind.Admin => new SeoHead(StatusCodes.Status200OK, Title(ad ?? Text(locale.Code, SeoCatalog.DocumentFallbackTitle)) + Robots("noindex")),
            _ => NotFound(locale.Code, ad),
        };
    }

    /// <summary>
    /// `0004` **D7** · **D11** — 404 belgesi: ilan ve içerik için TEK başlık (C6), <c>noindex</c>;
    /// kanonik, alternatif, Open Graph ve JSON-LD yok.
    /// </summary>
    private SeoHead NotFound(string locale, string? ad) =>
        new(StatusCodes.Status404NotFound, Title(WithName(Text(locale, SeoCatalog.NotFoundTitle), ad)) + Robots("noindex"));

    private SeoHead Home(string locale, string? ad, HomeTextsDto? homeTexts, PublicGalleryDto? gallery)
    {
        string title = ad ?? Text(locale, SeoCatalog.HomeFallbackTitle);
        string description = string.IsNullOrWhiteSpace(homeTexts?.IntroBody)
            ? Text(locale, SeoCatalog.HomeIntroBody)
            : homeTexts.IntroBody;
        string canonical = root.Absolute(HomePath(locale));

        StringBuilder tags = new();
        tags.Append(Title(title)).Append(Description(description));
        AppendLinks(tags, canonical, AllLocales(HomePath));
        AppendOpenGraph(tags, title, description, canonical, ad, image: null);

        // D15 — galeri ad okunamazsa blok HİÇ basılmaz (AC-53).
        if (ad is not null && gallery is not null)
        {
            JsonObject dealer = new()
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "AutoDealer",
                ["name"] = ad,
                ["url"] = canonical,
            };

            if (gallery.Branches.Count > 0)
            {
                dealer["department"] = new JsonArray([.. gallery.Branches.Select(branch =>
                {
                    JsonObject department = new()
                    {
                        ["@type"] = "AutoDealer",
                        ["name"] = branch.Name,
                        ["address"] = branch.Address,
                        ["telephone"] = branch.Phone,
                    };

                    // `0025` D9 — yalnız programı girilmiş şubede; kök `AutoDealer`'da hiç yok (AC-56).
                    if (branch.OpeningHours is { } hours)
                    {
                        department["openingHoursSpecification"] = OpeningHoursSpecification(hours);
                    }

                    return (JsonNode)department;
                })]);
            }

            // `0024` D8 — altlığın gösterdiği hesaplar, aynı sırayla (C3); hiç yoksa anahtar yok.
            // Şube öğeleri `sameAs` taşımaz (AC-48).
            if (gallery.SocialLinks.Count > 0)
            {
                dealer["sameAs"] = new JsonArray([.. gallery.SocialLinks.Select(link => (JsonNode)JsonValue.Create(link.Url))]);
            }

            tags.Append(JsonLd(dealer));
        }

        return new SeoHead(StatusCodes.Status200OK, tags.ToString());
    }

    /// <summary>
    /// `0025` **D9** · P6 — (opens, closes) çiftini taşıyan günler tek öğede birleşir; öğeler çiftin
    /// İLK göründüğü güne (D6 sırası), aynı günde aralık sırasına göre dizilir. Program girilmişken
    /// kapalı günler <c>00:00–00:00</c>'lık tek öğededir ve aynı kurala uyar (Pazartesi kapalıysa
    /// ilk öğedir). Saatler şubenin yerel duvar saatidir; fark taşımaz (`0025` D2).
    /// </summary>
    private static JsonArray OpeningHoursSpecification(IReadOnlyDictionary<string, IReadOnlyList<PublicOpeningIntervalDto>> hours)
    {
        // schema.org'nun "bütün gün kapalı" gösterimi: opens = closes = gece yarısı. Bir mesai
        // VARSAYIMI değil, veriden okunan kapalı günün kodlaması (D9); `TimeOnly.MinValue`'dan
        // türetilir, metin sabiti olarak yazılmaz (Yasak Listesi 34'ün kapısı `"HH:mm"` sabitini arar).
        string midnight = TimeOnly.MinValue.ToString("HH:mm", CultureInfo.InvariantCulture);
        List<(string Opens, string Closes, List<string> Days)> groups = [];

        foreach ((string day, IReadOnlyList<PublicOpeningIntervalDto> intervals) in hours)
        {
            string schemaDay = char.ToUpperInvariant(day[0]) + day[1..];
            IEnumerable<(string, string)> pairs = intervals.Count == 0
                ? [(midnight, midnight)]
                : intervals.Select(interval => (interval.Start, interval.End));

            foreach ((string opens, string closes) in pairs)
            {
                int index = groups.FindIndex(group => group.Opens == opens && group.Closes == closes);

                if (index < 0)
                {
                    groups.Add((opens, closes, [schemaDay]));
                }
                else
                {
                    groups[index].Days.Add(schemaDay);
                }
            }
        }

        return new JsonArray([.. groups.Select(group => (JsonNode)new JsonObject
        {
            ["@type"] = "OpeningHoursSpecification",
            ["dayOfWeek"] = new JsonArray([.. group.Days.Select(day => (JsonNode)JsonValue.Create(day))]),
            ["opens"] = group.Opens,
            ["closes"] = group.Closes,
        })]);
    }

    /// <summary>
    /// `0004` **D23** — listenin okuduğu bir parametre adreste varsa belge <c>noindex, follow</c>;
    /// kanonik, alternatif ve <c>og:url</c> yok. Kümede olmayan parametre (<c>utm_source</c>) yok
    /// sayılır. Anahtar eşleşmesi ORDİNAL: uç parametreleri harfe duyarlı okur.
    /// </summary>
    private SeoHead List(HttpContext context, string locale, string? ad)
    {
        string title = WithName(Text(locale, SeoCatalog.ListTitle), ad);
        string description = Text(locale, SeoCatalog.ListDescription);
        bool daraltilmis = context.Request.Query.Keys.Any(key => catalog.ListQueryParameters.Contains(key));

        StringBuilder tags = new();
        tags.Append(Title(title)).Append(Description(description));

        if (daraltilmis)
        {
            tags.Append(Robots("noindex, follow"));
            AppendOpenGraph(tags, title, description, url: null, ad, image: null);
        }
        else
        {
            string canonical = root.Absolute(ListPath(locale));
            AppendLinks(tags, canonical, AllLocales(ListPath));
            AppendOpenGraph(tags, title, description, canonical, ad, image: null);
        }

        return new SeoHead(StatusCodes.Status200OK, tags.ToString());
    }

    private async Task<SeoHead> DetailAsync(HttpContext context, string locale, string? ad, string slug)
    {
        IShowcaseListingDetailQueries queries = context.RequestServices.GetRequiredService<IShowcaseListingDetailQueries>();
        ShowcaseListingDetailDto? detail;

        try
        {
            detail = await queries.FindAsync(slug, locale, context.RequestAborted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Kişisel veri yok: slug ilanın public adresidir. Belge düşmez, detay etiketleri basılmaz.
            logger.LogWarning(exception, "İlan detayı belge için okunamadı ({Slug}); belge detay etiketleri olmadan basılıyor.", slug);

            ShowcaseImageDto? first = await queries.FindFirstImageAsync(slug, context.RequestAborted);

            return new SeoHead(
                StatusCodes.Status200OK,
                (first is null ? string.Empty : Preload(first)) + Title(ad ?? Text(locale, SeoCatalog.DocumentFallbackTitle)));
        }

        if (detail is null)
        {
            return NotFound(locale, ad);
        }

        string year = ShowcaseFormat.Year(detail.ModelYear, locale);
        string title = WithName($"{detail.ModelName} {year}", ad);
        string description = catalog.Format(locale, SeoCatalog.DetailDescription, new Dictionary<string, string>
        {
            ["year"] = year,
            ["mileage"] = catalog.Format(locale, SeoCatalog.MileageValue, new Dictionary<string, string>
            {
                ["kilometers"] = ShowcaseFormat.Mileage(detail.MileageKilometers, locale),
            }),
            ["price"] = detail.Price is { OnRequest: false, Amount: decimal amount, CurrencyCode: string currency, MinorUnitExponent: int exponent }
                ? ShowcaseFormat.Money(amount, currency, exponent, locale)
                : Text(locale, SeoCatalog.PriceOnRequest),
            ["branch"] = detail.BranchName,
        });

        string path(string dil) => $"/{dil}/{LocalePrefixMiddleware.ListSegment}/{Uri.EscapeDataString(detail.Slug)}";
        string canonical = root.Absolute(path(locale));
        string[] images = [.. detail.Images.Take(MaxStructuredImages).Select(image => Absolute(image.Large.Url))];

        StringBuilder tags = new();

        if (detail.Images.Count > 0)
        {
            tags.Append(Preload(detail.Images[0]));
        }

        tags.Append(Title(title)).Append(Description(description));
        AppendLinks(tags, canonical, AllLocales(path));
        AppendOpenGraph(tags, title, description, canonical, ad, images.FirstOrDefault());

        // D14 — kapalı alan listesi. Görselsiz ilanda `image` yok; "fiyat sorunuz"da `offers` yok.
        JsonObject car = new()
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Car",
            ["name"] = detail.ModelName,
            ["vehicleModelDate"] = detail.ModelYear.ToString(CultureInfo.InvariantCulture),
            ["mileageFromOdometer"] = new JsonObject
            {
                ["@type"] = "QuantitativeValue",
                ["value"] = detail.MileageKilometers,
                ["unitCode"] = "KMT",
            },
        };

        if (images.Length > 0)
        {
            car["image"] = new JsonArray([.. images.Select(image => (JsonNode)JsonValue.Create(image))]);
        }

        car["url"] = canonical;

        if (detail.Price is { OnRequest: false, Amount: decimal tutar, CurrencyCode: string kod, MinorUnitExponent: int us })
        {
            car["offers"] = new JsonObject
            {
                ["@type"] = "Offer",
                // C8 · tuzak 10 — dilden bağımsız: nokta ondalık, binlik ayracı yok, minor unit kadar ondalık.
                ["price"] = Math.Round(tutar, us, MidpointRounding.AwayFromZero)
                    .ToString("F" + us.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                ["priceCurrency"] = kod,
                ["availability"] = detail.IsReserved ? "https://schema.org/Reserved" : "https://schema.org/InStock",
                ["url"] = canonical,
            };
        }

        tags.Append(JsonLd(car));

        return new SeoHead(StatusCodes.Status200OK, tags.ToString());
    }

    /// <summary>
    /// `0004` **D7** · **D10** — yalnız o dilde GÖRÜNEN sayfa (`FindBySlugAsync`; girdisiz SSS
    /// dâhil değil) 200 alır. Alternatifler yalnız sayfanın görünür olduğu diller;
    /// <c>x-default</c> yalnız sayfa varsayılan dilde görünürse.
    /// </summary>
    private async Task<SeoHead> ContentAsync(HttpContext context, string locale, string? ad, string slug)
    {
        ContentPagePublicDto? sayfa = await context.RequestServices
            .GetRequiredService<IContentPublicQueries>()
            .FindBySlugAsync(locale, slug, context.RequestAborted);

        if (sayfa is null)
        {
            return NotFound(locale, ad);
        }

        string title = WithName(sayfa.Title, ad);
        string description = ContentDescription.From(sayfa.Body, DescriptionLimit);
        string canonical = root.Absolute(ContentPath(locale, slug));

        List<(string Locale, string Href)> alternatifler =
        [
            (locale, canonical),
            .. sayfa.Alternates.Select(diger => (diger.Locale, root.Absolute(ContentPath(diger.Locale, diger.Slug)))),
        ];

        StringBuilder tags = new();
        tags.Append(Title(title));

        if (description.Length > 0)
        {
            tags.Append(Description(description));
        }

        AppendLinks(tags, canonical, alternatifler);
        AppendOpenGraph(tags, title, description.Length > 0 ? description : null, canonical, ad, image: null);

        return new SeoHead(StatusCodes.Status200OK, tags.ToString());
    }

    private async Task<PublicGalleryDto?> GalleryAsync(HttpContext context, string locale, string? ad)
    {
        if (ad is null)
        {
            return null;
        }

        try
        {
            return await context.RequestServices
                .GetRequiredService<IPublicGalleryQueries>()
                .GetAsync(locale, context.RequestAborted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Galeri bilgisi okunamadı; ana sayfa yapısal verisiz basılıyor.");
            return null;
        }
    }

    private string Text(string locale, string key) => catalog.Text(locale, key);

    private IEnumerable<(string Locale, string Href)> AllLocales(Func<string, string> path) =>
        locales.Enabled.Select(dil => (dil, root.Absolute(path(dil))));

    /// <summary>
    /// D9 · D10 · D20 — kanonik ve dil alternatifleri. <c>x-default</c> varsayılan dilin
    /// adresidir ve yalnız o dil kümede varsa basılır.
    /// </summary>
    private void AppendLinks(StringBuilder tags, string canonical, IEnumerable<(string Locale, string Href)> alternates)
    {
        tags.Append($"<link rel=\"canonical\" href=\"{Attr(canonical)}\" />");
        string? varsayilan = null;

        foreach ((string dil, string href) in alternates)
        {
            tags.Append($"<link rel=\"alternate\" hreflang=\"{Attr(dil)}\" href=\"{Attr(href)}\" />");

            if (dil == locales.Default.Code)
            {
                varsayilan = href;
            }
        }

        if (varsayilan is not null)
        {
            tags.Append($"<link rel=\"alternate\" hreflang=\"x-default\" href=\"{Attr(varsayilan)}\" />");
        }
    }

    /// <summary>D13 — <c>og:type</c> her belgede <c>website</c>; <c>og:locale</c> basılmaz (X13).</summary>
    private static void AppendOpenGraph(StringBuilder tags, string title, string? description, string? url, string? siteName, string? image)
    {
        tags.Append(Meta("og:title", title));

        if (description is not null)
        {
            tags.Append(Meta("og:description", description));
        }

        if (url is not null)
        {
            tags.Append(Meta("og:url", url));
        }

        tags.Append(Meta("og:type", "website"));

        if (siteName is not null)
        {
            tags.Append(Meta("og:site_name", siteName));
        }

        if (image is not null)
        {
            tags.Append(Meta("og:image", image));
        }
    }

    /// <summary>D13 — göreli medya adresi sitenin köküyle tamamlanır; mutlak adres olduğu gibi kalır.</summary>
    private string Absolute(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? mutlak) && mutlak.Scheme is "http" or "https"
            ? url
            : root.Absolute(url.StartsWith('/') ? url : "/" + url);

    private static string WithName(string text, string? ad) => ad is null ? text : text + TitleSeparator + ad;

    internal static string HomePath(string locale) => $"/{locale}/";

    internal static string ListPath(string locale) => $"/{locale}/{LocalePrefixMiddleware.ListSegment}";

    internal static string ContentPath(string locale, string slug) => $"/{locale}/{Uri.EscapeDataString(slug)}";

    private static string Title(string text) => $"<title>{TextEncode(text)}</title>";

    private static string Description(string text) => $"<meta name=\"description\" content=\"{Attr(text)}\" />";

    private static string Robots(string value) => $"<meta name=\"robots\" content=\"{value}\" />";

    private static string Meta(string property, string value) => $"<meta property=\"{property}\" content=\"{Attr(value)}\" />";

    /// <summary>
    /// D16 · tuzak 3 — JSON-LD'de <c>&lt;</c>, <c>&gt;</c>, <c>&amp;</c> kaçırılır; hiçbir değer
    /// <c>&lt;/script&gt;</c>'i kapatamaz. Üçü JSON'da yalnız dize içinde geçer, yani değiştirmek
    /// JSON'u bozmaz.
    /// </summary>
    private static string JsonLd(JsonObject value)
    {
        string json = value.ToJsonString(JsonOptions)
            .Replace("<", "\\u003c", StringComparison.Ordinal)
            .Replace(">", "\\u003e", StringComparison.Ordinal)
            .Replace("&", "\\u0026", StringComparison.Ordinal);

        return $"<script type=\"application/ld+json\">{json}</script>";
    }

    /// <summary>
    /// `0016b` C13 — ilk görselin ön yükleme bağlantısı. <c>imagesrcset</c>/<c>imagesizes</c>
    /// uçtaki galeriyle BİREBİR aynı (`Gallery.tsx`); farklı olursa görsel iki kez iner.
    /// </summary>
    internal static string Preload(ShowcaseImageDto gorsel)
    {
        HashSet<int> gorulen = [];
        string srcset = string.Join(
            ", ",
            new[] { gorsel.Thumbnail, gorsel.Display, gorsel.Large }
                .Where(turev => gorulen.Add(turev.Width))
                .Select(turev => $"{turev.Url} {turev.Width.ToString(CultureInfo.InvariantCulture)}w"));

        return $"<link rel=\"preload\" as=\"image\" href=\"{WebUtility.HtmlEncode(gorsel.Display.Url)}\" "
            + $"imagesrcset=\"{WebUtility.HtmlEncode(srcset)}\" imagesizes=\"{IndexHtmlRenderer.GallerySizes}\" fetchpriority=\"high\" />";
    }

    private static string Attr(string value) => IndexHtmlRenderer.AttributeEncode(value);

    private static string TextEncode(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);
}
