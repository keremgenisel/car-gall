namespace CarGallery.Modules.VehicleReference.Domain;

/// <summary>
/// `0021` R1 — marka: kanonik Latin ad (galeri genelinde tekil), dil başına opsiyonel görünen
/// ad ve adres kısaltması. Kısaltma kanonik addan BİR KEZ üretilir ve bir daha değişmez
/// (D10): paylaşılmış liste bağlantısının (`?brand=toyota`) parçasıdır ve kanonik ad
/// düzeltildiğinde bağlantı çalışmaya devam etmelidir.
///
/// <para>Silinmez, pasif yapılır (D5): geçmiş araçların ve ilanların referansıdır.
/// "Ana sayfada göster" bir işarettir; sıra kanonik ada göre alfabetiktir (D7, X6).</para>
/// </summary>
internal sealed class VehicleBrand
{
    internal required Guid Id { get; init; }

    internal required string CanonicalName { get; set; }

    /// <summary>Tekillik anahtarı: kanonik adın değişmez kültürle büyük harfi (C3).</summary>
    internal required string CanonicalKey { get; set; }

    internal required string Slug { get; init; }

    internal bool IsActive { get; set; } = true;

    internal bool ShowOnHome { get; set; }

    /// <summary>İyimser eşzamanlılık; her yazma bir artırır (`0020` deseni).</summary>
    internal int Version { get; set; } = 1;

    internal List<VehicleBrandTranslation> Translations { get; init; } = [];
}

internal sealed class VehicleBrandTranslation
{
    internal required Guid BrandId { get; init; }

    internal required string Locale { get; init; }

    internal required string Name { get; set; }
}
