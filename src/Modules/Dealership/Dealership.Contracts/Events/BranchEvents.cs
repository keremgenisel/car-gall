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
