using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// 0009 D5 — ilan kimliğinden GÜNCEL slug. Vitrin okuma modelinden okunur: yayından
/// kalkmış bir ilanın bağlantısı zaten tıklanabilir olmamalı, dolayısıyla "bulunamadı"
/// doğru cevaptır (AC-32).
/// </summary>
internal sealed class ListingSlugQueries(VehicleCatalogDbContext context) : IListingSlugQueries
{
    public Task<string?> FindSlugAsync(Guid listingId, CancellationToken cancellationToken) =>
        context.ShowcaseListings
            .AsNoTracking()
            .Where(listing => listing.ListingId == listingId)
            .Select(listing => listing.Slug)
            .SingleOrDefaultAsync(cancellationToken);
}
