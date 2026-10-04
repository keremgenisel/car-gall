using System.Security.Claims;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Endpoints.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Identity.Endpoints;

internal sealed record LoginRequest(string? LoginId, string? Password);

/// <summary>
/// Oturumun yanıt gövdesi. Token BULUNMAZ: o yalnız HttpOnly çerezde taşınır (0006/AC-29).
///
/// <para><b>`StaffId` (0008 D1).</b> Ekranın listedeki KENDİ satırını tanıması gerekiyor:
/// "bu işlemden sonra panele erişemeyeceksiniz" uyarısı ancak böyle yazılabilir. `DisplayName`
/// tekil değil ve `LoginId` bu yanıtta hiç yok, yani başka güvenilir bir işaret kalmıyor.
/// Kimlik sunucuda zaten `ClaimTypes.NameIdentifier` olarak duruyor; yanıta konması yeni bir
/// uç değil, var olan bir yanıtın alanıdır.</para>
///
/// <para>Alan GİRİŞ yanıtında da görünüyor, yalnız `/me`'de değil: bu kayıt ikisi tarafından
/// paylaşılıyor ve uçtaki tek `CurrentSession` tipi ikisini birden tipliyor. Ayrı tutsaydık
/// tip, giriş yanıtının taşımadığı bir alanı taşıyormuş gibi gösterirdi — `docs/frontend.md`
/// § 2'nin "tip ile sözleşmenin ayrıştığı yer sessiz hata üreten yerdir" dediği durum.
/// Personelin kendi kimliğini kendisine söylemek bir sızıntı değil.</para>
/// </summary>
internal sealed record CurrentSessionResponse(
    Guid StaffId,
    string DisplayName,
    string Role,
    string PreferredLocale,
    bool MustChangePassword);

/// <summary>
/// Giriş ve çıkış. Bu uçlar yönetim kökünün (<see cref="AdminSurface.RoutePrefix"/>)
/// DIŞINDADIR ve bu bilinçlidir: giriş oturumsuz çalışmak zorundadır, oysa o kökün
/// altındaki her uç oturum ister (C18).
/// </summary>
internal static class SessionEndpoints
{
    internal const string RateLimitPolicy = "identity.login";

    internal static void MapSessionEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/identity/sessions", LoginAsync)
            .WithName("Login")
            .RequireRateLimiting(RateLimitPolicy)
            .Produces<CurrentSessionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        // R10 — zorunlu parola değişimi bekleyen personelin ÇIKIŞI çalışır.
        builder.MapDelete("/api/identity/sessions/current", LogoutAsync)
            .WithName("Logout")
            .RequireAuthorization(AdminSurface.AuthenticatedPolicy)
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<Results<Ok<CurrentSessionResponse>, JsonHttpResult<Dictionary<string, object?>>>>
        LoginAsync(
            LoginRequest request,
            HttpContext httpContext,
            Login handler,
            CancellationToken cancellationToken)
    {
        Result<LoginResult> result = await handler.HandleAsync(
            new LoginCommand(request.LoginId, request.Password, ResolveClientKey(httpContext)),
            cancellationToken);

        if (result.IsFailure)
        {
            return IdentityProblems.From(result.Error);
        }

        // Token yalnız çereze yazılır; gövdeye HİÇ girmez (C17, AC-29).
        SessionCookie.Write(httpContext, result.Value.Token, result.Value.Session.RefreshExpiresAt);

        return TypedResults.Ok(new CurrentSessionResponse(
            result.Value.Staff.Id.Value,
            result.Value.Staff.DisplayName,
            StaffRoleCode.ToCode(result.Value.Staff.Role),
            result.Value.Staff.PreferredLocale.Code,
            result.Value.Staff.MustChangePassword));
    }

    private static async Task<NoContent> LogoutAsync(
        HttpContext httpContext,
        Logout handler,
        CancellationToken cancellationToken)
    {
        string? sessionId = httpContext.User.FindFirstValue(SessionAuthenticationHandler.SessionIdClaim);

        if (Guid.TryParse(sessionId, out Guid parsed))
        {
            await handler.HandleAsync(parsed, cancellationToken);
        }

        SessionCookie.Clear(httpContext);

        return TypedResults.NoContent();
    }

    /// <summary>
    /// Kilitlenme sayacını bölen anahtar. Kaynak KİŞİSEL VERİDİR: yalnız sayaçta durur,
    /// loga yazılmaz (docs/conventions.md § 12).
    ///
    /// `X-Forwarded-For` burada OKUNMAZ: güvenilmeyen bir başlığa göre bölmek, sayacı
    /// istemcinin kendisinin kapatabilmesi demektir. Uygulama ters proxy arkasına
    /// konulursa gerçek adresi yazma işi boru hattının (Host) sorumluluğudur ve yalnızca
    /// açıkça listelenmiş proxy'lere güvenilerek yapılmalıdır.
    /// </summary>
    private static string ResolveClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
