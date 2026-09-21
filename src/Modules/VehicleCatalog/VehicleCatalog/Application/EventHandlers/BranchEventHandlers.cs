using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.EventHandlers;

/// <summary>
/// Şube adının vitrin okuma modelindeki kopyası YALNIZCA Dealership olaylarından beslenir
/// ve elle düzenlenmez (C9/0001, Yasak Listesi 33).
///
/// Idempotency artık taban sınıfta ve olayın KİMLİĞİNE dayanıyor (0003 D4). Önceden bu
/// işleyiciler yalnız ExecuteUpdate/ExecuteDelete kullandıkları için tekrara doğaları
/// gereği dayanıklıydı — ama bu bir tasarım değil şanstı.
/// </summary>
internal sealed class BranchDetailsChangedHandler(VehicleCatalogDbContext context, IClock clock)
    : IdempotentIntegrationEventHandler<BranchDetailsChanged, VehicleCatalogDbContext>(context, clock)
{
    protected override async Task HandleCoreAsync(
        BranchDetailsChanged integrationEvent,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> names = integrationEvent.Names
            .ToDictionary(name => name.Locale, name => name.Value);

        // Satırları belleğe çekmek, çok araçlı bir şubede tüm vitrini RAM'e almak demekti.
        await Context.ShowcaseListings
            .Where(listing => listing.BranchId == integrationEvent.BranchId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(listing => listing.BranchNames, names),
                cancellationToken);
    }
}

/// <summary>
/// A8'in güvenlik ağı: şube kapatıldığında o şubedeki araçlar vitrinden düşer.
/// Vitrin okuma modeli yalnız görünür ilanları taşıdığı için "yayından kalkmak" satırın
/// düşmesidir.
///
/// Eşzamanlılık denetimi yok ve bu BİLİNÇLİ: okuma modeli türetilmiş veridir, gerçeğin
/// kaynağı Dealership'tir; son yazan kazanır ve sonuç yayıncının son hâline yakınsar.
/// </summary>
internal sealed class BranchDeactivatedHandler(VehicleCatalogDbContext context, IClock clock)
    : IdempotentIntegrationEventHandler<BranchDeactivated, VehicleCatalogDbContext>(context, clock)
{
    protected override async Task HandleCoreAsync(
        BranchDeactivated integrationEvent,
        CancellationToken cancellationToken) =>
        await Context.ShowcaseListings
            .Where(listing => listing.BranchId == integrationEvent.BranchId)
            .ExecuteDeleteAsync(cancellationToken);
}
