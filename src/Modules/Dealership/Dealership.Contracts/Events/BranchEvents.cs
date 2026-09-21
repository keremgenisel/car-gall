using CarGallery.BuildingBlocks.Outbox;

namespace CarGallery.Modules.Dealership.Contracts.Events;

/// <summary>Şube adı çevrilebilir bir alandır (docs/architecture.md § 1).</summary>
public sealed record BranchNameDto(string Locale, string Value);

/// <summary>
/// Entegrasyon olayı: geçmiş zaman adı, record, yalnız primitive ve kendi DTO'ları
/// (docs/architecture.md § 2.B). Modül sınırını yalnızca bu geçer.
/// </summary>
public sealed record BranchOpened(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BranchId,
    IReadOnlyList<BranchNameDto> Names) : IIntegrationEvent;

public sealed record BranchDetailsChanged(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BranchId,
    IReadOnlyList<BranchNameDto> Names) : IIntegrationEvent;

/// <summary>
/// A8: şube kapatıldığında o şubedeki araçlar yayından kaldırılır ve "lokasyon ataması
/// bekliyor" durumuna geçer. Sessizce vitrinde kalmazlar. Bu olay o güvenlik ağıdır
/// (docs/architecture.md § 2.G).
/// </summary>
public sealed record BranchDeactivated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BranchId) : IIntegrationEvent;

/// <summary>
/// D6 — okuma yüzeyi bilerek dar: "şube kimliği geçerli mi ve ziyaretçinin locale'indeki
/// adı nedir". R12'nin bugün tüketicisi yok (`VehicleCatalog` şube bilgisini olaydan alıyor,
/// `Identity` modülü mevcut değil); genişletme, ihtiyacı doğuran spec'e bırakıldı.
/// </summary>
public sealed record BranchSummaryDto(Guid BranchId, string Name, string Status);

public interface IBranchQueries
{
    /// <summary>
    /// Bulunamayan şube için <c>null</c> döner. Ziyaretçinin locale'inde karşılığı olmayan
    /// ad için varsayılan locale'deki ad döner; boş ad DÖNMEZ (R5, AC-5).
    /// </summary>
    Task<BranchSummaryDto?> FindAsync(Guid branchId, string locale, CancellationToken cancellationToken);
}
