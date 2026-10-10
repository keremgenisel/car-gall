namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Opsiyonel alanın DEĞERİ. `VehicleFieldSetting` alanın açık/public olup olmadığını
/// söyler, `VehicleFieldDefinitions` tipini söyler; bu tip o alanın araçtaki karşılığını
/// taşır.
///
/// F1 — alanın kapatılması veriyi SİLMEZ: değer burada durmaya devam eder, yalnız
/// yanıta girmez (F3). İkisini karıştırmak, kapatmayı veri silmeye çevirirdi.
///
/// <para><b>0010b/C23 — C17'nin adlandırılmış istisnası.</b> Değer bu spec'e kadar
/// locale'li metindi ve tanımlı tek opsiyonel alan bir TUTAR'dı; parayı metinde saklamak
/// Altın Kural 4'ün ihlaliydi. Taşıyıcı artık tipli (Karar **D17**).</para>
/// </summary>
internal sealed class VehicleOptionalFieldValue
{
    internal required string FieldKey { get; init; }

    internal required OptionalFieldValue Value { get; set; }
}
