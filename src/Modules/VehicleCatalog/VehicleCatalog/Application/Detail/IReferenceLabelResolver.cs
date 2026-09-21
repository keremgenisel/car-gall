using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Application.Detail;

/// <summary>
/// Referans terim etiketlerinin dışa bakan portu. Uygulaması Infrastructure'da ve
/// yalnızca `VehicleReference.Contracts` üzerinden konuşur (architecture.md § 2.A).
/// </summary>
internal interface IReferenceLabelResolver
{
    Task<IReadOnlyDictionary<(string Kind, string Slug), string>> ResolveAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        Locale locale,
        CancellationToken cancellationToken);
}
