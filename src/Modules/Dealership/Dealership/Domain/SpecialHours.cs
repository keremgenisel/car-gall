using System.Globalization;
using System.Text.Json;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// `0027` **D2** — bir özel gün: şubenin yerel takviminde `From`–`Through` (ikisi dahil) ve her gün
/// aynı olan aralıklar. Aralık yoksa tam gün kapalıdır.
/// </summary>
internal sealed record SpecialDay(DateOnly From, DateOnly Through, IReadOnlyList<OpeningInterval> Intervals)
{
    internal bool Covers(DateOnly day) => day >= From && day <= Through;
}

/// <summary>Kaydetme gövdesindeki bir özel gün; şekli doğrulanmamıştır (`0027` D14).</summary>
internal sealed record SpecialDayInput(string? From, string? Through, IReadOnlyList<OpeningIntervalInput?>? Intervals);

/// <summary>
/// `0027` **D2**, **D3**, **D4**, **D14** — bir şubenin özel günleri. Biçim doğrulaması TEK
/// kapıdadır: tarih ham dizgeden tek bir <see cref="DateOnly.TryParseExact(string?, string?, IFormatProvider?, DateTimeStyles, out DateOnly)"/>
/// çağrısıyla, saat <see cref="OpeningHours.ParseTime"/> ile ayrıştırılır; ayrıştıran çağrı aynı
/// zamanda doğrulayandır (`0025` dersi: düzenli ifade + `ParseExact` iki kapıydı ve `500` üretti).
/// Hiçbir girdi istisna fırlatmaz.
///
/// <para>Sıra: kayıt başına kurallar gönderilen HER kayda uygulanır (geçmiş ama hatalı kayıt da
/// hatadır); sonra geçmiş kayıtlar sessizce düşer; küme kuralları (<c>too_far</c>,
/// <c>overlap</c>, <c>too_many</c>) kalanlara uygulanır. Hata anahtarındaki indeks daima
/// gönderilen dizinin indeksidir (AC-104).</para>
/// </summary>
internal sealed class SpecialHours
{
    internal const string InvalidDate = "special_hours.invalid_date";
    internal const string EndBeforeStart = "special_hours.end_before_start";
    internal const string TooLong = "special_hours.too_long";
    internal const string TooFar = "special_hours.too_far";
    internal const string Overlap = "special_hours.overlap";
    internal const string TooMany = "special_hours.too_many";
    internal const string InvalidEntry = "special_hours.invalid_entry";
    internal const string IntervalsRequired = "special_hours.intervals_required";

    /// <summary>D4 — güncel ya da gelecek en çok 12 kayıt; geçmiş sayılmaz.</summary>
    internal const int MaxDays = 12;

    /// <summary>D4 — bir kayıt en çok 60 gün sürer.</summary>
    internal const int MaxLength = 60;

    /// <summary>D4 — <c>through</c> bugünden en çok 366 gün ileride.</summary>
    internal const int MaxAhead = 366;

    /// <summary>D8 — ziyaretçiye gösterilen pencere: <c>from</c> ≤ bugün + 30.</summary>
    internal const int WindowDays = 30;

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private SpecialHours(IReadOnlyList<SpecialDay> days) => Days = days;

    internal static SpecialHours Empty { get; } = new([]);

    /// <summary>Kayıtlar, <c>From</c>'a göre sıralı.</summary>
    internal IReadOnlyList<SpecialDay> Days { get; }

    /// <summary>D4 — bitmemiş kayıtlar (<c>Through</c> ≥ bugün).</summary>
    internal IEnumerable<SpecialDay> Current(DateOnly today) => Days.Where(day => day.Through >= today);

    /// <summary>D8 — ziyaretçi penceresi: bitmemiş ve <c>From</c> ≤ bugün + 30.</summary>
    internal IEnumerable<SpecialDay> Window(DateOnly today) =>
        Current(today).Where(day => day.From <= today.AddDays(WindowDays));

    internal SpecialDay? Covering(DateOnly day) => Days.FirstOrDefault(item => item.Covers(day));

    /// <summary>
    /// Gövdenin listesini doğrular. <paramref name="today"/> şubenin (gövdedeki saat diliminin)
    /// bugünüdür; <c>null</c>'sa (saat dilimi geçersiz, alan zaten hatalı) küme kuralları atlanır.
    /// Hata varsa <c>null</c> döner ve hatalar <paramref name="errors"/>'a eklenir.
    /// </summary>
    internal static SpecialHours? Parse(IReadOnlyList<SpecialDayInput?> input, DateOnly? today, FieldErrors errors)
    {
        bool hata = false;
        List<(int Index, SpecialDay Day)> gecerli = [];

        for (int index = 0; index < input.Count; index++)
        {
            string field = $"specialHours.{index}";

            if (input[index] is not { } item)
            {
                errors.Add(field, InvalidEntry);
                hata = true;
                continue;
            }

            bool kayitHatali = false;
            DateOnly? from = item.From is null ? null : ParseDate(item.From);

            if (from is null)
            {
                errors.Add($"{field}.from", InvalidDate);
                kayitHatali = true;
            }

            // D2 — `through` yok ya da `null`: tek gün. Boş metin geçersizdir.
            DateOnly? through = item.Through is null ? from : ParseDate(item.Through);

            if (item.Through is not null && through is null)
            {
                errors.Add($"{field}.through", InvalidDate);
                kayitHatali = true;
            }

            IReadOnlyList<OpeningInterval>? intervals = ParseIntervals(item.Intervals, field, errors);
            kayitHatali |= intervals is null;

            if (from is { } start && through is { } end)
            {
                if (end < start)
                {
                    errors.Add($"{field}.through", EndBeforeStart);
                    kayitHatali = true;
                }
                else if (end.DayNumber - start.DayNumber + 1 > MaxLength)
                {
                    errors.Add($"{field}.through", TooLong);
                    kayitHatali = true;
                }
            }

            if (kayitHatali)
            {
                hata = true;
                continue;
            }

            gecerli.Add((index, new SpecialDay(from!.Value, through!.Value, intervals!)));
        }

        if (today is not { } bugun)
        {
            return null;
        }

        // D4 — geçmiş kayıt hata değildir, sessizce düşer.
        List<(int Index, SpecialDay Day)> kalan = [.. gecerli.Where(pair => pair.Day.Through >= bugun)];

        foreach ((int index, SpecialDay day) in kalan.Where(pair => pair.Day.Through.DayNumber - bugun.DayNumber > MaxAhead))
        {
            errors.Add($"specialHours.{index}.through", TooFar);
            hata = true;
        }

        // D3 — ortak tarihli iki kayıt: hata, sonraki kaydın (`<i>` büyük olan) `from` alanında.
        for (int i = 0; i < kalan.Count; i++)
        {
            for (int j = i + 1; j < kalan.Count; j++)
            {
                if (kalan[i].Day.From <= kalan[j].Day.Through && kalan[j].Day.From <= kalan[i].Day.Through)
                {
                    errors.Add($"specialHours.{Math.Max(kalan[i].Index, kalan[j].Index)}.from", Overlap);
                    hata = true;
                }
            }
        }

        if (kalan.Count > MaxDays)
        {
            errors.Add("specialHours", TooMany);
            hata = true;
        }

        return hata ? null : new SpecialHours([.. kalan.Select(pair => pair.Day).OrderBy(day => day.From)]);
    }

    /// <summary>Aralıklar `0025` D3'ün kurallarıyla ve kodlarıyla; alanlar kaydın altında.</summary>
    private static List<OpeningInterval>? ParseIntervals(
        IReadOnlyList<OpeningIntervalInput?>? items,
        string field,
        FieldErrors errors)
    {
        if (items is null)
        {
            errors.Add($"{field}.intervals", IntervalsRequired);
            return null;
        }

        bool hata = false;
        List<OpeningInterval> intervals = [];

        for (int index = 0; index < items.Count; index++)
        {
            string alan = $"{field}.intervals.{index}";

            if (items[index] is not { } item)
            {
                errors.Add(alan, InvalidEntry);
                hata = true;
                continue;
            }

            TimeOnly? start = OpeningHours.ParseTime(item.Start);
            TimeOnly? end = OpeningHours.ParseTime(item.End);

            if (start is null)
            {
                errors.Add($"{alan}.start", OpeningHours.InvalidTime);
            }

            if (end is null)
            {
                errors.Add($"{alan}.end", OpeningHours.InvalidTime);
            }

            if (start is null || end is null)
            {
                hata = true;
                continue;
            }

            if (end <= start)
            {
                errors.Add($"{alan}.end", OpeningHours.EndNotAfterStart);
                hata = true;
                continue;
            }

            intervals.Add(new OpeningInterval(start.Value, end.Value));
        }

        if (items.Count > OpeningHours.MaxIntervalsPerDay)
        {
            errors.Add(field, OpeningHours.TooManyIntervals);
            hata = true;
        }

        List<OpeningInterval> sirali = [.. intervals.OrderBy(interval => interval.Start)];

        // `0025` tuzak 5 — uç uca değen iki aralık da çakışmadır.
        if (sirali.Zip(sirali.Skip(1)).Any(pair => pair.Second.Start <= pair.First.End))
        {
            errors.Add(field, OpeningHours.Overlap);
            hata = true;
        }

        return hata ? null : sirali;
    }

    /// <summary>D14 — tek çağrı: `yyyy-MM-dd`, değişmez kültür, boşluk ve satır sonu kabul edilmez.</summary>
    private static DateOnly? ParseDate(string value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
            ? date
            : null;

    internal string? ToJson() =>
        Days.Count == 0
            ? null
            : JsonSerializer.Serialize(
                Days.Select(day => new Stored(
                    day.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    day.Through.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    [.. day.Intervals.Select(interval => new StoredInterval(interval.StartText, interval.EndText))])),
                Json);

    internal static SpecialHours FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return Empty;
        }

        List<Stored> stored = JsonSerializer.Deserialize<List<Stored>>(json, Json)
            ?? throw new InvalidOperationException("Özel günler okunamadı.");

        return new SpecialHours([.. stored
            .Select(item => new SpecialDay(
                DateOnly.ParseExact(item.From, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateOnly.ParseExact(item.Through, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                [.. item.Intervals.Select(interval => new OpeningInterval(
                    TimeOnly.ParseExact(interval.Start, "HH:mm", CultureInfo.InvariantCulture),
                    TimeOnly.ParseExact(interval.End, "HH:mm", CultureInfo.InvariantCulture)))]))
            .OrderBy(day => day.From)]);
    }

    private sealed record Stored(string From, string Through, List<StoredInterval> Intervals);

    private sealed record StoredInterval(string Start, string End);
}
