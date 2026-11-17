using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

internal sealed class ShowcaseListingQueries(
    VehicleCatalogDbContext context,
    IVehicleModelNameResolver modelNameResolver,
    IBodyTypeCatalog bodyTypeCatalog,
    ShowcaseLocaleOptions localeOptions) : IShowcaseListingQueries
{
    public async Task<PagedResult<ShowcaseListingDto>> GetAsync(
        ShowcaseListingFilter filter,
        CancellationToken cancellationToken)
    {
        IReadOnlySet<string> canonicalSlugs = await bodyTypeCatalog.GetCanonicalSlugsAsync(cancellationToken);
        ShowcaseListingRequest request =
            ShowcaseListingRequest.Normalize(filter.BodyTypes, filter.Page, canonicalSlugs, filter.PageSize);
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
            .InShowcaseOrder()
            .Skip((page - 1) * request.ResolvedPageSize)
            .Take(request.ResolvedPageSize)
            .ToListAsync(cancellationToken);

        List<ShowcaseListingDto> items =
            await new ShowcaseCards(modelNameResolver, localeOptions).ToDtosAsync(rows, locale, cancellationToken);

        return new PagedResult<ShowcaseListingDto>(items, page, request.ResolvedPageSize, totalCount);
    }
}
