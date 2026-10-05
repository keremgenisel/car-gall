using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// Kurulumun para birimi (**C3**, **P3**): **kurulum başına sabittir ve panelden
/// değiştirilemez**. Personel yalnız TUTAR girer; birim yapılandırmadan gelir.
///
/// <para>Üs ayrı taşınıyor çünkü KWD/BHD/JOD/OMR üç ondalıklıdır ve üs kaybolursa
/// `1250,500` sessizce `1250,50` gibi okunur (P2, AC-22).</para>
/// </summary>
internal sealed record CatalogCurrencyOptions(string Code, int MinorUnitExponent)
{
    internal Result<Money> Money(decimal amount) =>
        BuildingBlocks.Money.Create(amount, Code, MinorUnitExponent);
}
