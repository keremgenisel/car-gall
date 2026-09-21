using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.EventHandlers;

/// <summary>
/// Şube adının vitrin okuma modelindeki kopyası YALNIZCA Dealership olaylarından beslenir
/// ve elle düzenlenmez (C9, Yasak Listesi 33). Teslimat en-az-bir-kezdir, bu yüzden
/// işleyiciler idempotenttir: aynı olay iki kez gelirse sonuç değişmez.
///
/// Eşzamanlılık denetimi yoktur ve bu BİLİNÇLİDİR: vitrin okuma modeli türetilmiş veridir,
/// gerçeğin kaynağı Dealership'tir. İki olay çakışırsa son yazan kazanır ve sonuç yine
/// yayıncının son hâline yakınsar. Kaynak veriye böyle davranılmaz.
/// </summary>
internal sealed class BranchDetailsChangedHandler(VehicleCatalogDbContext context)
{
    public async Task HandleAsync(BranchDetailsChanged notification, CancellationToken cancellationToken)
    {
        Dictionary<string, string> names = notification.Names
            .ToDictionary(name => name.Locale, name => name.Value);

        // Satırları belleğe çekmek, çok araçlı bir şubede tüm vitrini RAM'e almak demekti.
        await context.ShowcaseListings
            .Where(listing => listing.BranchId == notification.BranchId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(listing => listing.BranchNames, names),
                cancellationToken);
    }
}

internal sealed class BranchDeactivatedHandler(VehicleCatalogDbContext context)
{
    public async Task HandleAsync(BranchDeactivated notification, CancellationToken cancellationToken)
    {
        // A8: şube kapandığında o şubedeki araçlar yayından kalkar. Vitrin okuma modeli
        // yalnız görünür ilanları taşıdığı için "yayından kalkmak" satırın düşmesidir.
        await context.ShowcaseListings
            .Where(listing => listing.BranchId == notification.BranchId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
