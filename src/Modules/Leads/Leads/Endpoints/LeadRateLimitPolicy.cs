using System.Text.Json;
using System.Threading.RateLimiting;
using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace CarGallery.Modules.Leads.Endpoints;

/// <summary>
/// Reddi POLİTİKAYA bağlar. `RateLimiterOptions.OnRejected` GLOBALDİR: iki modül de onu
/// kurarsa son kayıt kazanır ve bir modülün hata kodu diğerininkini bastırır — birleşik
/// koşuda giriş ucu `lead.rate_limited` döndürüyordu.
///
/// Her modül kendi reddini sahiplenir; global kancaya kimse dokunmaz.
/// </summary>
internal sealed class LeadRateLimitPolicy(LeadRateLimitOptions options) : IRateLimiterPolicy<string>
{
    public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected => async (context, cancellationToken) =>
    {
        // Gövde kararlı bir `errorCode` taşır ve teknik ayrıntı içermez. Varsayılan boş
        // 503 gövdesi bırakılsaydı uç, hız sınırını genel bir ağ hatasından ayıramazdı.
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        await context.HttpContext.Response.WriteAsync(
            JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["status"] = StatusCodes.Status429TooManyRequests,
                ["errorCode"] = LeadErrors.RateLimited,
            }),
            cancellationToken);
    };

    /// <summary>
    /// IP KİŞİSEL VERİDİR: yalnızca bellekteki sayaçta bölüm anahtarı olarak durur,
    /// kalıcı kayda ve loga YAZILMAZ (docs/conventions.md § 12).
    /// </summary>
    public RateLimitPartition<string> GetPartition(HttpContext httpContext) =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.RequestLimit,
                Window = TimeSpan.FromMinutes(options.WindowMinutes),
                QueueLimit = 0,
            });
}
