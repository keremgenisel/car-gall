using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarGallery.BuildingBlocks.Outbox;

public sealed record OutboxDispatchOptions(int BatchSize = 20, int MaximumAttempts = 5)
{
    /// <summary>D5(b) — artan aralık: 1, 2, 4, 8… dakika.</summary>
    public TimeSpan BackoffFor(int attemptCount) =>
        TimeSpan.FromMinutes(Math.Pow(2, Math.Max(0, attemptCount - 1)));
}

/// <summary>
/// D9 — JENERİK dağıtıcı. Tablo modülün kendi şemasında kalır ve onu okuyan da modülün
/// kendi `DbContext`'idir; genel bir dağıtıcının başka modülün outbox'ını okuması
/// Yasak Listesi 16'ya takılırdı. Her modül kendi `DbContext`'iyle bir kez kaydeder.
/// </summary>
public sealed class OutboxDispatcher<TDbContext>(
    TDbContext context,
    IServiceProvider services,
    IClock clock,
    OutboxDispatchOptions options,
    ILogger<OutboxDispatcher<TDbContext>> logger)
    where TDbContext : DbContext, IOutboxDbContext
{
    public async Task<int> DispatchPendingAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.UtcNow;

        List<OutboxMessage> pending = await context.OutboxMessages
            .Where(message => message.ProcessedAt == null
                && message.FailedAt == null
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.OccurredAt)
            .Take(options.BatchSize)
            .ToListAsync(cancellationToken);

        int dispatched = 0;

        foreach (OutboxMessage message in pending)
        {
            if (await TryDispatchAsync(message, cancellationToken))
            {
                dispatched++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return dispatched;
    }

    private async Task<bool> TryDispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        message.AttemptCount++;

        IIntegrationEvent? integrationEvent = Deserialize(message);

        if (integrationEvent is null)
        {
            // Tipi çözülemeyen olay yeniden denemeyle düzelmez; doğrudan başarısız işaretlenir.
            Fail(message, $"Bilinmeyen olay tipi: {message.Type}");
            return false;
        }

        List<IIntegrationEventDispatchTarget> targets = ResolveTargets(integrationEvent);
        List<string> failures = [];

        foreach (IIntegrationEventDispatchTarget target in targets)
        {
            try
            {
                await target.HandleAsync(integrationEvent, cancellationToken);
            }
#pragma warning disable CA1031 // Bir alıcının hatası diğerlerini engellememeli (AC-29).
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // AC-29 — döngü devam eder; bu alıcı sonraki turda yeniden denenir.
                // Idempotency kaydı başarılı alıcılar için yazıldığından tekrar çalışmazlar.
                failures.Add($"{target.HandlerName}: {exception.Message}");
                logger.LogError(exception, "Alıcı duyuruyu işleyemedi: {Handler}", target.HandlerName);
            }
        }

        if (failures.Count == 0)
        {
            message.ProcessedAt = clock.UtcNow;
            message.NextAttemptAt = null;
            return true;
        }

        string error = string.Join(" | ", failures);

        if (message.AttemptCount >= options.MaximumAttempts)
        {
            // D5(c) — denemeler tükendi: sessizce düşürülmez, işaretlenir ve loglanır.
            Fail(message, error);
            return false;
        }

        message.LastError = error;
        message.NextAttemptAt = clock.UtcNow.Add(options.BackoffFor(message.AttemptCount));
        return false;
    }

    private void Fail(OutboxMessage message, string error)
    {
        message.FailedAt = clock.UtcNow;
        message.LastError = error;
        message.NextAttemptAt = null;

        logger.LogError(
            "Duyuru {EventId} ({Type}) {Attempts} denemeden sonra başarısız: {Error}",
            message.Id,
            message.Type,
            message.AttemptCount,
            error);
    }

    private List<IIntegrationEventDispatchTarget> ResolveTargets(IIntegrationEvent integrationEvent)
    {
        Type targetType = typeof(IIntegrationEventDispatchTarget);

        return [.. services.GetServices(targetType)
            .OfType<IIntegrationEventDispatchTarget>()
            .Where(target => target.GetType().GetInterfaces().Any(contract =>
                contract.IsGenericType
                && contract.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)
                && contract.GenericTypeArguments[0] == integrationEvent.GetType()))];
    }

    private static IIntegrationEvent? Deserialize(OutboxMessage message)
    {
        Type? type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(message.Type))
            .FirstOrDefault(found => found is not null);

        if (type is null)
        {
            return null;
        }

        return JsonSerializer.Deserialize(message.Payload, type) as IIntegrationEvent;
    }
}
