using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Domain;
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
/// **A8**'in yazma tarafı (`0012b` **D15**). Bu spec'e kadar işleyici yalnız vitrin
/// satırını siliyordu: ilan `Yayında` kalıyor, aracın bayrağını kimse yazmıyordu ve biri
/// yeniden yayınladığında araç **ölü bir şubeyle** vitrine dönüyordu — sessizce.
///
/// <para><b>`Rezerve` ilanlar da kapsanıyor</b> (**D32**): `Unpublish()` artık oradan da
/// geçiyor ve `docs/domain.md` A3'e `Rezerve → Taslak` eklendi. Önceki hâli rezerve bir
/// ilanı çıkışı olmayan bir hâlde bırakıyordu. <b>`Satıldı` ve `Arşiv` kayıtlara
/// dokunulmuyor:</b> ikisi de vitrinde değil ve satılmış bir aracı "lokasyon bekliyor"
/// yapmak, kapanan şubenin satış geçmişini bozardı.</para>
///
/// <para><b>Seçim sınırı YOK</b> (**D33**): eylem şubedeki **bütün** araçlara uygulanır.
/// Kısmi başarı bir PERSONEL kararının karşılığıdır; sistemin kendi tepkisinde yarım
/// uygulanmış bir şube kapanışı A8'i deler. <i>Bedel, açıkça:</i> 300 araçlı bir şube tek
/// işlemde 300 geçiş, 300 bayrak ve 300 outbox satırı demektir.</para>
///
/// <para>Geçişler <b>duyurulur</b> (**D33**): duyuru personelin eylemine değil, olgunun
/// kendisine aittir.</para>
/// </summary>
internal sealed class BranchDeactivatedHandler(
    VehicleCatalogDbContext context,
    VehicleCatalogUnitOfWork unitOfWork,
    IClock clock)
    : IdempotentIntegrationEventHandler<BranchDeactivated, VehicleCatalogDbContext>(context, clock)
{
    protected override async Task HandleCoreAsync(
        BranchDeactivated integrationEvent,
        CancellationToken cancellationToken)
    {
        // Vitrin satırları KOŞULSUZ düşüyor ve bu sıra önemli: okuma modeli TÜRETİLMİŞ
        // veridir ve yazma modelinde karşılığı olmasa bile temizlenmek zorunda. İlk
        // yazımımda bu silme, "bu şubede araç yok" dalının ARKASINDAYDI ve okuma modeli
        // yalnız seed'den beslenen bir veritabanında satırlar yerinde kaldı — test
        // yakaladı (`0001`/AC-3, 6 satır beklenirken 2).
        await Context.ShowcaseListings
            .Where(listing => listing.BranchId == integrationEvent.BranchId)
            .ExecuteDeleteAsync(cancellationToken);

        List<Vehicle> araclar = await Context.Vehicles
            .Where(vehicle => vehicle.BranchId == integrationEvent.BranchId && !vehicle.IsArchived)
            .ToListAsync(cancellationToken);

        if (araclar.Count == 0)
        {
            return;
        }

        List<VehicleId> kimlikler = [.. araclar.Select(vehicle => vehicle.Id)];

        Dictionary<VehicleId, Listing> ilanlar = await Context.Listings
            .Where(listing => kimlikler.Contains(listing.VehicleId))
            .ToDictionaryAsync(listing => listing.VehicleId, cancellationToken);

        foreach (Vehicle vehicle in araclar)
        {
            if (!ilanlar.TryGetValue(vehicle.Id, out Listing? listing))
            {
                continue;
            }

            // `Satıldı` ve `Arşiv` dışarıda; `Unpublish` zaten onları reddediyor ve
            // sonucu bilerek yok sayılıyor — burada "yayında değilse dokunma" demek
            // isteniyor, bir hata bildirilmek değil.
            if (listing.Status is ListingStatus.Published or ListingStatus.Reserved)
            {
                listing.Unpublish();
            }

            vehicle.MarkAwaitingBranchAssignment();
            vehicle.Touch(Clock.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
