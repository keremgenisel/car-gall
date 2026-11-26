using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Content.Application;

/// <summary>
/// `0017` C4 — ziyaretçi okuması: YALNIZ yayındaki dil döner; taslak dilin metni yanıtın
/// hiçbir yerinde geçmez (AC-58, AC-112). `faq` o dilde yayında ama o dilde yayındaki
/// girdisi yoksa sayfa yayında DEĞİLMİŞ gibi davranır (R20, D34).
/// </summary>
internal sealed class ContentPublicQueries(ContentDbContext context, ContentLocaleOptions locales) : IContentPublicQueries
{
    /// <inheritdoc />
    public async Task<ContentPagePublicDto?> FindBySlugAsync(string locale, string slug, CancellationToken cancellationToken)
    {
        if (!locales.IsEnabled(locale))
        {
            return null;
        }

        ContentPage? page = await context.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Localizations.Any(satir =>
                    satir.Locale == locale && satir.Slug == slug && satir.Status == PublicationStatus.Published),
                cancellationToken);

        if (page is null)
        {
            return null;
        }

        List<FaqEntry> girdiler = page.SystemKey == SystemPageKeys.Faq ? await FaqEntriesAsync(cancellationToken) : [];
        List<ContentFaqItemDto> buDilde = Published(girdiler, locale);

        if (page.SystemKey == SystemPageKeys.Faq && buDilde.Count == 0)
        {
            return null;
        }

        PageLocalization satir = page.Localization(locale)!;

        List<ContentPageAlternateDto> alternatifler =
        [
            .. locales.Enabled
                .Where(diger => diger != locale)
                .Where(diger => IsVisible(page, diger, girdiler))
                .Select(diger => new ContentPageAlternateDto(diger, page.Localization(diger)!.Slug)),
        ];

        return new ContentPagePublicDto(page.SystemKey, satir.Title, satir.Body, alternatifler, buDilde);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ContentNavigationLinkDto>> NavigationAsync(string locale, CancellationToken cancellationToken)
    {
        if (!locales.IsEnabled(locale))
        {
            return [];
        }

        List<ContentPage> sistem = await context.Pages
            .AsNoTracking()
            .Where(page => page.SystemKey != null)
            .ToListAsync(cancellationToken);
        List<FaqEntry> girdiler = await FaqEntriesAsync(cancellationToken);

        return
        [
            .. SystemPageKeys.FooterOrder
                .Select(key => sistem.Find(page => page.SystemKey == key))
                .OfType<ContentPage>()
                .Where(page => IsVisible(page, locale, girdiler))
                .Select(page => new ContentNavigationLinkDto(page.SystemKey!, page.Localization(locale)!.Title, page.Localization(locale)!.Slug)),
        ];
    }

    /// <inheritdoc />
    public async Task<string?> FindCurrentSlugAsync(string locale, string oldSlug, CancellationToken cancellationToken)
    {
        if (!locales.IsEnabled(locale))
        {
            return null;
        }

        ContentPage? page = await context.Pages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.OldSlugs.Any(eski => eski.Locale == locale && eski.Slug == oldSlug),
                cancellationToken);

        if (page is null)
        {
            return null;
        }

        List<FaqEntry> girdiler = page.SystemKey == SystemPageKeys.Faq ? await FaqEntriesAsync(cancellationToken) : [];

        // Yönlendirme yalnız yayındaki güncel adrese: taslağa 301 vermek ziyaretçiyi
        // "bulunamadı"ya taşımaktan başka bir şey yapmazdı.
        return IsVisible(page, locale, girdiler) ? page.Localization(locale)!.Slug : null;
    }

    public async Task<IReadOnlyList<string>> ListVisibleSlugsAsync(string locale, CancellationToken cancellationToken)
    {
        if (!locales.IsEnabled(locale))
        {
            return [];
        }

        // Görünürlük kuralı `FindBySlugAsync` ve altlıkla AYNI yardımcıdan (IsVisible): sitemap'te
        // olup açılınca 404 veren ya da açılıp sitemap'te olmayan bir sayfa olmaz (CLARIFY B10).
        List<ContentPage> sayfalar = await context.Pages
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        List<FaqEntry> girdiler = await FaqEntriesAsync(cancellationToken);

        List<string> sluglar = [.. sayfalar
            .Where(page => IsVisible(page, locale, girdiler))
            .Select(page => page.Localization(locale)!.Slug)];

        sluglar.Sort(StringComparer.Ordinal);

        return sluglar;
    }

    private static bool IsVisible(ContentPage page, string locale, List<FaqEntry> girdiler)
    {
        PageLocalization? satir = page.Localization(locale);

        if (satir is not { Status: PublicationStatus.Published } || satir.Slug.Length == 0)
        {
            return false;
        }

        return page.SystemKey != SystemPageKeys.Faq || Published(girdiler, locale).Count > 0;
    }

    private static List<ContentFaqItemDto> Published(List<FaqEntry> girdiler, string locale) =>
    [
        .. girdiler
            .Select(entry => entry.Localization(locale))
            .Where(satir => satir is { Status: PublicationStatus.Published })
            .Select(satir => new ContentFaqItemDto(satir!.Question, satir.Answer)),
    ];

    private Task<List<FaqEntry>> FaqEntriesAsync(CancellationToken cancellationToken) =>
        context.FaqEntries
            .AsNoTracking()
            .OrderBy(entry => entry.Position)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);
}
