using System.Text;
using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// `0024` **D9**(c) · P9 — performans ortamında galeri profilinin EN KÖTÜ hâli. Yalnız geliştirme
/// bayrağıyla çağrılır (<c>Development:SeedGalleryProfile</c>); üretim yolunda yoktur. `0025`
/// şube programını, `0026` logoyu bu sınıfa ekler: perf koşusu tek bayrakla önceki kartların
/// tohumunu korur (`0025` AC-71, `0026` AC-77).
/// </summary>
internal static class GalleryProfileDevelopmentSeed
{
    /// <summary>
    /// AC-54'ün kurgusu: D1'in altı platformu, her adres TAM 200 bayt (D2'nin sınırı), yolu Arapça
    /// harflerle dolu (UTF-8'de iki bayt), hiçbirinde <c>&amp;</c> yok.
    /// </summary>
    internal static IReadOnlyDictionary<string, string?> SocialLinks() =>
        SocialPlatform.All.ToDictionary(
            platform => platform.Code,
            platform => (string?)WorstCase($"https://{platform.Hosts[^1]}/"),
            StringComparer.Ordinal);

    /// <summary>
    /// `0025` AC-60'ın kurgusu: yedi günün her birinde üç aralık ve 21 (açılış, kapanış) çiftinin
    /// hepsi farklı (gün başına dakika farklı), yani yapısal veride hiçbir öğe birleşmez.
    /// </summary>
    internal static IReadOnlyDictionary<string, IReadOnlyList<OpeningIntervalInput>?> OpeningHours() =>
        Domain.OpeningHours.Days.Select((day, index) => (day, index)).ToDictionary(
            pair => pair.day,
            pair => (IReadOnlyList<OpeningIntervalInput>?)
            [
                new($"08:{pair.index:D2}", $"10:{pair.index:D2}"),
                new($"12:{pair.index:D2}", $"14:{pair.index:D2}"),
                new($"16:{pair.index:D2}", $"21:{pair.index:D2}"),
            ],
            StringComparer.Ordinal);

    /// <summary>
    /// `0027` P10 · D15 · AC-70 — D12'nin en kötü durumu: penceredeki 12 kayıt, üçer aralık
    /// (yarından başlayıp iki günde bir, son kayıt bugün + 23; pencere bugün + 30).
    /// </summary>
    internal static IReadOnlyList<SpecialDayInput?> SpecialHours(DateOnly today) =>
    [
        .. Enumerable.Range(0, Domain.SpecialHours.MaxDays).Select(index =>
        {
            string day = today.AddDays(1 + 2 * index).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            // Aralıklar `0025` tohumunun Pazartesi'sinden: saat sabiti yazılmaz (Yasak Listesi 34).
            return (SpecialDayInput?)new SpecialDayInput(day, day, [.. OpeningHours()[Domain.OpeningHours.Days[0]]!]);
        }),
    ];

    private static string WorstCase(string root)
    {
        int kalan = SocialLink.MaxUtf8Bytes - Encoding.UTF8.GetByteCount(root);
        // Tek kalan bayt ASCII ile kapanır: değer yine tam sınırda.
        return root + new string('ع', kalan / 2) + (kalan % 2 == 1 ? "a" : string.Empty);
    }
}
