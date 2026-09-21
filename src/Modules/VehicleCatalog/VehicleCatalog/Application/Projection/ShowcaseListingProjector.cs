using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Application.Projection;

/// <summary>
/// Vitrinde görünürlük kararının TEK yeri. Gizleme sunum katmanında değil, Catalog'un
/// sorgu tarafında uygulanır (C10, Yasak Listesi 41) — burada karar verilir, okuma
/// modeline yalnızca görünür ilanlar yazılır.
/// </summary>
internal static class ShowcaseListingProjector
{
    /// <summary>
    /// R2: yalnız Yayında ve Rezerve. A8 uyarınca lokasyon ataması bekleyen araç,
    /// ilanı ne olursa olsun vitrinde yer almaz — "sessizce vitrinde kalmazlar".
    /// Görünmeyen ilan için <c>null</c> döner; çağıran satırı siler.
    /// </summary>
    internal static ShowcaseListing? Project(
        Vehicle vehicle,
        Listing listing,
        IReadOnlyDictionary<string, string> branchNames,
        string? coverMediaUrl)
    {
        if (vehicle.AwaitingBranchAssignment)
        {
            return null;
        }

        if (listing.Status is not (ListingStatus.Published or ListingStatus.Reserved))
        {
            return null;
        }

        if (listing.PublishedAt is null)
        {
            return null;
        }

        return new ShowcaseListing
        {
            ListingId = listing.Id.Value,
            VehicleId = vehicle.Id.Value,
            IsReserved = listing.Status is ListingStatus.Reserved,
            ShowcaseSortWeight = listing.ShowcaseSortWeight,
            PublishedAt = listing.PublishedAt.Value,
            BodyTypeSlug = vehicle.BodyType.Value,
            VehicleModelId = vehicle.VehicleModelId,
            ModelYear = vehicle.ModelYear,
            MileageKilometers = vehicle.MileageKilometers,
            PriceAmount = vehicle.Price.Amount?.Amount,
            PriceCurrencyCode = vehicle.Price.Amount?.CurrencyCode,
            PriceMinorUnitExponent = vehicle.Price.Amount?.MinorUnitExponent,
            PriceOnRequest = vehicle.Price.IsOnRequest,
            BranchId = vehicle.BranchId,
            BranchNames = new Dictionary<string, string>(branchNames),
            CoverAssetId = vehicle.CoverAssetId,
            CoverMediaUrl = coverMediaUrl,
        };
    }
}
