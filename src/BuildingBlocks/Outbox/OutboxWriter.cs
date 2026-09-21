using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.BuildingBlocks.Outbox;

public interface IOutboxWriter
{
    void Write(IIntegrationEvent integrationEvent);
}

/// <summary>
/// Duyuruyu iş verisiyle AYNI `DbContext` üzerinden yazar. Ayrı bir bağlantı veya ayrı bir
/// `SaveChanges` kullanılmaz: ikisi ayrı düşerse ya duyurulmayan bir değişiklik ya da
/// olmayan bir değişikliğin duyurusu kalır (R11, AC-25).
/// </summary>
public sealed class OutboxWriter<TDbContext>(TDbContext context, IClock clock) : IOutboxWriter
    where TDbContext : DbContext, IOutboxDbContext
{
    public void Write(IIntegrationEvent integrationEvent)
    {
        context.OutboxMessages.Add(new OutboxMessage
        {
            Id = integrationEvent.EventId,
            Type = integrationEvent.GetType().FullName!,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType()),
            OccurredAt = integrationEvent.OccurredAt,
            NextAttemptAt = clock.UtcNow,
        });
    }
}
