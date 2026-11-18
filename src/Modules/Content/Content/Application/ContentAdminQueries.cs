using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Content.Application;

/// <summary>Panelin bir sayfanın bir dildeki metnini görmesi; durum KOD olarak (C3).</summary>
internal sealed record AdminPageLocaleDto(string Locale, string Title, string Body, string Slug, string Status);

/// <summary>`0017` — panelin sayfası: tür, sürüm, etkin dillerin metni ve eksik diller (R9).</summary>
internal sealed record AdminPageDto(
    Guid Id,
    string? SystemKey,
    int Version,
    IReadOnlyList<AdminPageLocaleDto> Locales,
    IReadOnlyList<string> MissingLocales);

/// <summary>Panelin SSS girdisinin bir dildeki metni.</summary>
internal sealed record AdminFaqLocaleDto(string Locale, string Question, string Answer, string Status);

/// <summary>`0017` — panelin SSS girdisi; sıralı listede gelir.</summary>
internal sealed record AdminFaqEntryDto(
    Guid Id,
    int Version,
    IReadOnlyList<AdminFaqLocaleDto> Locales,
    IReadOnlyList<string> MissingLocales);

/// <summary>
/// `0017` — panelin okuma yolu. Yalnız ETKİN diller döner ve sayılır (R9, D16); satırı
/// olmayan dil boş metinli taslak olarak görünür (R3).
/// </summary>
internal sealed class ContentAdminQueries(ContentDbContext context, ContentLocaleOptions locales)
{
    /// <summary>Sistem sayfaları altlık sırasıyla, ardından serbest sayfalar oluşturulma sırasıyla.</summary>
    internal async Task<IReadOnlyList<AdminPageDto>> ListPagesAsync(CancellationToken cancellationToken)
    {
        List<ContentPage> pages = await context.Pages.AsNoTracking().ToListAsync(cancellationToken);

        return
        [
            .. pages
                .OrderBy(page => page.SystemKey is null ? int.MaxValue : SystemPageKeys.FooterOrder.ToList().IndexOf(page.SystemKey))
                .ThenBy(page => page.CreatedAt)
                .Select(ToDto),
        ];
    }

    /// <summary>Tek sayfa; yoksa <c>null</c> — silinen sayfa geri gelmez (AC-28).</summary>
    internal async Task<AdminPageDto?> FindPageAsync(Guid id, CancellationToken cancellationToken)
    {
        ContentPage? page = await context.Pages.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return page is null ? null : ToDto(page);
    }

    /// <summary>SSS girdileri global sırayla (R18).</summary>
    internal async Task<IReadOnlyList<AdminFaqEntryDto>> ListFaqAsync(CancellationToken cancellationToken)
    {
        List<FaqEntry> entries = await context.FaqEntries
            .AsNoTracking()
            .OrderBy(entry => entry.Position)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);

        return [.. entries.Select(ToDto)];
    }

    private AdminPageDto ToDto(ContentPage page)
    {
        List<AdminPageLocaleDto> satirlar = [];
        List<string> eksik = [];

        foreach (string locale in locales.Enabled)
        {
            PageLocalization? satir = page.Localization(locale);
            satirlar.Add(new AdminPageLocaleDto(
                locale,
                satir?.Title ?? string.Empty,
                satir?.Body ?? string.Empty,
                satir?.Slug ?? string.Empty,
                PublicationStatusCodes.ToCode(satir?.Status ?? PublicationStatus.Draft)));

            // D29 — eksik = R6'nın şartını karşılamayan dil; taslakta beklemek eksiklik değil.
            if (satir is not { IsComplete: true })
            {
                eksik.Add(locale);
            }
        }

        return new AdminPageDto(page.Id, page.SystemKey, page.Version, satirlar, eksik);
    }

    private AdminFaqEntryDto ToDto(FaqEntry entry)
    {
        List<AdminFaqLocaleDto> satirlar = [];
        List<string> eksik = [];

        foreach (string locale in locales.Enabled)
        {
            FaqLocalization? satir = entry.Localization(locale);
            satirlar.Add(new AdminFaqLocaleDto(
                locale,
                satir?.Question ?? string.Empty,
                satir?.Answer ?? string.Empty,
                PublicationStatusCodes.ToCode(satir?.Status ?? PublicationStatus.Draft)));

            if (satir is not { IsComplete: true })
            {
                eksik.Add(locale);
            }
        }

        return new AdminFaqEntryDto(entry.Id, entry.Version, satirlar, eksik);
    }
}
