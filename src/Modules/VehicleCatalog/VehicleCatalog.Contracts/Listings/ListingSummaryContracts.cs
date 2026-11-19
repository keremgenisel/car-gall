using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

namespace CarGallery.Modules.VehicleCatalog.Contracts.Listings;

/// <summary>
/// `0018` **D11** — bülten satırının değişken verisi: ilanın o anki adresi, model kimliği, yılı,
/// km'si, fiyatı, şubesi ve ilk yayın zamanı.
///
/// <para><b>Model ADI değil model KİMLİĞİ döner.</b> Adı Catalog `VehicleReference`'tan
/// çözüyor; burada çözseydi `Notifications → VehicleCatalog → VehicleReference` zinciri
/// kurulurdu (Yasak Listesi 13, `0018` C12). Adı çağıran kendi tek atlamasıyla alır.</para>
///
/// <para><b>Şube ADI da yok:</b> şube kimliği döner, ad `Dealership.Contracts`'tan okunur
/// (**D11**).</para>
/// </summary>
public sealed record ListingSummaryDto(
    Guid ListingId,
    string Slug,
    Guid VehicleModelId,
    int ModelYear,
    int MileageKilometers,
    ShowcasePriceDto Price,
    Guid BranchId,
    DateTimeOffset PublishedAt);

/// <summary>
/// `0018` **D12** · **D21** · **D30** — verilen ilanlardan YALNIZ şu an **Yayında** olanlar.
/// Rezerve, satılmış, arşivlenmiş, yayından kaldırılmış ve hiç var olmayan ilan sonuçta
/// yoktur ve ikisi ayırt edilmez: çağıranın sorusu "bültene girebilir mi", "neden girmez" değil.
/// </summary>
public interface IListingSummaryQueries
{
    Task<IReadOnlyList<ListingSummaryDto>> FindPublishedAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken);
}
