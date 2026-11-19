using CarGallery.Modules.Notifications.Application.Newsletter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications.Infrastructure;

/// <summary>
/// `0018` **D13** · **D20** — `Notifications`'ın ilk zamanlı işi. Yalnız TETİKLER: kararın
/// hepsi <see cref="NewsletterPeriodProcessor"/>'dadır ve o testten doğrudan çağrılır.
///
/// <para>İlk tur açılışta koşar: başlangıç dönemi kaydı yoksa yazılır (AC-72) ve uygulama
/// dönem anında kapalıysa kaçırılan dönem bir kez işlenir (AC-71). Turlar arası bir dakika —
/// dönem haftalıktır; dakikalık gecikme bülteni geciktirir ama kaybettirmez.</para>
///
/// <para>Tur başarısızlığı uygulamayı düşürmez ama LOGLANIR (`NotificationQueueHostedService`
/// deseni; kapanıştaki iptal bir hata değildir).</para>
/// </summary>
internal sealed class NewsletterPeriodHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<NewsletterPeriodHostedService> logger,
    TimeSpan? interval = null) : BackgroundService
{
    private readonly TimeSpan interval = interval ?? TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();

                await scope.ServiceProvider
                    .GetRequiredService<NewsletterPeriodProcessor>()
                    .ProcessDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Dönem turunun düşmesi uygulamayı düşürmemeli.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Bülten dönemi turu başarısız oldu.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
