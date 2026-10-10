using System.Buffers.Binary;
using SkiaSharp;

namespace CarGallery.Media.Tests.Fixtures;

/// <summary>
/// Test görselleri. <b>Depoda ikili dosya tutulmuyor</b> ve bunun iki nedeni var:
/// (1) EXIF taşıyan bir JPEG'in içinde ne olduğu dosyaya bakılarak anlaşılmaz, kodda
/// yazılınca anlaşılır; (2) `docs/testing.md` § 4 "test edilen alan AÇIKÇA yazılır" diyor.
///
/// <para>EXIF bloğu elle kuruluyor çünkü yazan bir kütüphane eklemek, çıktısını aynı
/// kütüphaneyle doğrulamak kadar kör bir döngü üretirdi. Burada YAZAN bu dosya, OKUYAN
/// `MetadataExtractor` — ikisi bağımsız.</para>
/// </summary>
internal static class GorselFixture
{
    /// <summary>Üst şeridi kırmızı: yönlendirme uygulanıp uygulanmadığı buradan okunur.</summary>
    internal static byte[] DikeyJpeg(int genislik = 600, int yukseklik = 900) =>
        Jpeg(genislik, yukseklik);

    /// <summary>
    /// Konum (GPS) ve cihaz (<c>Make</c>) bilgisi taşıyan JPEG. İkisi de R5'in adıyla
    /// saydığı şeyler ve AC-19 ikisinin de türevde bulunmadığını istiyor.
    /// </summary>
    /// <param name="orientation">
    /// EXIF <c>Orientation</c> değeri. <c>6</c> = "sağ kenar üstte", yani 90° saat yönünde
    /// döndürülmeli — telefonların dikey çektiğinde yazdığı en yaygın değer.
    /// </param>
    internal static byte[] ExifliJpeg(int genislik = 600, int yukseklik = 900, ushort orientation = 6)
    {
        byte[] govde = Jpeg(genislik, yukseklik);
        byte[] tiff = TiffBlogu(orientation);

        List<byte> sonuc = [0xFF, 0xD8, 0xFF, 0xE1];
        ushort uzunluk = (ushort)(2 + 6 + tiff.Length);
        sonuc.Add((byte)(uzunluk >> 8));
        sonuc.Add((byte)(uzunluk & 0xFF));
        sonuc.AddRange("Exif\0\0"u8);
        sonuc.AddRange(tiff);

        // SkiaSharp'ın ürettiği JPEG `FFD8` ile başlıyor; APP1 bloğu onun HEMEN ARDINA
        // giriyor, bu yüzden ilk iki bayt atlanıyor.
        sonuc.AddRange(govde.Skip(2));

        return [.. sonuc];
    }

    /// <summary>Filigran: yarı saydam beyaz şerit. Türevde varlığı piksel farkıyla ölçülüyor.</summary>
    internal static byte[] FiligranPng(int genislik = 240, int yukseklik = 80)
    {
        using SKBitmap bitmap = new(genislik, yukseklik);

        using (SKCanvas tuval = new(bitmap))
        {
            tuval.Clear(new SKColor(255, 255, 255, 220));
        }

        using SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);

        return png.ToArray();
    }

    /// <summary>D10(a)/AC-4 — `image/jpeg` adıyla gönderilse de içeriği SVG.</summary>
    internal static byte[] SvgGovdesi() =>
        """<?xml version="1.0"?><svg xmlns="http://www.w3.org/2000/svg" width="10" height="10"></svg>"""u8
            .ToArray();

    private static byte[] Jpeg(int genislik, int yukseklik)
    {
        using SKBitmap bitmap = new(genislik, yukseklik);

        using (SKCanvas tuval = new(bitmap))
        {
            tuval.Clear(new SKColor(40, 70, 120));
            tuval.DrawRect(
                new SKRect(0, 0, genislik, yukseklik / 8f),
                new SKPaint { Color = SKColors.Red });
        }

        using SKData jpeg = bitmap.Encode(SKEncodedImageFormat.Jpeg, 92);

        return jpeg.ToArray();
    }

    /// <summary>
    /// Elle kurulmuş TIFF bloğu: IFD0'da <c>Make</c> + <c>Orientation</c> + GPS IFD
    /// göstergesi; GPS IFD'de enlem/boylam. Offset'ler TIFF başlığının BAŞINA göredir.
    /// </summary>
    private static byte[] TiffBlogu(ushort orientation)
    {
        byte[] tiff = new byte[164];

        void U16(int at, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(tiff.AsSpan(at), value);
        void U32(int at, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(tiff.AsSpan(at), value);

        void Giris(int at, ushort tag, ushort type, uint count, uint value)
        {
            U16(at, tag);
            U16(at + 2, type);
            U32(at + 4, count);
            U32(at + 8, value);
        }

        tiff[0] = (byte)'I';
        tiff[1] = (byte)'I';
        U16(2, 42);
        U32(4, 8);

        U16(8, 3);
        Giris(10, 0x010F, 2, 11, 50);          // Make -> ASCII, offset 50
        Giris(22, 0x0112, 3, 1, orientation);  // Orientation
        Giris(34, 0x8825, 4, 1, 62);           // GPSInfo IFD -> offset 62
        U32(46, 0);

        "TestCihaz\0"u8.ToArray().CopyTo(tiff, 50);

        U16(62, 4);
        Giris(64, 0x0001, 2, 2, 'N');          // GPSLatitudeRef
        Giris(76, 0x0002, 5, 3, 116);          // GPSLatitude  -> üç rational
        Giris(88, 0x0003, 2, 2, 'E');          // GPSLongitudeRef
        Giris(100, 0x0004, 5, 3, 140);         // GPSLongitude -> üç rational
        U32(112, 0);

        (uint Pay, uint Payda)[] enlem = [(41, 1), (1, 1), (0, 1)];
        (uint Pay, uint Payda)[] boylam = [(29, 1), (2, 1), (0, 1)];

        for (int i = 0; i < 3; i++)
        {
            U32(116 + (i * 8), enlem[i].Pay);
            U32(120 + (i * 8), enlem[i].Payda);
            U32(140 + (i * 8), boylam[i].Pay);
            U32(144 + (i * 8), boylam[i].Payda);
        }

        return tiff;
    }
}
