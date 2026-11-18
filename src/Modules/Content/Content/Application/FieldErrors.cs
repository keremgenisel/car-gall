using CarGallery.BuildingBlocks;
using CarGallery.Modules.Content.Domain;

namespace CarGallery.Modules.Content.Application;

/// <summary>
/// `0017` C5/AC-119/AC-120 — bir istekteki BÜTÜN alan hatalarını toplar ve tek bir hataya
/// çevirir: <c>fields</c> virgüllü alan listesi, her alan kendi kodunu taşır
/// (<c>"tr.title": "content.title_too_long"</c>). Yanıtta cümle yoktur; `Error`'un
/// parametre sözlüğü dize taşır ve yapısal bir dizi eklemek Yasak Listesi 11'e girerdi —
/// panelin mevcut <c>fields</c>/<c>reasons</c> geleneğiyle aynı biçim.
/// </summary>
internal sealed class FieldErrors
{
    private readonly Dictionary<string, string> errors = new(StringComparer.Ordinal);

    /// <summary>Hata var mı.</summary>
    internal bool Any => errors.Count > 0;

    /// <summary>Alan için hata ekler; aynı alanın ilk hatası korunur.</summary>
    internal void Add(string field, string code) => errors.TryAdd(field, code);

    /// <summary>Tek hata.</summary>
    internal Error ToError()
    {
        Dictionary<string, string> parameters = new(errors, StringComparer.Ordinal)
        {
            ["fields"] = string.Join(',', errors.Keys),
        };

        return new Error(ContentErrors.ValidationFailed, parameters);
    }
}
