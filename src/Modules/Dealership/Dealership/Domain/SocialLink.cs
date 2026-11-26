using System.Text;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// `0024` **D1** — sosyal hesabın platformu: KAPALI ve SIRALI liste. Sıra panelde, altlıkta,
/// okuma yanıtında ve <c>sameAs</c>'ta aynıdır ve saklanmaz (D3). Görünen ad markanın kendi
/// adıdır ve koddadır (ön yüz); burada yalnız kod ve ana makine listesi yaşar (D2).
/// </summary>
internal sealed class SocialPlatform
{
    private SocialPlatform(string code, params string[] hosts)
    {
        Code = code;
        Hosts = hosts;
    }

    internal static IReadOnlyList<SocialPlatform> All { get; } =
    [
        new("instagram", "instagram.com", "www.instagram.com"),
        new("facebook", "facebook.com", "www.facebook.com", "m.facebook.com"),
        new("youtube", "youtube.com", "www.youtube.com", "m.youtube.com"),
        new("tiktok", "tiktok.com", "www.tiktok.com"),
        new("x", "x.com", "www.x.com", "twitter.com", "www.twitter.com"),
        new("linkedin", "linkedin.com", "www.linkedin.com"),
    ];

    /// <summary>Veritabanında saklanan kod (C8); çevrilmiş ad saklanmaz.</summary>
    internal string Code { get; }

    internal IReadOnlyList<string> Hosts { get; }

    /// <summary>D1'in sırasındaki yeri; listede olmayan kod için <c>-1</c>.</summary>
    internal static int OrderOf(string code)
    {
        for (int index = 0; index < All.Count; index++)
        {
            if (string.Equals(All[index].Code, code, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>D3 — D1'de olmayan kodla saklanmış satır yok sayılır; sıra D1'inkidir.</summary>
    internal static IEnumerable<SocialLink> InOrder(IEnumerable<SocialLink> links) =>
        links
            .Where(link => OrderOf(link.Platform) >= 0)
            .OrderBy(link => OrderOf(link.Platform));

    internal static SocialPlatform? Find(string code)
    {
        int index = OrderOf(code);
        return index < 0 ? null : All[index];
    }
}

/// <summary>
/// `0024` **D12** — Sosyal Hesap: galerinin D1'deki bir platformda tuttuğu, vitrinde gösterilen
/// hesap adresi. Galeri başına platform başına en çok bir tane (şemada birincil anahtar).
///
/// <para>Adres değiştirilebilir (P3): sahip olunan koleksiyonda aynı anahtarla sil-ve-ekle EF'in
/// izleyicisinde anahtar çakışmasıdır, bu yüzden değişiklik yerinde yapılır.</para>
/// </summary>
internal sealed class SocialLink
{
    internal const string Invalid = "gallery.social_link_invalid";
    internal const string TooLong = "gallery.social_link_too_long";
    internal const string PlatformUnknown = "gallery.social_platform_unknown";

    /// <summary>D2 — kırpıldıktan sonra UTF-8 bayt sınırı (CLARIFY B3; karakter değil).</summary>
    internal const int MaxUtf8Bytes = 200;

    internal SocialLink(string platform, string url)
    {
        Platform = platform;
        Url = url;
    }

    internal string Platform { get; private set; }

    internal string Url { get; private set; }

    internal void ChangeUrl(string url) => Url = url;

    /// <summary>
    /// D2 — adresin biçimsel doğrulaması. Değer kırpılır ve başka bir değişiklik yapılmadan
    /// döner; hata varsa hata kodu döner. Önce bayt sınırı, sonra biçim (P2).
    /// </summary>
    internal static (string? Url, string? Error) Validate(SocialPlatform platform, string trimmed)
    {
        if (Encoding.UTF8.GetByteCount(trimmed) > MaxUtf8Bytes)
        {
            return (null, TooLong);
        }

        return IsAccountAddress(platform, trimmed) ? (trimmed, null) : (null, Invalid);
    }

    private static bool IsAccountAddress(SocialPlatform platform, string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal))
        {
            return false;
        }

        // CLARIFY B1 — kullanıcı bilgisi taşımaz ve YAZILMIŞ her port reddedilir. `Uri.Port`
        // varsayılan `:443`'ü yazılmamışla aynı gösterir; bu yüzden yazılan otoriteye bakılır.
        if (uri.UserInfo.Length > 0 || WrittenAuthority(value).Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        // `System.Uri.Host` ile ordinal, harf duyarsız, BİREBİR: alt alan adı, son ek ya da
        // benzer harf (Kiril `а`) eşleşmez (AC-72, AC-74).
        if (!platform.Hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.Equals(uri.AbsolutePath, "/", StringComparison.Ordinal);
    }

    /// <summary>Şemadan sonraki, ilk <c>/</c>, <c>\</c>, <c>?</c> ya da <c>#</c>'e kadar olan bölüm.</summary>
    private static string WrittenAuthority(string value)
    {
        int start = value.IndexOf("://", StringComparison.Ordinal);
        string rest = start < 0 ? value : value[(start + 3)..];
        int end = rest.IndexOfAny(['/', '\\', '?', '#']);
        return end < 0 ? rest : rest[..end];
    }
}
