using CarGallery.Modules.Content.Contracts.Home;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Content.Application;

/// <summary>Panelin bir dildeki ana sayfa metinleri: alan anahtarı → kayıtlı değer (boş = varsayılan).</summary>
internal sealed record AdminHomeTextLocaleDto(string Locale, IReadOnlyDictionary<string, string> Values);

/// <summary>`0022` — panelin ana sayfa metinleri: sürüm ve YALNIZ etkin dillerin alanları (AC-36).</summary>
internal sealed record AdminHomeTextsDto(int Version, IReadOnlyList<AdminHomeTextLocaleDto> Locales);

/// <summary>
/// `0022` — ana sayfa metinlerinin okuma yolu: panel (sürümlü, etkin diller) ve vitrin
/// (bir dil, boş alan <c>null</c>). Etkin listeden çıkarılmış bir dilin satırı veritabanında
/// kalsa bile hiçbir yanıtta görünmez (AC-36).
/// </summary>
internal sealed class HomeTextQueries(ContentDbContext context, ContentLocaleOptions locales) : IHomeTextQueries
{
    private static readonly HomeTextsDto Bos = new(null, null, null, null, null, null);

    /// <summary>Panelin okuması.</summary>
    internal async Task<AdminHomeTextsDto> GetForAdminAsync(CancellationToken cancellationToken)
    {
        HomeTexts kayit = await LoadAsync(cancellationToken);

        return new AdminHomeTextsDto(
            kayit.Version,
            [
                .. locales.Enabled.Select(locale => new AdminHomeTextLocaleDto(
                    locale,
                    HomeTextField.All.ToDictionary(
                        alan => alan.Key,
                        alan => kayit.Localization(locale)?.Get(alan.Key) ?? string.Empty,
                        StringComparer.Ordinal))),
            ]);
    }

    /// <inheritdoc />
    public async Task<HomeTextsDto> FindAsync(string locale, CancellationToken cancellationToken)
    {
        if (!locales.IsEnabled(locale))
        {
            return Bos;
        }

        HomeTextLocalization? satir = await context.HomeTexts
            .AsNoTracking()
            .Where(item => item.Id == HomeTexts.SingletonId)
            .SelectMany(item => item.Localizations)
            .FirstOrDefaultAsync(item => item.Locale == locale, cancellationToken);

        if (satir is null)
        {
            return Bos;
        }

        return new HomeTextsDto(
            Dolu(satir.IntroTitle),
            Dolu(satir.IntroBody),
            Dolu(satir.FormTitle),
            Dolu(satir.FeaturedTitle),
            Dolu(satir.EmptyTitle),
            Dolu(satir.EmptyBody));
    }

    private static string? Dolu(string value) => ContentRules.Normalize(value) is { Length: > 0 } dolu ? dolu : null;

    private Task<HomeTexts> LoadAsync(CancellationToken cancellationToken) =>
        context.HomeTexts.AsNoTracking().FirstAsync(item => item.Id == HomeTexts.SingletonId, cancellationToken);
}
