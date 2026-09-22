using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// Etkin locale listesi DAĞITIM YAPILANDIRMASIDIR, veritabanı kaydı değildir
/// (docs/conventions.md § 11). Kod hiçbir yerde bir dili varsaymaz.
/// </summary>
internal sealed class LeadLocaleOptions(IReadOnlyList<Locale> enabled)
{
    internal IReadOnlyList<Locale> Enabled { get; } = enabled;

    /// <summary>
    /// R6/AC-16 — locale VARSAYILANA DÜŞMEZ. `SubmittedLocale` bugün yanlış kaydedilirse
    /// yarın geriye dönük düzeltilemez (**L1**); tahmin etmektense reddetmek ucuzdur.
    /// </summary>
    internal bool TryResolve(string? code, out Locale locale)
    {
        locale = default;

        if (code is null)
        {
            return false;
        }

        Result<Locale> parsed = Locale.Create(code);

        if (parsed.IsFailure)
        {
            return false;
        }

        foreach (Locale candidate in Enabled)
        {
            if (candidate.Code == parsed.Value.Code)
            {
                locale = candidate;
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// C17'nin eşiği ve R4'ün üst sınırları. Varsayılanlar spec'in kendi sayılarıdır;
/// yapılandırılabilir olmalarının tek nedeni kanıt üretimidir: görsel kanıt TR/AR/EN'de
/// başarılı gönderim ve hata senaryoları ister ve bunlar aynı IP'den kolayca eşiği aşar.
/// Spec'in sayısı varsayılan olarak KALIR ve AC-32/AC-33 o varsayılanla sınanır.
/// </summary>
internal sealed class LeadRateLimitOptions
{
    internal const int DefaultPermitLimit = 5;
    internal const int DefaultWindowMinutes = 10;

    internal int PermitLimit { get; init; } = DefaultPermitLimit;

    internal int WindowMinutes { get; init; } = DefaultWindowMinutes;
}
