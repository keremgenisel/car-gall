namespace CarGallery.Modules.VehicleReference.Domain;

/// <summary>
/// Referans veri: galeri yazmaz, sistem besler (docs/domain.md § 1). İş kuralı yoktur.
/// Çeviri, içeriğin sahibi olan modülde durur — burada, agregatın parçası olarak.
/// </summary>
internal sealed class BodyType
{
    internal required string Slug { get; init; }

    internal required string CanonicalName { get; init; }

    internal List<BodyTypeTranslation> Translations { get; init; } = [];
}

internal sealed class BodyTypeTranslation
{
    internal required string BodyTypeSlug { get; init; }

    internal required string Locale { get; init; }

    internal required string Name { get; set; }
}

/// <summary>
/// `0021` — model tam olarak bir markaya aittir (R2, D2) ve marka ondan hiç değişmez (D14):
/// <see cref="BrandId"/> yalnız kurulurken yazılır. Kanonik ad markasızdır ("Corolla") ve
/// marka içinde tekildir (D11) — tekillik <see cref="CanonicalKey"/> üzerinde, veritabanı
/// indeksiyle dayatılır. Silinmez, pasif yapılır (D5).
/// </summary>
internal sealed class VehicleModel
{
    internal required Guid Id { get; init; }

    internal required Guid BrandId { get; init; }

    internal required string CanonicalName { get; set; }

    /// <summary>Tekillik anahtarı: kanonik adın değişmez kültürle büyük harfi (C3).</summary>
    internal required string CanonicalKey { get; set; }

    internal bool IsActive { get; set; } = true;

    /// <summary>İyimser eşzamanlılık; her yazma bir artırır (`0020` deseni).</summary>
    internal int Version { get; set; } = 1;

    internal List<VehicleModelTranslation> Translations { get; init; } = [];
}

internal sealed class VehicleModelTranslation
{
    internal required Guid VehicleModelId { get; init; }

    internal required string Locale { get; init; }

    internal required string Name { get; set; }
}

/// <summary>
/// Kapalı kümeli referans terimleri: yakıt, vites, çekiş, donanım paketi (docs/domain.md).
/// Dördü de aynı şekle sahip — slug + kanonik ad + locale başına çeviri — bu yüzden tek
/// tiple, tür ayracıyla taşınırlar. Dört ayrı tip yazmak, dört ayrı yerde bakım demekti.
///
/// Kasa tipi bu tipe KATILMADI: 0001'de kendi yolunda çalışıyor ve onu buraya taşımak
/// 0002'nin kapsamı değildir (kapsam üyeye ait değildir).
/// </summary>
internal enum ReferenceTermKind
{
    FuelType = 1,
    TransmissionType = 2,
    DrivetrainType = 3,
    TrimLevel = 4,
}

internal sealed class ReferenceTerm
{
    internal required ReferenceTermKind Kind { get; init; }

    internal required string Slug { get; init; }

    internal required string CanonicalName { get; init; }

    internal List<ReferenceTermTranslation> Translations { get; init; } = [];
}

internal sealed class ReferenceTermTranslation
{
    internal required ReferenceTermKind Kind { get; init; }

    internal required string Slug { get; init; }

    internal required string Locale { get; init; }

    internal required string Name { get; set; }
}
