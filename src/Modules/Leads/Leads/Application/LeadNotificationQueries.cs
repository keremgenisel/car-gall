using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// 0009 D1 — `Notifications`'ın gönderim anında okuduğu dar yüzey.
///
/// Projeksiyon SORGUYA yazılıyor, belleğe değil: bütün `Lead` agregatı çekilseydi ad ve
/// telefon da süreç belleğine inerdi. DTO onları zaten taşımıyor; SQL'in de taşımaması
/// gerekiyor (C5, 0006'daki aynı düzeltmenin tekrarı).
/// </summary>
internal sealed class LeadNotificationQueries(LeadsDbContext context) : ILeadNotificationQueries
{
    public async Task<LeadNotificationDto?> FindAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var row = await context.Leads
            .AsNoTracking()
            .Where(lead => lead.Id == new LeadId(leadId))
            .Select(lead => new
            {
                Email = lead.Contact.Email,
                Locale = lead.SubmittedLocale,
                ModelName = lead.Vehicle.CanonicalModelName,
                lead.BranchId,
                ListingId = lead.Vehicle.ListingId,
            })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new LeadNotificationDto(
                leadId,
                row.Email,
                row.Locale.Code,
                row.ModelName,
                row.BranchId,
                row.ListingId);
    }
}
