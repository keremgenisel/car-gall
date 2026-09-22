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

    /// <summary>
    /// Kanonik Latin ad: dilden BAĞIMSIZDIR (docs/domain.md, Kanonik Ad). Locale almaması
    /// bir eksiklik değil, şartın kendisidir — 0005 R7/AC-18 talebin anlık görüntüsüne
    /// ziyaretçinin dilindeki adı değil, kanonik adı kopyalatır. Arapça bırakılan talebin
    /// model adı yalnız Arapça saklanırsa, o dili okumayan danışman için talep okunamaz
    /// hâle gelir ve kopya sonradan düzeltilemez.
    ///
    /// <see cref="ResolveNamesAsync"/> bunun yerini TUTAMAZ: o, çevirisi varsa çeviriyi
    /// döner; kanonik ada yalnız çeviri yoksa düşer.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        CancellationToken cancellationToken);
}

/// <summary>Kapalı kümeli referans terimi: slug + ziyaretçinin locale'indeki etiketi.</summary>
public sealed record ReferenceTermDto(string Kind, string Slug, string Label);

public interface IReferenceTermQueries
{
    /// <summary>
    /// Verilen (tür, slug) çiftlerinin etiketlerini çözer. Karşılığı olmayan locale'de
    /// kanonik Latin ada düşülür; ham anahtar ASLA dönmez.
    /// </summary>
    Task<IReadOnlyDictionary<(string Kind, string Slug), string>> ResolveLabelsAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        string locale,
        CancellationToken cancellationToken);
}
