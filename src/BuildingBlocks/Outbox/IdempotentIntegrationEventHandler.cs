using Microsoft.EntityFrameworkCore;

namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>
/// Alıcı tarafı idempotency'nin TEK yeri (D4). Alt sınıflar yalnız işi yazar; kaydın
/// tutulmasını burası üstlenir.
///
/// İş ile inbox kaydı AYNI `SaveChanges` içinde kalıcı olur: ayrı düşerlerse ya iş iki kez
/// yapılır ya da hiç yapılmadan işlenmiş sayılır.
/// </summary>
public abstract class IdempotentIntegrationEventHandler<TEvent, TDbContext>(TDbContext context, IClock clock)
    : IIntegrationEventHandler<TEvent>, IIntegrationEventDispatchTarget
    where TEvent : IIntegrationEvent
    where TDbContext : DbContext, IInboxDbContext
{
    public string HandlerName => GetType().Name;

    protected TDbContext Context => context;

    public async Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken)
    {
        bool alreadyProcessed = await context.InboxEntries.AnyAsync(
            entry => entry.EventId == integrationEvent.EventId && entry.HandlerName == HandlerName,
            cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        await HandleCoreAsync(integrationEvent, cancellationToken);

        context.InboxEntries.Add(new InboxEntry
        {
            EventId = integrationEvent.EventId,
            HandlerName = HandlerName,
            ProcessedAt = clock.UtcNow,
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        HandleAsync((TEvent)integrationEvent, cancellationToken);

    /// <summary>Alıcının kendi işi. `SaveChanges` çağırmaz — onu taban sınıf yapar.</summary>
    protected abstract Task HandleCoreAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
