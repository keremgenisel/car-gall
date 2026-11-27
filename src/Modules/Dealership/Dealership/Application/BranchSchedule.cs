using System.Globalization;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// `0027` **D4**, **D8** — şubenin saat bilgisinin okumaya çevrilmesi, TEK yerde: panel ve ziyaretçi
/// okuması aynı "bugün"ü (şubenin saat diliminde, verilen andan) kullanır.
/// </summary>
internal static class BranchSchedule
{
    /// <summary>Ziyaretçi okuması: "şu an" ve penceredeki özel günler. Programsız şubede ikisi de yok (D7).</summary>
    internal static (PublicOpenNowDto? OpenNow, IReadOnlyList<PublicSpecialDayDto> SpecialHours) ForVisitor(
        OpeningHours? weekly,
        SpecialHours? special,
        TimeZoneId timeZone,
        DateTimeOffset now)
    {
        if (weekly is null)
        {
            return (null, []);
        }

        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(timeZone.Value);
        SpecialHours days = special ?? SpecialHours.Empty;
        OpenNow durum = BranchClock.Compute(weekly, days, zone, now);
        DateOnly today = durum.Today;

        return (
            new PublicOpenNowDto(
                durum.Open,
                Date(today),
                durum.Next is { } next ? new PublicNextChangeDto(Date(next.Date), next.Time.ToString("HH:mm", CultureInfo.InvariantCulture)) : null,
                durum.RefreshInSeconds),
            [.. days.Window(today).Select(ToDto)]);
    }

    /// <summary>Panel okuması: güncel ve gelecek bütün kayıtlar (pencere yok, D13); geçmiş dönmez (D4).</summary>
    internal static IReadOnlyList<PublicSpecialDayDto> ForPanel(SpecialHours? special, TimeZoneId timeZone, DateTimeOffset now) =>
        special is null
            ? []
            : [.. special.Current(BranchClock.LocalDate(TimeZoneInfo.FindSystemTimeZoneById(timeZone.Value), now)).Select(ToDto)];

    private static PublicSpecialDayDto ToDto(SpecialDay day) =>
        new(
            Date(day.From),
            Date(day.Through),
            [.. day.Intervals.Select(interval => new PublicOpeningIntervalDto(interval.StartText, interval.EndText))]);

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
