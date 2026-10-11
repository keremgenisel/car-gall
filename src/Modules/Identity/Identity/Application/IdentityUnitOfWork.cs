using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Identity.Contracts.Events;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Domain.Events;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// Domain olayını entegrasyon olayına çevirir ve outbox'a yazar. Çeviri Application'ın
/// işidir: domain, modül dışındaki sözleşmeyi bilmez (architecture.md § 2.B).
///
/// Yazma, iş verisiyle AYNI `SaveChanges` içinde olur — bu yüzden ayrı bir birim değil,
/// kaydetmenin kendisidir. Personel değişikliği kaydedildiyse duyurusu da
/// kaydedilmiştir; değişiklik geri alındıysa duyuru da alınmamıştır (C5, AC-48).
/// </summary>
internal sealed class IdentityUnitOfWork(
    IdentityDbContext context,
    OutboxWriter<IdentityDbContext> outbox,
    IClock clock)
{
    internal async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        List<Staff> degisenler =
        [
            .. context.ChangeTracker.Entries<Staff>()
                .Select(entry => entry.Entity)
                .Where(staff => staff.DomainEvents.Count > 0)
        ];

        foreach (Staff staff in degisenler)
        {
            foreach (IDomainEvent domainEvent in staff.DomainEvents)
            {
                IIntegrationEvent? duyuru = Translate(domainEvent);

                if (duyuru is not null)
                {
                    outbox.Write(duyuru);
                }
            }

            staff.ClearDomainEvents();
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// AC-49 — her duyuru BENZERSİZ bir kimlik taşır. Teslimat en-az-bir-kezdir ve
    /// alıcıların yinelenen işleme koruması bu kimliğe dayanır (architecture.md § 2.C).
    /// Bugün alıcı yok (X6, X7); yayıncı tarafı yine de doğru kurulur.
    /// </summary>
    private IIntegrationEvent? Translate(IDomainEvent domainEvent) => domainEvent switch
    {
        StaffWasDeactivated deactivated =>
            new StaffDeactivated(Guid.CreateVersion7(), clock.UtcNow, deactivated.StaffId.Value),

        _ => null,
    };
}
