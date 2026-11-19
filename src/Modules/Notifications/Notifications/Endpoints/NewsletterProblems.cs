using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Notifications.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CarGallery.Modules.Notifications.Endpoints;

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir. Gövde KARARLI bir `errorCode` ve gerekirse
/// yapısal parametre (`field`) taşır; çevrilmiş metin, adres ya da anahtar TAŞIMAZ (C8,
/// AC-92). `TypedResults.Problem` kullanılmıyor: durum kodundan İngilizce bir `title`
/// türetiyor (`LeadProblems`'in aynı gerekçesi).
/// </summary>
internal static class NewsletterProblems
{
    /// <summary>
    /// **D33** — biçim ve dil hatası `422` (AC-7, AC-10; docs/conventions.md § 5), hız sınırı `429`, geçersiz bağlantı
    /// `404`; satır kilidi zaman aşımına uğradıysa `409` (docs/conventions.md § 5 — uç genel
    /// hata ve "tekrar dene" gösterir).
    /// </summary>
    internal static JsonHttpResult<Dictionary<string, object?>> From(Error error)
    {
        int status = error.Code switch
        {
            NotificationErrors.SubscriptionRateLimited => StatusCodes.Status429TooManyRequests,
            NotificationErrors.SubscriptionLinkInvalid => StatusCodes.Status404NotFound,
            ConcurrencyErrors.Lost => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        Dictionary<string, object?> body = new(StringComparer.Ordinal)
        {
            ["type"] = "about:blank",
            ["status"] = status,
            ["errorCode"] = error.Code,
        };

        if (error.Parameters is { Count: > 0 })
        {
            foreach (KeyValuePair<string, string> parameter in error.Parameters)
            {
                body[parameter.Key] = parameter.Value;
            }
        }

        return TypedResults.Json(body, contentType: "application/problem+json", statusCode: status);
    }
}
