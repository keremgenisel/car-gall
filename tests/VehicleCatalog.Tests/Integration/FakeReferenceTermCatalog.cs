using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Yakıt ve vitesin kanonik kümesi `VehicleReference`'a aittir; burada o sınırın ötesi
/// sahtelenir. Değerler `ReferenceTermConfiguration`'ın seed ettiği değerlerle aynıdır.
/// </summary>
internal sealed class FakeReferenceTermCatalog : IReferenceTermCatalog
{
    private static readonly Dictionary<string, HashSet<string>> Sets = new(StringComparer.Ordinal)
    {
        [ReferenceTermKinds.FuelType] = new(["petrol", "diesel", "hybrid", "electric"], StringComparer.Ordinal),
        [ReferenceTermKinds.TransmissionType] = new(["manual", "automatic"], StringComparer.Ordinal),
    };

    public Task<IReadOnlySet<string>> GetCanonicalSlugsAsync(string kind, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlySet<string>>(Sets[kind]);
}
