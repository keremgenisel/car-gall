using Microsoft.AspNetCore.Http;

namespace CarGallery.Modules.Identity.Endpoints.Authentication;

/// <summary>
/// D4/D19 — ara durumun çerezi. Oturum çerezinden **ayrı bir addır** ve onu yalnız
/// doğrulama ucu okur: tek bir çerezin iki anlamı olsaydı, ara durumu oturum sanan bir
/// sonraki kod parçası özelliğin tamamını geçersiz kılardı
/// (§ Bilinen tuzaklar 6).
///
/// Kurallar oturum çereziyle aynı (`SessionCookie`): `HttpOnly` — sayfaya sızan bir
/// script onu okuyamaz; `SameSite=Strict` — başka kökenden tetiklenen istek onu hiç
/// taşımaz. Ömrü **kısadır** (D19: varsayılan 5 dk) ve satırda yalnız özeti durur.
/// </summary>
internal static class PendingLoginCookie
{
    internal const string Name = "cargallery.pending_login";

    internal static void Write(HttpContext context, string token, DateTimeOffset expiresAt) =>
        context.Response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expiresAt,
        });

    internal static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
        });

    internal static string? Read(HttpContext context) =>
        context.Request.Cookies.TryGetValue(Name, out string? token) ? token : null;
}
