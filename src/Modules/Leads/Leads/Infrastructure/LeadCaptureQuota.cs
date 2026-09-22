using System.Collections.Concurrent;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Application;

namespace CarGallery.Modules.Leads.Infrastructure;

/// <summary>
/// Sabit pencereli, süreç içi sayaç. Kalıcı değildir ve olmamalıdır: C17 IP'nin yalnız
/// geçici sayaçta tutulmasını, kalıcı kayda ve loga yazılmamasını istiyor.
///
/// Tek kurulum = tek galeri = tek süreç (docs/architecture.md); birden çok örneğe
/// geçildiği gün bu sayaç paylaşılan bir sayaçla değiştirilir ve değişecek tek yer burasıdır.
/// </summary>
internal sealed class LeadCaptureQuota(IClock clock, int permitLimit, TimeSpan window)
    : ILeadCaptureQuota
{
    private readonly ConcurrentDictionary<string, Counter> counters = new(StringComparer.Ordinal);

    public bool TryAcquire(string clientKey)
    {
        Counter counter = counters.GetOrAdd(clientKey, _ => new Counter());

        lock (counter)
        {
            DateTimeOffset now = clock.UtcNow;

            if (now - counter.WindowStart >= window)
            {
                counter.WindowStart = now;
                counter.Used = 0;
            }

            if (counter.Used >= permitLimit)
            {
                return false;
            }

            counter.Used++;
            return true;
        }
    }

    public void Release(string clientKey)
    {
        if (!counters.TryGetValue(clientKey, out Counter? counter))
        {
            return;
        }

        lock (counter)
        {
            // Pencere bu arada döndüyse geri verilecek bir şey yok; eksiye düşmek,
            // sonraki pencerede bütçeyi sessizce büyütürdü.
            if (counter.Used > 0)
            {
                counter.Used--;
            }
        }
    }

    private sealed class Counter
    {
        internal DateTimeOffset WindowStart { get; set; }

        internal int Used { get; set; }
    }
}
