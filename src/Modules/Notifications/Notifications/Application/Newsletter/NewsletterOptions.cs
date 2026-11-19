using System.Globalization;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Contracts.Setup;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` — doğrulanmış bülten yapılandırması. Doğrulama KURULUŞTA yapılır; eksik ya da
/// kullanılamaz değerde uygulama ayağa kalkmaz (C14, AC-97). Hiçbir alanın burada bir
/// varsayılanı yoktur: üretilmiş bir dönem saati ya da imza sırrı, "çalışıyor" sanılan ama
/// kimsenin seçmediği bir davranış olurdu.
/// </summary>
internal sealed class NewsletterOptions
{
    /// <summary>**D25** — HMAC-SHA256 için en kısa anahtar: özetin kendi boyu.</summary>
    internal const int MinimumSigningKeyBytes = 32;

    internal required IReadOnlyList<string> EnabledLocales { get; init; }

    internal required DayOfWeek SendDayOfWeek { get; init; }

    internal required TimeOnly SendTimeUtc { get; init; }

    internal required TimeSpan ConfirmationLinkLifetime { get; init; }

    internal required int RequestLimit { get; init; }

    internal required TimeSpan RequestWindow { get; init; }

    internal required int ValidRequestLimit { get; init; }

    internal required TimeSpan ValidRequestWindow { get; init; }

    internal required int AddressConfirmationLimit { get; init; }

    internal required TimeSpan AddressWindow { get; init; }

    internal required byte[] UnsubscribeSigningKey { get; init; }

    internal bool IsEnabled(string locale) => EnabledLocales.Contains(locale, StringComparer.Ordinal);

    internal static Result<NewsletterOptions> Create(NewsletterSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        List<string> locales = [];

        foreach (string code in setup.EnabledLocales ?? [])
        {
            if (Locale.Create(code).IsFailure)
            {
                return Failure("notification.newsletter_locales_invalid");
            }

            locales.Add(code);
        }

        if (locales.Count == 0)
        {
            return Failure("notification.newsletter_locales_invalid");
        }

        // `Enum.TryParse` sayıyı da kabul eder ("8" → tanımsız gün); ad ZORUNLU.
        if (string.IsNullOrWhiteSpace(setup.SendDayOfWeek)
            || setup.SendDayOfWeek.Any(char.IsDigit)
            || !Enum.TryParse(setup.SendDayOfWeek, ignoreCase: false, out DayOfWeek day)
            || !Enum.IsDefined(day))
        {
            return Failure("notification.newsletter_schedule_missing");
        }

        if (!TimeOnly.TryParseExact(setup.SendTimeUtc, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly time))
        {
            return Failure("notification.newsletter_schedule_missing");
        }

        if (setup.ConfirmationLinkHours < 1)
        {
            return Failure("notification.newsletter_confirmation_lifetime_missing");
        }

        if (setup.RequestLimit < 1 || setup.RequestWindowMinutes < 1
            || setup.ValidRequestLimit < 1 || setup.ValidRequestWindowMinutes < 1
            || setup.AddressConfirmationLimit < 1 || setup.AddressWindowHours < 1)
        {
            return Failure("notification.newsletter_rate_limit_missing");
        }

        byte[] key;

        try
        {
            key = Convert.FromBase64String(setup.UnsubscribeSigningKey ?? string.Empty);
        }
        catch (FormatException)
        {
            return Failure("notification.newsletter_signing_key_missing");
        }

        if (key.Length < MinimumSigningKeyBytes)
        {
            return Failure("notification.newsletter_signing_key_missing");
        }

        return Result.Success(new NewsletterOptions
        {
            EnabledLocales = locales,
            SendDayOfWeek = day,
            SendTimeUtc = time,
            ConfirmationLinkLifetime = TimeSpan.FromHours(setup.ConfirmationLinkHours),
            RequestLimit = setup.RequestLimit,
            RequestWindow = TimeSpan.FromMinutes(setup.RequestWindowMinutes),
            ValidRequestLimit = setup.ValidRequestLimit,
            ValidRequestWindow = TimeSpan.FromMinutes(setup.ValidRequestWindowMinutes),
            AddressConfirmationLimit = setup.AddressConfirmationLimit,
            AddressWindow = TimeSpan.FromHours(setup.AddressWindowHours),
            UnsubscribeSigningKey = key,
        });
    }

    private static Result<NewsletterOptions> Failure(string code) =>
        Result.Failure<NewsletterOptions>(new Error(code));
}
