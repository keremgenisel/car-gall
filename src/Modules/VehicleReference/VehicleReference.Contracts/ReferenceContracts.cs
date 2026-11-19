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

/// <summary>
/// `0021` — araç formunun model seçeneği: model kimliği, markası ve personelin locale'indeki
/// MARKASIZ model adı ("Corolla"). Form önce markayı seçtirir, model listesini markaya göre
/// süzer (R8); tam ad burada değil, <see cref="IVehicleModelQueries.ResolveNamesAsync"/>'tadır.
/// </summary>
public sealed record VehicleModelDto(Guid Id, Guid BrandId, string Label);

/// <summary>
/// `0021` — modelin ad parçaları, personelin locale'inde: aracın düzenleme formu aracın
/// kendi (belki pasif) marka ve modelini ARAÇ VERİSİNDEN gösterir (D24, AC-26).
/// </summary>
public sealed record VehicleModelPartsDto(Guid ModelId, Guid BrandId, string BrandLabel, string ModelLabel);

public interface IVehicleModelQueries
{
    /// <summary>
    /// `0021` **D17** — MARKA + MODEL tam adı, locale'deki görünen adlarla ("Toyota Corolla",
    /// "تويوتا كورولا"). Locale'de karşılığı olmayan parça kanonik Latin adına düşer (R3).
    /// Vitrin kartı, detay, öne çıkanlar ve panel listesi bu adı gösterir (R10, AC-34).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// Kanonik Latin TAM ad (marka + model, `0021` **D17**): dilden BAĞIMSIZDIR (docs/domain.md,
    /// Kanonik Ad). Locale almaması bir eksiklik değil, şartın kendisidir — 0005 R7/AC-18
    /// talebin anlık görüntüsüne ziyaretçinin dilindeki adı değil, kanonik adı kopyalatır.
    /// İlan adresi de bu addan üretilir (`0012b` D13, `0021` C2): "Fiat Egea" →
    /// <c>fiat-egea-…</c>. Markasız ad buraya dönmez; dönseydi talep kopyası, bildirim
    /// e-postası ve yeni adresler sessizce markasız ada gerilerdi.
    ///
    /// <see cref="ResolveNamesAsync"/> bunun yerini TUTAMAZ: o, çevirisi varsa çeviriyi
    /// döner; kanonik ada yalnız çeviri yoksa düşer.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        CancellationToken cancellationToken);

    /// <summary>`0021` D24 — modellerin marka ve model parçaları; pasif kayıtlar dâhil.</summary>
    Task<IReadOnlyDictionary<Guid, VehicleModelPartsDto>> ResolvePartsAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// `0021` R6 · D24 — araç formunun model seçenekleri: YALNIZ aktif markaların aktif
    /// modelleri (AC-21, AC-22). Sıralama kanonik ada göre; etiket personelin dilinde
    /// değiştiğinde sıra değişmez.
    /// </summary>
    Task<IReadOnlyList<VehicleModelDto>> GetAllAsync(string locale, CancellationToken cancellationToken);

    /// <summary>
    /// `0021` D23 — modelin YENİ seçim olarak kabul edilip edilemeyeceği: <c>null</c> model
    /// yok, <c>false</c> model ya da markası pasif, <c>true</c> seçilebilir (AC-27).
    /// </summary>
    Task<bool?> FindSelectableAsync(Guid vehicleModelId, CancellationToken cancellationToken);
}

/// <summary>`0021` — markanın adres kısaltması ve ziyaretçinin locale'indeki görünen adı.</summary>
public sealed record VehicleBrandDto(Guid Id, string Slug, string Label);

/// <summary>
/// `0021` — vitrin marka ekseninin seçeneği. <c>CanonicalName</c> sıralama içindir (D7);
/// <c>ShowOnHome</c> ana sayfa formunun süzgecidir (R13). İkisi de modüller arası
/// sözleşmede kalır, ziyaretçiye dönen yanıta girmez.
/// </summary>
public sealed record ShowcaseBrandDto(string Slug, string Label, string CanonicalName, bool ShowOnHome);

/// <summary>
/// `0021` D15 — adresle gelen marka kısaltmalarının model kimliği kümesi. <c>KnownSlugs</c>
/// var olan kısaltmalardır (eşleşme tam ve harfe duyarlı — D20); hiçbiri tanınmazsa eksen
/// uygulanmaz. Tanınan ama modeli olmayan bir marka boş küme verir ve liste boş döner (D21).
/// </summary>
public sealed record BrandModelSetDto(IReadOnlyList<string> KnownSlugs, IReadOnlyList<Guid> ModelIds);

/// <summary>
/// `0021` — markaların okuma yüzeyi. Vitrin okuma modeli markayı TAŞIMAZ (C1, D15): stoğu
/// olan markalar yayındaki ilanların model kimliklerinden, marka seçimi model kimliği
/// kümesinden buradan çözülür.
/// </summary>
public interface IVehicleBrandQueries
{
    /// <summary>D24 — araç formunun marka seçenekleri: yalnız aktif markalar (AC-14, AC-16).</summary>
    Task<IReadOnlyList<VehicleBrandDto>> GetActiveAsync(string locale, CancellationToken cancellationToken);

    /// <summary>
    /// D9 · D15 — verilen modellerin markaları, tekil ve kanonik ada göre sıralı. Pasif marka
    /// DÂHİLDİR: ilanları vitrinde kalır ve eksende süzülebilir (AC-15).
    /// </summary>
    Task<IReadOnlyList<ShowcaseBrandDto>> GetBrandsOfModelsAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>D21 — kısaltması verilen var olan markalar; tam ve harfe duyarlı eşleşme.</summary>
    Task<IReadOnlyList<ShowcaseBrandDto>> FindBySlugsAsync(
        IReadOnlyCollection<string> slugs,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>D15 — kısaltmaların bütün modelleri (pasifler dâhil: kayıtlı araçlar değişmez).</summary>
    Task<BrandModelSetDto> GetModelSetAsync(IReadOnlyCollection<string> slugs, CancellationToken cancellationToken);
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
