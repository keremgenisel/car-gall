using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// Yönetim listesi. Şase BURADA bulunur ve bu doğrudur (AC-19): yanıt oturum gerektiren
/// bir uçtan dönüyor ve personel şaseyi kendi giriyor.
///
/// <para><b>Sıralama araç kimliğine göre, azalan.</b> Kimlik UUID v7 — zaman sıralı — yani
/// "yeni eklenen önce" ayrı bir zaman damgası kolonu olmadan elde ediliyor. `0010b`
/// "son güncellenen" eksenini eklediğinde kendi kolonunu getirecek.</para>
/// </summary>
internal sealed class VehicleAdminQueries(
    VehicleCatalogDbContext context,
    IVehicleModelQueries models) : IVehicleAdminQueries
{
    public async Task<IReadOnlyList<VehicleAdminListItemDto>> ListAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        List<Vehicle> vehicles = await context.Vehicles
            .AsNoTracking()
            .OrderByDescending(vehicle => vehicle.Id)
            .ToListAsync(cancellationToken);

        if (vehicles.Count == 0)
        {
            return [];
        }

        Dictionary<VehicleId, ListingStatus> statuses = await context.Listings
            .AsNoTracking()
            .ToDictionaryAsync(listing => listing.VehicleId, listing => listing.Status, cancellationToken);

        // Model adı REFERANS VERİDE yaşıyor; burada saklanmıyor (Yasak Listesi 33).
        IReadOnlyDictionary<Guid, string> names = await models.ResolveNamesAsync(
            [.. vehicles.Select(vehicle => vehicle.VehicleModelId).Distinct()],
            locale,
            cancellationToken);

        return
        [
            .. vehicles.Select(vehicle => new VehicleAdminListItemDto(
                vehicle.Id.Value,
                names.TryGetValue(vehicle.VehicleModelId, out string? name) ? name : string.Empty,
                vehicle.ModelYear,
                vehicle.MileageKilometers,
                Price(vehicle.Price),
                // C11 — çevrilmiş durum adı değil KOD döner; çeviri uçta yapılır.
                (statuses.TryGetValue(vehicle.Id, out ListingStatus status) ? status : ListingStatus.Draft).ToString(),
                vehicle.Vin.Value))
        ];
    }

    private static VehicleAdminPriceDto Price(ListingPrice price) => price.IsOnRequest
        ? new VehicleAdminPriceDto(true, null, null, null)
        : new VehicleAdminPriceDto(
            false,
            price.Amount!.Value.Amount,
            price.Amount.Value.CurrencyCode,
            price.Amount.Value.MinorUnitExponent);
}
