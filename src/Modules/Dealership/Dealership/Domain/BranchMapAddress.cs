using System.Globalization;
using System.Text;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// `0028` **D7** · **P2** — şubenin iki harita adresi TEK yerde: "Google Haritalar'da aç" (Maps URLs,
/// anahtarsız) ve tıkla-yükle haritanın gömme adresi (Maps Embed API, anahtarlı). Saf bir işlevdir:
/// veritabanı, yapılandırma ve günlük görmez; anahtar çağırandan gelir ve hiçbir yere yazılmaz (AC-83).
/// </summary>
internal static class BranchMapAddress
{
    /// <summary>D7 — Google Maps URLs'nin istek başına sınırı; iki adrese de uygulanır (CLARIFY B3).</summary>
    internal const int MaxUrlLength = 2048;

    private const string LinkBase = "https://www.google.com/maps/search/?api=1&query=";

    private const string EmbedBase = "https://www.google.com/maps/embed/v1/place?key=";

    /// <summary>
    /// D7 — koordinat varsa <c>"{enlem},{boylam}"</c> (InvariantCulture, sondaki sıfırlar atılmış),
    /// yoksa varsayılan dildeki adres. Sonuç kodlanmamıştır.
    /// </summary>
    internal static string Query(Coordinate? coordinate, string defaultAddress) =>
        coordinate is { } point
            ? $"{Number(point.Latitude)},{Number(point.Longitude)}"
            : Normalize(defaultAddress);

    /// <summary>
    /// D7 — iki adres; kodlanmış hâllerinden biri <see cref="MaxUrlLength"/>'i aşarsa <c>null</c>
    /// (AC-91): kesilmiş bir adres yanlış yeri gösterir. Anahtar yoksa ya da yalnız boşluksa gömme
    /// adresi yoktur (AC-28).
    ///
    /// <para>QA 2 BULGU-3 — normalleşmiş sorgu boşsa (adres yalnız denetim karakteri ve boşluktan
    /// oluşuyorsa) da <c>null</c>: boş bir arama Google'da rastgele bir yer gösterir. 2048 kuralıyla
    /// aynı yol: bağlantı, kutu ve <c>hasMap</c> basılmaz.</para>
    /// </summary>
    internal static (string LinkUrl, string? EmbedUrl)? Build(Coordinate? coordinate, string defaultAddress, string locale, string? embedKey)
    {
        string raw = Query(coordinate, defaultAddress);

        if (raw.Length == 0)
        {
            return null;
        }

        string query = Uri.EscapeDataString(raw);
        string link = LinkBase + query;

        if (link.Length > MaxUrlLength)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(embedKey))
        {
            return (link, null);
        }

        string embed = $"{EmbedBase}{Uri.EscapeDataString(embedKey.Trim())}&q={query}&language={Uri.EscapeDataString(locale)}";

        return embed.Length > MaxUrlLength ? null : (link, embed);
    }

    /// <summary>
    /// `0028` **D7** · CLARIFY B8 — her denetim karakteri (Unicode <c>Cc</c>: satır sonu, sekme, NUL)
    /// tek boşluğa çevrilir; ardışık boşluklar teke iner, baştaki ve sondaki atılır (AC-97).
    /// </summary>
    internal static string Normalize(string address)
    {
        StringBuilder text = new(address.Length);
        bool space = false;

        foreach (char character in address)
        {
            bool isSpace = character == ' ' || char.IsControl(character);

            if (isSpace)
            {
                space = text.Length > 0;
                continue;
            }

            if (space)
            {
                text.Append(' ');
                space = false;
            }

            text.Append(character);
        }

        return text.ToString();
    }

    /// <summary>Tuzak 6 — <c>tr-TR</c>'de de nokta ondalık; <c>decimal(18,8)</c>'in sondaki sıfırları yok.</summary>
    internal static string Number(decimal value) => value.ToString("0.############################", CultureInfo.InvariantCulture);
}
