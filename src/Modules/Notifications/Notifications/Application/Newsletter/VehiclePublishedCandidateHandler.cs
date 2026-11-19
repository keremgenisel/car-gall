using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` **D4** — `VehiclePublished`'ın İLK tüketicisi: her duyuru, ilan kimliği üzerinde
/// TEKİL bir Bülten Adayına dönüşür.
///
/// <para><b>Pencere olayın zamanına BAĞLANMAZ.</b> `0012b` D31'in <c>OccurredAt &gt;
/// PublishedAt</c> formülü kullanılmaz: iki değer ayrı saat okumalarından gelir ve gerçek
/// saatte her ilk yayını "yeniden yayın" sayar (Bilinen tuzak 8, AC-58). "Yeni" tanımı
/// yalnız iki şeydir: ilanın hiç aday olmamış olması ve ilk yayınının devreye giriş anından
/// sonra olması (AC-60).</para>
///
/// <para><b>Yeniden yayın ve tekrar teslim ikinci aday üretmez</b> (AC-57, AC-59, AC-73):
/// aynı olay inbox'ta, aynı ilanın ikinci olayı aday kaydının tekilliğinde düşer.</para>
///
/// <para>Gecikmeli işlenen duyuru KAYBOLMAZ: atanmamış aday bir sonraki işlenen dönemde yer
/// alır (Bilinen tuzak 11, AC-56).</para>
/// </summary>
internal sealed class VehiclePublishedCandidateHandler(
    NotificationsDbContext context,
    IClock clock,
    ILogger<VehiclePublishedCandidateHandler> logger)
    : IdempotentIntegrationEventHandler<VehiclePublished, NotificationsDbContext>(context, clock)
{
    protected override async Task HandleCoreAsync(VehiclePublished integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        DateTimeOffset now = Clock.UtcNow;

        DateTimeOffset? baseline = await new NewsletterBaseline(Context, Clock).FindAsync(cancellationToken);

        if (baseline is null)
        {
            // İlk duyuru zamanlı servisin ilk turundan önce geldi: devreye giriş anı şimdidir.
            // Kayıt bu işleyicinin SaveChanges'iyle yazılır; servis aynı anda yazarsa tekil
            // indeks bu teslimi düşürür ve outbox onu yeniden dener.
            NewsletterPeriod first = NewsletterPeriod.Baseline(now);
            Context.Periods.Add(first);
            baseline = first.CutoffAt;
        }

        if (integrationEvent.PublishedAt < baseline)
        {
            logger.LogInformation(
                "İlan devreye girişten önce yayınlanmış; bülten adayı değil: {ListingId}",
                integrationEvent.ListingId);

            return;
        }

        bool known = await Context.Candidates.AnyAsync(
            candidate => candidate.ListingId == integrationEvent.ListingId,
            cancellationToken);

        if (known)
        {
            return;
        }

        Context.Candidates.Add(NewsletterCandidate.Record(integrationEvent.ListingId, integrationEvent.PublishedAt, now));
    }
}
