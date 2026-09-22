namespace CarGallery.Modules.Leads.Endpoints;

/// <summary>
/// Talebin tel üzerindeki gövdesi.
///
/// İlan <b>slug</b> ile gelir, kimlikle değil: ilan detay sözleşmesi (`0002`) ziyaretçiye
/// kimlik vermiyor ve vermek için genişletilmesi gerekirdi. Slug zaten ilanın kalıcı
/// adresidir ve ziyaretçinin elindeki tek tanımlayıcıdır.
///
/// <see cref="Locale"/> gövdede AÇIKÇA gelir. `Accept-Language`'a düşmek R6'yı tahmine
/// bağlardı: `SubmittedLocale` bugün yanlış kaydedilirse yarın geriye dönük düzeltilemez.
///
/// Alanlar `null` olabilir — doğrulama uçta değil domainde yapılır ve bağlama hatası
/// yerine `errorCode` döner (C10).
/// </summary>
internal sealed record CaptureLeadRequest(
    string? ListingSlug,
    string? LeadType,
    string? Locale,
    string? FullName,
    string? Phone,
    string? Email,
    string? Message,
    bool ConsentGiven);

/// <summary>Oluşan talebin kimliği. Kişisel hiçbir alan geri dönmez.</summary>
internal sealed record CaptureLeadResponse(Guid LeadId);
