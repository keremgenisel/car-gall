using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// `0018` **D30** — vitrin okuma modelinden okunur. Model yalnız Yayında ve Rezerve ilanları
/// taşır ve durum geçişiyle AYNI transaction'da yazılır (`0012b` D12); rezerve bayrağı
/// kalkmış satır "Yayında"dır. Locale almaz ve çeviri çözmez — `VehicleReference`'a düşmez
/// (`0018` C12).
/// </summary>
internal sealed class ListingSummaryQueries(VehicleCatalogDbContext context) : IListingSummaryQueries
{
    public async Task<IReadOnlyList<ListingSummaryDto>> FindPublishedAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(listingIds);

        if (listingIds.Count == 0)
        {
            return [];
        }

        List<Guid> kimlikler = [.. listingIds.Distinct()];

        return await context.ShowcaseListings
            .AsNoTracking()
            .Where(row => kimlikler.Contains(row.ListingId) && !row.IsReserved)
            .Select(row => new ListingSummaryDto(
                row.ListingId,
                row.Slug,
                row.VehicleModelId,
                row.ModelYear,
                row.MileageKilometers,
                row.PriceOnRequest
                    ? new ShowcasePriceDto(true, null, null, null)
                    : new ShowcasePriceDto(false, row.PriceAmount, row.PriceCurrencyCode, row.PriceMinorUnitExponent),
                row.BranchId,
                row.PublishedAt))
            .ToListAsync(cancellationToken);
    }
}
