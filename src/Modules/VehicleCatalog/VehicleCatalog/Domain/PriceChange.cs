namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Fiyat geçmişi kaydı (docs/domain.md **A6**). **Silinemez ve düzeltilemez**; yanlış
/// giriş yeni bir kayıtla düzeltilir.
///
/// <para><b>İnvariant kodun ŞEKLİNDEDİR, bir kontrolde değil:</b> bu tipte hiçbir setter,
/// `Vehicle`'da hiçbir kaldırma metodu ve hiçbir uçta silme yolu yok. AC-24'ün gözlem
/// noktası C19'dadır — silme ucu olmadığı için endpoint testiyle sınanamaz; invariant
/// domain ve altyapı katmanında sınanır.</para>
///
/// <para>Geçmiş GERİYE DÖNÜK ÜRETİLEMEZ: bugün yazılmayan bir değişiklik yarın
/// oluşturulamaz. D3 bu yüzden ertelenmedi.</para>
/// </summary>
internal sealed class PriceChange
{
    internal required Guid Id { get; init; }

    internal required VehicleId VehicleId { get; init; }

    internal required ListingPrice Price { get; init; }

    internal required DateTimeOffset ChangedAt { get; init; }
}
