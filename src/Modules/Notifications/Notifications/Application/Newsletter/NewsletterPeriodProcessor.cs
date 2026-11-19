using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` **D13** · **D21** — haftalık dönemin işleyicisi. `IClock` alır ve testten DOĞRUDAN
/// çağrılır; zamanlı servis yalnız onu tetikler.
///
/// <para><b>Ne yapar:</b> en son geçmiş dönem anını bulur; işlenmemişse o dönemi BİR KEZ
/// işler. Atanmamış adayların HEPSİ karara bağlanır: o an Yayında olanlar döneme atanır,
/// olmayanlar elenir ve bir sonraki döneme kalmaz (**D21**, AC-62, AC-63). Yayında aday varsa
/// işlenme anında ETKİN olan her aboneye tek bir bülten kaydı kuyruğa girer (AC-68).</para>
///
/// <para><b>Kaçırılmış dönemler tek bültende birleşir</b> (AC-71): yalnız en son dönem anı
/// işlenir ve atanmamış adayların hepsi onundur.</para>
///
/// <para><b>"Tam bir kez" kısıttadır</b> (**D23**): dönem anı tekil, `(abonelik, dönem)` tekil.
/// Eşzamanlı ikinci işlenme dönem kaydında takılır ve transaction'ın tamamı geri alınır
/// (AC-70).</para>
/// </summary>
internal sealed class NewsletterPeriodProcessor(
    NotificationsDbContext context,
    IListingSummaryQueries listings,
    NewsletterOptions options,
    NotificationsOptions notificationOptions,
    IClock clock,
    ILogger<NewsletterPeriodProcessor> logger)
{
    /// <summary>İşlenen dönem kaydının kimliği; işlenecek dönem yoksa <c>null</c>.</summary>
    internal async Task<Guid?> ProcessDueAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset baseline = await new NewsletterBaseline(context, clock).EnsureAsync(cancellationToken);
        DateTimeOffset now = clock.UtcNow;
        DateTimeOffset due = LatestScheduledAtOrBefore(now);

        // AC-67 — dönem anından önce işlem yok; devreye girişten önceki an da dönem değildir.
        if (due <= baseline)
        {
            return null;
        }

        bool processed = await context.Periods.AnyAsync(
            period => period.Kind == NewsletterPeriodKind.Regular && period.CutoffAt >= due,
            cancellationToken);

        if (processed)
        {
            return null;
        }

        try
        {
            return await ProcessAsync(due, now, cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "ux_newsletter_periods_regular_cutoff",
        })
        {
            // AC-70 — aynı dönemi eşzamanlı başka bir çağrı işledi; bu çağrının hiçbir yazısı
            // kalıcı olmadı.
            context.ChangeTracker.Clear();
            logger.LogInformation("Bülten dönemi başka bir çağrı tarafından işlendi: {CutoffAt}", due);

            return null;
        }
    }

    /// <summary>
    /// **D13** — haftanın günü + saat, UTC. Yaz saati uygulanan ülkede yerel gönderim saati
    /// yılda iki kez kayar (D13'ün bedeli).
    /// </summary>
    internal DateTimeOffset LatestScheduledAtOrBefore(DateTimeOffset now)
    {
        DateTime utc = now.UtcDateTime;
        DateTimeOffset candidate = new(DateOnly.FromDateTime(utc).ToDateTime(options.SendTimeUtc), TimeSpan.Zero);
        int back = ((int)candidate.DayOfWeek - (int)options.SendDayOfWeek + 7) % 7;
        candidate = candidate.AddDays(-back);

        return candidate > now ? candidate.AddDays(-7) : candidate;
    }

    private async Task<Guid?> ProcessAsync(DateTimeOffset due, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        NewsletterPeriod period = NewsletterPeriod.Regular(due, now);
        context.Periods.Add(period);

        List<NewsletterCandidate> candidates = await context.Candidates
            .Where(candidate => candidate.Status == NewsletterCandidateStatus.Pending)
            .ToListAsync(cancellationToken);

        HashSet<Guid> published = candidates.Count == 0
            ? []
            : [.. (await listings.FindPublishedAsync([.. candidates.Select(candidate => candidate.ListingId)], cancellationToken))
                .Select(listing => listing.ListingId)];

        int assigned = 0;

        foreach (NewsletterCandidate candidate in candidates)
        {
            if (published.Contains(candidate.ListingId))
            {
                candidate.AssignTo(period.Id);
                context.PeriodListings.Add(new NewsletterPeriodListing
                {
                    PeriodId = period.Id,
                    ListingId = candidate.ListingId,
                    PublishedAt = candidate.PublishedAt,
                });
                assigned++;
            }
            else
            {
                candidate.Eliminate(period.Id);
            }
        }

        int queued = 0;

        // R9 · AC-64 — Yayında aday yoksa dönem kaydı yazılır ama ileti üretilmez.
        if (assigned > 0)
        {
            List<(Guid Id, string Locale)> subscribers = await context.Subscriptions
                .AsNoTracking()
                .Where(subscription => subscription.Status == SubscriptionStatus.Active)
                .Select(subscription => new ValueTuple<Guid, string>(subscription.Id, subscription.Locale))
                .ToListAsync(cancellationToken);

            foreach ((Guid id, string locale) in subscribers)
            {
                // AC-82 — abonenin dili kurulumda artık etkin değilse varsayılan dil.
                string messageLocale = options.IsEnabled(locale) ? locale : notificationOptions.DefaultLocale;

                context.Messages.Add(NotificationMessage.QueueNewsletter(id, period.Id, messageLocale, now));
                queued++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Bülten dönemi işlendi: {CutoffAt} {Assigned} ilan, {Eliminated} elenen, {Queued} ileti",
            due,
            assigned,
            candidates.Count - assigned,
            queued);

        return period.Id;
    }
}
