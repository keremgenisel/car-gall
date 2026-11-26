using System.Text;
using CarGallery.BuildingBlocks;
using CarGallery.Host.Localization;
using CarGallery.Host.Seo;
using CarGallery.Host.Theme;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0016b` C13 — kabuğun ilk görsel ön yüklemesi. Detayın LCP'si ön yükleme olmadan eşiği
/// aşıyordu (2552 ms, ölçüldü); bu testler bağlantının YALNIZ detay adresinde, YALNIZ
/// hazır görsel varken ve uçtakiyle aynı <c>srcset</c>/<c>sizes</c> ile basıldığını ölçer.
/// Sapma görseli iki kez indirtir — tarayıcı yarısını `e2e/galeri.spec.ts` AC-64 ölçer.
/// </summary>
public sealed class IlkGorselOnYuklemeTests
{
    private static readonly ShowcaseImageDto Gorsel = new(
        new ShowcaseImageVariantDto("https://cdn.test/a/thumbnail.webp", 400, 300),
        new ShowcaseImageVariantDto("https://cdn.test/a/display.webp", 800, 600),
        new ShowcaseImageVariantDto("https://cdn.test/a/large.webp", 1200, 900));

    /// <summary>
    /// `0004` **P5** ile bilerek değişti (Proje Yöneticisi şartı, 2026-11-26): kabuk artık detayı
    /// TEK okumayla (<c>FindAsync</c>) alır ve ön yüklemeyi onun ilk görselinden kurar. Bağlantının
    /// biçimi aynı; konumu `0004` AC-80 gereği <c>&lt;/head&gt;</c>'in hemen önü değil, kartın
    /// etiketlerinin (ilki <c>&lt;title&gt;</c>) hemen önü.
    /// </summary>
    [Fact]
    public async Task Basar_OnYukleme_DetayAdresindeHazirGorselVarken()
    {
        SahteDetay detay = new(Gorsel);

        (_, string html) = await RenderAsync("/tr/cars/toyota-corolla-2019-abc123", detay);

        Assert.Equal("toyota-corolla-2019-abc123", detay.SorulanAdres);
        Assert.Contains(
            "<link rel=\"preload\" as=\"image\" href=\"https://cdn.test/a/display.webp\" "
            + "imagesrcset=\"https://cdn.test/a/thumbnail.webp 400w, https://cdn.test/a/display.webp 800w, https://cdn.test/a/large.webp 1200w\" "
            + "imagesizes=\"(min-width: 1232px) 1200px, 100vw\" fetchpriority=\"high\" /><title>",
            html,
            StringComparison.Ordinal);
        Assert.False(detay.IlkGorselSoruldu, "Tam yolda ikinci okuma yapılmamalı (P5).");
    }

    /// <summary>
    /// `0004` **P5** — yedek yol: detay okuması hata verirse belge düşmez. 200 döner, ön yükleme
    /// <c>FindFirstImageAsync</c>'ten gelir, detay etiketleri basılmaz ve hata loglanır (sessiz
    /// yutma yok). Log yalnız ilanın public adresini taşır, kişisel veri taşımaz.
    /// </summary>
    [Fact]
    public async Task Basar_OnYukleme_DetayOkumasiDustugunde()
    {
        SahteDetay detay = new(Gorsel, okumaDuser: true);
        KayitTutanLog<SeoHeadBuilder> log = new();

        (int durum, string html) = await RenderAsync("/tr/cars/toyota-corolla-2019-abc123", detay, log);

        Assert.Equal(StatusCodes.Status200OK, durum);
        Assert.True(detay.IlkGorselSoruldu);
        Assert.Contains("<link rel=\"preload\" as=\"image\" href=\"https://cdn.test/a/display.webp\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("rel=\"canonical\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("application/ld+json", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"description\"", html, StringComparison.Ordinal);

        (LogLevel duzey, string mesaj, Exception? hata) = Assert.Single(log.Kayitlar);
        Assert.Equal(LogLevel.Warning, duzey);
        Assert.IsType<InvalidOperationException>(hata);
        Assert.Contains("toyota-corolla-2019-abc123", mesaj, StringComparison.Ordinal);
    }

    /// <summary>
    /// QA 2 BULGU-3 — yedek yolun ikinci okuması da düşerse belge yine düşmez: 200, ön yükleme
    /// yok, detay etiketleri yok. Bileşim hatası çizicide TEK bir Error olarak loglanır (önce
    /// bileşimin kendi Warning'i: ilk okuma); loglarda yalnız ilanın public adresi var, kişisel
    /// veri yok.
    /// </summary>
    [Fact]
    public async Task Basar_200_IkiOkumaDaDustugunde()
    {
        SahteDetay detay = new(Gorsel, okumaDuser: true, ilkGorselDuser: true);
        KayitTutanLog<SeoHeadBuilder> bilesim = new();
        KayitTutanLog<IndexHtmlRenderer> cizici = new();

        (int durum, string html) = await RenderAsync("/tr/cars/toyota-corolla-2019-abc123", detay, bilesim, cizici);

        Assert.Equal(StatusCodes.Status200OK, durum);
        Assert.True(detay.IlkGorselSoruldu);
        Assert.DoesNotContain("rel=\"preload\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("rel=\"canonical\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("application/ld+json", html, StringComparison.Ordinal);

        (LogLevel duzey, string mesaj, _) = Assert.Single(cizici.Kayitlar.Concat(bilesim.Kayitlar), kayit => kayit.Duzey == LogLevel.Error);
        Assert.Equal(LogLevel.Error, duzey);
        Assert.Single(bilesim.Kayitlar, kayit => kayit.Duzey == LogLevel.Warning);

        foreach ((_, string kayit, _) in cizici.Kayitlar.Concat(bilesim.Kayitlar))
        {
            Assert.DoesNotContain("@", kayit, StringComparison.Ordinal);
            Assert.DoesNotContain("+90", kayit, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("toyota-corolla-2019-abc123", mesaj, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Basmaz_OnYukleme_GorselYokken()
    {
        (_, string html) = await RenderAsync("/tr/cars/olmayan-ilan-abc123", new SahteDetay(null));

        Assert.DoesNotContain("rel=\"preload\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sormaz_DetayDisindakiAdreslerde()
    {
        SahteDetay detay = new(Gorsel);

        foreach (string adres in new[] { "/tr/", "/tr/cars", "/ar/cars/" })
        {
            (_, string html) = await RenderAsync(adres, detay);
            Assert.DoesNotContain("rel=\"preload\"", html, StringComparison.Ordinal);
        }

        Assert.Null(detay.SorulanAdres);
    }

    [Fact]
    public async Task YazmazAyniGenisligi_IkiKez()
    {
        // Küçük kaynakta üç türev aynı genişlikte olabilir; uç da tanımlayıcıyı bir kez yazar.
        ShowcaseImageDto kucuk = new(
            new ShowcaseImageVariantDto("https://cdn.test/k/thumbnail.webp", 64, 48),
            new ShowcaseImageVariantDto("https://cdn.test/k/display.webp", 64, 48),
            new ShowcaseImageVariantDto("https://cdn.test/k/large.webp", 64, 48));

        (_, string html) = await RenderAsync("/tr/cars/kucuk-abc123", new SahteDetay(kucuk));

        Assert.Contains("imagesrcset=\"https://cdn.test/k/thumbnail.webp 64w\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public void AyniSizesDegeri_UctakiGaleriyle()
    {
        // `sizes` iki dilde yazılı (C# kabuk, TS galeri); biri değişip öteki kalırsa ön yükleme
        // boşa iner ve görsel iki kez indirilir. Uçtaki dosya kaynaktan okunur.
        string galeri = File.ReadAllText(Path.Combine(
            CozumKoku(),
            "frontend", "src", "features", "catalog", "detail", "Gallery.tsx"));

        Assert.Contains($"sizes=\"{IndexHtmlRenderer.GallerySizes}\"", galeri, StringComparison.Ordinal);
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

    private static async Task<(int Durum, string Html)> RenderAsync(
        string adres,
        IShowcaseListingDetailQueries detay,
        ILogger<SeoHeadBuilder>? log = null,
        ILogger<IndexHtmlRenderer>? ciziciLog = null)
    {
        LocalePrefixResolver diller = new("tr", ["tr", "ar", "en"]);
        IndexHtmlRenderer renderer = new(new SabitWebRoot(), ThemeOptions.Default, diller, ciziciLog ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<IndexHtmlRenderer>.Instance);

        // `0004` P5 — ön yükleme artık tam yolda, SEO bileşiminin içinde ölçülür.
        SeoHeadBuilder seo = new(
            SiteRoot.Parse("https://galeri.test"),
            SeoCatalog.Load(AppContext.BaseDirectory, diller.Enabled),
            diller,
            log ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SeoHeadBuilder>.Instance);

        DefaultHttpContext context = new()
        {
            RequestServices = new ServiceCollection().AddSingleton(detay).AddSingleton(seo).BuildServiceProvider(),
        };
        context.Request.Path = adres;
        using MemoryStream govde = new();
        context.Response.Body = govde;

        await renderer.WriteAsync(context, Locale.Create(adres.Split('/', StringSplitOptions.RemoveEmptyEntries)[0]).Value);

        return (context.Response.StatusCode, Encoding.UTF8.GetString(govde.ToArray()));
    }

    /// <summary>
    /// Görünür ilanın sahte okuması; <c>gorsel</c> <c>null</c> ise ilan görünmüyor (<c>FindAsync</c>
    /// <c>null</c>). Gerçek okumanın biçimini taşır: görseller <c>Images</c>'ta, sırayla.
    /// </summary>
    private sealed class SahteDetay(ShowcaseImageDto? gorsel, bool okumaDuser = false, bool ilkGorselDuser = false) : IShowcaseListingDetailQueries
    {
        internal string? SorulanAdres { get; private set; }

        internal bool IlkGorselSoruldu { get; private set; }

        public Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale, CancellationToken cancellationToken)
        {
            SorulanAdres = slug;

            if (okumaDuser)
            {
                throw new InvalidOperationException("veritabanına ulaşılamadı");
            }

            return Task.FromResult<ShowcaseListingDetailDto?>(gorsel is null
                ? null
                : new ShowcaseListingDetailDto(
                    slug,
                    IsReserved: false,
                    [gorsel],
                    "Toyota Corolla",
                    2019,
                    125_000,
                    new ShowcasePriceDto(false, 1_250_000m, "TRY", 2),
                    "Kadıköy",
                    []));
        }

        public Task<ShowcaseImageDto?> FindFirstImageAsync(string slug, CancellationToken cancellationToken)
        {
            IlkGorselSoruldu = true;

            return ilkGorselDuser
                ? throw new InvalidOperationException("veritabanına yine ulaşılamadı")
                : Task.FromResult(gorsel);
        }
    }

    private sealed class KayitTutanLog<T> : ILogger<T>
    {
        internal List<(LogLevel Duzey, string Mesaj, Exception? Hata)> Kayitlar { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Kayitlar.Add((logLevel, formatter(state, exception), exception));
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
}
