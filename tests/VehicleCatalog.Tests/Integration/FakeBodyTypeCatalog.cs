using CarGallery.Modules.VehicleCatalog.Application.Showcase;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Kanonik küme VehicleReference'a aittir; bu testlerde o sınırın ötesi sahtelenir.
/// Küme, VehicleReference migration'ının seed ettiği değerlerle aynıdır.
/// </summary>
internal sealed class FakeBodyTypeCatalog : IBodyTypeCatalog
{
    private static readonly HashSet<string> CanonicalSlugs =
        new(["sedan", "suv", "hatchback", "station-wagon", "coupe", "pickup"], StringComparer.Ordinal);

    public Task<IReadOnlySet<string>> GetCanonicalSlugsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(CanonicalSlugs);
}
