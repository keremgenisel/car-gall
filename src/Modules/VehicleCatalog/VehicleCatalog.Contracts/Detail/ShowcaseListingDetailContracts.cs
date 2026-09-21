using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

namespace CarGallery.Modules.VehicleCatalog.Contracts.Detail;

/// <summary>Etiketi çözülmüş bir nitelik satırı: ziyaretçi ham slug görmez.</summary>
public sealed record ShowcaseAttributeDto(string Key, string Label, string Value);

/// <summary>
/// R7 + R8. Kapalı veya `Gizli` işaretli hiçbir alan bu yanıtta BULUNMAZ — boş değer
/// olarak da bulunmaz (F3, AC-12, AC-13). Şase, plaka ve iç notlar hiç taşınmaz (F4, AC-14).
/// </summary>
public sealed record ShowcaseListingDetailDto(
    string Slug,
    bool IsReserved,
    string? CoverImageUrl,
    string ModelName,
    int ModelYear,
    int MileageKilometers,
    ShowcasePriceDto Price,
    string BranchName,
    IReadOnlyList<ShowcaseAttributeDto> Attributes);

public interface IShowcaseListingDetailQueries
{
    /// <summary>
    /// Görünmeyen ilan ile hiç var olmayan ilan AYNI sonucu üretir: <c>null</c>.
    /// İkisi farklı kod yollarından geçmez (E7, C13, AC-8).
    /// </summary>
    Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale, CancellationToken cancellationToken);
}
