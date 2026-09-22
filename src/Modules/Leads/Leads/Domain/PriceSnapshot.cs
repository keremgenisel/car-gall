using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// Talep anındaki fiyatın KOPYASI. "Fiyat sorunuz" ayrı bir durumdur, `0` değildir
/// (docs/domain.md **P4**, C7, AC-21): tutarı `0` kopyalamak, fiyatı gerçekten `0` olan
/// bir aracı ayırt edilemez kılar ve kopya sonradan DÜZELTİLEMEZ — kaynak da değişmiş
/// olabilir (Bilinen tuzak 2).
///
/// `VehicleCatalog.ListingPrice` ile aynı şekle sahip olması tekrar değil, sınır boyunca
/// bilinçli kopyadır (Yasak Listesi 10): Catalog'un tipi aracın GÜNCEL fiyatını temsil
/// eder, bu tip talebin DONMUŞ gerçeğini. İkisi birlikte değişmek zorunda değildir.
/// </summary>
internal readonly record struct PriceSnapshot
{
    private PriceSnapshot(Money? amount, bool isOnRequest)
    {
        Amount = amount;
        IsOnRequest = isOnRequest;
    }

    internal Money? Amount { get; }

    internal bool IsOnRequest { get; }

    internal static PriceSnapshot OnRequest() => new(null, true);

    internal static PriceSnapshot Of(Money amount) => new(amount, false);
}
