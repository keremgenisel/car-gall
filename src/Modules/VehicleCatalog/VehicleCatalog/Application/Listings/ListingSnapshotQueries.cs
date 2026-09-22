using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// TEK SORGU, tek kod yolu. Vitrin okuma modeli zaten yalnız görünür ilanları taşıyor
/// (0001'de böyle kuruldu): gizli ilanın satırı yok. Böylece "gizli" ile "hiç yok" aynı
/// yoldan geçer ve aralarında ölçülebilir bir süre farkı doğmaz (0005 AC-14).
///
/// Locale ALMAZ ve çeviri çözmez: bu yüzey bir anlık görüntü kaynağıdır, bir gösterim
/// yüzeyi değil. Locale alsaydı `VehicleReference`'a düşmek zorunda kalır ve çağıranıyla
/// birlikte üç modüllü senkron zincir kurulurdu (Yasak Listesi 13).
/// </summary>
internal sealed class ListingSnapshotQueries(VehicleCatalogDbContext context) : IListingSnapshotQueries
{
    public async Task<ListingSnapshotDto?> FindBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        if (!ListingSlug.TryParse(slug, out ListingSlug parsed))
        {
            return null;
        }

        ShowcaseListing? row = await context.ShowcaseListings
            .AsNoTracking()
            .SingleOrDefaultAsync(listing => listing.Slug == parsed.Value, cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new ListingSnapshotDto(
            row.ListingId,
            row.IsReserved,
            row.VehicleModelId,
            row.PriceOnRequest
                ? ShowcasePriceDto.OnRequestPrice()
                : new ShowcasePriceDto(false, row.PriceAmount, row.PriceCurrencyCode, row.PriceMinorUnitExponent),
            row.BranchId);
    }
}
