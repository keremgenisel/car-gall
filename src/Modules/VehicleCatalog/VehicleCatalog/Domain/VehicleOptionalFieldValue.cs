using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Opsiyonel alanın DEĞERİ. `VehicleFieldSetting` alanın açık/public olup olmadığını
/// söyler; bu tip o alanın araçtaki karşılığını taşır.
///
/// F1 — alanın kapatılması veriyi SİLMEZ: değer burada durmaya devam eder, yalnız
/// yanıta girmez (F3). İkisini karıştırmak, kapatmayı veri silmeye çevirirdi.
/// </summary>
internal sealed class VehicleOptionalFieldValue
{
    internal required string FieldKey { get; init; }

    /// <summary>Serbest metin locale başına taşınır; sessiz fallback (docs/architecture.md § 3).</summary>
    internal required LocalizedText Value { get; set; }
}
