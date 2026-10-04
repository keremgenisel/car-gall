using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Contracts.Setup;

namespace CarGallery.Modules.Notifications.Tests;

/// <summary>
/// Yapılandırma KURULUŞTA doğrulanır (C9). "Var mı" yetmez: <b>kullanılabilir mi</b>
/// sorusu da burada sorulmalı.
///
/// <para>Boş olmayan ama ayrıştırılamayan bir adres, doğrulamayı geçip çalışma anında
/// kuyruğu tıkar — yani C9'un önlemeye çalıştığı şeyin tam olarak kendisi, bir adım
/// geriden.</para>
/// </summary>
public sealed class YapilandirmaTests
{
    [Theory]
    [InlineData("bildirim[at]ornek.com")]
    [InlineData("<@ornek.com")]
    [InlineData("iki@ornek.com, uc@ornek.com")]
    public void Reddeder_AyristirilamayanKurulumAdresini(string adres)
    {
        Result<NotificationsOptions> sonuc = NotificationsOptions.Create(Kurulum(setupAddress: adres));

        Assert.True(sonuc.IsFailure, $"'{adres}' kabul edildi; kuyruk çalışma anında tıkanır.");
    }

    [Theory]
    [InlineData("bildirim[at]ornek.com")]
    [InlineData("<@ornek.com")]
    public void Reddeder_AyristirilamayanGonderenAdresini(string adres)
    {
        Result<NotificationsOptions> sonuc = NotificationsOptions.Create(Kurulum(senderAddress: adres));

        Assert.True(sonuc.IsFailure, $"'{adres}' kabul edildi; hiçbir mesaj gönderilemez.");
    }

    [Fact]
    public void KabulEder_GecerliAdresleri() =>
        Assert.True(NotificationsOptions.Create(Kurulum()).IsSuccess);

    private static NotificationsSetup Kurulum(
        string setupAddress = "galeri@ornek.com",
        string senderAddress = "bildirim@ornek.com") => new(
        ConnectionString: "yok",
        DefaultLocale: "tr",
        SetupNotificationAddress: setupAddress,
        PublicBaseUrl: "https://ornek.com",
        SenderAddress: senderAddress,
        SmtpHost: "localhost",
        SmtpPort: 25,
        SmtpUser: "kullanici",
        SmtpPassword: "sir",
        SmtpUseStartTls: false,
        MaximumAttempts: 3,
        RetryBaseSeconds: 10,
        SendTimeoutSeconds: 10);
}
