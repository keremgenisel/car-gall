using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// C20 — **süreler ve eşikler dağıtım yapılandırmasından okunur.** Buradaki sabitler
/// yalnız VARSAYILANDIR (D4) ve değişmeleri spec revizyonu değildir. Kabul kriterleri
/// yapılandırılan değeri doğrular, bu sabitleri değil; bu yüzden kriter metinlerinde de
/// sayı yoktur.
/// </summary>
internal sealed class IdentityOptions
{
    internal const int DefaultAccessLifetimeMinutes = 30;
    internal const int DefaultRefreshLifetimeDays = 14;
    internal const int DefaultFailedAttemptThreshold = 5;
    internal const int DefaultLockoutMinutes = 15;
    internal const int DefaultMinimumPasswordLength = 12;

    /// <summary>
    /// PBKDF2 iterasyon sayısı (C6 "maliyet parametresi"). Donanım hızlandıkça
    /// yükseltilir; saklanan özet kendi maliyetini içinde taşıdığı için eski satırlar
    /// doğrulanmaya devam eder.
    /// </summary>
    internal const int DefaultPasswordIterations = 210_000;

    /// <summary>D9 — kurtarma kodu sayısı; eşik gibi yapılandırmadan gelir.</summary>
    internal const int DefaultRecoveryCodeCount = 10;

    /// <summary>
    /// D2/D38 — kurtarma kodu özetinin iterasyon sayısı PAROLADAN AYRIDIR. Girilen bir
    /// kodu saklanan on özetin hepsiyle karşılaştırmak on özetleme demek; üretim parola
    /// değeriyle (210.000) bu tek girişte saniyeler sürerdi. Düşük maliyet burada
    /// güvenli, çünkü kod sistem üretimi ve yüksek entropili — `Session.HashToken`'ın
    /// gerekçesiyle aynı.
    /// </summary>
    internal const int DefaultRecoveryCodeIterations = 10_000;

    /// <summary>D18 — doğrulanmamış kurulumun ömrü.</summary>
    internal const int DefaultPendingEnrollmentMinutes = 10;

    /// <summary>D19 — ara durumun ömrü.</summary>
    internal const int DefaultPendingLoginMinutes = 5;

    internal TimeSpan AccessLifetime { get; init; } = TimeSpan.FromMinutes(DefaultAccessLifetimeMinutes);

    internal TimeSpan RefreshLifetime { get; init; } = TimeSpan.FromDays(DefaultRefreshLifetimeDays);

    internal int FailedAttemptThreshold { get; init; } = DefaultFailedAttemptThreshold;

    internal TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(DefaultLockoutMinutes);

    internal int MinimumPasswordLength { get; init; } = DefaultMinimumPasswordLength;

    internal int PasswordIterations { get; init; } = DefaultPasswordIterations;

    internal int RecoveryCodeCount { get; init; } = DefaultRecoveryCodeCount;

    internal int RecoveryCodeIterations { get; init; } = DefaultRecoveryCodeIterations;

    internal TimeSpan PendingEnrollmentLifetime { get; init; } =
        TimeSpan.FromMinutes(DefaultPendingEnrollmentMinutes);

    internal TimeSpan PendingLoginLifetime { get; init; } =
        TimeSpan.FromMinutes(DefaultPendingLoginMinutes);

    /// <summary>
    /// D16 — adreste görünen kurulum adı. Parametre kümesinin aksine bu bir sözleşme
    /// değil, kurulum bazlı bir değerdir: değişmesi kurulmuş kayıtları geçersizleştirmez,
    /// yalnız uygulamadaki satırın etiketini değiştirir.
    /// </summary>
    internal string TwoFactorIssuer { get; init; } = "CarGallery";
}

/// <summary>
/// Etkin locale listesi ve varsayılan locale DAĞITIM YAPILANDIRMASIDIR, veritabanı kaydı
/// değildir (docs/conventions.md § 11). Kod hiçbir yerde bir dili varsaymaz.
/// </summary>
internal sealed class IdentityLocaleOptions(Locale defaultLocale, IReadOnlyList<Locale> enabled)
{
    internal Locale Default { get; } = defaultLocale;

    internal IReadOnlyList<Locale> Enabled { get; } = enabled;

    /// <summary>
    /// R16/AC-40/AC-41 — verilmediğinde kurulum varsayılanı atanır; etkin olmayan bir
    /// dille personel EKLENEMEZ. Sessizce varsayılana düşmek, yöneticinin yazdığı dili
    /// yok saymak olurdu.
    /// </summary>
    internal bool TryResolve(string? code, out Locale locale)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            locale = Default;
            return true;
        }

        Result<Locale> parsed = Locale.Create(code);

        if (parsed.IsFailure)
        {
            locale = default;
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

        locale = default;
        return false;
    }
}
