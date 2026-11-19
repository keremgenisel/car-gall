using CarGallery.Modules.Notifications.Contracts.Setup;
using Microsoft.AspNetCore.Hosting;

namespace CarGallery.Host.Tests;

/// <summary>
/// 0009/AC-28 gereği uygulama sağlayıcı kimlik bilgileri olmadan BAŞLAMAZ. 0006'nın
/// `IdentityTestSettings` ile yaptığı şeyin aynısı: başlangıç sözleşmesi değiştiği için
/// her test fabrikası bu ayarları da vermek zorunda.
///
/// Burada gerçek bir SMTP sunucusu YOK ve olması da gerekmiyor: bu testler uygulamanın
/// AYAĞA KALKMASINI sınıyor, posta gönderimini değil. Gönderimin gerçek bir sunucuya
/// karşı kanıtı `Notifications.Tests` içindeki tek entegrasyon testindedir (D6).
/// </summary>
internal static class NotificationsTestSettings
{
    internal static void Apply(IWebHostBuilder builder)
    {
        builder.UseSetting("Notifications:Smtp:Host", "localhost");
        builder.UseSetting("Notifications:Smtp:Port", "1025");
        builder.UseSetting("Notifications:Smtp:User", "test");
        builder.UseSetting("Notifications:Smtp:Password", "test");

        // `0018` D25/AC-50 — imza sırrı olmadan uygulama başlamaz. Test sırrı koşu başına
        // üretilir: depoda sabit bir anahtar, sızdığında her çıkış bağlantısını üretebilen
        // bir dize olurdu (ADR 0011'in deseni).
        builder.UseSetting("Notifications:Newsletter:UnsubscribeSigningKey", SigningKey);
    }

    /// <summary>Koşu başına üretilen imza sırrı (Base64, 32 bayt).</summary>
    internal static readonly string SigningKey =
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    /// <summary>Kurulum adımının bülten ayarları: `appsettings.json`'daki değerlerle aynı.</summary>
    internal static NewsletterSetup Newsletter() => new(
        ["tr", "ar", "en"],
        SendDayOfWeek: "Monday",
        SendTimeUtc: "07:00",
        ConfirmationLinkHours: 72,
        RequestLimit: 30,
        RequestWindowMinutes: 10,
        ValidRequestLimit: 5,
        ValidRequestWindowMinutes: 10,
        AddressConfirmationLimit: 3,
        AddressWindowHours: 24,
        UnsubscribeSigningKey: SigningKey);
}
