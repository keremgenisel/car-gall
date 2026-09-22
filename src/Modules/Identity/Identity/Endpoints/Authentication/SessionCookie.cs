using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Http;

namespace CarGallery.Modules.Identity.Endpoints.Authentication;

/// <summary>
/// C17/AC-29 — oturum bilgisi **sayfadaki JavaScript'ten okunabilir bir yerde
/// tutulmaz.** Çerez `HttpOnly`'dir; `localStorage` veya JS'e açık bir çerez
/// kullanılsaydı, sayfaya sızan tek bir script oturumu çalabilirdi.
///
/// `SameSite=Strict` AC-30'un tarayıcı tarafındaki yarısıdır: başka bir kökenden
/// tetiklenen istek çerezi hiç taşımaz. Sunucu tarafındaki yarısı
/// <see cref="CrossOriginGuard"/>'dadır — tek bir tarayıcı davranışına güvenmiyoruz.
/// </summary>
internal static class SessionCookie
{
    internal static void Write(HttpContext context, string token, DateTimeOffset expiresAt) =>
        context.Response.Cookies.Append(AdminSurface.SessionCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiresAt,
        });

    internal static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(AdminSurface.SessionCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });

    internal static string? Read(HttpContext context) =>
        context.Request.Cookies.TryGetValue(AdminSurface.SessionCookieName, out string? token) ? token : null;
}
