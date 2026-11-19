using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// `0020` R15 · C5 — bir kaydetmedeki BÜTÜN alan hatalarını toplar ve tek bir hataya çevirir:
/// <c>fields</c> virgüllü alan listesi, her alan kendi kodunu taşır
/// (<c>"slug": "branch.slug_taken"</c>). Yanıtta cümle yoktur; panel kodu çevirir.
/// `0017`'nin <c>content.validation_failed</c> biçimiyle aynı — panel iki ekranı aynı
/// okuyucuyla çözer.
/// </summary>
internal sealed class FieldErrors
{
    internal const string ValidationFailed = "dealership.validation_failed";

    private readonly Dictionary<string, string> errors = new(StringComparer.Ordinal);

    internal bool Any => errors.Count > 0;

    internal bool HasField(string field) => errors.ContainsKey(field);

    /// <summary>Alan için hata ekler; aynı alanın ilk hatası korunur.</summary>
    internal void Add(string field, string code) => errors.TryAdd(field, code);

    internal Error ToError()
    {
        Dictionary<string, string> parameters = new(errors, StringComparer.Ordinal)
        {
            ["fields"] = string.Join(',', errors.Keys),
        };

        return new Error(ValidationFailed, parameters);
    }

    /// <summary>Tek alanlık hata — yarış anında yakalanan tekillik ihlali gibi.</summary>
    internal static Error Single(string field, string code)
    {
        FieldErrors errors = new();
        errors.Add(field, code);
        return errors.ToError();
    }
}
