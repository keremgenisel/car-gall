using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// Model adı referans veridir ve VehicleReference'a aittir (docs/architecture.md § 3).
/// Bu arayüz Catalog'un dışa bakan portudur; uygulaması Infrastructure'da, yalnızca
/// VehicleReference.Contracts üzerinden konuşur. Domain bu portu hiç bilmez.
/// </summary>
internal interface IVehicleModelNameResolver
{
    Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        Locale locale,
        CancellationToken cancellationToken);
}
