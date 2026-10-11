using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Leads.Contracts.Events;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Domain.Events;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// Domain olayını entegrasyon olayına çevirir ve outbox'a yazar. Çeviri Application'ın
/// işidir: domain, modül dışındaki sözleşmeyi bilmez (architecture.md § 2.B).
///
/// Yazma, iş verisiyle AYNI `SaveChanges` içinde olur — bu yüzden ayrı bir birim değil,
/// kaydetmenin kendisidir. Talep kaydedildiyse duyuru da kaydedilmiştir; talep
/// kaydedilmediyse duyuru da kaydedilmemiştir (R14, AC-30, AC-31).
/// </summary>
internal sealed class LeadsUnitOfWork(
    LeadsDbContext context,
    OutboxWriter<LeadsDbContext> outbox,
    IClock clock)
{
    internal async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        List<Lead> degisenler =
        [
            .. context.ChangeTracker.Entries<Lead>()
                .Select(entry => entry.Entity)
                .Where(lead => lead.DomainEvents.Count > 0)
        ];

        foreach (Lead lead in degisenler)
        {
            foreach (IDomainEvent domainEvent in lead.DomainEvents)
            {
                IIntegrationEvent? duyuru = Translate(domainEvent, lead);

                if (duyuru is not null)
                {
                    outbox.Write(duyuru);
                }
            }

            lead.ClearDomainEvents();
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Duyuruya YALNIZCA istatistiksel alanlar girer. Ad, telefon, e-posta ve mesaj
    /// buraya konursa outbox satırında kalıcılaşır ve anonimleştirme onları temizleyemez
    /// (**L6**, R13).
    /// </summary>
    private IIntegrationEvent? Translate(IDomainEvent domainEvent, Lead lead) => domainEvent switch
    {
        LeadWasCaptured => new LeadCaptured(
            Guid.CreateVersion7(),
            clock.UtcNow,
            lead.Id.Value,
            lead.BranchId,
            lead.Vehicle.ListingId,
            LeadTypeCode.ToCode(lead.Type),
            lead.SubmittedLocale.Code),

        _ => null,
    };
}
