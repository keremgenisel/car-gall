namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>
/// Modül sınırını yalnızca bu geçer. <c>X.Contracts</c> içinde <c>record</c> olarak, geçmiş
/// zaman adıyla ve yalnız primitive/DTO taşıyarak tanımlanır (docs/architecture.md § 2.B).
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
