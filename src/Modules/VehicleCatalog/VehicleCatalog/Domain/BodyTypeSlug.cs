using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Kasa tipinin kanonik değeri: küçük harfli ASCII slug (C13). Eşleştirme TAM ve HARFE
/// DUYARLIDIR; <c>ILIKE</c>/<c>lower()</c> kullanılmaz (Yasak Listesi 27) — Türkçe
/// collation altında <c>i/İ/ı/I</c> sessizce yanlış sonuç verir.
/// </summary>
internal readonly record struct BodyTypeSlug
{
    internal const string InvalidSlug = "body_type.invalid_slug";

    private BodyTypeSlug(string value) => Value = value;

    internal string Value { get; }

    internal static Result<BodyTypeSlug> Create(string value)
    {
        if (!IsCanonicalSlug(value))
        {
            return Result.Failure<BodyTypeSlug>(new Error(InvalidSlug));
        }

        return Result.Success(new BodyTypeSlug(value));
    }

    /// <summary>Tanınmayan değer hata değildir; ziyaretçi girdisinde yok sayılır (C14, AC-17).</summary>
    internal static bool TryParse(string value, out BodyTypeSlug slug)
    {
        Result<BodyTypeSlug> result = Create(value);
        slug = result.IsSuccess ? result.Value : default;
        return result.IsSuccess;
    }

    public override string ToString() => Value;

    private static bool IsCanonicalSlug(string value)
    {
        if (value is not { Length: > 0 and <= 40 })
        {
            return false;
        }

        foreach (char character in value)
        {
            bool allowed = character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
            if (!allowed)
            {
                return false;
            }
        }

        return value[0] != '-' && value[^1] != '-';
    }
}
