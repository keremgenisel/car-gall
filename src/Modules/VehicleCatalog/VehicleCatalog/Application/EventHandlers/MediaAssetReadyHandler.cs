using CarGallery.Modules.Media.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.EventHandlers;

internal sealed class MediaAssetReadyHandler(VehicleCatalogDbContext context)
{
    public async Task HandleAsync(MediaAssetReady notification, Guid vehicleId, CancellationToken cancellationToken)
    {
        // Medya sahipsizdir; hangi araca ait olduğu Catalog'da bilinir.
        //
        // YALNIZCA kapak varlığı yazılır. Gelen her olayda üzerine yazmak, üç görselli bir
        // araçta en son hazır olan görseli kapak yapardı — medya işleme süreleri farklı ve
        // teslimat sıra garantisi vermiyor (docs/architecture.md § 2.C). Kapak, sıralı
        // listenin ilk elemanıdır (docs/domain.md) ve bu karar domain'e aittir, olay
        // sırasına değil.
        int updated = await context.ShowcaseListings
            .Where(listing => listing.VehicleId == vehicleId && listing.CoverAssetId == notification.MediaAssetId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(listing => listing.CoverMediaUrl, notification.Url),
                cancellationToken);

        _ = updated;
    }
}
