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

    /// <summary>
    /// İlanın kalıcı adresi (0002 R2, E9). Okuma modelinde SAKLANIR, yeniden hesaplanmaz —
    /// kararlılık yeniden üretmemekle sağlanır.
    /// </summary>
    internal required string Slug { get; init; }

    internal required string BodyTypeSlug { get; init; }

    internal required string FuelTypeSlug { get; init; }

    internal required string TransmissionTypeSlug { get; init; }

    internal required string DrivetrainTypeSlug { get; init; }

    internal required string TrimLevelSlug { get; init; }

    /// <summary>Renk serbest metindir ve locale başına taşınır (architecture.md § 3).</summary>
    internal Dictionary<string, string> ColorNames { get; set; } = [];

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

    /// <summary>
    /// YALNIZCA açık ve public opsiyonel alanların değerleri. Kapalı veya Gizli alan
    /// buraya HİÇ yazılmaz — gizleme sorgu tarafında değil, yansıtma anında uygulanır
    /// (C4, Yasak Listesi 41). Yanıta girmeyen alan, okuma modelinde de yoktur.
    /// </summary>
    internal List<ShowcaseListingOptionalValue> OptionalValues { get; init; } = [];
}
