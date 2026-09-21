using CarGallery.BuildingBlocks.Outbox;

namespace CarGallery.Modules.Media.Contracts.Events;

/// <summary>
/// Medya sahipsizdir — "hangi araca ait" bilgisi VehicleCatalog'dadır
/// (docs/architecture.md § 1).
/// </summary>
public sealed record MediaAssetReady(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MediaAssetId,
    string Url) : IIntegrationEvent;
