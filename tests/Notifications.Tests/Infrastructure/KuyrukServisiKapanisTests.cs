using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Infrastructure;

/// <summary>
/// Arka plan servisi NORMAL kapanışta istisna sızdırmaz.
///
/// <para><b>Bu bir kaçan hatanın regresyon testi.</b> Turlar arası bekleme
/// <c>try</c> bloğunun dışındaydı ve kapanışta <c>TaskCanceledException</c> fırlatıyordu;
/// <c>BackgroundService</c>'in varsayılanı <c>StopHost</c> olduğu için süreç <b>134</b> ile
/// düşüyordu. Asıl zarar gürültü değil <b>maskelemeydi</b>: bu istisna, kapanışa yol açan
/// gerçek hatayı gizliyordu — bir koşuda "port dolu" hatası tam olarak bunun altında
/// kayboldu ve yanlış yerde arandı.</para>
///
/// <para>Servis uzun bir aralıkla kuruluyor ki durdurulduğu an BEKLEMENİN içinde olsun;
/// kısa bir aralıkla test, hatanın yaşadığı yolu hiç geçmeyebilirdi.</para>
/// </summary>
public sealed class KuyrukServisiKapanisTests
{
    [Fact]
    public async Task SizdirmazIstisna_NormalKapanista()
    {
        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();

        using NotificationQueueHostedService service = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationQueueHostedService>.Instance,
            TimeSpan.FromMinutes(5));

        await service.StartAsync(CancellationToken.None);

        // Dağıtıcı çözülemediği için tur başarısız olur ve LOGLANIR — bu beklenen yol.
        // Sınanan şey o değil: servisin beklemede yakalanıp temiz durması.
        await service.StopAsync(CancellationToken.None);

        Assert.True(service.ExecuteTask!.IsCompletedSuccessfully);
    }
}
