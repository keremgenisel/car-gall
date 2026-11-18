using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure;

/// <summary>
/// `0015` D17 — ziyaretçinin gördüğü değer listesiyle AYNI kaynak: `GetByKindAsync`,
/// `/api/reference/fuel-types` ve `/transmission-types` uçlarının okuduğu sorgu. Küme
/// locale'den bağımsızdır; yalnız slug'lar okunur (`BodyTypeCatalog` deseni).
/// </summary>
internal sealed class ReferenceTermCatalog(IReferenceTermQueries queries, ShowcaseLocaleOptions localeOptions)
    : IReferenceTermCatalog
{
    public async Task<IReadOnlySet<string>> GetCanonicalSlugsAsync(string kind, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReferenceTermDto> terms =
            await queries.GetByKindAsync(kind, localeOptions.Default.Code, cancellationToken);
        return terms.Select(term => term.Slug).ToHashSet(StringComparer.Ordinal);
    }
}
