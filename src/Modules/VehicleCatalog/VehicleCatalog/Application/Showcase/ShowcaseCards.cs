using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// Okuma modeli satırından vitrin kartına (`0001` R9). Liste ve `0014`'ün öne çıkan bölümü
/// aynı kartı basar; kartın şekli tek yerde kurulur.
/// </summary>
internal sealed class ShowcaseCards(IVehicleModelNameResolver modelNameResolver, ShowcaseLocaleOptions localeOptions)
{
    internal async Task<List<ShowcaseListingDto>> ToDtosAsync(
        IReadOnlyList<ShowcaseListing> rows,
        Locale locale,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, string> modelNames = await modelNameResolver.ResolveAsync(
            [.. rows.Select(row => row.VehicleModelId).Distinct()],
            locale,
            cancellationToken);

        return
        [
            .. rows.Select(row => new ShowcaseListingDto(
                row.ListingId,
                row.Slug,
                row.IsReserved,
                row.CoverMediaUrl,
                modelNames.TryGetValue(row.VehicleModelId, out string? name) ? name : string.Empty,
                row.ModelYear,
                row.MileageKilometers,
                ToPrice(row),
                ResolveBranchName(row, locale)))
        ];
    }

    private static ShowcasePriceDto ToPrice(ShowcaseListing row) => row.PriceOnRequest
        ? ShowcasePriceDto.OnRequestPrice()
        : new ShowcasePriceDto(false, row.PriceAmount, row.PriceCurrencyCode, row.PriceMinorUnitExponent);

    private string ResolveBranchName(ShowcaseListing row, Locale locale)
    {
        if (row.BranchNames.TryGetValue(locale.Code, out string? name))
        {
            return name;
        }

        return row.BranchNames.TryGetValue(localeOptions.Default.Code, out string? fallback) ? fallback : string.Empty;
    }
}
