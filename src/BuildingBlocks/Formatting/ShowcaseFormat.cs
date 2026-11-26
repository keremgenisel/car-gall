using System.Globalization;

namespace CarGallery.BuildingBlocks.Formatting;

/// <summary>
/// Vitrinin fiyat, km ve yıl yazımı — `frontend/src/shared/i18n/format.ts`'in
/// <c>formatMoney</c>, <c>formatMileage</c>, <c>formatYear</c> çıktısıyla BAYT BAYT aynı.
/// **Ön yüz otoritedir** ve iki taraf aynı fikstürü ölçer
/// (`frontend/src/shared/i18n/format.fixtures.json`, `0018` **D24** · **D32**).
///
/// <para>`0004` **P4** — `Notifications`'ın içinden (eski <c>NewsletterFormat</c>) buraya taşındı:
/// Host'un belge açıklaması (`0004` AC-7, AC-9) aynı yazımı ister ve başka modülün internal
/// tipine erişemez (C4). Yalnız BİÇİM taşır; hangi alanın gösterileceği, "fiyat sorunuz" gibi
/// kararlar çağıranındır.</para>
///
/// <para><b>Ölçüm (D32):</b> km ve yıl nötr kültürün <c>N0</c>/<c>D</c> biçimiyle birebir
/// tutuyor. Para TUTMUYOR: nötr kültürün para simgesi <c>¤</c>'dır, <c>CultureInfo</c> para
/// birimini kodla seçemez ve <c>Intl</c>'in U+00A0 boşluğunu ve Arapça U+200F yön işaretini
/// üretmez. Bölgeli kültür (<c>ar-KW</c>) ise Arap ayraçlarına (U+066C, U+066B) geçer — vitrin
/// Batı Arap rakamı ve Latin ayraç sabitliyor (<c>-u-nu-latn</c>).</para>
///
/// <para><b>Kural:</b> sayı kısmı nötr kültürün <see cref="NumberFormatInfo"/>'suyla
/// (<c>N&lt;üs&gt;</c>), para ekleri <c>Intl</c> çıktısından türetilmiş (dil × para birimi)
/// tablosundan; tabloda olmayan para birimi için dilin genel kalıbı.</para>
/// </summary>
public static class ShowcaseFormat
{
    private const string NoBreakSpace = " ";

    private const string RightToLeftMark = "‏";

    /// <summary>
    /// (dil, para birimi) → (önek, sonek). Değerler `Intl.NumberFormat(…, {style:'currency'})`
    /// çıktısından ölçüldü (Node 24.22, ICU 78.3; fikstür). Yalnız bugün kurulan iki para birimi.
    /// </summary>
    private static readonly Dictionary<(string Locale, string Currency), (string Prefix, string Suffix)> Affixes = new()
    {
        [("tr", "TRY")] = ("₺", string.Empty),
        [("ar", "KWD")] = (RightToLeftMark, NoBreakSpace + "د.ك." + RightToLeftMark),
    };

    public static string Money(decimal amount, string currencyCode, int minorUnitExponent, string locale)
    {
        CultureInfo culture = CultureOf(locale);

        // Yuvarlama AÇIKÇA (docs/conventions.md § 7). `Intl`'in varsayılanı `halfExpand`:
        // sıfırdan uzağa — aynısı.
        decimal rounded = Math.Round(amount, minorUnitExponent, MidpointRounding.AwayFromZero);
        string number = rounded.ToString("N" + minorUnitExponent.ToString(CultureInfo.InvariantCulture), culture);

        (string prefix, string suffix) = Affixes.TryGetValue((locale, currencyCode), out (string, string) known)
            ? known
            : DefaultAffixes(locale, currencyCode);

        return prefix + number + suffix;
    }

    public static string Mileage(int kilometers, string locale) =>
        kilometers.ToString("N0", CultureOf(locale));

    /// <summary>Yıl hiçbir dilde gruplama almaz: 2019, "2.019" değil.</summary>
    public static string Year(int year, string locale) =>
        year.ToString("D", CultureOf(locale));

    /// <summary>
    /// `Intl`'in simgesi olmayan para biriminde kullandığı kalıp: soldan sağa dillerde
    /// <c>KOD</c> + U+00A0 + sayı, Arapçada U+200F + sayı + U+00A0 + <c>KOD</c>.
    /// </summary>
    private static (string Prefix, string Suffix) DefaultAffixes(string locale, string currencyCode) =>
        locale == "ar"
            ? (RightToLeftMark, NoBreakSpace + currencyCode)
            : (currencyCode + NoBreakSpace, string.Empty);

    /// <summary>
    /// Nötr kültür: ayraçlar ve Batı Arap rakamları vitrinin <c>-u-nu-latn</c> sabitlemesiyle
    /// aynı. Bölgeli kültür kullanılmaz (D32 ölçümü).
    /// </summary>
    private static CultureInfo CultureOf(string locale) => CultureInfo.GetCultureInfo(locale);
}
