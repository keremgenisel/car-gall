using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

internal sealed class ShowcaseListingQueries(
    VehicleCatalogDbContext context,
    IVehicleModelNameResolver modelNameResolver,
    ShowcaseLocaleOptions localeOptions) : IShowcaseListingQueries
{
    public async Task<PagedResult<ShowcaseListingDto>> GetAsync(
        ShowcaseListingFilter filter,
        CancellationToken cancellationToken)
    {
        ShowcaseListingRequest request = ShowcaseListingRequest.Normalize(filter.BodyTypes, filter.Page);
        Locale locale = localeOptions.ResolveOrDefault(filter.Locale);

        IQueryable<ShowcaseListing> query = context.ShowcaseListings.AsNoTracking();

        if (request.BodyTypes.Count > 0)
        {
            // R3: seçilenlerden HERHANGİ BİRİNE uyan ilanlar. Eşleştirme tam ve harfe
            // duyarlı; ham kolon üzerinde ILIKE/lower() yok (C11, Yasak Listesi 27).
            List<string> slugs = [.. request.BodyTypes.Select(slug => slug.Value)];
            query = query.Where(listing => slugs.Contains(listing.BodyTypeSlug));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int page = request.ResolvePage(totalCount);

        List<ShowcaseListing> rows = await query
            .OrderByDescending(listing => listing.ShowcaseSortWeight)
            .ThenByDescending(listing => listing.PublishedAt)
            .ThenBy(listing => listing.ListingId)
            .Skip((page - 1) * ShowcaseListingRequest.PageSize)
            .Take(ShowcaseListingRequest.PageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyDictionary<Guid, string> modelNames = await modelNameResolver.ResolveAsync(
            [.. rows.Select(row => row.VehicleModelId).Distinct()],
            locale,
            cancellationToken);

        List<ShowcaseListingDto> items =
        [
            .. rows.Select(row => new ShowcaseListingDto(
                row.ListingId,
                row.IsReserved,
                row.CoverMediaUrl,
                modelNames.TryGetValue(row.VehicleModelId, out string? name) ? name : string.Empty,
                row.ModelYear,
                row.MileageKilometers,
                ToPrice(row),
                ResolveBranchName(row, locale)))
        ];

        return new PagedResult<ShowcaseListingDto>(items, page, ShowcaseListingRequest.PageSize, totalCount);
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
