namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>
/// Olay, iş verisiyle AYNI transaction'da yazılır; dağıtım en-az-bir-kezdir, bu yüzden
/// her handler idempotent olmak zorundadır (docs/architecture.md § 2.C).
/// </summary>
public sealed class OutboxMessage
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required string Payload { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }
}
