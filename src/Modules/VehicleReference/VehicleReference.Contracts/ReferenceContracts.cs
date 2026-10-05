namespace CarGallery.Modules.VehicleReference.Contracts;

/// <summary>
/// Kasa tipi kanonik değeri ve ziyaretçinin locale'indeki etiketi. Kanonik ad dilden
/// bağımsızdır; slug ondan üretilir (docs/domain.md, Kanonik Ad).
/// </summary>
public sealed record BodyTypeDto(string Slug, string Label);

public interface IBodyTypeQueries
{
    Task<IReadOnlyList<BodyTypeDto>> GetAllAsync(string locale, CancellationToken cancellationToken);

    /// <summary>
    /// 0010a/AC-5 — kasa tipinin referans veride GERÇEKTEN var olup olmadığı.
    /// `VehicleSpecification` yalnız slug KALIBINI denetliyor; varlık denetimi
    /// `Application` katmanında, bu tek atlamayla yapılır (D14, C5).
    /// </summary>
    Task<bool> ExistsAsync(string slug, CancellationToken cancellationToken);
}

/// <summary>Model kimliği ve personelin locale'indeki etiketi (0010a/R4).</summary>
public sealed record VehicleModelDto(Guid Id, string Label);

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

    /// <summary>
    /// 0010a/R4 — panelin model seçimi. Marka YOKTUR (D6), bu yüzden liste düzdür ve
    /// model sayısı büyüdüğünde kullanılabilirliği düşer; marka taksonomisi
    /// `VehicleReference`'ın kendi spec'ine aittir.
    /// </summary>
    Task<IReadOnlyList<VehicleModelDto>> GetAllAsync(string locale, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid vehicleModelId, CancellationToken cancellationToken);
}

/// <summary>
/// Kapalı kümeli referans türlerinin adları. Sabit olarak YAYINLANIYOR çünkü tüketen
/// modül (`VehicleCatalog`) aksi hâlde dört dizeyi kendi tarafında yeniden yazardı ve iki
/// liste bir gün ayrışırdı.
/// </summary>
public static class ReferenceTermKinds
{
    public const string FuelType = "FuelType";
    public const string TransmissionType = "TransmissionType";
    public const string DrivetrainType = "DrivetrainType";
    public const string TrimLevel = "TrimLevel";
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

    /// <summary>0010a/R4 — panelin bir türün tüm seçeneklerini listelemesi.</summary>
    Task<IReadOnlyList<ReferenceTermDto>> GetByKindAsync(
        string kind,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verilen çiftlerden referans veride BULUNMAYANLARI döner — var olanları değil.
    /// 0010a/AC-5 "altı alan AYRI AYRI doğrulanır" diyor; hangi alanın düştüğünü
    /// söyleyebilmek için eksik olanın kendisi gerekiyor, bir `bool` yetmez.
    /// </summary>
    Task<IReadOnlyList<(string Kind, string Slug)>> FindMissingAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        CancellationToken cancellationToken);
}
