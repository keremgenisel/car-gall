using System.Text;
using System.Text.RegularExpressions;

namespace CarGallery.Host.Seo;

/// <summary>
/// `0004` **D11** · tuzak 7 — içerik sayfasının açıklaması: gövdenin (`0017` R16'nın CommonMark
/// alt kümesi) biçim işaretlerinden arındırılmış düz metninin ilk en çok <c>limit</c> karakteri,
/// kelime ortasında kesilmeden. Başlık işaretleri, liste imleri, kalın/italik yıldızları ve
/// bağlantının hedefi düşer; bağlantının METNİ kalır.
/// </summary>
internal static partial class ContentDescription
{
    internal static string From(string body, int limit)
    {
        StringBuilder duz = new();

        foreach (string satir in body.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            string temiz = ListMarker().Replace(Heading().Replace(satir, string.Empty), string.Empty);
            temiz = Link().Replace(temiz, "${metin}");
            temiz = temiz.Replace("**", string.Empty, StringComparison.Ordinal)
                .Replace("*", string.Empty, StringComparison.Ordinal);
            duz.Append(temiz).Append(' ');
        }

        string metin = Whitespace().Replace(duz.ToString(), " ").Trim();

        if (metin.Length <= limit)
        {
            return metin;
        }

        // Sınırdan sonraki karakter boşluksa kesim kelime sınırındadır; değilse son boşluğa geri çekilir.
        int kesim = char.IsWhiteSpace(metin[limit]) ? limit : metin.LastIndexOf(' ', limit - 1);

        return (kesim > 0 ? metin[..kesim] : metin[..limit]).TrimEnd();
    }

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Heading();

    [GeneratedRegex(@"^\s*(?:[-*+]|\d+[.)])\s+", RegexOptions.CultureInvariant)]
    private static partial Regex ListMarker();

    [GeneratedRegex(@"\[(?<metin>[^\]]*)\]\([^)]*\)", RegexOptions.CultureInvariant)]
    private static partial Regex Link();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
