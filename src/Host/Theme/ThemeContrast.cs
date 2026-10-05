using System.Globalization;

namespace CarGallery.Host.Theme;

/// <summary>Ayrıştırılmış renk. Alfa taşınır ama kontrast hesabına girmez.</summary>
internal readonly record struct ThemeColor(byte Red, byte Green, byte Blue);

/// <summary>
/// WCAG 2.1 bağıl parlaklık ve kontrast oranı (docs/frontend.md § 7).
///
/// <para><b>Neden `decimal`:</b> para kuralı (Yasak Listesi 26) `double`/`float` ile alan
/// tanımlanmasını yasaklıyor ve mimari test bunu tarıyor. Oran parasal değil ama kuralın
/// istisnasını açmaya değmez: 2.4 kuvveti tek bir ifadede alınıp sonuç `decimal`'e
/// çevriliyor, hiçbir yerde `double` alan tanımlanmıyor. Eşik karşılaştırması için
/// hassasiyet fazlasıyla yeterli.</para>
///
/// <para><b>Biçim TEK TİPTİR</b> (C11): altı haneli hex, alfa taşıyan token'lar için sekiz
/// haneli. `rgb()`, `oklch()` ve renk adı kabul edilmez. İki iş birden yapıyor: kontrast
/// hesabı ayrıştırma belirsizliğinden kurtulur, ve bu değer `index.html`'e enjekte
/// edildiği için serbest metin yüzeyi hiç açılmaz.</para>
/// </summary>
internal static class ThemeContrast
{
    /// <summary>
    /// Yapılandırmadan gelen değer: YALNIZ altı haneli (AC-13). Sekiz haneli biçim
    /// yalnızca sabit `color-backdrop` içindir ve o yapılandırılamaz.
    /// </summary>
    internal static bool TryParseSixDigit(string value, out ThemeColor color) =>
        TryParse(value, allowAlpha: false, out color);

    /// <summary>`tokens.css`'ten okunan değer: alfa taşıyan token da ayrıştırılabilir.</summary>
    internal static bool TryParseAny(string value, out ThemeColor color) =>
        TryParse(value, allowAlpha: true, out color);

    /// <summary>
    /// Oran her zaman ≥ 1 döner: açık olan paya, koyu olan paydaya gider. Sıraya
    /// bağımlı bir oran, çiftin hangi ucunun metin olduğuna göre farklı sonuç verir ve
    /// eşik karşılaştırması anlamsızlaşır.
    /// </summary>
    internal static decimal Ratio(ThemeColor first, ThemeColor second)
    {
        decimal one = RelativeLuminance(first);
        decimal other = RelativeLuminance(second);

        decimal lighter = Math.Max(one, other);
        decimal darker = Math.Min(one, other);

        return (lighter + 0.05m) / (darker + 0.05m);
    }

    private static bool TryParse(string value, bool allowAlpha, out ThemeColor color)
    {
        color = default;

        if (value is null)
        {
            return false;
        }

        string text = value.Trim();

        if (text.Length == 0 || text[0] != '#')
        {
            return false;
        }

        string digits = text[1..];

        if (digits.Length != 6 && !(allowAlpha && digits.Length == 8))
        {
            return false;
        }

        if (!digits.All(Uri.IsHexDigit))
        {
            return false;
        }

        color = new ThemeColor(Channel(digits, 0), Channel(digits, 2), Channel(digits, 4));

        return true;
    }

    private static byte Channel(string digits, int offset) =>
        byte.Parse(digits.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static decimal RelativeLuminance(ThemeColor color) =>
        (0.2126m * Linear(color.Red))
        + (0.7152m * Linear(color.Green))
        + (0.0722m * Linear(color.Blue));

    /// <summary>
    /// sRGB → doğrusal ışık. Kuvvet tek bir ifadede alınıyor ve sonuç anında `decimal`'e
    /// çevriliyor; bu yüzden hiçbir `double` alan tanımlanmıyor.
    /// </summary>
    private static decimal Linear(byte channel) => (decimal)(
        channel / 255.0 <= 0.03928
            ? channel / 255.0 / 12.92
            : Math.Pow(((channel / 255.0) + 0.055) / 1.055, 2.4));
}
