using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Contracts.Setup;

namespace CarGallery.Modules.Notifications.Application;

/// <summary>
/// Doğrulanmış yapılandırma (C9). Süre, sayı ve adres içeren kriterler YAPILANDIRILAN
/// değeri doğrular, koda gömülü bir sabiti değil (AC-26).
///
/// Doğrulama kuruluşta yapılır ve eksik değerde uygulama AYAĞA KALKMAZ (AC-27):
/// bildirim adresi yapılandırılmamışsa her talep sessizce kaybolur — bu spec'in varlık
/// nedeni tam olarak o durumdur ve bu kez çalıştığı sanılır.
/// </summary>
internal sealed class NotificationsOptions
{
    internal required string DefaultLocale { get; init; }

    internal required string SetupNotificationAddress { get; init; }

    /// <summary>Bağlantılar MUTLAK olmalı (AC-3); göreli bağlantı e-postada tıklanamaz.</summary>
    internal required Uri PublicBaseUrl { get; init; }

    internal required string SenderAddress { get; init; }

    internal required string SmtpHost { get; init; }

    internal required int SmtpPort { get; init; }

    internal required string SmtpUser { get; init; }

    internal required string SmtpPassword { get; init; }

    internal required bool SmtpUseStartTls { get; init; }

    internal required int MaximumAttempts { get; init; }

    internal required TimeSpan RetryBaseDelay { get; init; }

    internal required TimeSpan SendTimeout { get; init; }

    internal static Result<NotificationsOptions> Create(NotificationsSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        if (string.IsNullOrWhiteSpace(setup.SetupNotificationAddress))
        {
            return Result.Failure<NotificationsOptions>(
                new Error("notification.setup_address_missing"));
        }

        if (!Uri.TryCreate(setup.PublicBaseUrl, UriKind.Absolute, out Uri? baseUrl))
        {
            return Result.Failure<NotificationsOptions>(new Error("notification.base_url_missing"));
        }

        if (string.IsNullOrWhiteSpace(setup.SenderAddress))
        {
            return Result.Failure<NotificationsOptions>(new Error("notification.sender_missing"));
        }

        // AC-28 — sağlayıcı kimlik bilgileri eksikse uygulama ayağa kalkmaz. Kimlik
        // doğrulaması olmayan bir relay bu kriterin kapsamı DIŞINDADIR ve bu bilinçli:
        // üretimde kimliksiz relay, galerinin adresinden herkesin posta atabilmesi demek.
        if (string.IsNullOrWhiteSpace(setup.SmtpHost)
            || setup.SmtpPort < 1
            || string.IsNullOrWhiteSpace(setup.SmtpUser)
            || string.IsNullOrWhiteSpace(setup.SmtpPassword))
        {
            return Result.Failure<NotificationsOptions>(new Error("notification.provider_credentials_missing"));
        }

        if (setup.MaximumAttempts < 1 || setup.RetryBaseSeconds < 1 || setup.SendTimeoutSeconds < 1)
        {
            return Result.Failure<NotificationsOptions>(new Error("notification.invalid_retry_policy"));
        }

        return Result.Success(new NotificationsOptions
        {
            DefaultLocale = setup.DefaultLocale,
            SetupNotificationAddress = setup.SetupNotificationAddress,
            PublicBaseUrl = baseUrl,
            SenderAddress = setup.SenderAddress,
            SmtpHost = setup.SmtpHost,
            SmtpPort = setup.SmtpPort,
            SmtpUser = setup.SmtpUser,
            SmtpPassword = setup.SmtpPassword,
            SmtpUseStartTls = setup.SmtpUseStartTls,
            MaximumAttempts = setup.MaximumAttempts,
            RetryBaseDelay = TimeSpan.FromSeconds(setup.RetryBaseSeconds),
            SendTimeout = TimeSpan.FromSeconds(setup.SendTimeoutSeconds),
        });
    }
}
