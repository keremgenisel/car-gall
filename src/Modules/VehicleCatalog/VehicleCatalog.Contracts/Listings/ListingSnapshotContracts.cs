using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

namespace CarGallery.Modules.VehicleCatalog.Contracts.Listings;

/// <summary>
/// 0005 R7/R8 — talep anında ilanın kimliğini, fiyatını, şubesini ve model kimliğini veren
/// DAR okuma yüzeyi. Her alanın bir gerekçesi var; gerekçesi olmayan alan burada YOKTUR:
///
/// <list type="bullet">
/// <item><c>ListingId</c> — talebin izlenebilirliği (0005 R7).</item>
/// <item><c>IsReserved</c> — `Rezerve` ilana gönderilen talep REDDEDİLİR (0005 R2, AC-12).
/// Kararı `Leads` verir; burası yalnız olguyu söyler.</item>
/// <item><c>VehicleModelId</c> — kanonik Latin adın ANAHTARI (0005 AC-18). Adın kendisi
/// BURADAN DÖNMEZ: Catalog onu `VehicleReference`'tan çözüyor ve `Leads → VehicleCatalog
/// → VehicleReference` zinciri Yasak Listesi 13'ü ihlal ederdi. `Leads` adı kendi tek
/// atlamasıyla, doğrudan `VehicleReference`'tan alır.</item>
/// <item><c>Price</c> — anlık görüntüye kopyalanacak fiyat; "fiyat sorunuz" hâli dâhil (P4).</item>
/// <item><c>BranchId</c> — talep aracın şubesine bağlanır (docs/domain.md L3, AC-23).</item>
/// </list>
///
/// Gizli alan sızdırmaz: şase, plaka, iç notlar ve opsiyonel alanların hiçbiri burada
/// yoktur ve boş değer olarak da yoktur (F3, F4).
/// </summary>
public sealed record ListingSnapshotDto(
    Guid ListingId,
    bool IsReserved,
    Guid VehicleModelId,
    ShowcasePriceDto Price,
    Guid BranchId);

public interface IListingSnapshotQueries
{
    /// <summary>
    /// Görünmeyen ilan ile hiç var olmayan ilan AYNI sonucu üretir: <c>null</c>. İkisi
    /// farklı kod yolundan geçmez — ayrım yapılmadığı için sızdırılamaz (0005 R9, AC-14).
    /// </summary>
    Task<ListingSnapshotDto?> FindBySlugAsync(string slug, CancellationToken cancellationToken);
}
