using System.Security.Claims;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;

namespace CarGallery.Modules.Identity.Endpoints.Authentication;

/// <summary>
/// R10/AC-33 — **zorunlu parola değişimi bekleyen personelin yalnızca parola değiştirme
/// ve çıkış istekleri çalışır; diğer her yönetim isteği reddedilir.**
///
/// İzin verilen iki yol AÇIKÇA sayılır ve yönetim kökünün altındaki başka her yol
/// kapalıdır. Tersi (yasaklananları saymak) bir sonraki panel spec'i yeni bir uç
/// eklediğinde onu sessizce açık bırakırdı.
///
/// Karşılık `403`'tür, `401` değil: kimlik VARDIR, yalnız bu hâlde yetkisi yoktur.
/// `401` dönseydi uç giriş ekranına yönlendirir ve personel giriş–yönlendirme
/// döngüsüne girerdi (C19, AC-27).
/// </summary>
internal sealed class PasswordChangeGate(RequestDelegate next)
{
    internal const string OwnPasswordPath = $"{AdminSurface.RoutePrefix}/me/password";
    internal const string LogoutPath = "/api/identity/sessions/current";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsBlocked(context))
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
            ["errorCode"] = IdentityErrors.PasswordChangeRequired,
        });
    }

    private static bool IsBlocked(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(AdminSurface.RoutePrefix))
        {
            return false;
        }

        if (context.User.FindFirstValue(SessionAuthenticationHandler.MustChangePasswordClaim) != "true")
        {
            return false;
        }

        return !context.Request.Path.StartsWithSegments(OwnPasswordPath)
            && !context.Request.Path.StartsWithSegments(LogoutPath);
    }
}
