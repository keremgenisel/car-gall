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

    private static CarGallery.BuildingBlocks.Locale Locale(string code) => CarGallery.BuildingBlocks.Locale.Create(code).Value;
}
