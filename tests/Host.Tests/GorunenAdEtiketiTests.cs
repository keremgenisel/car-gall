using System.Text;
using CarGallery.BuildingBlocks;
using CarGallery.Host.Localization;
using CarGallery.Host.Theme;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0020` **P1** · **D16** — Host'un `index.html`'e bastığı görünen ad etiketi: metin olarak
/// kodlanır ve okuma düştüğünde etiket hiç basılmaz, sayfa basılır. Renderer kendi
/// bağımlılıklarıyla kuruluyor; boru hattından geçen hâli `GaleriYonetimiZinciriTests` ölçüyor.
/// </summary>
public sealed class GorunenAdEtiketiTests
{
    [Fact(DisplayName = "0020/AC-17: Görünen ad okunamadığında üst çubukta adın yeri boş kalır, yazılımın adı ya da teknik bir metin gösterilmez ve sayfanın geri kalanı çalışır (R5a, D16).")]
    public async Task BasilmazEtiket_SayfaBasilir_OkumaDustugunde()
    {
        (int durum, string html) = await RenderAsync(new DusenSorgu());

        Assert.Equal(StatusCodes.Status200OK, durum);
        Assert.Contains("<html lang=\"tr\" dir=\"ltr\">", html, StringComparison.Ordinal);
        Assert.DoesNotContain("gallery-display-name", html, StringComparison.Ordinal);
        Assert.DoesNotContain("veritabanı", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-17: Görünen ad okunamadığında üst çubukta adın yeri boş kalır, yazılımın adı ya da teknik bir metin gösterilmez ve sayfanın geri kalanı çalışır (R5a, D16).")]
    public async Task BasilmazEtiket_AdYoksa()
    {
        (_, string html) = await RenderAsync(new SabitSorgu(null));

        Assert.DoesNotContain("gallery-display-name", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-18: `<b>Galeri & \"Oto\"</b>` görünen adı üst çubukta ve giriş satırında bu karakterlerle **metin olarak** görünür; işaretleme olarak yorumlanmaz (R5a, D16).")]
    public async Task KodlanirMetinOlarak_OzelKarakterliAd()
    {
        (_, string html) = await RenderAsync(new SabitSorgu("<b>Galeri & \"Oto\"</b>"));

        Assert.Contains(
            "<meta name=\"gallery-display-name\" content=\"&lt;b&gt;Galeri &amp; &quot;Oto&quot;&lt;/b&gt;\" />",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Galeri", html, StringComparison.Ordinal);
    }

    private static async Task<(int Durum, string Html)> RenderAsync(IGalleryBrandingQueries sorgu)
    {
        IndexHtmlRenderer renderer = new(
            new SabitWebRoot(),
            ThemeOptions.Default,
            new LocalePrefixResolver("tr", ["tr", "ar", "en"]),
            NullLogger<IndexHtmlRenderer>.Instance);

        DefaultHttpContext context = new()
        {
            // `0026` D17 — Host belgeyi marka okumasıyla kurar (Proje Yöneticisi kararı, AP-08:
            // yalnız sahtenin tipi değişti; iddialar ve başlıklar aynı).
            RequestServices = new ServiceCollection().AddSingleton(sorgu).BuildServiceProvider(),
        };
        context.Request.Path = "/tr/";
        using MemoryStream govde = new();
        context.Response.Body = govde;

        await renderer.WriteAsync(context, Locale.Create("tr").Value);

        return (context.Response.StatusCode, Encoding.UTF8.GetString(govde.ToArray()));
    }

    private sealed class DusenSorgu : IGalleryBrandingQueries
    {
        public Task<GalleryBrandingDto?> FindBrandingAsync(string locale, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("veritabanı erişilemiyor");
    }

    private sealed class SabitSorgu(string? ad) : IGalleryBrandingQueries
    {
        public Task<GalleryBrandingDto?> FindBrandingAsync(string locale, CancellationToken cancellationToken) =>
            Task.FromResult<GalleryBrandingDto?>(new GalleryBrandingDto(ad, null));
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
