using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

internal sealed class ShowcaseListingQueries(
    VehicleCatalogDbContext context,
    IVehicleModelNameResolver modelNameResolver,
    IBodyTypeCatalog bodyTypeCatalog,
    IReferenceTermCatalog termCatalog,
    ShowcaseLocaleOptions localeOptions) : IShowcaseListingQueries
{
    public async Task<PagedResult<ShowcaseListingDto>> GetAsync(
        ShowcaseListingFilter filter,
        CancellationToken cancellationToken)
    {
        IReadOnlySet<string> canonicalSlugs = await bodyTypeCatalog.GetCanonicalSlugsAsync(cancellationToken);

        // 0015 — yakıt ve vitesin kanonik kümesi YALNIZ o eksen istendiyse okunur: eksensiz
        // istek (vitrinin çoğu) `VehicleReference`'a fazladan bir tur ödemez (C9).
        IReadOnlySet<string> fuelSlugs = filter.FuelTypes.Count == 0
            ? NoSlugs
            : await termCatalog.GetCanonicalSlugsAsync(ReferenceTermKinds.FuelType, cancellationToken);
        IReadOnlySet<string> transmissionSlugs = filter.TransmissionTypes.Count == 0
            ? NoSlugs
            : await termCatalog.GetCanonicalSlugsAsync(ReferenceTermKinds.TransmissionType, cancellationToken);

        ShowcaseListingRequest request =
            ShowcaseListingRequest.Normalize(filter, canonicalSlugs, fuelSlugs, transmissionSlugs);
        Locale locale = localeOptions.ResolveOrDefault(filter.Locale);

        IQueryable<ShowcaseListing> query = context.ShowcaseListings.AsNoTracking();

        // R2 — eksen İÇİNDE "veya" (`Contains`), eksenler ARASINDA "ve" (ardışık `Where`).
        // Eşleştirme tam ve harfe duyarlı; ham kolon üzerinde ILIKE/lower() yok (C1).
        if (request.BodyTypes.Count > 0)
        {
            List<string> slugs = [.. request.BodyTypes.Select(slug => slug.Value)];
            query = query.Where(listing => slugs.Contains(listing.BodyTypeSlug));
        }

        if (request.FuelTypes.Count > 0)
        {
            List<string> fuels = [.. request.FuelTypes];
            query = query.Where(listing => fuels.Contains(listing.FuelTypeSlug));
        }

        if (request.TransmissionTypes.Count > 0)
        {
            List<string> transmissions = [.. request.TransmissionTypes];
            query = query.Where(listing => transmissions.Contains(listing.TransmissionTypeSlug));
        }

        // R15 — sınırlar DAHİL.
        if (request.Year is { } year)
        {
            if (year.Min is int yearMin)
            {
                query = query.Where(listing => listing.ModelYear >= yearMin);
            }

            if (year.Max is int yearMax)
            {
                query = query.Where(listing => listing.ModelYear <= yearMax);
            }
        }

        if (request.Mileage is { } mileage)
        {
            if (mileage.Min is int mileageMin)
            {
                query = query.Where(listing => listing.MileageKilometers >= mileageMin);
            }

            if (mileage.Max is int mileageMax)
            {
                query = query.Where(listing => listing.MileageKilometers <= mileageMax);
            }
        }

        if (request.Price is { } price)
        {
            // R4 · P4 — fiyat aralığının EN AZ BİR sınırı verildiyse "fiyat sorunuz" ilan
            // listelenmez: bir fiyatı olmayan araç bir fiyat aralığına uymaz. Karşılaştırma
            // `decimal` üzerinde (C3); sınır ana birimde tam sayıdır.
            query = query.Where(listing => !listing.PriceOnRequest && listing.PriceAmount != null);

            if (price.Min is int priceMin)
            {
                decimal lower = priceMin;
                query = query.Where(listing => listing.PriceAmount >= lower);
            }

            if (price.Max is int priceMax)
            {
                decimal upper = priceMax;
                query = query.Where(listing => listing.PriceAmount <= upper);
            }
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int page = request.ResolvePage(totalCount);

        List<ShowcaseListing> rows = await query
            .InOrder(request.Sort)
            .Skip((page - 1) * request.ResolvedPageSize)
            .Take(request.ResolvedPageSize)
            .ToListAsync(cancellationToken);

        List<ShowcaseListingDto> items =
            await new ShowcaseCards(modelNameResolver, localeOptions).ToDtosAsync(rows, locale, cancellationToken);

        return new PagedResult<ShowcaseListingDto>(items, page, request.ResolvedPageSize, totalCount);
    }

    private static readonly IReadOnlySet<string> NoSlugs = new HashSet<string>(StringComparer.Ordinal);
}
