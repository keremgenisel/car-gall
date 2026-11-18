using System.Text.Json.Serialization;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

namespace CarGallery.Modules.VehicleCatalog.Contracts.Detail;

/// <summary>Etiketi çözülmüş bir nitelik satırı: ziyaretçi ham slug görmez.</summary>
public sealed record ShowcaseAttributeDto(string Key, string Label, string Value);

/// <summary>Bir türevin adresi ve GERÇEK boyutu (`0016b` D18).</summary>
public sealed record ShowcaseImageVariantDto(string Url, int Width, int Height);

/// <summary>
/// `0016b` C12 — galerinin bir görseli: üç türev. Orijinal dosyanın adresi yoktur (C7).
/// </summary>
public sealed record ShowcaseImageDto(
    ShowcaseImageVariantDto Thumbnail,
    ShowcaseImageVariantDto Display,
    ShowcaseImageVariantDto Large);

/// <summary>`0016b` C12 — haritanın bir parçası; YALNIZ kod (`0016a` C10, C6).</summary>
public sealed record ShowcasePaintChangeDto(string Part, string State);

/// <summary>
/// R7 + R8. Kapalı veya `Gizli` işaretli hiçbir alan bu yanıtta BULUNMAZ — boş değer
/// olarak da bulunmaz (F3, AC-12, AC-13). Şase, plaka ve iç notlar hiç taşınmaz (F4, AC-14).
///
/// <para>`0016b` C12/D22 — tek kapak adresi kaldırıldı; yerine galerinin hazır görselleri
/// medya sırasıyla gelir (<c>Images</c>, D14). Parça haritası ayrı bir özelliktir ve alan
/// kapalı, gizli ya da değersizken JSON anahtarı HİÇ yazılmaz — <c>null</c> olarak da (D9,
/// C1). <c>Attributes</c>'ta tekrar edilmez.</para>
/// </summary>
public sealed record ShowcaseListingDetailDto(
    string Slug,
    bool IsReserved,
    IReadOnlyList<ShowcaseImageDto> Images,
    string ModelName,
    int ModelYear,
    int MileageKilometers,
    ShowcasePriceDto Price,
    string BranchName,
    IReadOnlyList<ShowcaseAttributeDto> Attributes,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<ShowcasePaintChangeDto>? PaintChangeMap = null);

public interface IShowcaseListingDetailQueries
{
    /// <summary>
    /// Görünmeyen ilan ile hiç var olmayan ilan AYNI sonucu üretir: <c>null</c>.
    /// İkisi farklı kod yollarından geçmez (E7, C13, AC-8).
    /// </summary>
    Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale, CancellationToken cancellationToken);

    /// <summary>
    /// `0016b` C13 — detayın galerisinin İLK hazır görseli; yoksa <c>null</c>. Host kabuğu onu
    /// HTML'e ön yükleme olarak basar: görsel JS ve detay isteği beklenmeden keşfedilir
    /// (LCP adayı). Detay sorgusunun hafif kardeşi — model adı, nitelik ve fiyat çözülmez.
    /// </summary>
    Task<ShowcaseImageDto?> FindFirstImageAsync(string slug, CancellationToken cancellationToken);
}
