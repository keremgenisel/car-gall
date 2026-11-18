namespace CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

/// <summary>
/// `0016b` D25 — ilanın galerisinin bir yuvası: aracın medya sırasındaki konumu, varlığın
/// durumu ve hazırsa türevlerinin adresi ile GERÇEK boyutları (D18). Galeri detay
/// isteğinde `Media`'ya gitmez; bu tabloyu yansıtıcı ve medya olay işleyicileri doldurur.
/// </summary>
internal sealed class ShowcaseListingMedia
{
    /// <summary>Yuvanın ait olduğu vitrin satırı; satır silinince yuvalar da silinir.</summary>
    internal required Guid ListingId { get; init; }

    /// <summary>Aracın medya sırasındaki konum (0'dan). Galeri bu sırayla gezilir (R1).</summary>
    internal required int Position { get; init; }

    /// <summary>Yuvadaki varlık. Sıra değişince aynı konuma başka varlık yazılabilir.</summary>
    internal required Guid MediaAssetId { get; set; }

    /// <summary>Varlığın durumu; galeri yalnız <see cref="ShowcaseMediaStatus.Ready"/> yuvaları gösterir (D14).</summary>
    internal required ShowcaseMediaStatus Status { get; set; }

    /// <summary>Hazır varlığın türevleri; hazır olmayanda boştur. Orijinal dosya burada yoktur (C7).</summary>
    internal List<ShowcaseMediaVariant> Variants { get; set; } = [];
}

/// <summary>Yuvadaki varlığın vitrin açısından durumu (`0016b` D14).</summary>
internal enum ShowcaseMediaStatus
{
    /// <summary>Yüklendi ya da işleniyor; galeride yer almaz.</summary>
    Processing = 1,

    /// <summary>Türevleri hazır; galeride yer alır.</summary>
    Ready = 2,

    /// <summary>İşleme başarısız; galeride yer almaz.</summary>
    Failed = 3,
}

/// <summary>Bir türevin adresi ve gerçek boyutu (`0016b` D18 — `1600` değil, üretilen boyut).</summary>
internal sealed record ShowcaseMediaVariant(string Variant, string Url, int Width, int Height);
