using CarGallery.BuildingBlocks;
using CarGallery.Host.Theme;

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
internal sealed class IndexHtmlRenderer(IWebHostEnvironment environment, ThemeOptions theme, LocalePrefixResolver locales)
{
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

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }
}
