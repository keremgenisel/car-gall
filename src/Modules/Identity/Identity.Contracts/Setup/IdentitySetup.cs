namespace CarGallery.Modules.Identity.Contracts.Setup;

/// <summary>
/// Modülün kuruluş ayarları. **Hepsi dağıtım yapılandırmasından gelir** (C20); hiçbiri
/// koda gömülü değildir ve kabul kriterleri yapılandırılan değeri doğrular.
///
/// `Contracts` içinde yaşamasının nedeni erişimdir: Host'un bunu kurabilmesi gerekir ve
/// modülün iç projesinde hiçbir şey public değildir (docs/architecture.md § 5). Bu tip
/// bir entegrasyon sözleşmesi değil, modülün KURULUM yüzeyidir.
/// </summary>
public sealed record IdentitySetup(
    string ConnectionString,
    string DefaultLocale,
    IReadOnlyList<string> EnabledLocales,
    string SeedAdminLoginId,
    string SeedAdminPassword,
    string SeedAdminDisplayName,
    int AccessLifetimeMinutes,
    int RefreshLifetimeDays,
    int FailedAttemptThreshold,
    int LockoutMinutes,
    int MinimumPasswordLength,
    int PasswordIterations,
    int LoginRequestLimit,
    int LoginRequestWindowMinutes);
