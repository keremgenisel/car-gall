namespace CarGallery.Modules.Notifications.Contracts.Setup;

/// <summary>
/// `0018` — bülten aboneliğinin kuruluş ayarları. Hepsi dağıtım yapılandırmasından gelir ve
/// hiçbirinin koda gömülü bir varsayılanı yoktur (C14): biri eksikse uygulama ayağa kalkmaz
/// (AC-97). Kabul kriterleri yapılandırılan değeri doğrular (AC-98).
///
/// <para>`NotificationsSetup`'a eklenmedi: o kayıt `0009`'un sözleşmesidir ve altı yerde
/// konumsal olarak kuruluyor. Bülten ayrı bir yaşam döngüsü taşıyor (dönem, bağlantı süresi,
/// üç sınır, imza sırrı).</para>
/// </summary>
/// <param name="EnabledLocales">Kurulumun etkin dilleri (**D14**: etkin olmayan dil reddedilir).</param>
/// <param name="SendDayOfWeek">**D13** — dönemin günü (`Monday` … `Sunday`), UTC.</param>
/// <param name="SendTimeUtc">**D13** — dönemin saati, `HH:mm`, UTC (C9).</param>
/// <param name="ConfirmationLinkHours">**D7** — teyit bağlantısının süresi.</param>
/// <param name="UnsubscribeSigningKey">
/// **D25** — çıkış bağlantılarını imzalayan SIR: Base64, en az 32 bayt. Depoda durmaz
/// (docs/conventions.md § 11). Değişirse eski bütün çıkış bağlantıları geçersiz olur (AC-47).
/// </param>
public sealed record NewsletterSetup(
    IReadOnlyList<string> EnabledLocales,
    string SendDayOfWeek,
    string SendTimeUtc,
    int ConfirmationLinkHours,
    int RequestLimit,
    int RequestWindowMinutes,
    int ValidRequestLimit,
    int ValidRequestWindowMinutes,
    int AddressConfirmationLimit,
    int AddressWindowHours,
    string UnsubscribeSigningKey);
