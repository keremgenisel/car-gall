using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Dealership.Domain;

internal sealed record SocialLink(string Platform, string Url);

/// <summary>
/// Kurulumun ait olduğu tek işletme (docs/domain.md). **Tekil agregat**: kurulumda tam
/// olarak bir galeri profili vardır (R1, AC-1).
///
/// Tekillik kimliğin sabitlenmesiyle sağlanıyor ve veritabanında bir kısıtla dayatılıyor —
/// "uygulama ikincisini yazmaz" demek yetmez, çünkü onu çiğneyeni hiçbir test yakalamaz.
/// `TenantId` benzeri bir alan YOKTUR: kurulum = galeri (C11).
/// </summary>
internal sealed class Gallery : Entity<Guid>
{
    internal const string NotFound = "gallery.not_found";
    internal const string CommercialNameRequired = "gallery.commercial_name_required";
    internal const string CommercialNameTooLong = "gallery.commercial_name_too_long";
    internal const string TaxNumberTooLong = "gallery.tax_number_too_long";
    internal const string TradeRegistryNumberTooLong = "gallery.trade_registry_number_too_long";
    internal const string DefaultLocaleDisplayNameRequired = "gallery.default_locale_display_name_required";
    internal const string DisplayNameTooLong = "gallery.display_name_too_long";

    /// <summary>Tek satırın sabit kimliği; şema bu değeri bir CHECK kısıtıyla zorunlu kılar.</summary>
    internal static readonly Guid SingletonId = Guid.Parse("00000000-0000-7000-8000-00000000da01");

    private readonly List<SocialLink> socialLinks = [];

    private Gallery(string commercialName)
        : base(SingletonId) => CommercialName = commercialName;

    internal string CommercialName { get; private set; }

    /// <summary>D7 — yalnızca referans; görselin kendisi `Media` spec'ine aittir.</summary>
    internal Guid? LogoMediaAssetId { get; private set; }

    internal string? TaxNumber { get; private set; }

    internal string? TradeRegistryNumber { get; private set; }

    internal IReadOnlyList<SocialLink> SocialLinks => socialLinks;

    /// <summary>
    /// `0020` R3 · D4 — ziyaretçiye gösterilen ad, dil başına. Ticari unvan yasal addır ve
    /// vitrinde gösterilmez; görünen ad çevrilebilir bir metindir. Varsayılan dildeki
    /// karşılığı zorunludur (<see cref="UpdateProfile"/>).
    /// </summary>
    internal LocalizedText DisplayNames { get; private set; } = LocalizedText.Empty;

    /// <summary>`0020` R16 · D17 — iyimser eşzamanlılık; her kaydetme bir artırır.</summary>
    internal int Version { get; private set; } = 1;

    internal static Result<Gallery> Create(string commercialName)
    {
        if (string.IsNullOrWhiteSpace(commercialName))
        {
            return Result.Failure<Gallery>(new Error(CommercialNameRequired));
        }

        return Result.Success(new Gallery(commercialName.Trim()));
    }

    /// <summary>
    /// `0020` R1–R3a — profilin dört alanı tek kaydetmede. Hatalar toplanır: yönetici bir
    /// kaydetmede bütün kural dışı alanları görür (R15'in alan başına hata ilkesi). Hata
    /// varsa HİÇBİR alan değişmez (AC-3, AC-5).
    ///
    /// Boşluktan ibaret değer boştur; vergi ve sicil numarası boşaltılabilir (AC-4).
    /// Görünen ad dil başına kırpılır ve boş dil hiç saklanmaz (AC-6) — böylece eksik dil
    /// okumada varsayılan dile düşer (AC-10).
    /// </summary>
    internal Result UpdateProfile(
        string? commercialName,
        string? taxNumber,
        string? tradeRegistryNumber,
        IReadOnlyDictionary<Locale, string?> displayNames,
        Locale defaultLocale)
    {
        FieldErrors errors = new();
        string unvan = (commercialName ?? string.Empty).Trim();
        string? vergi = Optional(taxNumber);
        string? sicil = Optional(tradeRegistryNumber);

        if (unvan.Length == 0)
        {
            errors.Add("commercialName", CommercialNameRequired);
        }
        else if (unvan.Length > DealershipLimits.CommercialName)
        {
            errors.Add("commercialName", CommercialNameTooLong);
        }

        if (vergi is { Length: > DealershipLimits.RegistryNumber })
        {
            errors.Add("taxNumber", TaxNumberTooLong);
        }

        if (sicil is { Length: > DealershipLimits.RegistryNumber })
        {
            errors.Add("tradeRegistryNumber", TradeRegistryNumberTooLong);
        }

        List<KeyValuePair<Locale, string>> adlar = [];

        foreach ((Locale locale, string? value) in displayNames)
        {
            string ad = (value ?? string.Empty).Trim();

            if (ad.Length > DealershipLimits.DisplayName)
            {
                errors.Add($"displayName.{locale.Code}", DisplayNameTooLong);
            }
            else if (ad.Length > 0)
            {
                adlar.Add(new KeyValuePair<Locale, string>(locale, ad));
            }
        }

        if (!adlar.Any(pair => pair.Key == defaultLocale) && !errors.HasField($"displayName.{defaultLocale.Code}"))
        {
            errors.Add($"displayName.{defaultLocale.Code}", DefaultLocaleDisplayNameRequired);
        }

        if (errors.Any)
        {
            return Result.Failure(errors.ToError());
        }

        CommercialName = unvan;
        TaxNumber = vergi;
        TradeRegistryNumber = sicil;
        DisplayNames = LocalizedText.From(adlar);
        Version++;

        return Result.Success();
    }

    private static string? Optional(string? value)
    {
        string trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    internal void SetLogo(Guid mediaAssetId) => LogoMediaAssetId = mediaAssetId;

    internal void SetRegistration(string? taxNumber, string? tradeRegistryNumber)
    {
        TaxNumber = taxNumber;
        TradeRegistryNumber = tradeRegistryNumber;
    }

    internal void AddSocialLink(SocialLink link)
    {
        if (!socialLinks.Any(item => item.Platform == link.Platform))
        {
            socialLinks.Add(link);
        }
    }
}
