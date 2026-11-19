using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` **D13** — devreye giriş anı: bu kartla ilk açılışta yazılan başlangıç dönemi
/// kaydının zamanı. Sonraki açılışlar onu DEĞİŞTİRMEZ (AC-72). Aday eşiği budur (**D4**):
/// kart öncesi yayınlanmış ilanlar hiçbir bültene girmez (AC-60).
///
/// <para>İki yerden istenebilir — zamanlı servisin ilk turu ve ilk aday duyurusu — ve ikisi
/// yarışabilir. Yarışı tekil indeks kapatır; kaybeden kazananın kaydını okur.</para>
/// </summary>
internal sealed class NewsletterBaseline(NotificationsDbContext context, IClock clock)
{
    internal async Task<DateTimeOffset> EnsureAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset? existing = await FindAsync(cancellationToken);

        if (existing is not null)
        {
            return existing.Value;
        }

        NewsletterPeriod baseline = NewsletterPeriod.Baseline(clock.UtcNow);
        context.Periods.Add(baseline);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return baseline.CutoffAt;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            context.Entry(baseline).State = EntityState.Detached;
            return (await FindAsync(cancellationToken))!.Value;
        }
    }

    internal Task<DateTimeOffset?> FindAsync(CancellationToken cancellationToken) =>
        context.Periods
            .AsNoTracking()
            .Where(period => period.Kind == NewsletterPeriodKind.Baseline)
            .Select(period => (DateTimeOffset?)period.CutoffAt)
            .SingleOrDefaultAsync(cancellationToken);
}
