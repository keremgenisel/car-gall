namespace CarGallery.Modules.Notifications.Contracts.Setup;

/// <summary>
/// Modülün kuruluş ayarları. **Hepsi dağıtım yapılandırmasından gelir** (C9); hiçbiri koda
/// gömülü değildir ve kabul kriterleri yapılandırılan değeri doğrular (AC-26).
///
/// `Contracts` içinde yaşamasının nedeni erişimdir: Host'un bunu kurabilmesi gerekir ve
/// modülün iç projesinde hiçbir şey public değildir (architecture.md § 5).
///
/// <para><b>Kurulum bildirim adresi ve taban URL eksikse uygulama AYAĞA KALKMAZ</b>
/// (AC-27). Gerekçe: adres yapılandırılmamışsa her talep sessizce kaybolur — bu spec'in
/// varlık nedeni tam olarak o durumdur ve bu kez çalıştığı sanılır. Taban URL eksikse
/// e-postadaki bağlantı göreli kalır ve tıklanamaz.</para>
/// </summary>
/// <param name="SmtpPassword">
/// C8 — sır repoda DURMAZ; geliştirmede user-secrets, üretimde ortam değişkeni
/// (`docs/conventions.md` § 11). Eksikse uygulama ayağa kalkmaz (AC-28).
/// </param>
public sealed record NotificationsSetup(
    string ConnectionString,
    string DefaultLocale,
    string SetupNotificationAddress,
    string PublicBaseUrl,
    string SenderAddress,
    string SmtpHost,
    int SmtpPort,
    string SmtpUser,
    string SmtpPassword,
    bool SmtpUseStartTls,
    int MaximumAttempts,
    int RetryBaseSeconds,
    int SendTimeoutSeconds);
