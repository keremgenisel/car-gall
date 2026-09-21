using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// C3 — `Dealership` başka modüle senkron çağrı YAPMAZ. Bu sorgu yalnızca kendi verisini
/// okur; ziyaretçi locale'i parametre olarak gelir, başka bir modülden sorulmaz.
/// </summary>
internal sealed class BranchQueries(DealershipDbContext context, Locale defaultLocale) : IBranchQueries
{
    public async Task<BranchSummaryDto?> FindAsync(
        Guid branchId,
        string locale,
        CancellationToken cancellationToken)
    {
        Branch? branch = await context.Branches
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == new BranchId(branchId), cancellationToken);

        if (branch is null)
        {
            return null;
        }

        Result<Locale> requested = Locale.Create(locale);
        Locale resolved = requested.IsSuccess ? requested.Value : defaultLocale;

        // Varsayılan locale'deki ad her zaman vardır (R2/AC-6 onu zorunlu kılıyor),
        // dolayısıyla bu yol hiçbir koşulda boş ad döndürmez.
        string name = branch.Names.Resolve(resolved, defaultLocale) ?? string.Empty;

        return new BranchSummaryDto(branch.Id.Value, name, branch.Status.ToString());
    }
}
