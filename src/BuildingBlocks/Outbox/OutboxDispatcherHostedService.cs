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

            if (!await BekleAsync(stoppingToken))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Turlar arası bekleme. **Kendi `try`'ı var ve bu bir hatanın bedeliydi:** çıplak
    /// <c>await Task.Delay(interval, stoppingToken)</c> döngünün sonunda duruyordu ve
    /// normal kapanışta <c>TaskCanceledException</c> fırlatıyordu. `BackgroundService`'in
    /// varsayılan davranışı <c>StopHost</c> olduğu için süreç <b>134</b> ile düşüyor, ve
    /// daha kötüsü bu istisna <b>gerçek başlangıç hatasını maskeliyordu</b> — bir dağıtımda
    /// "port dolu" hatası tam olarak bunun altında kayboldu.
    ///
    /// <para>Kapanış bir hata değildir: <c>false</c> dönülür ve döngü sessizce biter.</para>
    /// </summary>
    private async Task<bool> BekleAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(interval, stoppingToken);

            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
