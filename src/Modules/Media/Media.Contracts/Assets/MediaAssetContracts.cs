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
    /// **hazır** durumda"* (`docs/domain.md` M3). Bu spec yüzeyi <b>sunar, tüketmez</b>
    /// (D11, X7) — `Vehicle.EnsurePublishable()` bugün hâlâ yalnız sayıya bakıyor ve
    /// kabul edilen bedel X7'de yazılı.</para>
    ///
    /// <para><b>Boş küme <c>false</c> döner</b> ve bu bilinçli: A2 en az bir varlık
    /// istiyor, yani "hiç varlık yok" sorusunun doğru cevabı "hazır değil". <c>true</c>
    /// dönmek, fotoğrafı olmayan aracı yayına uygun göstermekti.</para>
    /// </summary>
    Task<bool> AreAllReadyAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken);
}
