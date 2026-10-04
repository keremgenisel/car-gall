using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Infrastructure;

/// <summary>
/// Taşıyıcının SÖZLEŞMESİ: <see cref="INotificationTransport.SendAsync"/> bir
/// <see cref="SendResult"/> DÖNER — fırlatmaz. Sınıflandırma taşıyıcının işidir (R11) ve
/// sınıflandırılamayan bir hata, sınıflandırılmamış bir hatadır.
///
/// <para>Burada sunucuya hiç bağlanılmıyor: adres çözümü bağlantıdan ÖNCE yapılıyor ve
/// sınanan şey tam olarak o aşama.</para>
/// </summary>
public sealed class SmtpTasiyiciTests
{
    /// <summary>
    /// Bozuk alıcı adresi KALICI hatadır: yeniden denemek aynı adresi aynı şekilde
    /// reddedecektir.
    ///
    /// <para>Bu yol teorik değil: 0005'in <c>EmailAddress</c> doğrulaması bilinçli olarak
    /// dardır ve <c>&lt;@ornek.com</c> onu GEÇER — yani public talep formundan buraya
    /// gelebilir.</para>
    /// </summary>
    [Theory(DisplayName = "0009/AC-17: Kalıcı bir hatada — alıcı adresi reddedildiğinde — yeniden deneme yapılmaz; kayıt derhal başarısız işaretlenir")]
    [InlineData("<@ornek.com")]
    [InlineData("ziyaretci@")]
    [InlineData("iki adres@ornek.com, baska@ornek.com")]
    public async Task Doner_KaliciHata_BozukAliciAdresinde(string alici)
    {
        SmtpNotificationTransport tasiyici = new(Secenekler(), NullLogger<SmtpNotificationTransport>.Instance);

        SendResult sonuc = await tasiyici.SendAsync(
            new OutboundMessage(alici, "Konu", "Gövde"),
            CancellationToken.None);

        Assert.Equal(SendFailureKind.Permanent, sonuc.Failure);
        Assert.Equal(NotificationErrors.RecipientRejected, sonuc.Code);
    }

    private static NotificationsOptions Secenekler() => NotificationsOptions.Create(new NotificationsSetup(
        ConnectionString: "yok",
        DefaultLocale: "tr",
        SetupNotificationAddress: "galeri@ornek.com",
        PublicBaseUrl: "https://ornek.com",
        SenderAddress: "bildirim@ornek.com",
        SmtpHost: "localhost",
        SmtpPort: 1,
        SmtpUser: "kullanici",
        SmtpPassword: "sir",
        SmtpUseStartTls: false,
        MaximumAttempts: 3,
        RetryBaseSeconds: 10,
        SendTimeoutSeconds: 10)).Value;
}
