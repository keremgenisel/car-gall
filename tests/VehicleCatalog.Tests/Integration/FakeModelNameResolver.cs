using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Model adı VehicleReference'a aittir; bu testlerde o sınırın ötesi sahtelenir.
/// Gerçek adaptör adım 9'da, VehicleReference.Contracts üzerinden gelir.
/// </summary>
internal sealed class FakeModelNameResolver : IVehicleModelNameResolver
{
    private readonly Dictionary<Guid, Dictionary<string, string>> namesByModel = [];

    internal void Register(Guid vehicleModelId, string tr, string ar, string en) =>
        namesByModel[vehicleModelId] = new Dictionary<string, string>
        {
            ["tr"] = tr,
            ["ar"] = ar,
            ["en"] = en,
        };

    public Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        Locale locale,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, string> resolved = [];

        foreach (Guid id in vehicleModelIds)
        {
            resolved[id] = namesByModel.TryGetValue(id, out Dictionary<string, string>? names)
                && names.TryGetValue(locale.Code, out string? name)
                    ? name
                    : "Toyota Corolla";
        }

        return Task.FromResult<IReadOnlyDictionary<Guid, string>>(resolved);
    }

    /// <summary>
    /// `0012b` **D13** — ilanın adresi kanonik addan türüyor. Sahte Latin bir ad veriyor:
    /// boş ya da Arapça bir ad `ListingSlug`'ı `invalid_source` ile reddettirir ve testin
    /// ölçtüğü şey adres üretimi değil.
    /// </summary>
    public Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(
            vehicleModelIds.ToDictionary(id => id, _ => "Toyota Corolla"));

}
