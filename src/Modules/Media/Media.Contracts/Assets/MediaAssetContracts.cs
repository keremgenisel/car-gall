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
    string? FailureReason,
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
}
