namespace CarGallery.Modules.VehicleReference.Contracts;

/// <summary>
/// Kasa tipi kanonik değeri ve ziyaretçinin locale'indeki etiketi. Kanonik ad dilden
/// bağımsızdır; slug ondan üretilir (docs/domain.md, Kanonik Ad).
/// </summary>
public sealed record BodyTypeDto(string Slug, string Label);

public interface IBodyTypeQueries
{
    Task<IReadOnlyList<BodyTypeDto>> GetAllAsync(string locale, CancellationToken cancellationToken);
}

public interface IVehicleModelQueries
{
    /// <summary>Locale'de karşılığı olmayan model için kanonik Latin ada düşülür.</summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken);
}
