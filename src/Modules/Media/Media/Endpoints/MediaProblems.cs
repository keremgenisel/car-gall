using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CarGallery.Modules.Media.Endpoints;

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir. Gövde KARARLI bir `errorCode` ve
/// gerekirse yapısal parametre taşır; çevrilmiş metin taşımaz — uç kendi kataloğundan
/// çevirir (C7, Yasak Listesi 23).
///
/// <para>`title` ve `detail` bilerek yok ve `TypedResults.Problem` bilerek kullanılmıyor:
/// o, gövdeye durum kodundan türetilmiş İngilizce bir `title` koyar ("Unprocessable
/// Content") ve uçta yanlışlıkla basılabilecek bir metin bırakır. `0005`, `0006` ve `0010a`
/// aynı kararı verdi; dördüncü bir dialekt açmıyoruz.</para>
/// </summary>
internal static class MediaProblems
{
    internal static JsonHttpResult<Dictionary<string, object?>> From(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        int status = error.Code switch
        {
            // İstek geçerli, hedef yok.
            MediaErrors.NotFound => StatusCodes.Status404NotFound,
            // AC-5 için `413` daha spesifik görünür ama hata sözleşmesini ikiye bölerdi:
            // uç tek bir `errorCode` okuma yolu bekliyor ve sınır aşımı bir doğrulama
            // hatasıdır (`docs/conventions.md` § 5).
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
