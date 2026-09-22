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
/// İKİ AYRI eşik, çünkü iki ayrı şeyi koruyorlar:
///
/// <list type="bullet">
/// <item><see cref="CapturedLeadLimit"/> — C17'nin sayısı (10 dakikada 5). Yalnız KAYIT
/// OLUŞAN istek bu bütçeyi tüketir; AC-32'nin "kaydedildikten sonra" ifadesi bunu
/// söylüyor.</item>
/// <item><see cref="RequestLimit"/> — kaba tekrarı kesen daha gevşek istek sınırı (R16,
/// Karar D4). Boru hattında yaşar ve geçersiz istekleri de sayar; sayısı, formu birkaç
/// kez yanlış dolduran dürüst bir ziyaretçiyi kilitlemeyecek kadar yüksektir.</item>
/// </list>
///
/// Varsayılanlar spec'in kendi sayılarıdır; yapılandırılabilir olmalarının tek nedeni
/// kanıt üretimidir. AC-32/AC-33 varsayılanla sınanır.
/// </summary>
internal sealed class LeadRateLimitOptions
{
    internal const int DefaultCapturedLeadLimit = 5;
    internal const int DefaultRequestLimit = 30;
    internal const int DefaultWindowMinutes = 10;

    internal int CapturedLeadLimit { get; init; } = DefaultCapturedLeadLimit;

    internal int RequestLimit { get; init; } = DefaultRequestLimit;

    internal int WindowMinutes { get; init; } = DefaultWindowMinutes;
}
