using System.Security.Cryptography;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Application.Newsletter;
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

    /// <summary>Koşu başına üretilen imza sırrı; depoda sabit bir anahtar durmaz (ADR 0011 deseni).</summary>
    internal static readonly string ImzaSirri = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>
    /// Bülten kurulumu. Değerler BİLEREK spec'in önerilerinden farklı (AC-98): testler
    /// yapılandırılan değeri kullanır, koda gömülü bir sabiti değil — 72 saat yerine 48,
    /// 5 yerine 4, 3 yerine 2 ile koşan bir test, sabite bağlı bir kodu yakalar.
    /// </summary>
    internal static NewsletterSetup BultenKurulumu(
        int teyitSaati = 48,
        int gecerliIstek = 4,
        int adresSiniri = 2,
        string imzaSirri = "",
        string gun = "Wednesday",
        string saat = "08:30",
        IReadOnlyList<string>? diller = null) => new(
        diller ?? ["tr", "ar", "en"],
        SendDayOfWeek: gun,
        SendTimeUtc: saat,
        ConfirmationLinkHours: teyitSaati,
        RequestLimit: 30,
        RequestWindowMinutes: 10,
        ValidRequestLimit: gecerliIstek,
        ValidRequestWindowMinutes: 10,
        AddressConfirmationLimit: adresSiniri,
        AddressWindowHours: 24,
        UnsubscribeSigningKey: imzaSirri.Length == 0 ? ImzaSirri : imzaSirri);

    internal static NewsletterOptions BultenSecenekleri(NewsletterSetup? kurulum = null) =>
        NewsletterOptions.Create(kurulum ?? BultenKurulumu()).Value;

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
