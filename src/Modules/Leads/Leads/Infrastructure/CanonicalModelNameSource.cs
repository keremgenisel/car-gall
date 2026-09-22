using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.Leads.Infrastructure;

/// <summary>
/// Application'daki portun dışa bakan adaptörü. `VehicleReference`'a yalnızca
/// `Contracts` üzerinden konuşur; bu `Leads`'in İKİNCİ tek atlamasıdır, zincirin ikinci
/// halkası değil (docs/architecture.md § 2.A, Yasak Listesi 13).
/// </summary>
internal sealed class CanonicalModelNameSource(IVehicleModelQueries queries) : ICanonicalModelNameSource
{
    public async Task<string?> FindAsync(Guid vehicleModelId, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, string> names =
            await queries.ResolveCanonicalNamesAsync([vehicleModelId], cancellationToken);

        return names.TryGetValue(vehicleModelId, out string? name) ? name : null;
    }
}
