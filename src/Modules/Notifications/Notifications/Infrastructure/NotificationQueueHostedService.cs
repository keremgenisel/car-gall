using CarGallery.Modules.Notifications.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications.Infrastructure;

/// <summary>
/// Kuyruğu düzenli aralıklarla işleyen arka plan servisi (R10).
///
/// <para><b>Neden BuildingBlocks'ta değil:</b> outbox dağıtıcısı jeneriktir çünkü her
/// modülün aynı outbox'ı vardır. Bildirim kuyruğu ise `Notifications`'a özgüdür — tekrar
/// deneme aralığı, deneme sayısı ve hata sınıfları bu modülün iş kuralıdır
/// (Yasak Listesi 11).</para>
///
/// <para><b>AC-14'ün karşılığı burasıdır:</b> bekleyen gönderimler süreç yeniden
/// başladıktan sonra kaybolmaz. Kayıt veritabanında <c>Queued</c> durur ve bu servis
/// uygulama her açıldığında sırası gelmiş olanları yeniden eline alır. Kuyruk bellekte
/// olsaydı yeniden başlatma onu sessizce silerdi.</para>
///
/// <para><b>Tur başarısızlığı uygulamayı düşürmez</b> ama LOGLANIR: sessizce duran bir
/// dağıtıcı, çalıştığı sanılarak her bildirimin kaybolması demektir.</para>
/// </summary>
internal sealed class NotificationQueueHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationQueueHostedService> logger,
    TimeSpan? interval = null) : BackgroundService
{
    private readonly TimeSpan interval = interval ?? TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();

                await scope.ServiceProvider
                    .GetRequiredService<NotificationDispatcher>()
                    .DispatchDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Kuyruğun düşmesi uygulamayı düşürmemeli.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                logger.LogError(exception, "Bildirim kuyruğu turu başarısız oldu.");
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
