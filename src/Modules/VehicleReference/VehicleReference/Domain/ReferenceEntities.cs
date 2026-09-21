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
