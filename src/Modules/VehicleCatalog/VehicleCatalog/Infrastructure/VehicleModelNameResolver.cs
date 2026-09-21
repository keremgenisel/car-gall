using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure;

/// <summary>
/// Application'daki portun dışa bakan adaptörü. VehicleReference'a YALNIZCA Contracts
/// üzerinden, salt okuma, tek atlama ve aynı istek içinde konuşur — senkron doğrudan
/// çağrının üç koşulu da sağlanır (docs/architecture.md § 2.A).
/// </summary>
internal sealed class VehicleModelNameResolver(IVehicleModelQueries queries) : IVehicleModelNameResolver
{
    public Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        Locale locale,
        CancellationToken cancellationToken) =>
        queries.ResolveNamesAsync(vehicleModelIds, locale.Code, cancellationToken);
}
