namespace CarGallery.Modules.Media.Contracts.Assets;

/// <summary>
/// Bir medya varlığının işleme durumu (R6, `docs/domain.md` § 1 + § 2.10 M1).
///
/// `Contracts` sınırında KENDİ enum'u olarak geçer; tüketici modül kendi enum'unu tanımlar
/// ve eşler (Yasak Listesi 9). Veritabanında çevrilmiş ad DEĞİL kod saklanır
/// (Yasak Listesi 22).
/// </summary>
public enum MediaProcessingStatus
{
    Uploaded = 1,
    Processing = 2,
    Ready = 3,
    Failed = 4,
}

/// <summary>Bir türevin dışarıya görünen hâli: boyutu ve erişim yolu.</summary>
public sealed record MediaDerivativeDto(string Variant, int Width, int Height, string Url);

/// <summary>
/// Varlığın dar okuma yüzeyi. <b>Orijinalin yolu burada YOK</b> ve bu bilinçli (D16):
/// orijinal ham saklanıyor, yani konum bilgisini taşıyor; R5'in garantisi dışarı çıkanı
/// temizlemek olduğu için orijinalin yolu hiçbir yanıtta dönmez.
/// </summary>
public sealed record MediaAssetDto(
    Guid Id,
    MediaProcessingStatus Status,
    MediaFailureReason? FailureReason,
    IReadOnlyDictionary<string, string> AltText,
    IReadOnlyList<MediaDerivativeDto> Derivatives);

/// <summary>
/// D11 — türev boyutlarına ihtiyaç duyan tüketici buradan çözer, olaydan değil.
/// `docs/architecture.md` § 2.A'nın saydığı meşru senkron okuma budur:
/// `VehicleCatalog → Media` (görseller hazır mı, URL çöz).
/// </summary>
public interface IMediaAssetQueries
{
    Task<MediaAssetDto?> FindAsync(Guid mediaAssetId, CancellationToken cancellationToken);

    /// <summary>
    /// AC-42 — bir varlık KÜMESİNİN hazır olup olmadığını yanıtlar.
    ///
    /// <para>`0012` bunu A2'nin yayın şartına bağlayacak: *"en az bir medya varlığı
    /// **hazır** durumda"* (`docs/domain.md` M3). `0011` bu yüzeyi <b>sundu, tüketmedi</b>
    /// (D11, X7) ve `Vehicle.EnsurePublishable()` o gün yalnız sayıya bakıyordu.
    /// <b>Bu cümle 2026-10-11'de düzeltildi:</b> `0012a`/D6 gövdeyi "en az bir HAZIR varlık"
    /// şartına genişletti ve şart artık `ReadyAmongAsync` üzerinden kuruluyor — A2'yi ölçen
    /// okuma <b>bu metot değil</b>, onun altındaki `ReadyAmongAsync`'tir (`0012a` Tuzak 1:
    /// "hepsi hazır" ile "en az bir hazır" aynı soru değil).</para>
    ///
    /// <para><b>Boş küme <c>false</c> döner</b> ve bu bilinçli: A2 en az bir varlık
    /// istiyor, yani "hiç varlık yok" sorusunun doğru cevabı "hazır değil". <c>true</c>
    /// dönmek, fotoğrafı olmayan aracı yayına uygun göstermekti.</para>
    /// </summary>
    Task<bool> AreAllReadyAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// `0012a`/AC-23 — verilen kümeden **HAZIR OLANLARIN** kümesini döner.
    ///
    /// <para><b>Neden <see cref="AreAllReadyAsync"/> yetmiyor:</b> o "hepsi hazır mı" diye
    /// soruyor, **A2** ise *"en az bir medya varlığı hazır"* istiyor. Üç varlıklı, biri
    /// hazır bir araçta ikisi <b>zıt</b> cevap verir ve `AreAllReadyAsync` ile A2'yi ölçen
    /// bir uygulama o aracı yayına uygun saymaz (`0012a` D5, Tuzak 1).</para>
    ///
    /// <para>Dönen küme çağıranın verdiğinin alt kümesidir; bilinmeyen kimlikler sessizce
    /// düşer — "var olmayan varlık hazır değildir" ile "hazır değildir" aynı cevap.</para>
    /// </summary>
    Task<IReadOnlySet<Guid>> ReadyAmongAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// `0012a`/AC-24, D10 — birden çok varlığın detayını **tek** çağrıda okur.
    ///
    /// <para><b>Neden toplu:</b> panel aracın sıralı listesini durum, `alt` metni ve türev
    /// yollarıyla birlikte gösteriyor. Varlık başına <see cref="FindAsync"/> çağırmak N+1
    /// istek üretir ve sıralama/birleştirme kuralını tarayıcıya taşır; birleştirme
    /// `VehicleCatalog`'un sorgusunda, sunucuda yapılıyor
    /// (`docs/architecture.md` § 2.A, tek atlama).</para>
    ///
    /// <para>Sonuç çağıranın verdiği SIRADA değil, kimlikle eşlenebilir biçimde döner:
    /// sıra `VehicleCatalog`'un verisidir (M2) ve `Media` onu bilemez.</para>
    /// </summary>
    Task<IReadOnlyList<MediaAssetDto>> FindManyAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken);
}

