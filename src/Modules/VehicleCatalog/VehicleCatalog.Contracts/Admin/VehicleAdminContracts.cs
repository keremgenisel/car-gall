using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Contracts.Admin;

/// <summary>
/// **Yönetim yüzeyi — vitrin yüzeyinden AYRI bir sözleşmedir** (C20).
///
/// <para>İki sözleşme aynı DTO ile taşınmaz ve bu bir biçim tercihi değil: gizli alanlar
/// vitrin yanıtında <b>hiç bulunmaz</b>, boş değer olarak da bulunmaz (Yasak Listesi 41,
/// `0010a` AC-18). Tek bir DTO paylaşılsaydı şase alanı vitrin yanıtında `null` olarak
/// görünürdü ve "gizli" sözü kâğıt üstünde kalırdı.</para>
///
/// <para>Buradaki her yanıt oturum gerektirir (`0010a` AC-19, C1).</para>
/// </summary>
public sealed record VehicleAdminListItemDto(
    Guid VehicleId,
    string ModelName,
    int ModelYear,
    int MileageKilometers,
    VehicleAdminPriceDto Price,
    string ListingStatus,
    string Vin,
    /// <summary>0010b **D25** — listenin "son güncellenen" ekseni.</summary>
    DateTimeOffset UpdatedAt,
    /// <summary>
    /// 0010b **R12/AC-22** — metni eksik olan diller. Bir dil, başlık VE açıklamasının
    /// ikisi de doluysa tamdır. Liste KOD taşır, çevrilmiş dil adı değil (C11).
    /// </summary>
    IReadOnlyList<string> MissingLocales,
    /// <summary>
    /// 0010b **R9/C21** — zorunlu işaretli bir opsiyonel alanı boş olan araç. SAKLANMAZ,
    /// sorguda hesaplanır: ayar değiştiğinde hiçbir yazma olmadan değişmesi gerekiyor.
    /// </summary>
    bool HasMissingRequiredInfo,
    /// <summary>
    /// `0012b` **R13**/**D15** — araç lokasyon ataması bekliyor: şubesi kapatıldı ve
    /// yenisi atanmadı. Liste bunu AYIRT EDER; şubesi atanmadan yayına alınamaz.
    /// </summary>
    bool AwaitingBranchAssignment);

/// <summary>
/// "Fiyat sorunuz" AYRI bir durumdur, `0` değildir (P4). Tutar, para kodu ve minor unit
/// üssü birlikte taşınır; üs kaybolursa üç ondalıklı para birimleri sessizce bozulur (P2).
/// </summary>
public sealed record VehicleAdminPriceDto(
    bool OnRequest,
    decimal? Amount,
    string? CurrencyCode,
    int? MinorUnitExponent);

/// <summary>
/// Düzenleme ekranının okuduğu tam kayıt (0010b R1).
///
/// <para><b>Yalnız AÇIK opsiyonel alanların değerleri gelir</b> (R7): kapalı alanın değeri
/// veritabanında durmaya devam eder (F1) ama yanıta girmez (F3) — ayrım sorgu tarafında
/// yapılır, sunum katmanında değil (Yasak Listesi 41).</para>
/// </summary>
public sealed record VehicleAdminDetailDto(
    Guid VehicleId,
    Guid BranchId,
    Guid VehicleModelId,
    string BodyType,
    string FuelType,
    string TransmissionType,
    string DrivetrainType,
    string TrimLevel,
    IReadOnlyDictionary<string, string> Color,
    int ModelYear,
    int MileageKilometers,
    VehicleAdminPriceDto Price,
    string Vin,
    string ListingStatus,
    IReadOnlyDictionary<string, string> ListingTitle,
    IReadOnlyDictionary<string, string> ListingDescription,
    IReadOnlyList<VehicleAdminOptionalValueDto> OptionalValues,
    DateTimeOffset UpdatedAt,
    /// <summary>
    /// `0012b` **R13** · **D15** — araç lokasyon ataması bekliyor. Düzenleme ekranı bunu
    /// **düğmeye basılmadan** söylüyor: sunucu da reddediyor, ama personeli boşuna
    /// denemekten kurtarmak eylem yüzeyinin işi.
    /// </summary>
    bool AwaitingBranchAssignment = false);

/// <summary>
/// Opsiyonel alanın değeri, TİPİYLE birlikte. Biçimlendirme UÇTA yapılır
/// (`docs/conventions.md` § 7): sunucu ham değeri ve tipini söyler, metne çevirmez.
/// </summary>
public sealed record VehicleAdminOptionalValueDto(
    string FieldKey,
    string Kind,
    string? Value,
    IReadOnlyDictionary<string, string>? Values);

/// <summary>
/// Formun kurabilmesi için alan tanımı: anahtar, tip ve zorunluluk. Tip KODDAN gelir
/// (0010b **D21**), zorunluluk ayardan. Kapalı alanlar bu listede BULUNMAZ (R7).
/// </summary>
public sealed record VehicleAdminFieldDto(string FieldKey, string Kind, bool IsRequired);

/// <summary>
/// R15/AC-23 — çeviri eksik ilan sayısı **tüm araçlar** üzerinden; gösterilen sayı o anki
/// sayfanın içeriğine bağlı değildir.
///
/// <para><b>Neden ayrı bir yanıt.</b> AC-25 liste yanıtının `PagedResult&lt;T&gt;`
/// biçiminde olmasını istiyor; özeti o zarfın içine sıkıştırmak zarfı bozardı. Ayrı
/// okuma, sayının sayfadan bağımsız olduğunu sözleşmede de görünür kılıyor.</para>
/// </summary>
public sealed record VehicleAdminSummaryDto(int TotalCount, int TranslationIncompleteCount);

/// <summary>
/// Liste sıralaması (R14). İkisinde de eşitlik ARAÇ KİMLİĞİYLE kırılır: kırıcı olmadan
/// aynı dakikada güncellenmiş iki araç kararsız sıralanır ve aynı araç iki sayfada
/// görünebilir — `0001`'in SCORECARD'ında kayıtlı ders.
/// </summary>
public enum VehicleAdminListSort
{
    LastUpdated = 0,
    RecentlyAdded = 1,
}

public sealed record VehicleAdminListFilter(
    string Locale,
    int Page,
    int? PageSize,
    VehicleAdminListSort Sort,
    /// <summary>
    /// `0012b` **R17**/**D19** — arşivlenen araç VARSAYILAN görünümde yer almaz; arşiv bu
    /// projenin "silme"sidir (`0010b` X19) ve varsayılan listede kalırsa liste sonsuz
    /// büyür, eylem de hiçbir şey yapmamış görünür.
    ///
    /// <para><b>Aynı koşul toplam sayıya ve özet sayaçlarına da uygulanır</b>
    /// (**D35**): liste ile sayaç aynı evreni saymak zorunda, yoksa "12 araç" yazan bir
    /// özetin altında 9 satır görünür.</para>
    /// </summary>
    bool IncludeArchived = false);

public interface IVehicleAdminQueries
{
    /// <summary>
    /// 0010b — sayfalı, sıralı ve göstergeli liste. `0010a`'nın sade listesi bu sözleşmeyi
    /// bilerek dar bırakmıştı ("Sayfalama, sıralama ve göstergeler `0010b`'dedir").
    /// </summary>
    Task<PagedResult<VehicleAdminListItemDto>> ListAsync(
        VehicleAdminListFilter filter,
        CancellationToken cancellationToken);

    /// <summary>
    /// `0012b` **D35** — sayaç listeyle **AYNI evreni** sayar. Arşiv bayrağı buraya da
    /// geliyor ve varsayılanı `false`: parametre eklemeli, mevcut çağıranlar kırılmıyor.
    ///
    /// <para>İlk hâli bayrağı hiç almıyordu ve sayaç her zaman arşivi eliyordu; personel
    /// arşiv modunda "3 araç" yazan bir özetin altında beş satır görüyordu. Kriterin
    /// lafzı karşılanıyordu, kararın amacı karşılanmıyordu.</para>
    /// </summary>
    Task<VehicleAdminSummaryDto> GetSummaryAsync(
        bool includeArchived,
        CancellationToken cancellationToken);

    /// <summary>Bulunamayan araç için <c>null</c> döner; çağıran `404`'e çevirir (AC-4).</summary>
    Task<VehicleAdminDetailDto?> FindAsync(Guid vehicleId, CancellationToken cancellationToken);

    /// <summary>Formun kurabilmesi için AÇIK opsiyonel alanlar (R7).</summary>
    Task<IReadOnlyList<VehicleAdminFieldDto>> GetFormFieldsAsync(CancellationToken cancellationToken);
}

/// <summary>
/// `0012a`/AC-24 — aracın sıralı medyası, her varlığın `Media` tarafındaki hâliyle
/// birlikte. **Sıra `VehicleCatalog`'un verisidir** (M2) ve listenin indeksi onu taşıyor;
/// ayrı bir "sıra" alanı aynı olguyu iki yerde tutardı.
/// </summary>
/// <param name="Status">
/// `Media`'nın işleme durumunun KODU (`uploaded` · `processing` · `ready` · `failed`).
/// Çevrilmiş ad değil: gösterim uçta, koddan çevrilir (Yasak Listesi 22–23).
/// </param>
/// <param name="FailureReason">
/// Yalnız `failed` durumda dolu; kapalı bir kümeden bir kod (`0011`/AC-33).
/// </param>
public sealed record VehicleMediaItemDto(
    Guid MediaAssetId,
    string Status,
    string? FailureReason,
    IReadOnlyDictionary<string, string> AltText,
    IReadOnlyList<VehicleMediaDerivativeDto> Derivatives);

/// <summary>Türevin boyutu ve erişim yolu. `docs/frontend.md` § 7 açık `width`/`height` istiyor.</summary>
public sealed record VehicleMediaDerivativeDto(string Variant, int Width, int Height, string Url);

/// <summary>
/// Aracın medya yüzeyi. `CoverMediaAssetId` **türetilmiş**: sıralı listenin ilk elemanı
/// (D2). Yanıtta bulunması bir kolaylık, ikinci bir gerçek kaynak değil — liste boşsa
/// <c>null</c>.
/// </summary>
public sealed record VehicleMediaDto(
    Guid VehicleId,
    Guid? CoverMediaAssetId,
    IReadOnlyList<VehicleMediaItemDto> Items);

/// <summary>
/// `0012a` D12 — sıralı kimlik listesinin TAMAMI. Bağlama, sıralama ve çıkarma tek
/// idempotent istektir ve kapak sıranın SONUCU.
///
/// <para>Boş liste geçerlidir: "bütün fotoğrafları çıkar" anlamına gelir ve varlıkları
/// silmez (R3, AC-7).</para>
/// </summary>
public sealed record UpdateVehicleMediaRequest(IReadOnlyList<Guid> MediaAssetIds);

/// <summary>`0012a` — aracın medya yüzeyinin okuma sözleşmesi.</summary>
public interface IVehicleMediaQueries
{
    Task<VehicleMediaDto?> FindAsync(Guid vehicleId, CancellationToken cancellationToken);
}
