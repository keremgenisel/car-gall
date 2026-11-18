using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.EventHandlers;

/// <summary>
/// `0016b` D25 — işlemesi başarısız olan varlığın galeri yuvalarını `başarısız` yapar; galeri
/// o yuvayı göstermez ve sayaç onu saymaz (D14, AC-4). Görünürlük kararı vermez: ilan
/// vitrinde kalır, yalnız kopyalanmış bir kolon tazelenir (`MediaAssetReadyHandler` gibi).
/// </summary>
internal sealed class MediaAssetFailedHandler(VehicleCatalogDbContext context, IClock clock)
    : IdempotentIntegrationEventHandler<MediaAssetFailed, VehicleCatalogDbContext>(context, clock)
{
    /// <inheritdoc />
    protected override async Task HandleCoreAsync(
        MediaAssetFailed integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        List<ShowcaseMediaVariant> bos = [];

        await Context.ShowcaseListingMedia
            .Where(yuva => yuva.MediaAssetId == integrationEvent.MediaAssetId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(yuva => yuva.Status, ShowcaseMediaStatus.Failed)
                    .SetProperty(yuva => yuva.Variants, bos),
                cancellationToken);
    }
}
