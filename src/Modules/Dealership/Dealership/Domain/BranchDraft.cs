using CarGallery.BuildingBlocks;
using CoordinateValue = CarGallery.Modules.Dealership.Domain.Coordinate;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// `0020` R9 — panelden gelen şube alanlarının ham hâli. Dil anahtarları çağıran tarafından
/// etkin dillere süzülmüş gelir; değerler henüz doğrulanmamıştır. Boşluktan ibaret değer
/// boştur ve opsiyonel alanda "temizle" demektir (D9).
/// </summary>
internal sealed record BranchInput(
    IReadOnlyDictionary<Locale, string?> Names,
    IReadOnlyDictionary<Locale, string?> Addresses,
    string? Phone,
    string? WhatsApp,
    string? Email,
    string? TimeZone,
    string? Slug,
    decimal? Latitude,
    decimal? Longitude);

/// <summary>
/// `0020` R9 · R15 — doğrulanmış şube alanları. Açma ve düzenleme AYNI çözümlemeden geçer:
/// AC-41 "doğrulamalar düzenlemede de aynı hatayla geçerlidir" diyor ve iki ayrı yol iki
/// ayrı hata üretmeye başlardı.
/// </summary>
internal sealed record BranchDraft(
    LocalizedText Names,
    LocalizedText Addresses,
    PhoneNumber Phone,
    PhoneNumber? WhatsApp,
    EmailAddress? Email,
    TimeZoneId TimeZone,
    BranchSlug Slug,
    Coordinate? Coordinate)
{
    internal const string NameTooLong = "branch.name_too_long";
    internal const string AddressTooLong = "branch.address_too_long";
    internal const string PhoneRequired = "branch.phone_required";
    internal const string TimeZoneRequired = "branch.time_zone_required";
    internal const string SlugRequired = "branch.slug_required";
    internal const string CoordinateIncomplete = "coordinate.incomplete";

    /// <summary>
    /// Bütün alanları doğrular ve hataları TOPLAR: yönetici bir kaydetmede kural dışı her
    /// alanı görür. Kod, alanın kendi değer tipinin kodudur (telefon için
    /// <c>phone_number.invalid_format</c>) — `0003`'ün kuralları tek yerde yaşar.
    /// </summary>
    internal static Result<BranchDraft> Parse(BranchInput input, Locale defaultLocale)
    {
        FieldErrors errors = new();

        LocalizedText names = Texts(input.Names, "name", DealershipLimits.BranchName, NameTooLong, errors);
        LocalizedText addresses = Texts(input.Addresses, "address", DealershipLimits.BranchAddress, AddressTooLong, errors);

        if (!names.HasValueFor(defaultLocale) && !errors.HasField($"name.{defaultLocale.Code}"))
        {
            errors.Add($"name.{defaultLocale.Code}", Branch.DefaultLocaleNameRequired);
        }

        if (!addresses.HasValueFor(defaultLocale) && !errors.HasField($"address.{defaultLocale.Code}"))
        {
            errors.Add($"address.{defaultLocale.Code}", Branch.DefaultLocaleAddressRequired);
        }

        PhoneNumber? phone = Required(input.Phone, "phone", PhoneRequired, PhoneNumber.Create, errors);
        PhoneNumber? whatsApp = Optional(input.WhatsApp, "whatsApp", PhoneNumber.Create, errors);
        EmailAddress? email = Optional(input.Email, "email", EmailAddress.Create, errors);
        TimeZoneId? timeZone = Required(input.TimeZone, "timeZone", TimeZoneRequired, TimeZoneId.Create, errors);
        BranchSlug? slug = Required(input.Slug, "slug", SlugRequired, BranchSlug.Create, errors);
        CoordinateValue? coordinate = null;

        if (input.Latitude is not null && input.Longitude is not null)
        {
            Result<CoordinateValue> parsed = CoordinateValue.Create(input.Latitude.Value, input.Longitude.Value);

            if (parsed.IsFailure)
            {
                errors.Add("coordinate", parsed.Error.Code);
            }
            else
            {
                coordinate = parsed.Value;
            }
        }
        else if (input.Latitude is not null || input.Longitude is not null)
        {
            // Yarım koordinat bir konum değildir; sessizce düşürmek yöneticinin yazdığını
            // kaybetmek olurdu.
            errors.Add("coordinate", CoordinateIncomplete);
        }

        if (errors.Any)
        {
            return Result.Failure<BranchDraft>(errors.ToError());
        }

        return Result.Success(new BranchDraft(
            names,
            addresses,
            phone!.Value,
            whatsApp,
            email,
            timeZone!.Value,
            slug!.Value,
            coordinate));
    }

    private static LocalizedText Texts(
        IReadOnlyDictionary<Locale, string?> values,
        string field,
        int limit,
        string tooLong,
        FieldErrors errors)
    {
        List<KeyValuePair<Locale, string>> kept = [];

        foreach ((Locale locale, string? value) in values)
        {
            string text = (value ?? string.Empty).Trim();

            if (text.Length > limit)
            {
                errors.Add($"{field}.{locale.Code}", tooLong);
            }
            else if (text.Length > 0)
            {
                kept.Add(new KeyValuePair<Locale, string>(locale, text));
            }
        }

        return LocalizedText.From(kept);
    }

    private static T? Required<T>(
        string? value,
        string field,
        string requiredCode,
        Func<string, Result<T>> create,
        FieldErrors errors)
        where T : struct
    {
        string text = (value ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            errors.Add(field, requiredCode);
            return null;
        }

        Result<T> parsed = create(text);

        if (parsed.IsFailure)
        {
            errors.Add(field, parsed.Error.Code);
            return null;
        }

        return parsed.Value;
    }

    private static T? Optional<T>(string? value, string field, Func<string, Result<T>> create, FieldErrors errors)
        where T : struct
    {
        string text = (value ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return null;
        }

        Result<T> parsed = create(text);

        if (parsed.IsFailure)
        {
            errors.Add(field, parsed.Error.Code);
            return null;
        }

        return parsed.Value;
    }
}
