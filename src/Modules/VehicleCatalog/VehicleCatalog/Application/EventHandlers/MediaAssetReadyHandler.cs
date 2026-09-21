using CarGallery.Modules.Media.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.EventHandlers;

internal sealed class MediaAssetReadyHandler(VehicleCatalogDbContext context)
{
    internal async Task HandleAsync(MediaAssetReady notification, Guid vehicleId, CancellationToken cancellationToken)
    {
        // Medya sahipsizdir; hangi araca ait olduğu Catalog'da bilinir.
        List<ShowcaseListing> affected = await context.ShowcaseListings
            .Where(listing => listing.VehicleId == vehicleId)
            .ToListAsync(cancellationToken);

        foreach (ShowcaseListing listing in affected)
        {
            listing.CoverMediaUrl = notification.Url;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
