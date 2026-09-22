using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

namespace CarGallery.Modules.Leads.Infrastructure;

/// <summary>
/// Application'daki portun dışa bakan adaptörü. `VehicleCatalog`'a YALNIZCA `Contracts`
/// üzerinden, salt okuma, tek atlama ve aynı istek içinde konuşur — senkron doğrudan
/// çağrının üç koşulu da sağlanır (docs/architecture.md § 2.A).
/// </summary>
internal sealed class ListingSnapshotSource(IListingSnapshotQueries queries) : IListingSnapshotSource
{
    public async Task<ListingSnapshot?> FindAsync(string slug, CancellationToken cancellationToken)
    {
        ListingSnapshotDto? dto = await queries.FindBySlugAsync(slug, cancellationToken);

        if (dto is null)
        {
            return null;
        }

        return new ListingSnapshot(
            dto.ListingId,
            dto.IsReserved,
            dto.VehicleModelId,
            ToPrice(dto.Price),
            dto.BranchId);
    }

    /// <summary>
    /// "Fiyat sorunuz" hâli KOPYALANIR; tutar `0`'a çevrilmez (P4, C7). Tutar varken
    /// `Money` kurulamıyorsa bu sessizce düzeltilecek bir durum değildir — kaynak bozuk
    /// demektir ve talebin yanlış bir kopya taşımasındansa "fiyat sorunuz" taşıması
    /// dürüsttür.
    /// </summary>
    private static PriceSnapshot ToPrice(ShowcasePriceDto price)
    {
        if (price.OnRequest || price.Amount is null || price.CurrencyCode is null || price.MinorUnitExponent is null)
        {
            return PriceSnapshot.OnRequest();
        }

        Result<Money> money = Money.Create(price.Amount.Value, price.CurrencyCode, price.MinorUnitExponent.Value);

        return money.IsSuccess ? PriceSnapshot.Of(money.Value) : PriceSnapshot.OnRequest();
    }
}
