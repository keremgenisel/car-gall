using System.Text;
using System.Text.Json;
using AngleSharp.Html.Parser;
using CarGallery.Host.Localization;
using CarGallery.Host.Seo;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0004` **D17** (a) · **P10** — detay belgesine bu kartın eklediği etiketlerin bayt bütçesi,
/// kurgulanmış en kötü durumda ve **ar** belgesinde (CLARIFY B12). Ölçüm sıkıştırmasız UTF-8.
/// </summary>
public sealed class SeoButceTests(Xunit.Abstractions.ITestOutputHelper olcum)
{
    /// <summary>
    /// Model adı alan sınırında: marka kanonik adı (80) + boşluk + model görünen adı (120)
    /// (`VehicleReference` `ReferenceNames`). Şube adı en uzun: 120 (`0020` AC-33). Arapça harf
    /// UTF-8'de iki bayttır. Görsel sayısının üründe üst sınırı YOK; 20 görsel, P10'un sınırının
    /// (3) çok üstünde. Kök ve medya adresi uzun bir alan adıyla, fiyat en uzun KWD tutarıyla.
    /// Görünen ad (120, Arapça; `<title>`, `og:title`, `og:site_name`) ve slug (160; sekiz adres)
    /// QA 2 BULGU-1 ile eklendi: ilk kurgu bu ikisini sınırın altında tutuyordu (3994 B ölçmüştü).
    /// Bugünkü ölçüm 5647 B.
    /// </summary>
    [Fact(DisplayName = "0004/AC-83: Kurgulanmış en kötü durumdaki bir ilanın **ar** detay belgesinde — model adı alan sınırındaki uzunlukta, görsel sayısı ilanın üst sınırında, şube adı en uzun, galerinin görünen adı alan sınırındaki uzunlukta (Arapça), slug en uzun — bu kartın eklediği etiketlerin (`<title>`, açıklama, kanonik, dil alternatifleri, `og:*`, JSON-LD) sıkıştırmasız toplam boyutu en çok 6144 bayttır.")]
    public async Task Sigar_4096Bayta_EnKotuDurumda()
    {
        SeoHead head = await BuildAsync(gorsel: 20);
        string kart = head.Tags[head.Tags.IndexOf("<title>", StringComparison.Ordinal)..];

        int bayt = Encoding.UTF8.GetByteCount(kart);

        olcum.WriteLine($"Ölçülen: {bayt} B (bütçe 6144 B)");
        Assert.InRange(bayt, 3_000, 6_144);

        JsonElement car = JsonDocument.Parse(new HtmlParser().ParseDocument($"<head>{kart}</head>")
            .QuerySelector("script[type='application/ld+json']")!.TextContent).RootElement;
        Assert.Equal(SeoHeadBuilder.MaxStructuredImages, car.GetProperty("image").GetArrayLength());
    }

    /// <summary>`ListingSlug.TryParse`'ın kabul ettiği en uzun slug: 160 karakter (QA 2 BULGU-1).</summary>
    private static readonly string Slug = new string('a', 153) + "-5c10b1";

    /// <summary>Görünen adın sınırı 120 karakter (`DealershipLimits.DisplayName`), Arapça (QA 2 BULGU-1).</summary>
    private static readonly string GorunenAd = new('ع', 120);

    private static async Task<SeoHead> BuildAsync(int gorsel)
    {
        const string Medya = "https://media.uzun-bir-galeri-alanadi.com.tr/cargallery-media/public/assets/0192c000-0000-7000-8000-";
        LocalePrefixResolver diller = new("tr", ["tr", "ar", "en"]);
        SeoHeadBuilder seo = new(
            SiteRoot.Parse("https://www.uzun-bir-galeri-alanadi.com.tr"),
            SeoCatalog.Load(AppContext.BaseDirectory, diller.Enabled),
            diller,
            NullLogger<SeoHeadBuilder>.Instance);

        ShowcaseListingDetailDto detay = new(
            Slug,
            false,
            [.. Enumerable.Range(0, gorsel).Select(i => new ShowcaseImageDto(
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
        context.Request.Path = $"/ar/cars/{detay.Slug}";

        return await seo.BuildAsync(context, CarGallery.BuildingBlocks.Locale.Create("ar").Value, GorunenAd, null);
    }

    private sealed class Sabit(ShowcaseListingDetailDto detay) : IShowcaseListingDetailQueries
    {
        public Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale, CancellationToken cancellationToken) =>
            Task.FromResult<ShowcaseListingDetailDto?>(detay);

        public Task<ShowcaseImageDto?> FindFirstImageAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(detay.Images.FirstOrDefault());
    }
}
