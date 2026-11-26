using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using CarGallery.Modules.Content.Contracts.Home;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0004` — belge kriterlerinin ortamı: GERÇEK HTTP boru hattı (ara katman, kabuk, durum kodu),
/// modüllerin okumaları sahte. Sahteler sözleşmenin gerçek biçimini taşır; veriyi kriterin
/// istediği uca (KWD, "fiyat sorunuz", gizli alan, `&lt;/script&gt;`'li ad) getirmek bu yüzden mümkün.
/// Belge JavaScript çalıştırılmadan AngleSharp ile ayrıştırılır (D22 a).
/// </summary>
internal sealed class SeoOrtami : IDisposable
{
    /// <summary>
    /// Kurulan her türetilmiş fabrika test bitince kapatılır. Kapatılmayan her fabrika bir
    /// yapılandırma dosyası izleyicisi açık bırakıyordu; Linux'ta (CI) inotify sınırı (128)
    /// dolunca SONRAKİ testlerin uygulaması başlamıyordu — ölçüldü, PR #126'nın ilk koşusu.
    /// </summary>
    private readonly List<IDisposable> fabrikalar = [];

    private HttpClient? varsayilan;

    internal const string Kok = "http://localhost:5000";

    internal Dictionary<string, string?> Adlar { get; } = new() { ["tr"] = "Oto Galeri", ["ar"] = "معرض السيارات", ["en"] = "Auto Gallery" };

    internal bool AdOkunamaz { get; set; }

    internal Dictionary<(string Slug, string Locale), ShowcaseListingDetailDto> Ilanlar { get; } = [];

    internal Dictionary<(string Locale, string Slug), ContentPagePublicDto> Sayfalar { get; } = [];

    internal Dictionary<string, HomeTextsDto> AnaSayfaMetinleri { get; } = [];

    internal PublicGalleryDto? Galeri { get; set; } = new("Oto Galeri", [new("Kadıköy", "Bağdat Cad. 1, İstanbul", "+902160000001")], []);

    internal int DetayOkumasi { get; private set; }

    internal int IlkGorselOkumasi { get; private set; }

    /// <summary>Ek ayar yoksa test boyunca TEK fabrika ve tek istemci kullanılır.</summary>
    internal HttpClient Istemci(HostFactory factory, Action<IWebHostBuilder>? ek = null)
    {
        if (ek is null && varsayilan is not null)
        {
            return varsayilan;
        }

        WebApplicationFactory<Program> turetilmis = factory.WithWebHostBuilder(builder =>
        {
            ek?.Invoke(builder);
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IGalleryDisplayNameQueries>(_ => new SahteAd(this));
                services.AddScoped<IPublicGalleryQueries>(_ => new SahteGaleri(this));
                services.AddScoped<IShowcaseListingDetailQueries>(_ => new SahteDetay(this));
                services.AddScoped<IContentPublicQueries>(_ => new SahteIcerik(this));
                services.AddScoped<IHomeTextQueries>(_ => new SahteMetin(this));
                services.AddScoped<IShowcaseSitemapQueries>(_ => new SahteSitemap(this));
            });
        });

        fabrikalar.Add(turetilmis);
        HttpClient istemci = turetilmis.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        if (ek is null)
        {
            varsayilan = istemci;
        }

        return istemci;
    }

    public void Dispose()
    {
        foreach (IDisposable fabrika in fabrikalar)
        {
            fabrika.Dispose();
        }

        fabrikalar.Clear();
        varsayilan = null;
    }

    internal static ShowcaseListingDetailDto Ilan(
        string slug,
        string modelName = "Toyota Corolla",
        ShowcasePriceDto? price = null,
        bool reserved = false,
        int images = 2,
        IReadOnlyList<ShowcaseAttributeDto>? attributes = null) =>
        new(
            slug,
            reserved,
            [.. Enumerable.Range(0, images).Select(index => index == 0 ? Gorsel($"/seed-media/{slug}-0") : Gorsel($"https://cdn.galeri.test/{slug}-{index}"))],
            modelName,
            2019,
            125_000,
            price ?? new ShowcasePriceDto(false, 1_250_000m, "TRY", 2),
            "Kadıköy",
            attributes ?? []);

    internal static ShowcaseImageDto Gorsel(string kok) => new(
        new ShowcaseImageVariantDto($"{kok}-thumbnail.webp", 400, 300),
        new ShowcaseImageVariantDto($"{kok}-display.webp", 800, 600),
        new ShowcaseImageVariantDto($"{kok}-large.webp", 1200, 900));

    internal static ContentPagePublicDto Sayfa(string title, string body, params ContentPageAlternateDto[] alternates) =>
        new(null, title, body, alternates, []);

    internal static async Task<(HttpResponseMessage Yanit, IDocument Belge, string Html)> GetAsync(HttpClient istemci, string adres, Action<HttpRequestMessage>? ayarla = null)
    {
        HttpRequestMessage istek = new(HttpMethod.Get, adres);
        ayarla?.Invoke(istek);
        HttpResponseMessage yanit = await istemci.SendAsync(istek);
        string html = await yanit.Content.ReadAsStringAsync();

        return (yanit, new HtmlParser().ParseDocument(html), html);
    }

    private sealed class SahteAd(SeoOrtami ortam) : IGalleryDisplayNameQueries
    {
        public Task<string?> FindDisplayNameAsync(string locale, CancellationToken cancellationToken) =>
            ortam.AdOkunamaz
                ? throw new InvalidOperationException("görünen ad okunamadı")
                : Task.FromResult(ortam.Adlar.GetValueOrDefault(locale));
    }

    private sealed class SahteGaleri(SeoOrtami ortam) : IPublicGalleryQueries
    {
        public Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken) => Task.FromResult(ortam.Galeri);
    }

    private sealed class SahteDetay(SeoOrtami ortam) : IShowcaseListingDetailQueries
    {
        public Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale, CancellationToken cancellationToken)
        {
            ortam.DetayOkumasi++;
            return Task.FromResult(ortam.Ilanlar.GetValueOrDefault((slug, locale)) ?? ortam.Ilanlar.GetValueOrDefault((slug, "*")));
        }

        public Task<ShowcaseImageDto?> FindFirstImageAsync(string slug, CancellationToken cancellationToken)
        {
            ortam.IlkGorselOkumasi++;
            return Task.FromResult(ortam.Ilanlar.GetValueOrDefault((slug, "*"))?.Images.FirstOrDefault());
        }
    }

    private sealed class SahteIcerik(SeoOrtami ortam) : IContentPublicQueries
    {
        public Task<ContentPagePublicDto?> FindBySlugAsync(string locale, string slug, CancellationToken cancellationToken) =>
            Task.FromResult(ortam.Sayfalar.GetValueOrDefault((locale, slug)));

        public Task<IReadOnlyList<ContentNavigationLinkDto>> NavigationAsync(string locale, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ContentNavigationLinkDto>>([]);

        public Task<string?> FindCurrentSlugAsync(string locale, string oldSlug, CancellationToken cancellationToken) =>
            Task.FromResult(oldSlug == "eski-kvkk" ? "kvkk" : null);

        public Task<IReadOnlyList<string>> ListVisibleSlugsAsync(string locale, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([.. ortam.Sayfalar.Keys.Where(key => key.Locale == locale).Select(key => key.Slug)]);
    }

    private sealed class SahteSitemap(SeoOrtami ortam) : IShowcaseSitemapQueries
    {
        public Task<IReadOnlyList<string>> ListVisibleSlugsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>([.. ortam.Ilanlar.Keys.Where(key => key.Locale == "*").Select(key => key.Slug).Order(StringComparer.Ordinal)]);
    }

    private sealed class SahteMetin(SeoOrtami ortam) : IHomeTextQueries
    {
        public Task<HomeTextsDto> FindAsync(string locale, CancellationToken cancellationToken) =>
            Task.FromResult(ortam.AnaSayfaMetinleri.GetValueOrDefault(locale) ?? new HomeTextsDto(null, null, null, null, null, null));
    }
}

/// <summary>Belgeden okuma yardımcıları — öğe düzeyinde, dize araması değil (D22).</summary>
internal static class Belge
{
    internal static string Baslik(this IDocument belge) => Assert.Single(belge.QuerySelectorAll("title")).TextContent;

    internal static string? Meta(this IDocument belge, string ad) =>
        belge.QuerySelector($"meta[name='{ad}']")?.GetAttribute("content");

    internal static string? Og(this IDocument belge, string ozellik) =>
        belge.QuerySelector($"meta[property='{ozellik}']")?.GetAttribute("content");

    internal static IReadOnlyList<string> Kanonikler(this IDocument belge) =>
        [.. belge.QuerySelectorAll("link[rel='canonical']").Select(link => link.GetAttribute("href") ?? string.Empty)];

    internal static IReadOnlyDictionary<string, string> Alternatifler(this IDocument belge) =>
        belge.QuerySelectorAll("link[rel='alternate'][hreflang]")
            .ToDictionary(link => link.GetAttribute("hreflang")!, link => link.GetAttribute("href")!);

    internal static IReadOnlyList<System.Text.Json.JsonElement> JsonLd(this IDocument belge) =>
        [.. belge.QuerySelectorAll("script[type='application/ld+json']")
            .Select(script => System.Text.Json.JsonDocument.Parse(script.TextContent).RootElement)];

    internal static IReadOnlyList<string> Anahtarlar(this System.Text.Json.JsonElement nesne) =>
        [.. nesne.EnumerateObject().Select(ozellik => ozellik.Name)];
}
