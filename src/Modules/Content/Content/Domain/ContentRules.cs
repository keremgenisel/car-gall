using System.Text.RegularExpressions;
using CarGallery.Modules.Content.Contracts.Pages;

namespace CarGallery.Modules.Content.Domain;

/// <summary>
/// `0017` C12, R13, R17 — alan kurallarının TEK yeri. Komutlar her kayıtta dolu alanları
/// buradan geçirir (D30); ziyaretçi tarafının bağlantı şeması listesi bununla aynıdır (D26).
/// Uzunluk BAYT değil karakterdir (AC-115): Unicode skaler değerleri sayılır.
/// </summary>
internal static partial class ContentRules
{
    internal const int TitleMax = 120;
    internal const int BodyMax = 50_000;
    internal const int SlugMin = 2;
    internal const int SlugMax = 80;
    internal const int QuestionMax = 200;
    internal const int AnswerMax = 2_000;

    // `0022` R4 · D5 — ana sayfa metinlerinin sınırları; sayım aynı kural (Length, D15).
    internal const int HomeTitleMax = 120;
    internal const int HomeDescriptionMax = 500;

    /// <summary>D32 — yalnız boşluktan oluşan değer boştur; <c>null</c> da boştur.</summary>
    internal static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value;

    /// <summary>Karakter sayısı (Unicode skaler değeri).</summary>
    internal static int Length(string value) => value.EnumerateRunes().Count();

    /// <summary>Slug'ın biçim ve uzunluk hatası; geçerliyse <c>null</c>. Tekillik komutun işi.</summary>
    internal static string? SlugError(string slug)
    {
        if (slug.Length is < SlugMin or > SlugMax || !SlugPattern().IsMatch(slug))
        {
            return ContentErrors.SlugInvalid;
        }

        return ContentReservedSlugs.Contains(slug) ? ContentErrors.SlugReserved : null;
    }

    /// <summary>
    /// R17 — gövdedeki <c>[metin](hedef)</c> bağlantılarından biri izinsiz şemadaysa hata.
    /// Ham HTML REDDEDİLMEZ (R17 metin olarak basar); reddedilen yalnız şemadır (D26).
    /// </summary>
    internal static string? LinkError(string markdown)
    {
        foreach (Match match in LinkPattern().Matches(markdown))
        {
            if (!IsAllowedTarget(match.Groups["target"].Value))
            {
                return ContentErrors.LinkSchemeNotAllowed;
            }
        }

        return null;
    }

    /// <summary>
    /// İzinli hedef: <c>http</c>, <c>https</c>, <c>mailto</c>, <c>tel</c> ya da şemasız
    /// (göreli) adres. Şema büyük-küçük harfe duyarsız okunur ve başındaki boşluk ile
    /// kontrol karakterleri ile CommonMark'ın açılı ayraçları atılarak bakılır —
    /// <c>" JavaScript:"</c> ve <c>"&lt;javascript:x&gt;"</c> de şemadır.
    /// </summary>
    internal static bool IsAllowedTarget(string target)
    {
        string temiz = new([.. target.Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c) && c is not '<' and not '>')]);
        Match scheme = SchemePattern().Match(temiz);

        return !scheme.Success
            || scheme.Groups[1].Value.ToLowerInvariant() is "http" or "https" or "mailto" or "tel";
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    // Hedefin TAMAMI okunur (boşluk dahil): tarayıcının URL çözümleyicisi sekme ve satır
    // sonunu atar, yani "java\tscript:" bir çizicinin elinde şemaya dönüşebilir. Sunucu
    // temkinli tarafta kalır; bunun bedeli bağlantı olmayan bir dizginin de reddedilmesidir.
    [GeneratedRegex(@"\[[^\]]*\]\((?<target>[^)]*)\)", RegexOptions.CultureInvariant)]
    private static partial Regex LinkPattern();

    [GeneratedRegex("^([a-zA-Z][a-zA-Z0-9+.-]*):", RegexOptions.CultureInvariant)]
    private static partial Regex SchemePattern();
}
