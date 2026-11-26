using System.Text;
using System.Text.Json;
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
/// `0024` **D8** · **D9**(a) — ana sayfa belgesinin <c>sameAs</c>'ı ve bayt bütçesi, sunucunun
/// yanıtı JavaScript çalıştırılmadan ayrıştırılarak (`0004` C13). Kalıcılıkla birlikte "kaydettikten
/// sonraki ilk belge" (AC-52) gerçek PostgreSQL'le `SosyalHesapZinciriTests`'te.
/// </summary>
public sealed class SosyalHesapBelgeTests(HostFactory factory, Xunit.Abstractions.ITestOutputHelper olcum)
    : IClassFixture<HostFactory>, IDisposable
{
    private const string Slug = "toyota-corolla-2019-abc123";

    private static readonly PublicSocialLinkDto[] UcHesap =
    [
        new("instagram", "https://www.instagram.com/otogaleri"),
        new("youtube", "https://www.youtube.com/@otogaleri"),
        new("linkedin", "https://www.linkedin.com/company/otogaleri"),
    ];

    private readonly SeoOrtami ortam = Ortam();

    public void Dispose() => ortam.Dispose();

    private static SeoOrtami Ortam()
    {
        SeoOrtami ortam = new();
        ortam.Ilanlar[(Slug, "*")] = SeoOrtami.Ilan(Slug);
        ortam.Sayfalar[("tr", "kvkk")] = SeoOrtami.Sayfa("KVKK Aydınlatma Metni", "Kişisel verileriniz korunur.");
        ortam.Galeri = ortam.Galeri! with
        {
            Branches = [new("Kadıköy", "Bağdat Cad. 1", "+902160000001"), new("Ataşehir", "Barbaros 7", "+902160000002")],
            SocialLinks = UcHesap,
        };
        return ortam;
    }

    private async Task<IDocument> BelgeAsync(string adres) => (await SeoOrtami.GetAsync(ortam.Istemci(factory), adres)).Belge;

    [Fact(DisplayName = "0024/AC-46: Instagram, YouTube ve LinkedIn hesaplı galerinin `/tr/` belgesinde `AutoDealer`'ın `sameAs`'ı tam olarak bu üç adrestir, bu sırayla.")]
    public async Task SameAs_UcHesap_D1Sirasiyla()
    {
        JsonElement dealer = Assert.Single((await BelgeAsync("/tr/")).JsonLd());

        Assert.Equal(UcHesap.Select(hesap => hesap.Url), dealer.GetProperty("sameAs").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact(DisplayName = "0024/AC-47: Hiç hesabı olmayan galerinin `/tr/` belgesinde `AutoDealer` `sameAs` anahtarı taşımaz.")]
    public async Task SameAs_Yok_HesapYokken()
    {
        ortam.Galeri = ortam.Galeri! with { SocialLinks = [] };

        JsonElement dealer = Assert.Single((await BelgeAsync("/tr/")).JsonLd());

        Assert.DoesNotContain("sameAs", dealer.Anahtarlar());
    }

    [Fact(DisplayName = "0024/AC-48: `department` öğelerinin hiçbiri `sameAs` taşımaz.")]
    public async Task SameAs_SubeOgelerindeYok()
    {
        JsonElement[] subeler = [.. Assert.Single((await BelgeAsync("/tr/")).JsonLd()).GetProperty("department").EnumerateArray()];

        Assert.Equal(2, subeler.Length);
        Assert.All(subeler, sube => Assert.DoesNotContain("sameAs", sube.Anahtarlar()));
    }

    [Fact(DisplayName = "0024/AC-49: `&` taşıyan adres JSON-LD'nin ham metninde `\\u0026` ile kodlanır; ayrıştırılan değer kayıtlı adrese eşittir.")]
    public async Task SameAs_AmpersandKodlanir()
    {
        const string Adres = "https://www.facebook.com/profile.php?id=1&sk=about";
        ortam.Galeri = ortam.Galeri! with { SocialLinks = [new("facebook", Adres)] };

        IElement betik = Assert.Single((await BelgeAsync("/tr/")).QuerySelectorAll("script[type='application/ld+json']"));

        Assert.Contains("profile.php?id=1\\u0026sk=about", betik.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("&", betik.TextContent, StringComparison.Ordinal);
        Assert.Equal(Adres, JsonDocument.Parse(betik.TextContent).RootElement.GetProperty("sameAs")[0].GetString());
    }

    [Fact(DisplayName = "0024/AC-50: Görünen ad okunamadığında, hesap kayıtlı olsa bile `/tr/` belgesi `AutoDealer` bloğu taşımaz.")]
    public async Task AutoDealer_Yok_AdOkunamazken_HesapliKurulumda()
    {
        ortam.AdOkunamaz = true;

        IDocument belge = await BelgeAsync("/tr/");

        Assert.Empty(belge.JsonLd());
        Assert.DoesNotContain("instagram.com", belge.Head!.InnerHtml, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0024/AC-51: Liste, detay ve içerik belgelerinde `sameAs` anahtarı taşıyan JSON-LD yoktur.")]
    public async Task SameAs_YalnizAnaSayfada()
    {
        foreach (string adres in new[] { "/tr/cars", $"/tr/cars/{Slug}", "/tr/kvkk" })
        {
            IDocument belge = await BelgeAsync(adres);
            Assert.DoesNotContain(belge.JsonLd(), json => json.Anahtarlar().Contains("sameAs"));
            Assert.DoesNotContain("instagram.com/otogaleri", belge.Head!.InnerHtml, StringComparison.Ordinal);
        }
    }

    [Fact(DisplayName = "0024/AC-53: `/tr/` belgesi hesap kayıtlıyken de tam olarak bir JSON-LD bloğu taşır.")]
    public async Task TekJsonLd_HesapliKurulumda()
    {
        Assert.Single((await BelgeAsync("/tr/")).QuerySelectorAll("script[type='application/ld+json']"));
    }

    /// <summary>
    /// D9(a) — altı platformun hepsi, her adres tam 200 bayt ve yolu Arapça harflerle dolu, hiçbirinde
    /// `&` yok (CLARIFY B3). Fark aynı kurulumun hesapsız `/ar/` belgesine göre, sıkıştırmasız UTF-8.
    /// Kök ve şubeler iki tarafta aynı: ölçülen yalnız bu kartın eklediği <c>sameAs</c>.
    /// </summary>
    [Fact(DisplayName = "0024/AC-54: Kurgulanmış en kötü durumda (altı hesap, her adres tam 200 bayt Arapça yollu, `&` yok) `/ar/` ana sayfa belgesi hesapsız belgeden en çok 1536 B büyüktür.")]
    public async Task Sigar_1536Bayta_EnKotuDurumda()
    {
        IReadOnlyList<PublicSocialLinkDto> alti = EnKotuHesaplar();
        Assert.All(alti, hesap => Assert.Equal(200, Encoding.UTF8.GetByteCount(hesap.Url)));
        Assert.All(alti, hesap => Assert.DoesNotContain('&', hesap.Url));

        int hesapsiz = Encoding.UTF8.GetByteCount((await EvAsync([])).Tags);
        int hesapli = Encoding.UTF8.GetByteCount((await EvAsync(alti)).Tags);

        olcum.WriteLine($"Ölçülen fark: {hesapli - hesapsiz} B (bütçe 1536 B)");
        Assert.InRange(hesapli - hesapsiz, 1_200, 1_536);
    }

    /// <summary>`0024` D1'in altı platformu; ana makineden sonra yol Arapça harfle 200 bayta tamamlanır.</summary>
    internal static IReadOnlyList<PublicSocialLinkDto> EnKotuHesaplar() =>
    [
        .. new (string Kod, string Kok)[]
        {
            ("instagram", "https://www.instagram.com/"),
            ("facebook", "https://www.facebook.com/"),
            ("youtube", "https://www.youtube.com/"),
            ("tiktok", "https://www.tiktok.com/"),
            ("x", "https://www.twitter.com/"),
            ("linkedin", "https://www.linkedin.com/"),
        }.Select(platform =>
        {
            int kalan = 200 - Encoding.UTF8.GetByteCount(platform.Kok);
            // Arapça harf iki bayt; tek kalan bayt ASCII ile kapanır.
            return new PublicSocialLinkDto(platform.Kod, platform.Kok + new string('ع', kalan / 2) + (kalan % 2 == 1 ? "a" : string.Empty));
        }),
    ];

    private static async Task<SeoHead> EvAsync(IReadOnlyList<PublicSocialLinkDto> hesaplar)
    {
        LocalePrefixResolver diller = new("tr", ["tr", "ar", "en"]);
        SeoHeadBuilder seo = new(
            SiteRoot.Parse("https://www.uzun-bir-galeri-alanadi.com.tr"),
            SeoCatalog.Load(AppContext.BaseDirectory, diller.Enabled),
            diller,
            NullLogger<SeoHeadBuilder>.Instance);

        PublicGalleryDto galeri = new(
            new string('ع', 120),
            [new(new string('ف', 120), new string('ش', 300), "+96550000001")],
            hesaplar);

        DefaultHttpContext context = new()
        {
            RequestServices = new ServiceCollection().AddSingleton<IPublicGalleryQueries>(new Sabit(galeri)).BuildServiceProvider(),
        };
        context.Request.Path = "/ar/";

        return await seo.BuildAsync(context, CarGallery.BuildingBlocks.Locale.Create("ar").Value, galeri.DisplayName, null);
    }

    private sealed class Sabit(PublicGalleryDto galeri) : IPublicGalleryQueries
    {
        public Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken) =>
            Task.FromResult<PublicGalleryDto?>(galeri);
    }
}
