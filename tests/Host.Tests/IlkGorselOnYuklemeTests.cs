using System.Text;
using CarGallery.BuildingBlocks;
using CarGallery.Host.Localization;
using CarGallery.Host.Theme;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

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

    [Fact]
    public async Task Basar_OnYukleme_DetayAdresindeHazirGorselVarken()
    {
        SahteDetay detay = new(Gorsel);

        string html = await RenderAsync("/tr/cars/toyota-corolla-2019-abc123", detay);

        Assert.Equal("toyota-corolla-2019-abc123", detay.SorulanAdres);
        Assert.Contains(
            "<link rel=\"preload\" as=\"image\" href=\"https://cdn.test/a/display.webp\" "
            + "imagesrcset=\"https://cdn.test/a/thumbnail.webp 400w, https://cdn.test/a/display.webp 800w, https://cdn.test/a/large.webp 1200w\" "
            + "imagesizes=\"(min-width: 1232px) 1200px, 100vw\" fetchpriority=\"high\" /></head>",
            html,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Basmaz_OnYukleme_GorselYokken()
    {
        string html = await RenderAsync("/tr/cars/olmayan-ilan-abc123", new SahteDetay(null));

        Assert.DoesNotContain("rel=\"preload\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sormaz_DetayDisindakiAdreslerde()
    {
        SahteDetay detay = new(Gorsel);

        foreach (string adres in new[] { "/tr/", "/tr/cars", "/ar/cars/" })
        {
            string html = await RenderAsync(adres, detay);
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

        string html = await RenderAsync("/tr/cars/kucuk-abc123", new SahteDetay(kucuk));

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

    private static async Task<string> RenderAsync(string adres, IShowcaseListingDetailQueries detay)
    {
        IndexHtmlRenderer renderer = new(new SabitWebRoot(), ThemeOptions.Default, new LocalePrefixResolver("tr", ["tr", "ar", "en"]));

        DefaultHttpContext context = new()
        {
            RequestServices = new ServiceCollection().AddSingleton(detay).BuildServiceProvider(),
        };
        context.Request.Path = adres;
        using MemoryStream govde = new();
        context.Response.Body = govde;

        await renderer.WriteAsync(context, Locale.Create(adres.Split('/', StringSplitOptions.RemoveEmptyEntries)[0]).Value);

        return Encoding.UTF8.GetString(govde.ToArray());
    }

    private sealed class SahteDetay(ShowcaseImageDto? gorsel) : IShowcaseListingDetailQueries
    {
        internal string? SorulanAdres { get; private set; }

        public Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Kabuk tam detayı sormamalı; yalnız ilk görseli.");

        public Task<ShowcaseImageDto?> FindFirstImageAsync(string slug, CancellationToken cancellationToken)
        {
            SorulanAdres = slug;
            return Task.FromResult(gorsel);
        }
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
