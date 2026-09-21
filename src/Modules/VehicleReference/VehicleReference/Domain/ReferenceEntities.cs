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

internal sealed class VehicleModel
{
    internal required Guid Id { get; init; }

    internal required string CanonicalName { get; init; }

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
