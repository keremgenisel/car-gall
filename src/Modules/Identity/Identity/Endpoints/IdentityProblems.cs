using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Identity.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CarGallery.Modules.Identity.Endpoints;

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir (docs/conventions.md § 5). Gövde
/// KARARLI bir `errorCode` ve gerekirse yapısal parametre taşır; ÇEVRİLMİŞ METİN
/// TAŞIMAZ — uç kodu kendi kataloğundan çevirir (C8, AC-59).
///
/// `title` ve `detail` bilerek YOK: ASP.NET'in varsayılan İngilizce başlığı ("Unauthorized")
/// ziyaretçiye gösterilebilecek bir metin gibi görünür ve uçta yanlışlıkla basılabilir.
/// RFC 7807'nin tüm üyeleri opsiyoneldir.
/// </summary>
internal static class IdentityProblems
{
    /// <summary>
    /// C19 — **kimlik ile yetki ayrılır.** Oturumsuz istek `401`, oturumu olup yetkisi
    /// yetmeyen istek `403`. Uç yalnız `401`'de giriş ekranına yönlendirir; aksi hâlde
    /// yetkisi olmayan personel giriş–yönlendirme döngüsüne girerdi (AC-27).
    /// </summary>
    internal static JsonHttpResult<Dictionary<string, object?>> From(Error error)
    {
        int status = error.Code switch
        {
            IdentityErrors.NoSession => StatusCodes.Status401Unauthorized,
            IdentityErrors.InvalidCredentials => StatusCodes.Status401Unauthorized,
            IdentityErrors.PasswordChangeRequired => StatusCodes.Status403Forbidden,
            IdentityErrors.LastActiveManager => StatusCodes.Status409Conflict,

            // 0007/AC-13 — eşzamanlılık kaybı da bir DURUM ÇAKIŞMASIDIR ve `409` döner.
            // Kural gereği redden AYRI bir kod taşır (AC-12): bu geçicidir, tekrar denemek
            // işe yarar; kural gereği red kalıcıdır ve tekrar denemek asla işe yaramaz.
            ConcurrencyErrors.Lost => StatusCodes.Status409Conflict,
            IdentityErrors.StaffNotFound => StatusCodes.Status404NotFound,
            IdentityErrors.RateLimited => StatusCodes.Status429TooManyRequests,
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
