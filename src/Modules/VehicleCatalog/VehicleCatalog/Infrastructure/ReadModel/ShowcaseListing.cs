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
    /*
      0012b D12 — satır artık YAZILMAKLA kalmıyor, GÜNCELLENİYOR. Bu spec'e kadar okuma
      modeline yazan tek yol geliştirme seed'iydi ve her satır bir kez kuruluyordu; araçla
      birlikte değişebilen alanlar bu yüzden `init` yazılmıştı.

      `init` kalan ikisi gerçekten değişmez: ilanın kimliği ve ADRESİ (D13 — ilk yayında
      üretilir ve bir daha değişmez). Aracın kimliği de öyle: satır ilanın satırıdır ve
      ilan bir araca aittir.
    */
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

    internal required string BodyTypeSlug { get; set; }

    internal required string FuelTypeSlug { get; set; }

    internal required string TransmissionTypeSlug { get; set; }

    internal required string DrivetrainTypeSlug { get; set; }

    internal required string TrimLevelSlug { get; set; }

    /// <summary>Renk serbest metindir ve locale başına taşınır (architecture.md § 3).</summary>
    internal Dictionary<string, string> ColorNames { get; set; } = [];

    internal required Guid VehicleModelId { get; set; }

    internal required int ModelYear { get; set; }

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
    internal List<ShowcaseListingOptionalValue> OptionalValues { get; set; } = [];
}
