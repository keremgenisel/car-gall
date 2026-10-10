using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Media.Application;

/// <summary>Kaynağın çözülmüş ölçüleri — yönlendirme UYGULANDIKTAN sonraki hâli.</summary>
internal sealed record ImageMeasurement(int Width, int Height);

/// <summary>Üretilmiş bir türevin baytları ve gerçekleşen boyutu.</summary>
internal sealed record RenderedDerivative(string VariantCode, int Width, int Height, byte[] Content);

/// <summary>
/// D7 — görüntü işleme portu. Kütüphane (SkiaSharp) `Infrastructure`'da kalır; `Domain` ve
/// `Application` onu hiç görmez.
///
/// <para><b>Neden port:</b> en yaygın alternatif ImageSharp v3'ten beri ticari kullanımda
/// ücretli lisans istiyor ve bu ürün white-label satılıyor. Lisans bedelini işleme hattının
/// içine gömmek, ürün satıldığı gün doğan bir yükümlülük olurdu — ve o gün kütüphaneyi
/// değiştirmek bütün hattı yeniden yazmak demekti.</para>
/// </summary>
internal interface IImageProcessor
{
    /// <summary>
    /// D10(c) — türev üretmeden ÖNCE ölçer. Piksel bütçesi ancak burada görülebilir:
    /// 2 MB'lık bir PNG gigapiksele açılabilir ve bayt sınırı bunu göremez.
    /// </summary>
    Result<ImageMeasurement> Measure(byte[] source);

    /// <summary>
    /// D8 — sıra: yönlendirmeyi piksele uygula → ölçekle → filigranı bas → metadata'sız
    /// WebP olarak encode et.
    ///
    /// <para>Yönlendirmeyi atlamak R5'i karşılayan ama KULLANILAMAZ bir çıktı üretirdi:
    /// dikey çekilmiş fotoğrafın dikeyliği piksellerde değil EXIF etiketindedir.</para>
    /// </summary>
    Result<IReadOnlyList<RenderedDerivative>> Render(byte[] source, byte[] watermark);
}
