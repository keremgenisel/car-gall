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

    /// <summary>
    /// Alt sınıflar saate ihtiyaç duyabilir (zaman damgası yazan bir alıcı gibi). Taban
    /// sınıf onu zaten tutuyor; ayrıca yakalamak CS9107 üretir ve aynı bağımlılığın iki
    /// kopyasını doğururdu.
    /// </summary>
    protected IClock Clock => clock;

    public async Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken)
    {
        bool alreadyProcessed = await context.InboxEntries.AnyAsync(
            entry => entry.EventId == integrationEvent.EventId && entry.HandlerName == HandlerName,
            cancellationToken);

        if (alreadyProcessed)
        {
            return;
        }

        // İş ile inbox kaydı AYNI transaction'da olmalı. `ExecuteUpdate`/`ExecuteDelete`
        // kullanan bir alıcıda iş ANINDA çalışır, inbox kaydı ise SaveChanges'te; ikisi
        // arasında süreç düşerse iş yapılmış ama kaydedilmemiş olur ve tekrar çalışır.
        // Bugünkü iki tüketici tekrara dayanıklı olduğu için bu bugün zararsız — ama
        // sayaç artıran ilk tüketicide sessizce bozulurdu.
        bool ownsTransaction = context.Database.CurrentTransaction is null;

        var transaction = ownsTransaction
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            await HandleCoreAsync(integrationEvent, cancellationToken);

            context.InboxEntries.Add(new InboxEntry
            {
                EventId = integrationEvent.EventId,
                HandlerName = HandlerName,
                ProcessedAt = clock.UtcNow,
            });

            await context.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public Task HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        HandleAsync((TEvent)integrationEvent, cancellationToken);

    /// <summary>Alıcının kendi işi. `SaveChanges` çağırmaz — onu taban sınıf yapar.</summary>
    protected abstract Task HandleCoreAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
