using System.Net;
using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using CarGallery.Host.Localization;
using CarGallery.Host.Seo;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0026` **D3** · **D5**–**D7** · **D10** · **D11** — Host'un logo çözümü ve belgedeki izleri, sunucu
/// yanıtı JavaScript çalıştırılmadan ayrıştırılarak (`0004` C13). `Media` ve `Dealership` sahte
/// okumayla (`SeoOrtami`); kaydetmeden sonraki ilk belge (AC-41) gerçek DB'yle zincir testinde.
/// </summary>
public sealed class GaleriLogosuBelgeTests(HostFactory factory, Xunit.Abstractions.ITestOutputHelper olcum)
    : IClassFixture<HostFactory>, IDisposable
{
    private const string Slug = "toyota-corolla-2019-abc123";
    private const string Gorselsiz = "fiat-egea-2021-img000";
    private const string Kok = "https://cdn.galeri.test/media/";

    private readonly SeoOrtami ortam = Ortam();

    private readonly Guid logo = Guid.CreateVersion7();

    public void Dispose() => ortam.Dispose();

    private static SeoOrtami Ortam()
    {
        SeoOrtami ortam = new();
        ortam.Ilanlar[(Slug, "*")] = SeoOrtami.Ilan(Slug);
        ortam.Ilanlar[(Gorselsiz, "*")] = SeoOrtami.Ilan(Gorselsiz, images: 0);
        ortam.Sayfalar[("tr", "kvkk")] = SeoOrtami.Sayfa("KVKK Aydınlatma Metni", "Kişisel verileriniz korunur.");
        return ortam;
    }

    private void Logolu(MediaAssetDto? varlik = null)
    {
        ortam.LogoKimligi = logo;
        ortam.LogoVarligi = varlik ?? SeoOrtami.LogoDto(logo, Kok);
    }

    private string Kucuk => $"{Kok}public/assets/{logo:D}/logo-small.webp";

    private string Buyuk => $"{Kok}public/assets/{logo:D}/logo-large.webp";

    private Task<(HttpResponseMessage Yanit, IDocument Belge, string Html)> GetAsync(string adres) =>
        SeoOrtami.GetAsync(ortam.Istemci(factory), adres);

    private static IElement? LogoMeta(IDocument belge) => belge.QuerySelector("meta[name='gallery-logo']");

    [Fact(DisplayName = "0026/AC-31, AC-32: `/tr/` belgesi JavaScript çalıştırılmadan `logo-small`'un adresini, gerçek genişliğini ve yüksekliğini taşır.")]
    public async Task Tasir_LogoBilgisi()
    {
        Logolu();

        IElement meta = LogoMeta((await GetAsync("/tr/")).Belge)!;

        Assert.Equal(Kucuk, meta.GetAttribute("content"));
        Assert.Equal(("320", "80"), (meta.GetAttribute("data-width"), meta.GetAttribute("data-height")));
    }

    [Fact(DisplayName = "0026/AC-34, AC-43: Logo atanmamış kurulumda belge bu karttan önceki hâlindedir (logo bilgisi yok) ve `Media`'ya hiç okuma yapılmaz.")]
    public async Task LogoYok_OkumaYok()
    {
        IDocument belge = (await GetAsync("/tr/")).Belge;

        Assert.Null(LogoMeta(belge));
        Assert.Null(belge.Og("og:image"));
        Assert.Equal(0, ortam.MedyaOkumasi);
    }

    [Theory(DisplayName = "0026/AC-35, AC-36, AC-37: İşleniyor, başarısız, fotoğraf türünde ya da `Media`'da olmayan atanmış kimlikte logo yoktur; belge 200 döner.")]
    [InlineData("islenyor")]
    [InlineData("basarisiz")]
    [InlineData("fotograf")]
    [InlineData("yok")]
    public async Task LogoYok_KullanilamazVarlik(string durum)
    {
        Logolu(durum switch
        {
            "islenyor" => SeoOrtami.LogoDto(logo, Kok, MediaProcessingStatus.Processing),
            "basarisiz" => SeoOrtami.LogoDto(logo, Kok, MediaProcessingStatus.Failed),
            "fotograf" => SeoOrtami.LogoDto(logo, Kok, tur: MediaAssetKind.Photo),
            _ => SeoOrtami.LogoDto(Guid.CreateVersion7(), Kok),
        });

        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync("/tr/");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Null(LogoMeta(belge));
        Assert.Null(belge.Og("og:image"));
        Assert.NotNull(belge.QuerySelector("meta[name='gallery-display-name']"));
    }

    [Fact(DisplayName = "0026/AC-38: Logo atanmış ama `Media` okuması hata verdiğinde belge 200 döner, ad etiketi durur ve logo yoktur.")]
    public async Task MedyaDuserse_Belge200_AdVar_LogoYok()
    {
        Logolu();
        ortam.MedyaOkunamaz = true;

        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync("/tr/");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal("Oto Galeri", belge.QuerySelector("meta[name='gallery-display-name']")?.GetAttribute("content"));
        Assert.Null(LogoMeta(belge));
        Assert.Null(belge.Og("og:image"));
        Assert.Equal(1, ortam.MedyaOkumasi);
    }

    [Fact(DisplayName = "0026/AC-39: Görünen ad okunamadığında logo atanmış olsa bile logo yoktur ve `Media` okunmaz.")]
    public async Task AdYoksa_LogoYok()
    {
        Logolu();
        ortam.AdOkunamaz = true;

        (HttpResponseMessage yanit, IDocument belge, _) = await GetAsync("/tr/");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Null(LogoMeta(belge));
        Assert.Equal(0, ortam.MedyaOkumasi);
    }

    [Fact(DisplayName = "0026/AC-40, AC-43: Ana sayfa, liste, detay, içerik ve 404 belgelerinin hepsi logo bilgisini taşır; her belge için `Media`'ya tam bir okuma.")]
    public async Task Tasir_HerBelge_TekOkuma()
    {
        Logolu();
        string[] adresler = ["/tr/", "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk", "/tr/olmayan-sayfa"];

        foreach ((string adres, int sira) in adresler.Select((adres, sira) => (adres, sira)))
        {
            Assert.Equal(Kucuk, LogoMeta((await GetAsync(adres)).Belge)?.GetAttribute("content"));
            Assert.Equal(sira + 1, ortam.MedyaOkumasi);
        }
    }

    [Fact(DisplayName = "0026/AC-42: Hiçbir belge logo için ön yükleme taşımaz; detayda ilk görselin ön yüklemesi bütün etiketlerden önce kalır.")]
    public async Task OnYukleme_YokLogo_DetaydaIlkGorselOnde()
    {
        Logolu();

        foreach (string adres in new[] { "/tr/", "/tr/cars", $"/tr/cars/{Slug}" })
        {
            string html = (await GetAsync(adres)).Html;
            Assert.DoesNotContain(html.Split("<link").Where(link => link.Contains("preload", StringComparison.Ordinal)), link => link.Contains("logo-", StringComparison.Ordinal));
        }

        string detay = (await GetAsync($"/tr/cars/{Slug}")).Html;
        int onYukleme = detay.IndexOf("rel=\"preload\"", StringComparison.Ordinal);
        Assert.True(onYukleme > 0 && onYukleme < detay.IndexOf("<title>", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0026/AC-46, AC-49: Görselli ilanın `og:image`'i logolu kurulumda da ilk görselin büyük türevidir; 404 belgesi `og:image` taşımaz.")]
    public async Task OgImage_GorselliIlan_Ve404()
    {
        Logolu();

        Assert.EndsWith("-0-large.webp", (await GetAsync($"/tr/cars/{Slug}")).Belge.Og("og:image"), StringComparison.Ordinal);
        Assert.Null((await GetAsync("/tr/olmayan-sayfa")).Belge.Og("og:image"));
    }

    [Fact(DisplayName = "0026/AC-50, AC-51: `AutoDealer.logo` `logo-large`'ın mutlak adresidir; `department` öğeleri logo taşımaz; logo yokken kök `logo` anahtarı yoktur.")]
    public async Task AutoDealer_Logo()
    {
        JsonElement logosuz = Assert.Single((await GetAsync("/tr/")).Belge.JsonLd());
        Assert.DoesNotContain("logo", logosuz.Anahtarlar());

        Logolu();
        JsonElement dealer = Assert.Single((await GetAsync("/tr/")).Belge.JsonLd());

        Assert.Equal(Buyuk, dealer.GetProperty("logo").GetString());
        Assert.All(dealer.GetProperty("department").EnumerateArray(), sube => Assert.DoesNotContain("logo", sube.Anahtarlar()));
    }

    [Fact(DisplayName = "0026/AC-53: Medya kökü `&` içerdiğinde `logo` JSON-LD'nin ham metninde `\\u0026` ile kodlanır; üst çubuk bilgisinin ve `og:image`'in özniteliği bozulmaz; ayrıştırılan değerler kurulan adrese eşittir.")]
    public async Task Kodlama_Ampersand()
    {
        const string KotuKok = "https://cdn.galeri.test/m?a=1&b=\"x\"/";
        Logolu(SeoOrtami.LogoDto(logo, KotuKok));
        string buyuk = $"{KotuKok}public/assets/{logo:D}/logo-large.webp";

        (_, IDocument belge, string html) = await GetAsync("/tr/");
        IElement betik = Assert.Single(belge.QuerySelectorAll("script[type='application/ld+json']"));

        Assert.Contains("\\u0026", betik.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("&", betik.TextContent, StringComparison.Ordinal);
        Assert.Equal(buyuk, JsonDocument.Parse(betik.TextContent).RootElement.GetProperty("logo").GetString());
        Assert.Equal(buyuk, belge.Og("og:image"));
        Assert.Equal($"{KotuKok}public/assets/{logo:D}/logo-small.webp", LogoMeta(belge)!.GetAttribute("content"));
        Assert.DoesNotContain("b=\"x\"", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0026/AC-54, AC-68: Hazır logolu `/ar/` ana sayfa belgesi, test yapılandırmasının medya köküyle, logosuz belgeden en çok 768 B büyüktür; belge altlık için ayrı bilgi taşımaz.")]
    public async Task Sigar_768Bayta()
    {
        int logosuz = Encoding.UTF8.GetByteCount((await GetAsync("/ar/")).Html);

        // Test yapılandırmasının medya kökü (`MediaTestSettings`): `http://localhost:54566/cargallery-media-test/`.
        Logolu(SeoOrtami.LogoDto(logo, "http://localhost:54566/cargallery-media-test/"));
        string html = (await GetAsync("/ar/")).Html;
        int logolu = Encoding.UTF8.GetByteCount(html);

        olcum.WriteLine($"Ölçülen fark: {logolu - logosuz} B (bütçe 768 B)");
        Assert.InRange(logolu - logosuz, 300, 768);
        Assert.Single(new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html).QuerySelectorAll("meta[name='gallery-logo']"));
    }

    /// <summary>
    /// D11(b) — `0004` AC-83'ün kurgusu (20 görsel, sınırdaki adlar ve slug) logolu kurulumda: Host'un
    /// belgeye eklediği logo bilgisi + bu kartın etiketleri. Görselli detayın Open Graph'ı değişmez.
    /// </summary>
    [Fact(DisplayName = "0026/AC-55: `0004` AC-83'ün kurgusu hazır logolu kurulumda da en çok 6144 B'tır.")]
    public async Task Sigar_6144Bayta_Logolu()
    {
        const string Medya = "https://media.uzun-bir-galeri-alanadi.com.tr/cargallery-media/public/assets/";
        string slug = new string('a', 153) + "-5c10b1";
        GalleryLogo logoBilgisi = new($"{Medya}{logo:D}/logo-small.webp", 320, 80, $"{Medya}{logo:D}/logo-large.webp");
        LocalePrefixResolver diller = new("tr", ["tr", "ar", "en"]);
        SeoHeadBuilder seo = new(
            SiteRoot.Parse("https://www.uzun-bir-galeri-alanadi.com.tr"),
            SeoCatalog.Load(AppContext.BaseDirectory, diller.Enabled),
            diller,
            NullLogger<SeoHeadBuilder>.Instance);
        ShowcaseListingDetailDto detay = new(
            slug,
            false,
            [.. Enumerable.Range(0, 20).Select(i => new ShowcaseImageDto(
                new ShowcaseImageVariantDto($"{Medya}{i:D12}/thumbnail.webp", 400, 300),
                new ShowcaseImageVariantDto($"{Medya}{i:D12}/display.webp", 800, 600),
                new ShowcaseImageVariantDto($"{Medya}{i:D12}/large.webp", 1200, 900)))],
            new string('م', 201),
            2021,
            999_999,
            new ShowcasePriceDto(false, 999_999_999.999m, "KWD", 3),
            new string('ف', 120),
            []);
        DefaultHttpContext context = new()
        {
            RequestServices = new ServiceCollection().AddSingleton<IShowcaseListingDetailQueries>(new Sabit(detay)).BuildServiceProvider(),
        };
        context.Request.Path = $"/ar/cars/{slug}";

        SeoHead head = await seo.BuildAsync(context, CarGallery.BuildingBlocks.Locale.Create("ar").Value, new string('ع', 120), null, logoBilgisi);
        string kart = head.Tags[head.Tags.IndexOf("<title>", StringComparison.Ordinal)..];
        string logoMeta = $"<meta name=\"{IndexHtmlRenderer.LogoMeta}\" content=\"{logoBilgisi.SmallUrl}\" data-width=\"320\" data-height=\"80\" />";
        int bayt = Encoding.UTF8.GetByteCount(kart) + Encoding.UTF8.GetByteCount(logoMeta);

        olcum.WriteLine($"Ölçülen: {bayt} B (bütçe 6144 B)");
        Assert.InRange(bayt, 3_000, 6_144);
        Assert.DoesNotContain("logo-large", kart, StringComparison.Ordinal);
    }

    private sealed class Sabit(ShowcaseListingDetailDto detay) : IShowcaseListingDetailQueries
    {
        public Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale, CancellationToken cancellationToken) =>
            Task.FromResult<ShowcaseListingDetailDto?>(detay);

        public Task<ShowcaseImageDto?> FindFirstImageAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(detay.Images.FirstOrDefault());
    }
}
