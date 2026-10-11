using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Domain.Events;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// Domain olayını entegrasyon olayına çevirir ve outbox'a yazar. Çeviri Application'ın
/// işidir: domain, modül dışındaki sözleşmeyi bilmez (architecture.md § 2.B).
///
/// Yazma, iş verisiyle AYNI `SaveChanges` içinde olur — bu yüzden ayrı bir birim değil,
/// kaydetmenin kendisidir (R11, AC-25).
/// </summary>
internal sealed class DealershipUnitOfWork(
    DealershipDbContext context,
    OutboxWriter<DealershipDbContext> outbox,
    IClock clock)
{
    internal async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        List<Branch> degisenler =
        [
            .. context.ChangeTracker.Entries<Branch>()
                .Select(entry => entry.Entity)
                .Where(branch => branch.DomainEvents.Count > 0)
        ];

        foreach (Branch branch in degisenler)
        {
            foreach (IDomainEvent domainEvent in branch.DomainEvents)
            {
                IIntegrationEvent? duyuru = Translate(domainEvent, branch);

                if (duyuru is not null)
                {
                    outbox.Write(duyuru);
                }
            }

            branch.ClearDomainEvents();
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

    private IIntegrationEvent? Translate(IDomainEvent domainEvent, Branch branch) => domainEvent switch
    {
        BranchWasOpened => new BranchOpened(Guid.CreateVersion7(), clock.UtcNow, branch.Id.Value, Names(branch)),

        // R8/AC-21 — duyuru GÜNCEL adları taşır; alıcı ikinci bir sorgu yapmak zorunda kalmaz.
        BranchDisplayNameWasChanged => new BranchDetailsChanged(
            Guid.CreateVersion7(), clock.UtcNow, branch.Id.Value, Names(branch)),

        BranchWasClosed => new BranchDeactivated(Guid.CreateVersion7(), clock.UtcNow, branch.Id.Value),

        _ => null,
    };

    private static List<BranchNameDto> Names(Branch branch) =>
        [.. branch.Names.ToDictionary().Select(pair => new BranchNameDto(pair.Key, pair.Value))];
}
