using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.VehicleCatalog.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Domain.Events;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application;

/// <summary>
/// Domain olayını entegrasyon olayına çevirir ve outbox'a yazar (`0012b` **D14**).
/// Çeviri `Application`'ın işidir: `Domain`, modül dışındaki sözleşmeyi bilmez
/// (`docs/architecture.md` § 2.B).
///
/// <para>Yazma, iş verisiyle <b>AYNI</b> <c>SaveChanges</c> içinde olur — bu yüzden ayrı
/// bir birim değil, kaydetmenin kendisidir. Geçiş kaydedildiyse duyurusu da
/// kaydedilmiştir; geçiş geri alındıysa duyuru da alınmamıştır (C3, AC-36).</para>
///
/// <para><b>Yazıcı KONKRET tiple enjekte ediliyor</b> (C11): paylaşılan non-generic bir
/// soyutlama 2026-10-11'de silindi (`7e8bdef`, #52) çünkü DI'da son kayıt kazanıyordu ve
/// bir modülün duyurusu hiç kalıcı olmuyordu. `VehicleCatalog` outbox tutan dördüncü
/// modül; `OutboxYazicisiTests`'in üçüncü iddiası onu kendiliğinden kapsıyor.</para>
/// </summary>
internal sealed class VehicleCatalogUnitOfWork(
    VehicleCatalogDbContext context,
    OutboxWriter<VehicleCatalogDbContext> outbox,
    IClock clock)
{
    internal async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        List<Listing> ilanlar =
        [
            .. context.ChangeTracker.Entries<Listing>()
                .Select(entry => entry.Entity)
                .Where(listing => listing.DomainEvents.Count > 0)
        ];

        List<Vehicle> araclar =
        [
            .. context.ChangeTracker.Entries<Vehicle>()
                .Select(entry => entry.Entity)
                .Where(vehicle => vehicle.DomainEvents.Count > 0)
        ];

        foreach (Listing listing in ilanlar)
        {
            foreach (IDomainEvent domainEvent in listing.DomainEvents)
            {
                IIntegrationEvent? duyuru = Translate(domainEvent, listing);

                if (duyuru is not null)
                {
                    outbox.Write(duyuru);
                }
            }

            listing.ClearDomainEvents();
        }

        foreach (Vehicle vehicle in araclar)
        {
            // Aracın olayı ilanın kimliğini ve adresini taşımak zorunda (**D30**) ve ikisi
            // de `Listing`'de. Düzenleme komutu ilanı zaten yüklüyor; yüklenmediği bir yol
            // doğarsa tek sorguyla çözülüyor — duyuruyu sessizce düşürmek, `0006`'nın
            // ikinci kaçan hatasının aynı ailesi olurdu.
            Listing? listing = await IlanAsync(vehicle.Id, cancellationToken);

            foreach (IDomainEvent domainEvent in vehicle.DomainEvents)
            {
                IIntegrationEvent? duyuru = listing is null ? null : Translate(domainEvent, listing);

                if (duyuru is not null)
                {
                    outbox.Write(duyuru);
                }
            }

            vehicle.ClearDomainEvents();
        }

        return await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Listing?> IlanAsync(VehicleId vehicleId, CancellationToken cancellationToken)
    {
        Listing? izlenen = context.ChangeTracker.Entries<Listing>()
            .Select(entry => entry.Entity)
            .FirstOrDefault(listing => listing.VehicleId == vehicleId);

        return izlenen ?? await context.Listings
            .FirstOrDefaultAsync(listing => listing.VehicleId == vehicleId, cancellationToken);
    }

    /// <summary>
    /// Sekiz olay, sekiz dal. **Her duyuru BENZERSİZ bir kimlik taşır**: teslimat
    /// en-az-bir-kezdir ve alıcıların yinelenen işleme koruması bu kimliğe dayanır
    /// (`docs/architecture.md` § 2.C).
    ///
    /// <para><b>Adres `string.Empty` olabilir</b> ve bu bilinçli: hiç yayınlanmamış bir
    /// ilanın adresi yoktur (AC-49). O ilandan **üç** olay çıkabilir — `VehicleArchived`
    /// (`Taslak → Arşiv`, **D10**) ile taslak bir aracın fiyatı veya şubesi düzenlendiğinde
    /// `VehiclePriceChanged` ve `VehicleRelocated`. `null` yerine boş dize, sözleşmeyi
    /// nullable yapmadan aynı şeyi söylüyor; tüketici "adres yok" ile "adres boş"u ayırmak
    /// zorunda değil.</para>
    /// </summary>
    private IIntegrationEvent? Translate(IDomainEvent domainEvent, Listing listing)
    {
        Guid kimlik = Guid.CreateVersion7();
        DateTimeOffset an = clock.UtcNow;
        Guid aracId = listing.VehicleId.Value;
        Guid ilanId = listing.Id.Value;
        string adres = listing.Slug?.Value ?? string.Empty;

        return domainEvent switch
        {
            ListingPublished published =>
                new VehiclePublished(kimlik, an, aracId, ilanId, adres, published.PublishedAt),
            ListingUnpublished => new VehicleUnpublished(kimlik, an, aracId, ilanId, adres),
            ListingReserved => new VehicleReserved(kimlik, an, aracId, ilanId, adres),
            ListingReturnedToShowcase =>
                new VehicleReturnedToShowcase(kimlik, an, aracId, ilanId, adres),
            ListingSold => new VehicleSold(kimlik, an, aracId, ilanId, adres),
            ListingArchived => new VehicleArchived(kimlik, an, aracId, ilanId, adres),
            VehiclePriceWasChanged => new VehiclePriceChanged(kimlik, an, aracId, ilanId, adres),
            VehicleWasRelocated => new VehicleRelocated(kimlik, an, aracId, ilanId, adres),
            _ => null,
        };
    }
}
