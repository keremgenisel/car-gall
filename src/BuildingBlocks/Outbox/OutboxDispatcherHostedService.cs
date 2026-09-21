using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>
/// Arka plan dağıtıcısı (architecture.md § 2.C). V1'de broker yok; aynı süreç, veritabanı
/// outbox'ı. Broker'a geçiş yalnız bu sınıfı değiştirir.
/// </summary>
public sealed class OutboxDispatcherHostedService<TDbContext>(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcherHostedService<TDbContext>> logger,
    TimeSpan? interval = null) : BackgroundService
    where TDbContext : DbContext, IOutboxDbContext
{
    private readonly TimeSpan interval = interval ?? TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                OutboxDispatcher<TDbContext> dispatcher =
                    scope.ServiceProvider.GetRequiredService<OutboxDispatcher<TDbContext>>();

                await dispatcher.DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Dağıtıcının düşmesi uygulamayı düşürmemeli.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // Sessizce durmak, A8'i bugünkü durumdan farksız biçimde yine öldürürdü —
                // üstelik bu kez çalıştığı sanılarak (D5'in gerekçesi).
                logger.LogError(exception, "Outbox dağıtım turu başarısız oldu.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}
