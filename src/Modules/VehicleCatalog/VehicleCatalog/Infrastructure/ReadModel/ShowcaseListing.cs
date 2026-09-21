namespace CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

/// <summary>
/// Vitrin okuma modeli (denormalize). YALNIZCA vitrinde görünen ilanları taşır: Yayında ve
/// Rezerve. Diğer durumlar satır olarak hiç bulunmaz — böylece AC-3'ün süzgeci sorgunun
/// where'inde değil, modelin kendisinde yaşar ve unutulamaz.
///
/// Şube adı burada denormalize durur; kaynağı YALNIZCA Dealership olaylarıdır ve bu kopya
/// düzenlenmez (C9, Yasak Listesi 33).
/// </summary>
internal sealed class ShowcaseListing
{
    internal required Guid ListingId { get; init; }

    internal required Guid VehicleId { get; init; }

    /// <summary>R6: Rezerve ilan vitrinde kalır, rozetle.</summary>
    internal required bool IsReserved { get; set; }

    internal required int ShowcaseSortWeight { get; set; }

    internal required DateTimeOffset PublishedAt { get; set; }

    internal required string BodyTypeSlug { get; init; }

    internal required Guid VehicleModelId { get; init; }

    internal required int ModelYear { get; init; }

    internal required int MileageKilometers { get; set; }

    /// <summary>Saklama decimal(19,4); "fiyat sorunuz" hâlinde null (P4, 0 DEĞİL).</summary>
    internal decimal? PriceAmount { get; set; }

    internal string? PriceCurrencyCode { get; set; }

    internal int? PriceMinorUnitExponent { get; set; }

    internal required bool PriceOnRequest { get; set; }

    internal required Guid BranchId { get; set; }

    /// <summary>Locale → şube adı. Şube adı çevrilebilir bir alandır (docs/architecture.md § 1).</summary>
    internal Dictionary<string, string> BranchNames { get; set; } = [];

    /// <summary>
    /// Kapak, sıralı medya listesinin İLK elemanıdır (docs/domain.md, Kapak Görseli).
    /// Hangi varlığın kapak olduğu burada tutulur; aksi hâlde MediaAssetReady olayları
    /// sıra dışı geldiğinde en son hazır olan görsel kapak olur ve domain kuralı bozulur.
    /// Teslimat sıra garantisi vermez (docs/architecture.md § 2.C).
    /// </summary>
    internal Guid? CoverAssetId { get; set; }

    internal string? CoverMediaUrl { get; set; }
}
