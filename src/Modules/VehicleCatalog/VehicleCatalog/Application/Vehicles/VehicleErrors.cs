namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// Kararlı hata kodları: `&lt;alan&gt;.&lt;durum&gt;`, snake_case
/// (docs/conventions.md § 5). Burada yalnız KOD vardır; çeviri uçta yapılır (C10).
/// </summary>
internal static class VehicleErrors
{
    /// <summary>AC-3 — şubesiz araç kaydedilemez (A1).</summary>
    internal const string BranchRequired = "vehicle.branch_required";

    /// <summary>AC-4 — var olmayan şube (C6).</summary>
    internal const string BranchNotFound = "vehicle.branch_not_found";

    /// <summary>
    /// AC-5 — referans veride bulunmayan değer. HANGİ alanın düştüğü `Error.Parameters`
    /// ile taşınır: altı alan ayrı ayrı doğrulanıyor ve "bir şey yanlış" diyen bir kod
    /// personele hangisini düzelteceğini söylemez.
    /// </summary>
    internal const string ReferenceNotFound = "vehicle.reference_not_found";

    /// <summary>AC-16 — galeride aynı şase ikinci kez kaydedilemez (A5).</summary>
    internal const string DuplicateVin = "vehicle.duplicate_vin";

    /// <summary>AC-9 — serbest metin üst uzunluk sınırını aştı (R11).</summary>
    internal const string TextTooLong = "vehicle.text_too_long";

    /// <summary>
    /// Şase BİÇİMİ geçersiz: boş ya da iç boşluk taşıyor. `TextTooLong`'dan ayrıdır —
    /// aynı koda eşlendiğinde personel boş alanda "uzunluk aşıldı" mesajı görüyordu.
    /// </summary>
    internal const string InvalidVin = "vehicle.invalid_vin";

    /// <summary>
    /// Kurulumda etkin olmayan bir locale için değer gönderildi. Değer SESSİZCE
    /// ATILMAZ: bu depoda sessiz veri kaybı tekrar eden bir derstir (`0009` — tanınmayan
    /// slug'ın sessizce kaybolması). Filtre eleyeceği bir değer bulduğunda reddeder.
    /// </summary>
    internal const string LocaleNotEnabled = "vehicle.locale_not_enabled";

    /// <summary>AC-6 — çekirdek alanlardan biri eksik (D8).</summary>
    internal const string CoreFieldMissing = "vehicle.core_field_missing";

    /// <summary>Para birimi veya tutar geçersiz (P1–P2).</summary>
    internal const string InvalidPrice = "vehicle.invalid_price";
}
