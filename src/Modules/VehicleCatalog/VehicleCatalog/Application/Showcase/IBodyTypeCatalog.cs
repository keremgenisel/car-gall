namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// Kasa tipinin KANONİK KÜMESİ referans veridir ve VehicleReference'a aittir
/// (docs/architecture.md § 3, C13). Bu arayüz Catalog'un dışa bakan portudur;
/// uygulaması Infrastructure'da, yalnızca VehicleReference.Contracts üzerinden konuşur.
///
/// Biçim doğrulaması kümenin yerini TUTMAZ: "banana" geçerli bir slug biçimidir ama
/// kanonik değildir ve daraltmaya girerse boş liste üretir (AC-17).
/// </summary>
internal interface IBodyTypeCatalog
{
    Task<IReadOnlySet<string>> GetCanonicalSlugsAsync(CancellationToken cancellationToken);
}
