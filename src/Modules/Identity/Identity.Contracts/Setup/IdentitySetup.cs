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
    int LoginRequestWindowMinutes,
    /// <param name="TwoFactorIssuer">
    /// `otpauth://` adresinde görünen kurulum adı (**D16**). Doğrulama uygulamaları hesabı
    /// bu adla listeler; kurulum başına tek galeri olduğu için değer dağıtım
    /// yapılandırmasındadır (`docs/conventions.md` § 11).
    /// </param>
    string TwoFactorIssuer,
    /// <param name="TwoFactorEncryptionKey">
    /// Doğrulama sırrını şifreleyen anahtarın Base64 hâli (**D3**). **Sırdır**: repoda
    /// durmaz, koşuyu başlatandan gelir — kurulum parolasıyla aynı disiplin (ADR `0011`).
    /// Eksik ya da 32 bayt değilse uygulama ayağa KALKMAZ (AC-49).
    /// </param>
    string TwoFactorEncryptionKey,
    int RecoveryCodeCount,
    int RecoveryCodeIterations,
    int PendingEnrollmentMinutes,
    int PendingLoginMinutes);
