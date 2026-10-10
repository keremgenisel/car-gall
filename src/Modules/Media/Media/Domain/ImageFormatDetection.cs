namespace CarGallery.Modules.Media.Domain;

/// <summary>
/// D10(a) — tür dosyanın İÇERİĞİNDEN belirlenir. Bildirilen <c>Content-Type</c> ve dosya
/// uzantısı istemcinin denetimindedir; ikisi de doğrulama değil temennidir.
///
/// <para><b>Kabul kümesi KODDA sabittir, yapılandırmada değil</b> — ve bu bilinçli bir
/// daraltma: yapılandırılabilir bir izin listesi, bir gün `image/svg+xml` eklenmesine izin
/// verirdi. SVG aynı origin'de script çalıştırır; bu depo o tuzağa `Program.cs`'in seed
/// ucunda bir kez bakıp yüzeyi kapatmış. Bir yapılandırma satırıyla geri açılabilen bir
/// güvenlik kararı, verilmemiş bir karardır.</para>
/// </summary>
internal enum DetectedImageFormat
{
    Unknown = 0,
    Jpeg = 1,
    Png = 2,
    Webp = 3,

    /// <summary>
    /// AYRI bir değer ve bu kasıtlı: SVG'yi <c>Unknown</c> saymak onu "tanınmayan dosya"
    /// diye reddederdi ve neden reddedildiği logdan okunamazdı. Tanınıyor ve
    /// <b>reddediliyor</b>.
    /// </summary>
    Svg = 4,
}

internal static class ImageFormatDetection
{
    /// <summary>Sihirli baytların hepsini görmek için yeterli ön ek.</summary>
    internal const int SniffLength = 64;

    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] RiffMagic = [0x52, 0x49, 0x46, 0x46]; // "RIFF"
    private static readonly byte[] WebpMagic = [0x57, 0x45, 0x42, 0x50]; // "WEBP"

    internal static DetectedImageFormat Detect(ReadOnlySpan<byte> prefix)
    {
        if (StartsWith(prefix, JpegMagic))
        {
            return DetectedImageFormat.Jpeg;
        }

        if (StartsWith(prefix, PngMagic))
        {
            return DetectedImageFormat.Png;
        }

        // WebP: "RIFF" + 4 bayt uzunluk + "WEBP". Uzunluğu atlamadan bakmak, RIFF taşıyan
        // her sesi (WAV) WebP sanmak demekti.
        if (StartsWith(prefix, RiffMagic) && prefix.Length >= 12
            && StartsWith(prefix[8..], WebpMagic))
        {
            return DetectedImageFormat.Webp;
        }

        return LooksLikeSvg(prefix) ? DetectedImageFormat.Svg : DetectedImageFormat.Unknown;
    }

    /// <summary>Kabul edilen türler. SVG bilinçli olarak dışarıda (yukarıdaki not).</summary>
    internal static bool IsAccepted(DetectedImageFormat format) =>
        format is DetectedImageFormat.Jpeg or DetectedImageFormat.Png or DetectedImageFormat.Webp;

    internal static string Extension(DetectedImageFormat format) => format switch
    {
        DetectedImageFormat.Jpeg => ".jpg",
        DetectedImageFormat.Png => ".png",
        DetectedImageFormat.Webp => ".webp",
        _ => ".bin",
    };

    internal static string ContentType(DetectedImageFormat format) => format switch
    {
        DetectedImageFormat.Jpeg => "image/jpeg",
        DetectedImageFormat.Png => "image/png",
        DetectedImageFormat.Webp => "image/webp",
        _ => "application/octet-stream",
    };

    private static bool StartsWith(ReadOnlySpan<byte> value, ReadOnlySpan<byte> magic) =>
        value.Length >= magic.Length && value[..magic.Length].SequenceEqual(magic);

    /// <summary>
    /// SVG metindir ve sihirli baytı yoktur: dosya BOM, boşluk, XML bildirimi veya yorumla
    /// başlayabilir. Bu yüzden ön ek küçük harfe indirilip içinde <c>&lt;svg</c> ya da
    /// <c>&lt;?xml</c> aranıyor — tam bir ayrıştırıcı değil, bir REDDETME sezgisi.
    /// Yanlış pozitifin bedeli bir görselin reddedilmesi; yanlış negatifin bedeli aynı
    /// origin'de script çalışması.
    /// </summary>
    private static bool LooksLikeSvg(ReadOnlySpan<byte> prefix)
    {
        Span<char> karakterler = stackalloc char[prefix.Length];

        for (int i = 0; i < prefix.Length; i++)
        {
            karakterler[i] = char.ToLowerInvariant((char)prefix[i]);
        }

        ReadOnlySpan<char> metin = karakterler;

        return metin.Contains("<svg", StringComparison.Ordinal)
            || metin.Contains("<?xml", StringComparison.Ordinal);
    }
}
