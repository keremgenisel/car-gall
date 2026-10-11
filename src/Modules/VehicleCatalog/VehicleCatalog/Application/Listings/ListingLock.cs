using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.VehicleCatalog.Infrastructure;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// Bir ilanın değişmezlerini koruyan satır kilidi, **TEK yerde**.
///
/// <para>Geçiş komutu ile öne çıkarma komutu <b>aynı</b> satırı kilitlemek zorunda: ikisi
/// de `listings` üzerinde yazıyor ve ayrı kilitler seçselerdi aynı ilan üzerinde eşzamanlı
/// koşabilirlerdi. Kilidin tanımını iki yere yazmak, o eşitliği bir gün sessizce bozmanın
/// en kolay yolu olurdu.</para>
///
/// <para>Yüklem <c>const</c>'tur ve değer parametredir; dizeye gömülmez
/// (`InvariantLock`, `0007`/AC-9).</para>
/// </summary>
internal static class ListingLock
{
    internal static InvariantLock Row(Guid vehicleId) => new(
        VehicleCatalogDbContext.Schema,
        "listings",
        "vehicle_id = {0}",
        [vehicleId]);
}
