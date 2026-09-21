using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// "Fiyat sorunuz" AYRI BİR DURUMDUR, 0 değildir (docs/domain.md P4). Tutarı 0 ile
/// temsil etmek, fiyatı gerçekten 0 olan bir aracı ayırt edilemez kılar ve aralık
/// filtrelerinde sessizce yanlış sonuç üretir.
/// </summary>
internal readonly record struct ListingPrice
{
    private ListingPrice(Money? amount, bool isOnRequest)
    {
        Amount = amount;
        IsOnRequest = isOnRequest;
    }

    internal Money? Amount { get; }

    internal bool IsOnRequest { get; }

    internal static ListingPrice OnRequest() => new(null, true);

    internal static ListingPrice Of(Money amount) => new(amount, false);
}
