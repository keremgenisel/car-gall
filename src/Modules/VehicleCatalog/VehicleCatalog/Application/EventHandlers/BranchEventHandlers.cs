using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.EventHandlers;

/// <summary>
/// Şube adının vitrin okuma modelindeki kopyası YALNIZCA Dealership olaylarından beslenir
/// ve elle düzenlenmez (C9, Yasak Listesi 33). Teslimat en-az-bir-kezdir, bu yüzden
/// işleyiciler idempotenttir: aynı olay iki kez gelirse sonuç değişmez.
/// </summary>
internal sealed class BranchDetailsChangedHandler(VehicleCatalogDbContext context)
{
    internal async Task HandleAsync(BranchDetailsChanged notification, CancellationToken cancellationToken)
    {
        List<ShowcaseListing> affected = await context.ShowcaseListings
            .Where(listing => listing.BranchId == notification.BranchId)
            .ToListAsync(cancellationToken);

        Dictionary<string, string> names = notification.Names
            .ToDictionary(name => name.Locale, name => name.Value);

        foreach (ShowcaseListing listing in affected)
        {
            listing.BranchNames = new Dictionary<string, string>(names);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class BranchDeactivatedHandler(VehicleCatalogDbContext context)
{
    internal async Task HandleAsync(BranchDeactivated notification, CancellationToken cancellationToken)
    {
        // A8: şube kapandığında o şubedeki araçlar yayından kalkar. Vitrin okuma modeli
        // yalnız görünür ilanları taşıdığı için "yayından kalkmak" satırın düşmesidir.
        List<ShowcaseListing> affected = await context.ShowcaseListings
            .Where(listing => listing.BranchId == notification.BranchId)
            .ToListAsync(cancellationToken);

        context.ShowcaseListings.RemoveRange(affected);
        await context.SaveChangesAsync(cancellationToken);
    }
}
