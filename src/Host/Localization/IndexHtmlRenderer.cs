using System.Net;
using CarGallery.BuildingBlocks;
using CarGallery.Host.Theme;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;

namespace CarGallery.Host.Localization;

/// <summary>
/// Tam SSR yok; host index.html'i servis ederken rota bazlı lang/dir ve meta enjekte eder
/// (docs/architecture.md § 3, § 6). Yön locale'in özelliğidir ve SUNUCUDAN basılır;
/// uçta yeniden hesaplanmaz (docs/frontend.md § 6).
///
/// <para><b>frontend-002 — kuruluma özel tema aynı kapıdan girer</b> (C4, D10). İkinci bir
/// mekanizma kurulmuyor: zaten değiştirdiğimiz <c>&lt;html&gt;</c> etiketine bir `style`
/// özniteliği ekleniyor. Panel de buradan geçiyor (<c>AdminShellRenderer</c> bu sınıfı
/// çağırıyor), yani tek yer hem vitrini hem paneli kapsıyor.</para>
///
/// <para><b>Neden `&lt;style&gt;` bloğu değil:</b> Vite, CSS <c>&lt;link&gt;</c>'ini build
/// sırasında <c>&lt;head&gt;</c>'e kendisi ekliyor. Bir <c>:root{…}</c> bloğu o link'ten
/// ÖNCE düşerse `tokens.css` temayı ezer — aynı özgüllükte son bildirim kazanır — ve bu
/// sessizce olur. Inline `style` özniteliği `:root` kuralını her zaman yener ve sıraya hiç
/// bakmaz.</para>
/// </summary>
internal sealed class IndexHtmlRenderer(
    IWebHostEnvironment environment,
    ThemeOptions theme,
    LocalePrefixResolver locales,
    ILogger<IndexHtmlRenderer> logger)
{
    /// <summary>`0020` P1 — üst çubuğun okuduğu etiketin adı (`frontend/src/shared/lib/galleryName.ts`).</summary>
    internal const string DisplayNameMeta = "gallery-display-name";

    private const string Placeholder = "<html lang=\"\" dir=\"\">";

    private const string HeadEnd = "</head>";

    public async Task WriteAsync(HttpContext context, Locale locale)
    {
        string indexPath = Path.Combine(environment.WebRootPath, "index.html");

        if (!File.Exists(indexPath))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        string html = await File.ReadAllTextAsync(indexPath, context.RequestAborted);
        string direction = locale.Direction is TextDirection.RightToLeft ? "rtl" : "ltr";

        // Tema verilmediğinde öznitelik HİÇ yazılmaz; arayüz `tokens.css`'in paletiyle
        // çalışır (R6, AC-10). Boş bir `style=""` yazmak, "tema var ama boş" diye okunan
        // üçüncü bir durum üretirdi.
        string themeAttribute = theme.IsDefault
            ? string.Empty
            : $" style=\"{theme.StyleAttributeValue}\"";

        html = html.Replace(
            Placeholder,
            $"<html lang=\"{locale.Code}\" dir=\"{direction}\"{themeAttribute}>",
            StringComparison.Ordinal);

        // 0014 D22 — uç etkin locale'leri bilmiyor (kodda sabit üç dil var); iki dilli bir
        // kurulumda altlığın üçüncü dil bağlantısı 404'e giderdi. Liste yapılandırmadan
        // basılır, ayrı bir istek gerekmez. `<html>` etiketine değil `<head>`'e yazılıyor:
        // `<html>` etiketinin biçimini tema testleri birebir ölçüyor.
        html = html.Replace(
            HeadEnd,
            $"<meta name=\"enabled-locales\" content=\"{string.Join(' ', locales.Enabled)}\" />{HeadEnd}",
            StringComparison.Ordinal);

        string ad = await GorunenAdEtiketiAsync(context, locale);

        if (ad.Length > 0)
        {
            html = html.Replace(HeadEnd, ad + HeadEnd, StringComparison.Ordinal);
        }

        string onYukleme = await IlkGorselOnYuklemesiAsync(context);

        if (onYukleme.Length > 0)
        {
            html = html.Replace(HeadEnd, onYukleme + HeadEnd, StringComparison.Ordinal);
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    /// <summary>
    /// `0020` **P1** · **D16** — galerinin ziyaretçinin dilindeki görünen adı, bir `&lt;meta&gt;`
    /// olarak. Üst çubuk onu ilk boyamada senkron okur: ad sonradan gelip üst çubuğu
    /// kaydırmaz (C6, AC-15) ve ayrı bir istek gerekmez. Önbellek yok — profil değişikliği bir
    /// sonraki belge isteğinde görünür (D6, AC-14).
    ///
    /// <para><b>Metin olarak:</b> değer HTML olarak kodlanır; <c>&lt;</c>, <c>"</c>, <c>&amp;</c>
    /// özniteliği kapatamaz ve işaretleme olarak yorumlanmaz (R5a, AC-18).</para>
    ///
    /// <para><b>Okunamazsa etiket hiç basılmaz</b> ve sayfa çalışmaya devam eder: üst çubukta
    /// adın yeri boş kalır, yazılımın adı ya da teknik metin gösterilmez (R5a, AC-17). Bu yüzden
    /// istisna burada yakalanır — global işleyiciye gitseydi ziyaretçi bir ad yüzünden boş bir
    /// hata sayfası görürdü. İstisna yutulmaz, loga yazılır.</para>
    /// </summary>
    private async Task<string> GorunenAdEtiketiAsync(HttpContext context, Locale locale)
    {
        string? ad;

        try
        {
            ad = await context.RequestServices
                .GetRequiredService<IGalleryDisplayNameQueries>()
                .FindDisplayNameAsync(locale.Code, context.RequestAborted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Galerinin görünen adı okunamadı; üst çubuk adsız basılıyor.");
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(ad)
            ? string.Empty
            : $"<meta name=\"{DisplayNameMeta}\" content=\"{AttributeEncode(ad)}\" />";
    }

    /// <summary>
    /// Öznitelik değeri için YALNIZ işaretleme karakterleri kodlanır. <c>WebUtility.HtmlEncode</c>
    /// Latin-1 harflerini de (<c>ü</c>, <c>ö</c>) sayısal varlığa çeviriyordu: tarayıcı onları
    /// çözer ama belgeyi okuyan her araç (test, arama motoru önizlemesi) bozuk bir ad görürdü.
    /// </summary>
    private static string AttributeEncode(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("'", "&#39;", StringComparison.Ordinal);

    /// <summary>
    /// `0016b` C13 — ilan detayının adresiyse galerinin İLK hazır görseli için bir ön yükleme
    /// bağlantısı; değilse boş metin. Ölçüldü: görsel JS paketi yüklenip detay isteği
    /// döndükten SONRA keşfediliyordu (HTML → JS → API → görsel zinciri) ve detayın mobil
    /// LCP medyanı 2552 ms ile eşiği aşıyordu. Ön yükleme görseli zincirin dışına alır.
    ///
    /// <para><b><c>imagesrcset</c> ve <c>imagesizes</c> uçtakiyle BİREBİR aynı</b>
    /// (`frontend/src/features/catalog/detail/Gallery.tsx`, <c>srcSet</c> ve <c>sizes</c>):
    /// farklı olursa tarayıcı iki ayrı aday seçer ve görsel İKİ KEZ indirilir. Bu sapmayı
    /// `e2e/galeri.spec.ts`'in AC-64'ü yakalar — etkileşimden önce galeriden tek istek.</para>
    ///
    /// <para>Görünmeyen ilan ile hiç olmayan ilan aynı yoldan geçer ve ikisi de bağlantı
    /// üretmez (`0002` C13'ün "ayırt edilemezlik" kuralı).</para>
    /// </summary>
    private static async Task<string> IlkGorselOnYuklemesiAsync(HttpContext context)
    {
        string[] parcalar = (context.Request.Path.Value ?? string.Empty)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parcalar.Length != 3 || parcalar[1] != LocalePrefixMiddleware.ListSegment)
        {
            return string.Empty;
        }

        ShowcaseImageDto? gorsel = await context.RequestServices
            .GetRequiredService<IShowcaseListingDetailQueries>()
            .FindFirstImageAsync(Uri.UnescapeDataString(parcalar[2]), context.RequestAborted);

        if (gorsel is null)
        {
            return string.Empty;
        }

        HashSet<int> gorulen = [];
        string srcset = string.Join(
            ", ",
            new[] { gorsel.Thumbnail, gorsel.Display, gorsel.Large }
                .Where(turev => gorulen.Add(turev.Width))
                .Select(turev => $"{turev.Url} {turev.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}w"));

        return $"<link rel=\"preload\" as=\"image\" href=\"{WebUtility.HtmlEncode(gorsel.Display.Url)}\" "
            + $"imagesrcset=\"{WebUtility.HtmlEncode(srcset)}\" imagesizes=\"{GallerySizes}\" fetchpriority=\"high\" />";
    }

    /// <summary>Galerinin `sizes` değeri; `Gallery.tsx` ile aynı (yukarıdaki not).</summary>
    internal const string GallerySizes = "(min-width: 1232px) 1200px, 100vw";
}
