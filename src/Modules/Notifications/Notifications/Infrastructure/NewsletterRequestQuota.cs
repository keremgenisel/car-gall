using System.Collections.Concurrent;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application.Newsletter;

namespace CarGallery.Modules.Notifications.Infrastructure;

/// <summary>
/// Sabit pencereli, süreç içi sayaç — `Leads`'in `LeadCaptureQuota` deseni. Kalıcı değildir
/// ve olmamalıdır: IP geçici sayaçta tutulur (AC-15). Tek kurulum = tek süreç; çok örnekli
/// çalışmada değişecek tek yer burasıdır.
/// </summary>
internal sealed class NewsletterRequestQuota(IClock clock, NewsletterOptions options) : INewsletterRequestQuota
{
    private readonly ConcurrentDictionary<string, Counter> counters = new(StringComparer.Ordinal);

    public bool TryAcquire(string clientKey)
    {
        Counter counter = counters.GetOrAdd(clientKey, _ => new Counter());

        lock (counter)
        {
            DateTimeOffset now = clock.UtcNow;

            if (now - counter.WindowStart >= options.ValidRequestWindow)
            {
                counter.WindowStart = now;
                counter.Used = 0;
            }

            if (counter.Used >= options.ValidRequestLimit)
            {
                return false;
            }

            counter.Used++;
            return true;
        }
    }

    private sealed class Counter
    {
        internal DateTimeOffset WindowStart { get; set; }

        internal int Used { get; set; }
    }
}
