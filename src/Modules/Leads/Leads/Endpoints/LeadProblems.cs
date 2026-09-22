using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CarGallery.Modules.Leads.Endpoints;

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir (docs/conventions.md § 5). Gövde
/// KARARLI bir `errorCode` ve gerekirse yapısal parametre taşır; ÇEVRİLMİŞ METİN TAŞIMAZ
/// — uç kodu kendi kataloğundan çevirir (Yasak Listesi 23, AC-34, AC-39).
///
/// `TypedResults.Problem` KULLANILMIYOR ve bu bilinçli: o, gövdeye durum kodundan
/// türetilmiş İngilizce bir `title` koyar ("Unprocessable Content"). RFC 7807'de `title`
/// "kısa, insan tarafından okunabilir özet"tir — yani tam olarak API'nin döndürmemesi
/// gereken şey. Uçta yanlışlıkla basılabilecek bir metin bırakmaktansa hiç göndermiyoruz;
/// RFC'nin tüm üyeleri opsiyoneldir.
/// </summary>
internal static class LeadProblems
{
    /// <summary>
    /// Doğrulama hataları `422`, hız sınırı `429`. `429` docs/conventions.md § 5'in
    /// listesine 0005 ile eklendi (C17).
    /// </summary>
    internal static JsonHttpResult<Dictionary<string, object?>> From(Error error)
    {
        int status = error.Code == LeadErrors.RateLimited
            ? StatusCodes.Status429TooManyRequests
            : StatusCodes.Status422UnprocessableEntity;

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
