using CarGallery.Modules.Media.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Media.Infrastructure;

/// <summary>
/// D1/D14 — hattı düzenli aralıklarla işleyen arka plan servisi.
///
/// <para><b>Neden `BuildingBlocks`'ta değil:</b> outbox dağıtıcısı jeneriktir çünkü her
/// modülün aynı outbox'ı var. Medya hattı `Media`'ya özgüdür — türev kümesi, filigran ve
/// tekrar deneme politikası bu modülün iş kuralıdır (Yasak Listesi 11). `Notifications`
/// kuyruğu aynı gerekçeyle kendi modülünde duruyor.</para>
///
/// <para><b>Kayıt veritabanında durduğu için yeniden başlatma bekleyen işi kaybetmez:</b>
/// varlık <c>Yüklendi</c> hâlinde kalır ve uygulama her açıldığında sırası gelmiş olanlar
/// yeniden eline alınır. Kuyruk bellekte olsaydı yeniden başlatma onu sessizce silerdi.</para>
/// </summary>
internal sealed class MediaProcessingHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<MediaProcessingHostedService> logger,
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
                    .GetRequiredService<MediaProcessingService>()
                    .ProcessDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
#pragma warning disable CA1031 // Hattın düşmesi uygulamayı düşürmemeli.
            catch (Exception exception)
#pragma warning restore CA1031
            {
                // Sessizce duran bir hat, her fotoğrafın sonsuza kadar `İşleniyor` kalması
                // demektir — ve bu, R8'in önlemeye çalıştığı sessizliğin ta kendisi.
                logger.LogError(exception, "Medya işleme turu başarısız oldu.");
            }

            if (!await BekleAsync(stoppingToken))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Turlar arası bekleme. Kendi `try`'ı var: çıplak <c>await Task.Delay(interval, token)</c>
    /// normal kapanışta <c>TaskCanceledException</c> fırlatır ve `BackgroundService`'in
    /// varsayılan davranışı <c>StopHost</c> olduğu için süreç hata koduyla düşer — bu
    /// depoda `Notifications` ve outbox dağıtıcısı aynı bedeli bir kez ödedi.
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
