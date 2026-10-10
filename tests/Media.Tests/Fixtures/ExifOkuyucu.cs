using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace CarGallery.Media.Tests.Fixtures;

/// <summary>
/// AC-19'un bağımsız tanığı. Türevi üreten kütüphane (SkiaSharp) ile doğrulayan kütüphane
/// (MetadataExtractor) AYRI — aynı olsaydı, o kütüphanenin metadata yazıcısındaki bir hata
/// testi boş yere yeşil verirdi.
/// </summary>
internal static class ExifOkuyucu
{
    /// <summary>Bulunan konum ve cihaz etiketleri. Boş liste = temiz.</summary>
    internal static IReadOnlyList<string> KonumVeCihazEtiketleri(byte[] content)
    {
        List<string> bulunan = [];

        IReadOnlyList<MetadataExtractor.Directory> dizinler;

        try
        {
            using MemoryStream akis = new(content, writable: false);
            dizinler = ImageMetadataReader.ReadMetadata(akis);
        }
        catch (ImageProcessingException)
        {
            // Okuyucu formatı hiç tanımadıysa metadata da yoktur. Sessiz geçmek yerine
            // boş liste dönmek doğru: "etiket bulamadım" ile "dosyayı açamadım" aynı
            // sonucu verir ve testin iddiası birincisi.
            return bulunan;
        }

        foreach (MetadataExtractor.Directory dizin in dizinler)
        {
            bool gpsDizini = dizin is GpsDirectory;

            foreach (Tag etiket in dizin.Tags)
            {
                bool ilgili = gpsDizini
                    || etiket.Name is "Orientation" or "Make" or "Model"
                    || etiket.Name.Contains("GPS", StringComparison.Ordinal);

                if (ilgili)
                {
                    bulunan.Add($"{dizin.Name}/{etiket.Name}={etiket.Description}");
                }
            }
        }

        return bulunan;
    }
}
