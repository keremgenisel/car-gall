namespace CarGallery.Modules.Dealership.Domain;

/// <summary>`0027` D8 — "şu an" durumu: açık mı, şubenin bugünü, sıradaki değişim, yenileme süresi.</summary>
internal sealed record OpenNow(bool Open, DateOnly Today, (DateOnly Date, TimeOnly Time)? Next, int RefreshInSeconds);

/// <summary>
/// `0027` **D6**, **D7** — "şu an açık mı?" sunucuda, şubenin IANA saat diliminde ve verilen anla
/// (çağıran `IClock`'tan okur) hesaplanır; sunucunun yerel saati ve saat dilimi hiç kullanılmaz
/// (Yasak Listesi 30, 35).
///
/// <para><b>Yöntem.</b> Günlerin etkin aralıkları (özel gün varsa onunki, yoksa haftalığın) GERÇEK
/// anlara çevrilir; karşılaştırma anlarla yapılır, duvar saatiyle değil. Çeviri bölge kurallarıyla:</para>
/// <list type="bullet">
/// <item>var olmayan yerel saat (ileri geçiş) ondan sonraki ilk geçerli dakikaya kayar (AC-87);</item>
/// <item>iki kez yaşanan yerel saat (geri geçiş) ilk, yani erken anı alır (AC-89, AC-90);</item>
/// <item>23:59 bitiş gün sonudur: ertesi yerel gece yarısının anı (tuzak 2). Var olmayan gece yarısı
/// (ör. `Asia/Beirut`) günün ilk geçerli anıdır (AC-91).</item>
/// </list>
/// <para>Değen anlar birleşir; gece yarısını geçen ve iki güne bölünmüş aralığın zinciri buradan
/// çıkar (tuzak 3). Zincir 7 günü aşarsa kapanış söylenmez (D7).</para>
/// </summary>
internal static class BranchClock
{
    /// <summary>`0025` D3 — 23:59 bitiş gün sonudur.</summary>
    private static readonly TimeOnly EndOfDay = new(23, 59);

    /// <summary>D8 — yenileme süresinin üst sınırı (25 saatlik gün).</summary>
    internal const int MaxRefreshSeconds = 90000;

    internal static OpenNow Compute(OpeningHours weekly, SpecialHours special, TimeZoneInfo zone, DateTimeOffset now)
    {
        DateOnly today = LocalDate(zone, now);
        DateOnly last = special.Days.Count == 0 ? today : Max(today, special.Days.Max(day => day.Through));

        // D7 (B4 d) — açılış arama sınırı max(bugün, en son through) + 7 (o gün dahil). Bir gün önceden
        // başlanır: dünden taşan zincir; bir gün sonrası: sınır gününün zinciri.
        DateOnly searchEnd = last.AddDays(7);
        List<(DateTimeOffset Start, DateTimeOffset End)> merged = Merge(weekly, special, zone, today.AddDays(-1), searchEnd.AddDays(1));
        DateTimeOffset midnight = Instant(zone, today.AddDays(1), TimeOnly.MinValue);

        foreach ((DateTimeOffset start, DateTimeOffset end) in merged)
        {
            // B4 (c) — başlangıç anı açık, bitiş anı kapalı.
            if (start <= now && now < end)
            {
                bool surekli = end - now > TimeSpan.FromDays(7);
                return new OpenNow(true, today, surekli ? null : Local(zone, end), Refresh(now, surekli ? midnight : Min(end, midnight)));
            }
        }

        foreach ((DateTimeOffset start, _) in merged)
        {
            if (start > now && LocalDate(zone, start) <= searchEnd)
            {
                return new OpenNow(false, today, Local(zone, start), Refresh(now, Min(start, midnight)));
            }
        }

        return new OpenNow(false, today, null, Refresh(now, midnight));
    }

    /// <summary>Şubenin yerel takvimindeki bugünü (D4, D8).</summary>
    internal static DateOnly LocalDate(TimeZoneInfo zone, DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    private static List<(DateTimeOffset Start, DateTimeOffset End)> Merge(
        OpeningHours weekly,
        SpecialHours special,
        TimeZoneInfo zone,
        DateOnly from,
        DateOnly through)
    {
        List<(DateTimeOffset Start, DateTimeOffset End)> anlar = [];

        for (DateOnly day = from; day <= through; day = day.AddDays(1))
        {
            // D3 — özel gün kapsadığı günü tamamen ezer.
            IReadOnlyList<OpeningInterval> intervals = special.Covering(day)?.Intervals ?? weekly.For(day.DayOfWeek);

            foreach (OpeningInterval interval in intervals)
            {
                DateTimeOffset end = interval.End == EndOfDay
                    ? Instant(zone, day.AddDays(1), TimeOnly.MinValue)
                    : Instant(zone, day, interval.End);
                anlar.Add((Instant(zone, day, interval.Start), end));
            }
        }

        List<(DateTimeOffset Start, DateTimeOffset End)> merged = [];

        foreach ((DateTimeOffset start, DateTimeOffset end) in anlar.OrderBy(pair => pair.Start))
        {
            if (merged.Count > 0 && start <= merged[^1].End)
            {
                merged[^1] = (merged[^1].Start, Max(merged[^1].End, end));
            }
            else if (end > start)
            {
                merged.Add((start, end));
            }
        }

        return merged;
    }

    /// <summary>
    /// Yerel tarih + saat → an, bölge kurallarıyla (D7, PM şartı c): var olmayan saat ilk geçerli
    /// dakikaya kayar, iki kez yaşanan saat erken anı alır. Sabit fark ve "+24 saat" yoktur.
    /// </summary>
    private static DateTimeOffset Instant(TimeZoneInfo zone, DateOnly date, TimeOnly time)
    {
        DateTime local = date.ToDateTime(time, DateTimeKind.Unspecified);

        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(1);
        }

        TimeSpan offset = zone.IsAmbiguousTime(local)
            ? zone.GetAmbiguousTimeOffsets(local).Max()
            : zone.GetUtcOffset(local);

        return new DateTimeOffset(local, offset);
    }

    private static (DateOnly Date, TimeOnly Time) Local(TimeZoneInfo zone, DateTimeOffset instant)
    {
        DateTime local = TimeZoneInfo.ConvertTime(instant, zone).DateTime;
        return (DateOnly.FromDateTime(local), new TimeOnly(local.Hour, local.Minute));
    }

    /// <summary>D8 — gerçek saniye, yukarı yuvarlanır; en az 1, en çok 90000.</summary>
    private static int Refresh(DateTimeOffset now, DateTimeOffset change) =>
        (int)Math.Clamp(Math.Ceiling((change - now).TotalSeconds), 1, MaxRefreshSeconds);

    private static DateOnly Max(DateOnly left, DateOnly right) => left > right ? left : right;

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right) => left > right ? left : right;

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right) => left < right ? left : right;
}
