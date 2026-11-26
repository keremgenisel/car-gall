using CarGallery.Modules.Media.Contracts.Assets;

namespace CarGallery.Modules.Media.Domain;

/// <summary>
/// Türev varyantı — <b>kodda tanımlı, sabit ve sürümlü</b> (`docs/architecture.md` § 4'ün
/// dördüncü kuralının aynı mantığı: liste kodda yaşar, kullanıcı yeni varyant tanımlayamaz).
///
/// <para>Veritabanında <b>kod</b> saklanır, çevrilmiş ad değil (Yasak Listesi 22).</para>
/// </summary>
internal sealed record MediaVariant(string Code, int TargetWidth)
{
    /// <summary>Vitrin detay ve galeri görüntüleyici için en büyük türev.</summary>
    internal static readonly MediaVariant Large = new("large", 1600);

    /// <summary>
    /// Gösterim türevi. <c>MediaAssetReady.Url</c> bunu taşır (D11): vitrin bugün tek bir
    /// kapak URL'i tutuyor ve kart genişlikleri bu boyutta keskin çıkıyor.
    /// </summary>
    internal static readonly MediaVariant Display = new("display", 800);

    /// <summary>Liste kartı ve şerit küçük resmi.</summary>
    internal static readonly MediaVariant Thumbnail = new("thumbnail", 400);

    /// <summary>
    /// Mockup'ın saydığı üç boyut (1600 / 800 / 400 WebP). Sıra büyükten küçüğe: işleme
    /// hattı en büyüğü bir kez ölçekleyip küçükleri ondan üretmez — her türev kaynaktan
    /// üretilir, çünkü zincirleme ölçekleme kaybı küçük türevde görünür hâle gelir.
    /// </summary>
    internal static readonly IReadOnlyList<MediaVariant> All = [Large, Display, Thumbnail];

    /// <summary>`0026` D1 — paylaşım önizlemesi ve <c>AutoDealer.logo</c>.</summary>
    internal static readonly MediaVariant LogoLarge = new("logo-large", 1200);

    /// <summary>`0026` D1 · D5 — üst çubuk ve altlık (160 CSS piksel × 2 yoğunluk).</summary>
    internal static readonly MediaVariant LogoSmall = new("logo-small", 320);

    /// <summary>`0026` D1 — logo türünün iki türevi; filigransız, saydam.</summary>
    internal static readonly IReadOnlyList<MediaVariant> Logo = [LogoLarge, LogoSmall];

    /// <summary>Türün türev kümesi.</summary>
    internal static IReadOnlyList<MediaVariant> For(MediaAssetKind kind) =>
        kind == MediaAssetKind.Logo ? Logo : All;

    internal static MediaVariant? Find(string code) =>
        All.Concat(Logo).FirstOrDefault(variant => variant.Code == code);
}
