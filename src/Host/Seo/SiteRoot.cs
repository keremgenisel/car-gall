namespace CarGallery.Host.Seo;

/// <summary>
/// `0004` **D2** · **P1** — sitenin mutlak kökü: kanonik, dil alternatifleri, `og:url`,
/// `og:image`'in göreli adresleri, sitemap, robots.txt ve e-postalardaki bağlantılar TEK bu
/// değerden kurulur. İstek başlıklarından (`Host`, `X-Forwarded-Host`) türetilmez (C10).
///
/// <para><b>Kural:</b> yalnız <c>http</c>/<c>https</c> şemalı mutlak adres; yol, sorgu, parça
/// ve kullanıcı bilgisi taşımaz; sondaki <c>/</c> normalize edilir. <c>Uri.TryCreate(…,
/// Absolute)</c> yetmiyordu: Unix'te <c>/galeri</c>'yi <c>file:</c> adresi sayıp kabul ediyor
/// (CLARIFY B5).</para>
///
/// <para><b>Anahtar adı değişmedi</b> (<c>Notifications:PublicBaseUrl</c>, P1): değer artık
/// siteye ait, adı tarihsel. Hata mesajı anahtarın adını söyler (AC-22).</para>
/// </summary>
internal sealed class SiteRoot
{
    internal const string ConfigurationKey = "Notifications:PublicBaseUrl";

    private SiteRoot(string value) => Value = value;

    /// <summary>Sonunda <c>/</c> olmayan kök: <c>https://galeri.test</c>.</summary>
    internal string Value { get; }

    /// <summary><paramref name="path"/> <c>/</c> ile başlar: <c>/tr/cars</c>.</summary>
    internal string Absolute(string path) => Value + path;

    internal static SiteRoot Parse(string? raw)
    {
        string deger = raw?.Trim() ?? string.Empty;

        bool gecerli = (deger.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || deger.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            && Uri.TryCreate(deger, UriKind.Absolute, out Uri? adres)
            && adres.Scheme is "http" or "https"
            && adres.AbsolutePath == "/"
            && adres.Query.Length == 0
            && adres.Fragment.Length == 0
            && adres.UserInfo.Length == 0
            && adres.Host.Length > 0;

        if (!gecerli)
        {
            throw new InvalidOperationException(
                $"'{ConfigurationKey}' yapılandırması sitenin kökü olmalı: yalnız http/https şemalı, yol "
                + "bölümü olmayan mutlak bir adres (örn. https://galeri-alanadi.com). Uygulama başlamıyor.");
        }

        return new SiteRoot(new Uri(deger).GetLeftPart(UriPartial.Authority));
    }
}
