using CarGallery.BuildingBlocks;
using CarGallery.Media.Tests.Fixtures;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using SkiaSharp;

namespace CarGallery.Media.Tests.Infrastructure;

/// <summary>
/// Türev üretiminin GERÇEK piksellerle sınandığı yer. Sahte bir işleyiciyle koşsaydı
/// AC-13…20'nin hiçbiri kanıtlanmazdı: hepsi çıktının kendisi hakkında.
/// </summary>
public sealed class GorselIslemeTests
{
    private readonly SkiaImageProcessor islemci = new();

    [Fact(DisplayName = "0011/AC-17: Hazır varlık 1600 / 800 / 400 piksel genişlikte üç WebP türev taşır")]
    public void Tasir_UcTurev_BeklenenGenisliklerde()
    {
        // Kaynak üç hedefin hepsinden geniş: büyütme kuralı devreye girmesin.
        byte[] kaynak = GorselFixture.DikeyJpeg(genislik: 2400, yukseklik: 1600);

        Result<IReadOnlyList<RenderedDerivative>> sonuc =
            islemci.Render(kaynak, GorselFixture.FiligranPng());

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(3, sonuc.Value.Count);
        Assert.Equal([1600, 800, 400], [.. sonuc.Value.Select(turev => turev.Width)]);

        foreach (RenderedDerivative turev in sonuc.Value)
        {
            // WebP sihirli baytı: "RIFF" + uzunluk + "WEBP".
            Assert.Equal(
                DetectedImageFormat.Webp,
                ImageFormatDetection.Detect(turev.Content.AsSpan(0, 32)));
        }
    }

    [Fact(DisplayName = "0011/AC-18: Kaynak görsel bir türev genişliğinden küçükse o türev kaynaktan büyütülmez")]
    public void Buyutulmez_Turev_KaynaktanGenisse()
    {
        byte[] kaynak = GorselFixture.DikeyJpeg(genislik: 600, yukseklik: 400);

        Result<IReadOnlyList<RenderedDerivative>> sonuc =
            islemci.Render(kaynak, GorselFixture.FiligranPng());

        Assert.True(sonuc.IsSuccess);

        // 1600 ve 800 hedefleri kaynaktan geniş: ikisi de 600'de kalır. 400 küçültülür.
        Assert.Equal([600, 600, 400], [.. sonuc.Value.Select(turev => turev.Width)]);
        Assert.DoesNotContain(sonuc.Value, turev => turev.Width > 600);
    }

    [Fact(DisplayName = "0011/AC-21: Üç türevin hepsinde filigran bulunur")]
    public void Bulunur_Filigran_UcTurevinHepsinde()
    {
        byte[] kaynak = GorselFixture.DikeyJpeg(genislik: 2400, yukseklik: 1600);

        Result<IReadOnlyList<RenderedDerivative>> filigranli =
            islemci.Render(kaynak, GorselFixture.FiligranPng());

        Assert.True(filigranli.IsSuccess);

        foreach (RenderedDerivative turev in filigranli.Value)
        {
            // Filigran sağ-alt köşeye basılıyor. Kaynak o bölgede koyu lacivert
            // (40,70,120); filigran yarı saydam beyaz. Köşenin parlaklığı kaynağın
            // parlaklığından BELİRGİN biçimde yüksek olmalı.
            using SKBitmap cozulen = SKBitmap.Decode(turev.Content);
            SKColor kose = Ortalama(cozulen, sagAlt: true);
            SKColor merkez = cozulen.GetPixel(cozulen.Width / 2, cozulen.Height / 2);

            Assert.True(
                Parlaklik(kose) > Parlaklik(merkez) + 40,
                $"{turev.VariantCode}: köşe {Parlaklik(kose):F0}, merkez {Parlaklik(merkez):F0} — "
                + "filigran basılmamış görünüyor.");
        }
    }

    [Fact(DisplayName = "0011/AC-24: Yüklenen görselin konum ve cihaz bilgisi türevlerde bulunmaz")]
    public void Bulunmaz_KonumVeCihazBilgisi_Turevlerde()
    {
        byte[] kaynak = GorselFixture.ExifliJpeg();

        // Testin boş yere yeşil vermediğinin kanıtı: GİRDİDE gerçekten var.
        IReadOnlyList<string> girdideki = ExifOkuyucu.KonumVeCihazEtiketleri(kaynak);
        Assert.Contains(girdideki, etiket => etiket.Contains("GPS", StringComparison.Ordinal));
        Assert.Contains(girdideki, etiket => etiket.Contains("Make=TestCihaz", StringComparison.Ordinal));

        Result<IReadOnlyList<RenderedDerivative>> sonuc =
            islemci.Render(kaynak, GorselFixture.FiligranPng());

        Assert.True(sonuc.IsSuccess);

        foreach (RenderedDerivative turev in sonuc.Value)
        {
            Assert.Empty(ExifOkuyucu.KonumVeCihazEtiketleri(turev.Content));
        }
    }

    [Fact(DisplayName = "0011/AC-25: EXIF yönlendirme etiketi taşıyan görselin türevi doğru yönde üretilir")]
    public void Uretilir_Turev_DogruYonde()
    {
        // Dosyada 600x900 (dikey) duruyor ama `Orientation=6` "90° saat yönünde döndür"
        // diyor; doğru çıktı YATAY olmalı: 900x600.
        byte[] kaynak = GorselFixture.ExifliJpeg(genislik: 600, yukseklik: 900, orientation: 6);

        Result<IReadOnlyList<RenderedDerivative>> sonuc =
            islemci.Render(kaynak, GorselFixture.FiligranPng());

        Assert.True(sonuc.IsSuccess);

        RenderedDerivative kucuk = sonuc.Value.Single(turev => turev.VariantCode == "thumbnail");

        Assert.True(
            kucuk.Width > kucuk.Height,
            $"Türev {kucuk.Width}x{kucuk.Height} — yönlendirme piksele uygulanmamış, görsel yan yatmış.");

        // Üst şeridi kırmızıydı; 90° saat yönünde dönünce SAĞ kenara geçmeli.
        using SKBitmap cozulen = SKBitmap.Decode(kucuk.Content);
        SKColor sag = cozulen.GetPixel(cozulen.Width - 3, cozulen.Height / 2);
        SKColor sol = cozulen.GetPixel(2, cozulen.Height / 2);

        Assert.True(sag.Red > sol.Red + 60, $"Kırmızı şerit sağda değil: sağ={sag}, sol={sol}");
    }

    /// <summary>
    /// `Measure` yönlendirmeden SONRAKİ ölçüyü vermeli: kaynak boyutu olarak saklanan
    /// değer dosyadaki ham boyut olsaydı, dikey bir fotoğraf kayıtta yatay görünürdü.
    /// </summary>
    [Fact]
    public void Verir_Olcu_YonlendirmedenSonra()
    {
        byte[] kaynak = GorselFixture.ExifliJpeg(genislik: 600, yukseklik: 900, orientation: 6);

        Result<ImageMeasurement> olcu = islemci.Measure(kaynak);

        Assert.True(olcu.IsSuccess);
        Assert.Equal(900, olcu.Value.Width);
        Assert.Equal(600, olcu.Value.Height);
    }

    [Fact]
    public void Dusmez_Islemci_BozukGovdede()
    {
        byte[] bozuk = [0xFF, 0xD8, 0xFF, 0x01, 0x02, 0x03];

        Result<ImageMeasurement> olcu = islemci.Measure(bozuk);

        Assert.True(olcu.IsFailure);
        Assert.Equal(MediaErrors.Undecodable, olcu.Error.Code);
    }

    private static SKColor Ortalama(SKBitmap bitmap, bool sagAlt)
    {
        int genislikPay = Math.Max(4, bitmap.Width / 12);
        int yukseklikPay = Math.Max(4, bitmap.Height / 12);

        int baslangicX = sagAlt ? bitmap.Width - genislikPay - 2 : 2;
        int baslangicY = sagAlt ? bitmap.Height - yukseklikPay - 2 : 2;

        long kirmizi = 0;
        long yesil = 0;
        long mavi = 0;
        int sayi = 0;

        for (int x = baslangicX; x < baslangicX + genislikPay && x < bitmap.Width; x++)
        {
            for (int y = baslangicY; y < baslangicY + yukseklikPay && y < bitmap.Height; y++)
            {
                SKColor piksel = bitmap.GetPixel(x, y);
                kirmizi += piksel.Red;
                yesil += piksel.Green;
                mavi += piksel.Blue;
                sayi++;
            }
        }

        return new SKColor((byte)(kirmizi / sayi), (byte)(yesil / sayi), (byte)(mavi / sayi));
    }

    private static double Parlaklik(SKColor color) =>
        (0.299 * color.Red) + (0.587 * color.Green) + (0.114 * color.Blue);
}
