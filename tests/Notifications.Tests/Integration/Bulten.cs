using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Contracts.Setup;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `0018` testlerinin ortak kurulumu. Sahte saatin başlangıcı takvimde sabit bir güne
/// YAZILMAZ (docs/testing.md § 4): gerçek saatten alınır ve <see cref="IleriSaat.Advance"/>
/// ile ilerletilir.
/// </summary>
internal static class Bulten
{
    internal static NotificationsOptions Secenekler() => NotificationsOptions.Create(new NotificationsSetup(
        ConnectionString: "yok",
        DefaultLocale: "tr",
        SetupNotificationAddress: "galeri@ornek.com",
        PublicBaseUrl: "https://ornek.com",
        SenderAddress: "bildirim@ornek.com",
        SmtpHost: "localhost",
        SmtpPort: 25,
        SmtpUser: "kullanici",
        SmtpPassword: "sir",
        SmtpUseStartTls: false,
        MaximumAttempts: 3,
        RetryBaseSeconds: 10,
        SendTimeoutSeconds: 10)).Value;

    internal sealed class SabitSaat(DateTimeOffset an) : IClock
    {
        public DateTimeOffset UtcNow { get; } = an;
    }

    /// <summary>Elle ilerletilen saat; testin ölçtüğü şey geçen süredir.</summary>
    internal sealed class IleriSaat(DateTimeOffset baslangic) : IClock
    {
        private DateTimeOffset an = baslangic;

        public DateTimeOffset UtcNow => an;

        internal void Advance(TimeSpan sure) => an += sure;

        internal void Set(DateTimeOffset yeni) => an = yeni;
    }
}
