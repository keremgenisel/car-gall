using System.Text.Json;
using System.Threading.RateLimiting;
using CarGallery.Modules.Notifications.Application.Newsletter;
using CarGallery.Modules.Notifications.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace CarGallery.Modules.Notifications.Endpoints;

/// <summary>
/// **D9** — kaba istek sınırı: aynı IP'den pencerede HER istek sayılır, geçersizler dâhil
/// (AC-14). Reddi POLİTİKAYA bağlar: `RateLimiterOptions.OnRejected` globaldir ve iki modül
/// onu kurarsa son kayıt kazanır (`LeadRateLimitPolicy`'nin dersi).
///
/// IP yalnız bellekteki bölüm anahtarıdır; kalıcı kayda ve loga yazılmaz (AC-15).
/// </summary>
internal sealed class NewsletterRateLimitPolicy(NewsletterOptions options) : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["type"] = "about:blank",
                ["status"] = StatusCodes.Status429TooManyRequests,
                ["errorCode"] = NotificationErrors.SubscriptionRateLimited,
            }),
            cancellationToken);
    };

    public RateLimitPartition<string> GetPartition(HttpContext httpContext) =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.RequestLimit,
                Window = options.RequestWindow,
                QueueLimit = 0,
            });
}
