using CarGallery.Host.Localization;
using CarGallery.Modules.Content.Contracts.Pages;

namespace CarGallery.Host.Seo;

internal enum SeoPageKind
{
    Home,
    List,
    Detail,
    Content,
    Newsletter,

    /// <summary>`frontend-003` **D1** · **D2** — `/{locale}/favorites`: ziyaretçinin kendi seçimi.</summary>
    Favorites,

    /// <summary>`frontend-003` **D1** · **D2** · **D3** — `/{locale}/compare?listing=…`.</summary>
    Compare,
    Admin,
    NotFound,
}

/// <summary>
/// `0004` **P6** · **D8** — belgenin sayfa türü, yalnız yoldan. Ön yüzün rota çözümüyle aynı
/// sınıflandırma (`frontend/src/shared/lib/route.ts`); ikisi ayrışırsa sunucu 404 verirken uç
/// başka bir ekran basar (tuzak 9). Tanınmayan her vitrin yolu — iki ve daha çok parçalı yol,
/// detayın altındaki yol, ayrılmış ad (`/tr/admin`, `/tr/newsletter`) — <see cref="SeoPageKind.NotFound"/>'dur.
/// </summary>
internal sealed record SeoPage(SeoPageKind Kind, string? Slug = null)
{
    internal static SeoPage Of(PathString path, string locale)
    {
        if (path.StartsWithSegments(LocalePrefixMiddleware.AdminPathPrefix))
        {
            return new(SeoPageKind.Admin);
        }

        string[] parcalar = (path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parcalar.Length == 0 || parcalar[0] != locale)
        {
            return new(SeoPageKind.NotFound);
        }

        if (parcalar.Length == 1)
        {
            return new(SeoPageKind.Home);
        }

        if (parcalar[1] == LocalePrefixMiddleware.ListSegment)
        {
            return parcalar.Length switch
            {
                2 => new(SeoPageKind.List),
                3 => new(SeoPageKind.Detail, Uri.UnescapeDataString(parcalar[2])),
                _ => new(SeoPageKind.NotFound),
            };
        }

        // `frontend-003` D1 — iki ayrılmış ad; alt yolları (`/tr/favorites/x`) aşağıda NotFound'dur.
        if (parcalar.Length == 2 && parcalar[1] == FavoritesSegment)
        {
            return new(SeoPageKind.Favorites);
        }

        if (parcalar.Length == 2 && parcalar[1] == CompareSegment)
        {
            return new(SeoPageKind.Compare);
        }

        if (IndexHtmlRenderer.IsNewsletterLinkPage(path, Locale(locale)))
        {
            return new(SeoPageKind.Newsletter);
        }

        if (parcalar.Length == 2 && LocalePrefixMiddleware.ContentSlugOf(path, locale) is string slug)
        {
            return new(SeoPageKind.Content, slug);
        }

        return new(SeoPageKind.NotFound);
    }

    /// <summary>`frontend-003` D1 — `ContentReservedSlugs`'taki iki ad; ön yüzde `route.ts` aynası.</summary>
    internal const string FavoritesSegment = "favorites";

    internal const string CompareSegment = "compare";

    /// <summary>
    /// `0018` D35 · `frontend-003` D2 — yanıtı <c>Referrer-Policy: no-referrer</c> taşıyan sayfalar:
    /// adresin kendisi (bülten anahtarı, karşılaştırılan ilanlar) başka bir siteye sızmamalı.
    /// </summary>
    internal bool IsPrivate => Kind is SeoPageKind.Newsletter or SeoPageKind.Favorites or SeoPageKind.Compare;

    private static CarGallery.BuildingBlocks.Locale Locale(string code) => CarGallery.BuildingBlocks.Locale.Create(code).Value;
}
