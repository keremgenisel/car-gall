using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// Vitrin okuma modeli yalnız görünür ilanları taşır (`0001`): görünmeyen ilanın satırı yoktur,
/// bu yüzden burada durum filtresi yoktur — "görünür" kuralı tek yerde, yansıtıcıda kalır.
/// </summary>
internal sealed class ShowcaseSitemapQueries(VehicleCatalogDbContext context) : IShowcaseSitemapQueries
{
    public async Task<IReadOnlyList<string>> ListVisibleSlugsAsync(CancellationToken cancellationToken)
    {
        List<string> slugs = await context.ShowcaseListings
            .AsNoTracking()
            .Select(listing => listing.Slug)
            .ToListAsync(cancellationToken);

        slugs.Sort(StringComparer.Ordinal);

        return slugs;
    }
}
