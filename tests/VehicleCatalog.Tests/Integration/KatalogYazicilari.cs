using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Application;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012b` — komutların kazandığı iki iş birliği: vitrin yansıtması (**D12**) ve duyuru
/// yazan birim (**D14**). Testler onları elle kurmak zorunda; fabrika, imza
/// değişikliğini on ayrı testin gövdesine taşımamak için var.
/// </summary>
internal static class KatalogYazicilari
{
    internal static ShowcaseProjectionWriter Yansitma(
        VehicleCatalogDbContext context,
        IMediaAssetQueries? medya = null) =>
        new(context, new SahteSubeSorgusu(), medya ?? new FakeMediaAssetQueries());

    internal static VehicleCatalogUnitOfWork Birim(VehicleCatalogDbContext context, IClock clock) =>
        new(context, new OutboxWriter<VehicleCatalogDbContext>(context, clock), clock);
}
