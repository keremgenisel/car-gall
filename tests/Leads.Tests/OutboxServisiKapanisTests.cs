using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Leads.Tests;

/// <summary>
/// Arka plan dağıtıcısı NORMAL kapanışta istisna sızdırmaz.
///
/// <para>Aynı kusurun jenerik yarısı: turlar arası bekleme <c>try</c> bloğunun dışındaydı
/// ve kapanışta <c>TaskCanceledException</c> fırlatıyordu. <c>BackgroundService</c>'in
/// varsayılanı <c>StopHost</c> olduğu için süreç <b>134</b> ile düşüyor ve bu istisna
/// kapanışa yol açan GERÇEK hatayı maskeliyordu.</para>
///
/// <para>Burada yaşıyor çünkü sınıf jenerik ve somut bir <c>IOutboxDbContext</c> istiyor;
/// <c>LeadsDbContext</c> gerçek bir outbox taşıyan en yakın örnek. Veritabanına hiç
/// gidilmiyor — servis dağıtıcıyı çözemeyip turu loglayarak geçiyor ve sınanan şey zaten
/// kapanış.</para>
/// </summary>
public sealed class OutboxServisiKapanisTests
{
    [Fact]
    public async Task SizdirmazIstisna_NormalKapanista()
    {
        await using ServiceProvider provider = new ServiceCollection().BuildServiceProvider();

        using OutboxDispatcherHostedService<LeadsDbContext> service = new(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxDispatcherHostedService<LeadsDbContext>>.Instance,
            TimeSpan.FromMinutes(5));

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        Assert.True(service.ExecuteTask!.IsCompletedSuccessfully);
    }
}
