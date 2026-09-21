using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure;

/// <summary>
/// Kanonik slug kümesi locale'den BAĞIMSIZDIR (C13: "locale'den bağımsız, İngilizce ad"),
/// bu yüzden sorgu tek bir locale ile yapılır ve yalnız slug'lar okunur.
/// </summary>
internal sealed class BodyTypeCatalog(IBodyTypeQueries queries, ShowcaseLocaleOptions localeOptions)
    : IBodyTypeCatalog
{
    public async Task<IReadOnlySet<string>> GetCanonicalSlugsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<BodyTypeDto> bodyTypes =
            await queries.GetAllAsync(localeOptions.Default.Code, cancellationToken);

        return bodyTypes.Select(bodyType => bodyType.Slug).ToHashSet(StringComparer.Ordinal);
    }
}
