using CarGallery.Modules.Media.Domain;

namespace CarGallery.Modules.Media.Application;

/// <summary>
/// D12/AC-29 — erişim yolu yapılandırılan taban adresten üretilir; kodda hiçbir alan adı
/// yoktur. C3 CDN'i ilk günden istiyor ve CDN kodda tam olarak bu tek değerdir.
/// </summary>
internal sealed class MediaUrlResolver(MediaOptions options)
{
    /// <summary>
    /// <b>Yalnız public önekli anahtar çözülür.</b> D16 gereği orijinal `private/`
    /// önekindedir ve ona bir URL üretmek, ham EXIF taşıyan dosyayı yayınlamak demekti —
    /// yani R5'in kapattığı deliği arka kapıdan açmak. Burada sessizce geçmek yerine
    /// patlıyor: sessiz bir <c>null</c>, çağıranın onu bir yere yazmasına izin verirdi.
    /// </summary>
    internal Uri Resolve(string objectKey)
    {
        if (!MediaObjectKeys.IsPubliclyReachable(objectKey))
        {
            throw new InvalidOperationException(
                $"Public olmayan anahtar için erişim yolu üretilemez: '{objectKey}' (D16).");
        }

        return new Uri(options.PublicBaseUrl, objectKey);
    }
}
