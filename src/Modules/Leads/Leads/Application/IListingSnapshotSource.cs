using CarGallery.Modules.Leads.Domain;

namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// Talep anındaki ilan gerçeği. `Leads`'in dışa bakan PORTUDUR; uygulaması
/// Infrastructure'da ve yalnız `VehicleCatalog.Contracts` üzerinden konuşur. Domain bu
/// portu hiç bilmez (docs/conventions.md § 4).
///
/// <c>null</c> = ilan çözülemiyor: ya hiç yok ya da vitrinde görünür değil. İki durum
/// AYIRT EDİLEMEZ ve bu bilinçlidir (R9, AC-14) — ayrım yapılmadığı için sızdırılamaz.
/// </summary>
internal interface IListingSnapshotSource
{
    Task<ListingSnapshot?> FindAsync(string slug, CancellationToken cancellationToken);
}

/// <summary>
/// Sınırın bu yanındaki şekil. `ListingSnapshotDto`'yu doğrudan taşımak, Catalog'un
/// sözleşmesini `Leads`'in iç katmanlarına sızdırırdı; sınır boyunca tekrar bilinçlidir
/// (Yasak Listesi 10).
/// </summary>
internal sealed record ListingSnapshot(
    Guid ListingId,
    bool IsReserved,
    Guid VehicleModelId,
    PriceSnapshot Price,
    Guid BranchId);
