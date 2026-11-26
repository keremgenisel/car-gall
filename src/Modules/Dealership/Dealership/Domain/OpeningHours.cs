using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>`0025` — bir günün yerel saat aralığı. Bitiş başlangıçtan sonradır (D3).</summary>
internal sealed record OpeningInterval(TimeOnly Start, TimeOnly End)
{
    internal string StartText => Start.ToString("HH:mm", CultureInfo.InvariantCulture);

    internal string EndText => End.ToString("HH:mm", CultureInfo.InvariantCulture);
}

/// <summary>`0025` — panelden gelen ham aralık; değerler henüz doğrulanmamıştır.</summary>
internal sealed record OpeningIntervalInput(string? Start, string? End);

/// <summary>
/// `0025` **D15** — Çalışma Programı: şubenin haftanın her günü için 0..N yerel saat aralığı.
/// Aralıksız gün KAPALIDIR (`docs/domain.md` B2); hangi günün kapalı olduğu veriden okunur,
/// kodda varsayılmaz (C1).
///
/// <para>**D2** — değer şubenin DUVAR SAATİDİR: `TimeOnly`, tarih ve fark yok. UTC'ye çevrilmez;
/// şubenin saat dilimi değişse de 09:00 09:00 kalır.</para>
///
/// <para>**D4** — yedi günün hepsi boşsa program "girilmemiş"tir ve saklanmaz
/// (<see cref="IsEmpty"/>; kolon <c>NULL</c>).</para>
/// </summary>
internal sealed partial class OpeningHours
{
    internal const string InvalidTime = "opening_hours.invalid_time";
    internal const string EndNotAfterStart = "opening_hours.end_not_after_start";
    internal const string TooManyIntervals = "opening_hours.too_many_intervals";
    internal const string Overlap = "opening_hours.overlap";
    internal const string UnknownDay = "opening_hours.unknown_day";
    internal const string MissingDay = "opening_hours.missing_day";

    /// <summary>D3 — günde en çok üç aralık; bir sınır, "tek aralık" varsayımı değil (C1).</summary>
    internal const int MaxIntervalsPerDay = 3;

    /// <summary>D6 — her dilde Pazartesi → Pazar (ISO 8601). Kodlar harfe duyarlıdır (D11).</summary>
    internal static IReadOnlyList<string> Days { get; } =
        ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly IReadOnlyDictionary<string, IReadOnlyList<OpeningInterval>> days;

    private OpeningHours(IReadOnlyDictionary<string, IReadOnlyList<OpeningInterval>> days) => this.days = days;

    /// <summary>Yedi günün hepsi boş: program "girilmemiş" (D4).</summary>
    internal bool IsEmpty => days.Values.All(intervals => intervals.Count == 0);

    /// <summary>D6 sırasıyla yedi gün; aralıklar başlangıca göre sıralı (D3).</summary>
    internal IEnumerable<(string Day, IReadOnlyList<OpeningInterval> Intervals)> InOrder() =>
        Days.Select(day => (day, days[day]));

    /// <summary>
    /// D3 · D11 · D12 — programı doğrular. Hatalar çağıranın <paramref name="errors"/>'una
    /// toplanır (şubenin diğer alan hatalarıyla aynı yanıt, AC-20, AC-21); hata yoksa programı
    /// döner. Doğrulama sıralamadan ÖNCE yapılır: hata girilen sıradaki aralığa bağlanır.
    /// </summary>
    internal static OpeningHours? Parse(
        IReadOnlyDictionary<string, IReadOnlyList<OpeningIntervalInput>?> input,
        FieldErrors errors)
    {
        bool hata = false;

        foreach (string code in input.Keys.Where(code => !Days.Contains(code, StringComparer.Ordinal)))
        {
            errors.Add($"openingHours.{code}", UnknownDay);
            hata = true;
        }

        Dictionary<string, IReadOnlyList<OpeningInterval>> parsed = new(StringComparer.Ordinal);

        foreach (string day in Days)
        {
            string field = $"openingHours.{day}";

            if (!input.TryGetValue(day, out IReadOnlyList<OpeningIntervalInput>? raw))
            {
                errors.Add(field, MissingDay);
                hata = true;
                continue;
            }

            // P1 — günün değeri `null` ise anahtar var, aralık yok: kapalı gün.
            IReadOnlyList<OpeningIntervalInput> items = raw ?? [];
            List<OpeningInterval> intervals = [];
            bool gunHatali = false;

            for (int index = 0; index < items.Count; index++)
            {
                TimeOnly? start = ParseTime(items[index].Start);
                TimeOnly? end = ParseTime(items[index].End);

                if (start is null)
                {
                    errors.Add($"{field}.{index}.start", InvalidTime);
                }

                if (end is null)
                {
                    errors.Add($"{field}.{index}.end", InvalidTime);
                }

                if (start is null || end is null)
                {
                    gunHatali = true;
                    continue;
                }

                if (end <= start)
                {
                    errors.Add($"{field}.{index}.end", EndNotAfterStart);
                    gunHatali = true;
                    continue;
                }

                intervals.Add(new OpeningInterval(start.Value, end.Value));
            }

            if (items.Count > MaxIntervalsPerDay)
            {
                errors.Add(field, TooManyIntervals);
                gunHatali = true;
            }

            List<OpeningInterval> sirali = [.. intervals.OrderBy(interval => interval.Start)];

            // Tuzak 5 — uç uca değen iki aralık (13:00 ve 13:00) da çakışmadır.
            if (sirali.Zip(sirali.Skip(1)).Any(pair => pair.Second.Start <= pair.First.End))
            {
                errors.Add(field, Overlap);
                gunHatali = true;
            }

            hata |= gunHatali;
            parsed[day] = sirali;
        }

        return hata ? null : new OpeningHours(parsed);
    }

    /// <summary>D8 — okumaların biçimi: D6 sırasıyla yedi anahtar.</summary>
    internal IReadOnlyDictionary<string, IReadOnlyList<Contracts.Gallery.PublicOpeningIntervalDto>> ToDto() =>
        InOrder().ToDictionary(
            pair => pair.Day,
            pair => (IReadOnlyList<Contracts.Gallery.PublicOpeningIntervalDto>)[.. pair.Intervals.Select(interval =>
                new Contracts.Gallery.PublicOpeningIntervalDto(interval.StartText, interval.EndText))],
            StringComparer.Ordinal);

    /// <summary>D8 biçiminde JSON: yedi anahtar, `{ "start", "end" }` dizileri. Saklama da budur.</summary>
    internal string ToJson() =>
        JsonSerializer.Serialize(
            InOrder().ToDictionary(
                pair => pair.Day,
                pair => pair.Intervals.Select(interval => new Stored(interval.StartText, interval.EndText)).ToList()),
            Json);

    internal static OpeningHours FromJson(string json)
    {
        Dictionary<string, List<Stored>> stored = JsonSerializer.Deserialize<Dictionary<string, List<Stored>>>(json, Json)
            ?? throw new InvalidOperationException("Çalışma programı okunamadı.");

        return new OpeningHours(Days.ToDictionary(
            day => day,
            day => (IReadOnlyList<OpeningInterval>)[.. (stored.GetValueOrDefault(day) ?? []).Select(item =>
                new OpeningInterval(TimeOnly.ParseExact(item.Start, "HH:mm", CultureInfo.InvariantCulture), TimeOnly.ParseExact(item.End, "HH:mm", CultureInfo.InvariantCulture)))],
            StringComparer.Ordinal));
    }

    private static TimeOnly? ParseTime(string? value) =>
        value is not null && TimeFormat().IsMatch(value)
            ? TimeOnly.ParseExact(value, "HH:mm", CultureInfo.InvariantCulture)
            : null;

    /// <summary>D3 — `HH:mm`: saat 00–23, dakika 00–59; `9:00` ve `24:00` geçersiz.</summary>
    [GeneratedRegex("^([01][0-9]|2[0-3]):[0-5][0-9]$", RegexOptions.CultureInvariant)]
    private static partial Regex TimeFormat();

    private sealed record Stored(string Start, string End);
}
