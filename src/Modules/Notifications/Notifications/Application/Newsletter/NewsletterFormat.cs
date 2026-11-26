using CarGallery.BuildingBlocks.Formatting;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` **D24** · **D32** — bültendeki fiyat, km ve yıl. Uygulama `0004` **P4** ile
/// <see cref="ShowcaseFormat"/>'a (BuildingBlocks) taşındı; bu sınıf bültenin çağrı noktası
/// olarak kalıyor ve hiçbir şey eklemiyor — iki yazım yolu yok.
/// </summary>
internal static class NewsletterFormat
{
    internal static string Money(decimal amount, string currencyCode, int minorUnitExponent, string locale) =>
        ShowcaseFormat.Money(amount, currencyCode, minorUnitExponent, locale);

    internal static string Mileage(int kilometers, string locale) => ShowcaseFormat.Mileage(kilometers, locale);

    internal static string Year(int year, string locale) => ShowcaseFormat.Year(year, locale);
}
