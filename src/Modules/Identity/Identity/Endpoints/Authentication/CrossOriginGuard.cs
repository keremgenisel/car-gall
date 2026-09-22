using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;

namespace CarGallery.Modules.Identity.Endpoints.Authentication;

/// <summary>
/// C17/AC-30 — **durum değiştiren bir yönetim isteği başka bir kökenden
/// tetiklendiğinde reddedilir.**
///
/// `SameSite=Strict` çerezi zaten çapraz kökende taşımaz; bu kontrol onun YEDEĞİDİR.
/// Tek bir tarayıcı davranışına güvenmek, o davranışın gevşediği veya eski bir
/// istemcinin onu uygulamadığı gün korumayı sessizce kaldırırdı.
///
/// Yalnız `Origin` başlığına bakılır ve **yalnız güvenli olmayan metotlarda**: `GET` ve
/// `HEAD` durum değiştirmez ve tarayıcılar onlara çoğu zaman `Origin` göndermez.
/// </summary>
internal sealed class CrossOriginGuard(RequestDelegate next)
{
    private static readonly string[] SafeMethods = ["GET", "HEAD", "OPTIONS"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsCrossOriginStateChange(context))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["type"] = "about:blank",
            ["status"] = StatusCodes.Status403Forbidden,
            ["errorCode"] = IdentityErrors.NoSession,
        });
    }

    private static bool IsCrossOriginStateChange(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(AdminSurface.RoutePrefix)
            && !context.Request.Path.StartsWithSegments("/api/identity"))
        {
            return false;
        }

        if (Array.IndexOf(SafeMethods, context.Request.Method) >= 0)
        {
            return false;
        }

        string? origin = context.Request.Headers.Origin;

        if (string.IsNullOrEmpty(origin))
        {
            return false;
        }

        string expected = $"{context.Request.Scheme}://{context.Request.Host.Value}";

        return !string.Equals(origin, expected, StringComparison.OrdinalIgnoreCase);
    }
}
