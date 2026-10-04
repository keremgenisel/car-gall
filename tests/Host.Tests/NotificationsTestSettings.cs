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
    }
}
