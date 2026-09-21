using CarGallery.BuildingBlocks;

namespace CarGallery.Host.Localization;

/// <summary>
/// Tam SSR yok; host index.html'i servis ederken rota bazlı lang/dir ve meta enjekte eder
/// (docs/architecture.md § 3, § 6). Yön locale'in özelliğidir ve SUNUCUDAN basılır;
/// uçta yeniden hesaplanmaz (docs/frontend.md § 6).
/// </summary>
internal sealed class IndexHtmlRenderer(IWebHostEnvironment environment)
{
    private const string Placeholder = "<html lang=\"\" dir=\"\">";

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

        html = html.Replace(Placeholder, $"<html lang=\"{locale.Code}\" dir=\"{direction}\">", StringComparison.Ordinal);

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }
}
