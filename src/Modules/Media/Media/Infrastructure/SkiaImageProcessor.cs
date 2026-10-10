using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Domain;
using SkiaSharp;

namespace CarGallery.Modules.Media.Infrastructure;

/// <summary>
/// D7/D8 — görüntü işlemenin tek uygulaması. Kütüphane (SkiaSharp, MIT) yalnız burada
/// görünür; `Application` ve `Domain` onu hiç bilmez.
///
/// <para><b>Sıra pazarlığa kapalı:</b> yönlendirmeyi piksele uygula → ölçekle → filigranı
/// bas → metadata'sız WebP. Ölçüldü: <c>SKBitmap.Decode</c> EXIF yönlendirmesini
/// UYGULAMAZ — 600×900 dikey bir fotoğraf 600×900 olarak çözülüyor ve etiket atıldığında
/// yan yatmış kalıyor. Yani yalnız "EXIF'i temizle" yazan bir hat R5'i karşılar ve
/// kullanılamaz çıktı üretir.</para>
///
/// <para><b>WebP encode metadata taşımıyor</b> — ölçüldü: bağımsız bir okuyucu
/// (MetadataExtractor) üç türevin hiçbirinde EXIF ya da GPS bulamıyor. R5 bu yüzden ayrı
/// bir "strip" adımı gerektirmiyor; gereken şey onun TESTLE kanıtlanması.</para>
/// </summary>
internal sealed class SkiaImageProcessor : IImageProcessor
{
    /// <summary>
    /// Filigran genişliği = türev genişliği / bu bölen. Sabit piksel genişliği
    /// kullanılamaz: 400 px'lik bir türevde 240 px'lik logo görseli yutar, 1600 px'likte
    /// görünmez kalır.
    ///
    /// <para><b>Neden oran değil bölen:</b> Yasak Listesi 26 `double`/`float` bildirimini
    /// istisnasız yasaklıyor ve mimari test onu tarıyor. Kuralın amacı para ve yüzdedir
    /// ama dayatması blanket — ve haklı: `0.22f` ile `0.22m` arasındaki farkı gözle ayırt
    /// etmek mümkün değil, dayatmayı gevşetmek o farkı geri getirirdi. Raster geometrisi
    /// zaten tam sayı piksel sınırlarında daha doğru çalışıyor.</para>
    /// </summary>
    private const int WatermarkWidthDivisor = 5;

    /// <summary>Kenar boşluğu da türev genişliğine bağlı; sabit 8 px, 400'lükte 1600'lükten dört kat büyük durur.</summary>
    private const int MarginDivisor = 50;

    private const int WebpQuality = 82;

    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

    public Result<ImageMeasurement> Measure(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using MemoryStream akis = new(source, writable: false);
        using SKCodec? kodek = SKCodec.Create(akis);

        if (kodek is null)
        {
            return Result.Failure<ImageMeasurement>(new Error(MediaErrors.Undecodable));
        }

        // Ölçü YÖNLENDİRMEDEN SONRAKİ hâli: devrik bir fotoğrafta genişlik ve yükseklik
        // yer değiştirir ve piksel bütçesi ikisinin ÇARPIMI olduğu için sonuç aynı kalır —
        // ama çağıran bu ölçüyü kaynak boyutu olarak saklıyor ve orada yanlış olurdu.
        bool devrik = IsTransposed(kodek.EncodedOrigin);

        return Result.Success(new ImageMeasurement(
            devrik ? kodek.Info.Height : kodek.Info.Width,
            devrik ? kodek.Info.Width : kodek.Info.Height));
    }

    public Result<IReadOnlyList<RenderedDerivative>> Render(byte[] source, byte[] watermark)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(watermark);

        using MemoryStream akis = new(source, writable: false);
        using SKCodec? kodek = SKCodec.Create(akis);

        if (kodek is null)
        {
            return Result.Failure<IReadOnlyList<RenderedDerivative>>(new Error(MediaErrors.Undecodable));
        }

        using SKBitmap? cozulen = SKBitmap.Decode(kodek);

        if (cozulen is null)
        {
            return Result.Failure<IReadOnlyList<RenderedDerivative>>(new Error(MediaErrors.Undecodable));
        }

        using SKBitmap? filigran = SKBitmap.Decode(watermark);

        if (filigran is null)
        {
            return Result.Failure<IReadOnlyList<RenderedDerivative>>(new Error(MediaErrors.WatermarkUnavailable));
        }

        using SKBitmap yonlendirilmis = ApplyOrientation(cozulen, kodek.EncodedOrigin);

        List<RenderedDerivative> uretilen = [];

        foreach (MediaVariant variant in MediaVariant.All)
        {
            // AC-14 — kaynak hedeften küçükse BÜYÜTÜLMEZ. Büyütmek dosyayı büyütür,
            // görüntüyü iyileştirmez ve vitrine bulanık bir kart koyar.
            int genislik = Math.Min(variant.TargetWidth, yonlendirilmis.Width);
            int yukseklik = Math.Max(
                1,
                (int)Math.Round(yonlendirilmis.Height * (genislik / (double)yonlendirilmis.Width)));

            using SKBitmap turev = new(genislik, yukseklik);

            using (SKCanvas tuval = new(turev))
            {
                tuval.Clear(SKColors.Black);
                tuval.DrawBitmap(yonlendirilmis, new SKRect(0, 0, genislik, yukseklik), Sampling);
                DrawWatermark(tuval, filigran, genislik, yukseklik);
            }

            using SKData webp = turev.Encode(SKEncodedImageFormat.Webp, WebpQuality);

            if (webp is null || webp.Size == 0)
            {
                return Result.Failure<IReadOnlyList<RenderedDerivative>>(new Error(MediaErrors.Undecodable));
            }

            uretilen.Add(new RenderedDerivative(variant.Code, genislik, yukseklik, webp.ToArray()));
        }

        return Result.Success<IReadOnlyList<RenderedDerivative>>(uretilen);
    }

    /// <summary>R4/AC-16 — filigran ÜÇ türevin hepsine basılır, yalnız büyüklere değil.</summary>
    private static void DrawWatermark(SKCanvas canvas, SKBitmap watermark, int width, int height)
    {
        int hedefGenislik = Math.Max(1, width / WatermarkWidthDivisor);
        int hedefYukseklik = Math.Max(1, hedefGenislik * watermark.Height / watermark.Width);
        int bosluk = Math.Max(1, width / MarginDivisor);

        // Filigran türevden taşmamalı: 400 px'lik bir türevde bile oran taşma üretmiyor,
        // ama kaynak aşırı dar olduğunda (AC-14 gereği türev kaynak genişliğinde kalır)
        // yükseklik taşabilir ve `DrawBitmap` sessizce kırpar.
        int sol = Math.Max(0, width - hedefGenislik - bosluk);
        int ust = Math.Max(0, height - hedefYukseklik - bosluk);

        canvas.DrawBitmap(
            watermark,
            new SKRect(sol, ust, width - bosluk, height - bosluk),
            Sampling);
    }

    /// <summary>
    /// D8 — EXIF yönlendirmesini PİKSELE uygular. Sekiz `SKEncodedOrigin` değerinin hepsi
    /// ele alınıyor; eksik bırakılan bir değer, o yönle çekilmiş fotoğrafın sessizce yan
    /// yatması demektir ve hangi telefonun hangi değeri yazdığı önceden bilinemez.
    /// </summary>
    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin == SKEncodedOrigin.TopLeft)
        {
            return source.Copy();
        }

        bool devrik = IsTransposed(origin);
        int genislik = devrik ? source.Height : source.Width;
        int yukseklik = devrik ? source.Width : source.Height;

        SKBitmap hedef = new(genislik, yukseklik);

        using SKCanvas tuval = new(hedef);
        tuval.Clear(SKColors.Transparent);
        tuval.SetMatrix(MatrixFor(origin, genislik, yukseklik));
        tuval.DrawBitmap(source, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));

        return hedef;
    }

    private static bool IsTransposed(SKEncodedOrigin origin) =>
        origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

    private static SKMatrix MatrixFor(SKEncodedOrigin origin, int width, int height) => origin switch
    {
        SKEncodedOrigin.TopRight => SKMatrix.CreateScale(-1, 1)
            .PostConcat(SKMatrix.CreateTranslation(width, 0)),
        SKEncodedOrigin.BottomRight => SKMatrix.CreateScale(-1, -1)
            .PostConcat(SKMatrix.CreateTranslation(width, height)),
        SKEncodedOrigin.BottomLeft => SKMatrix.CreateScale(1, -1)
            .PostConcat(SKMatrix.CreateTranslation(0, height)),
        SKEncodedOrigin.LeftTop => SKMatrix.CreateRotationDegrees(90)
            .PostConcat(SKMatrix.CreateScale(1, -1)),
        SKEncodedOrigin.RightTop => SKMatrix.CreateRotationDegrees(90)
            .PostConcat(SKMatrix.CreateTranslation(width, 0)),
        SKEncodedOrigin.RightBottom => SKMatrix.CreateRotationDegrees(270)
            .PostConcat(SKMatrix.CreateScale(-1, 1))
            .PostConcat(SKMatrix.CreateTranslation(width, 0)),
        SKEncodedOrigin.LeftBottom => SKMatrix.CreateRotationDegrees(270)
            .PostConcat(SKMatrix.CreateTranslation(0, height)),
        _ => SKMatrix.Identity,
    };
}
