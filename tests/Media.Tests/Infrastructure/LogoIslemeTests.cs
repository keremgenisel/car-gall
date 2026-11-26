using CarGallery.BuildingBlocks;
using CarGallery.Media.Tests.Fixtures;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using SkiaSharp;

namespace CarGallery.Media.Tests.Infrastructure;

/// <summary>
/// `0026` D1 — logo türünün türevleri GERÇEK piksellerle: filigran yok, saydamlık korunur, iki
/// WebP türev. Fotoğraf yolunun değişmediği de aynı yerde ölçülür (AC-6).
/// </summary>
public sealed class LogoIslemeTests
{
    private readonly SkiaImageProcessor islemci = new();

    [Fact(DisplayName = "0026/AC-1, AC-5: Logo türü tam olarak iki WebP türev üretir: `logo-large` ve `logo-small`.")]
    public void Uretir_IkiWebpTurev()
    {
        Result<IReadOnlyList<RenderedDerivative>> sonuc = islemci.RenderLogo(DuzRenkPng(2000, 800));

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(["logo-large", "logo-small"], sonuc.Value.Select(turev => turev.VariantCode));
        Assert.All(sonuc.Value, turev =>
            Assert.Equal(DetectedImageFormat.Webp, ImageFormatDetection.Detect(turev.Content.AsSpan(0, 32))));
    }

    [Theory(DisplayName = "0026/AC-2: 2000 px kaynaktan `logo-small` 320, `logo-large` 1200; 200 px kaynaktan ikisi de 200; oran korunur.")]
    [InlineData(2000, 800, 1200, 480, 320, 128)]
    [InlineData(200, 50, 200, 50, 200, 50)]
    public void Olcekler_OranKorunarak(int genislik, int yukseklik, int buyukG, int buyukY, int kucukG, int kucukY)
    {
        IReadOnlyList<RenderedDerivative> turevler = islemci.RenderLogo(DuzRenkPng(genislik, yukseklik)).Value;

        Assert.Equal((buyukG, buyukY), (turevler[0].Width, turevler[0].Height));
        Assert.Equal((kucukG, kucukY), (turevler[1].Width, turevler[1].Height));
    }

    [Fact(DisplayName = "0026/AC-3: Düz renkli opak kaynağın iki logo türevinde sağ-alt köşe ile merkez parlaklık farkı en çok 5 (filigran yok; `0011` AC-21'in ölçümü).")]
    public void Yok_Filigran()
    {
        foreach (RenderedDerivative turev in islemci.RenderLogo(DuzRenkPng(2000, 800)).Value)
        {
            using SKBitmap cozulen = SKBitmap.Decode(turev.Content);
            double fark = Math.Abs(Parlaklik(SagAltOrtalama(cozulen)) - Parlaklik(cozulen.GetPixel(cozulen.Width / 2, cozulen.Height / 2)));

            Assert.True(fark <= 5, $"{turev.VariantCode}: fark {fark:F1}");
        }

        // Ölçümün körlüğe düşmediğinin kanıtı: aynı kaynak fotoğraf yolunda filigranla > 40.
        RenderedDerivative fotograf = islemci.Render(DuzRenkPng(2000, 800), GorselFixture.FiligranPng()).Value[0];
        using SKBitmap foto = SKBitmap.Decode(fotograf.Content);
        Assert.True(Parlaklik(SagAltOrtalama(foto)) > Parlaklik(foto.GetPixel(foto.Width / 2, foto.Height / 2)) + 40);
    }

    [Fact(DisplayName = "0026/AC-4, AC-6: Kaynakta saydam köşe iki logo türevinde saydam; fotoğraf yolunda aynı köşe siyah ve üç türev.")]
    public void Korur_Saydamlik_LogodaYalniz()
    {
        byte[] kaynak = SaydamKoseliPng(1600, 400);

        foreach (RenderedDerivative turev in islemci.RenderLogo(kaynak).Value)
        {
            using SKBitmap cozulen = SKBitmap.Decode(turev.Content);
            Assert.Equal(0, cozulen.GetPixel(0, 0).Alpha);
            Assert.Equal(255, cozulen.GetPixel(cozulen.Width / 2, cozulen.Height / 2).Alpha);
        }

        IReadOnlyList<RenderedDerivative> fotograf = islemci.Render(kaynak, GorselFixture.FiligranPng()).Value;
        Assert.Equal(["large", "display", "thumbnail"], fotograf.Select(turev => turev.VariantCode));
        using SKBitmap foto = SKBitmap.Decode(fotograf[0].Content);
        SKColor kose = foto.GetPixel(0, 0);
        Assert.Equal((255, 0, 0, 0), ((int)kose.Alpha, (int)kose.Red, (int)kose.Green, (int)kose.Blue));
    }

    [Fact(DisplayName = "0026/AC-10: Konum bilgisi taşıyan JPEG'in logo türevlerinde EXIF konum bilgisi yoktur.")]
    public void Bulunmaz_KonumBilgisi()
    {
        byte[] kaynak = GorselFixture.ExifliJpeg();
        Assert.Contains(ExifOkuyucu.KonumVeCihazEtiketleri(kaynak), etiket => etiket.Contains("GPS", StringComparison.Ordinal));

        foreach (RenderedDerivative turev in islemci.RenderLogo(kaynak).Value)
        {
            Assert.DoesNotContain(ExifOkuyucu.KonumVeCihazEtiketleri(turev.Content), etiket => etiket.Contains("GPS", StringComparison.Ordinal));
        }
    }

    internal static byte[] DuzRenkPng(int genislik, int yukseklik)
    {
        using SKBitmap bitmap = new(genislik, yukseklik);
        bitmap.Erase(new SKColor(40, 70, 120));
        using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    /// <summary>Opak lacivert, sol-üst çeyreği tamamen saydam (alfa 0).</summary>
    internal static byte[] SaydamKoseliPng(int genislik, int yukseklik)
    {
        using SKBitmap bitmap = new(new SKImageInfo(genislik, yukseklik, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        bitmap.Erase(new SKColor(40, 70, 120));

        for (int x = 0; x < genislik / 4; x++)
        {
            for (int y = 0; y < yukseklik / 4; y++)
            {
                bitmap.SetPixel(x, y, SKColors.Transparent);
            }
        }

        using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return png.ToArray();
    }

    private static SKColor SagAltOrtalama(SKBitmap bitmap)
    {
        int genislikPay = Math.Max(4, bitmap.Width / 12);
        int yukseklikPay = Math.Max(4, bitmap.Height / 12);
        long r = 0, g = 0, b = 0;
        int sayi = 0;

        for (int x = bitmap.Width - genislikPay - 2; x < bitmap.Width - 2; x++)
        {
            for (int y = bitmap.Height - yukseklikPay - 2; y < bitmap.Height - 2; y++)
            {
                SKColor piksel = bitmap.GetPixel(x, y);
                r += piksel.Red;
                g += piksel.Green;
                b += piksel.Blue;
                sayi++;
            }
        }

        return new SKColor((byte)(r / sayi), (byte)(g / sayi), (byte)(b / sayi));
    }

    private static double Parlaklik(SKColor color) =>
        (0.299 * color.Red) + (0.587 * color.Green) + (0.114 * color.Blue);
}
