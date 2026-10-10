using Microsoft.AspNetCore.Hosting;

namespace CarGallery.Host.Tests;

/// <summary>
/// 0011/C6 gereği uygulama depolama kimlik bilgileri, erişim taban adresi ve filigran
/// kaynağı olmadan BAŞLAMAZ (AC-31…33). `0006`'nın `IdentityTestSettings`'i ve `0009`'un
/// `NotificationsTestSettings`'i ile aynı şey: <b>başlangıç sözleşmesi değiştiği için her
/// test fabrikası bu ayarları da vermek zorunda.</b>
///
/// <para>Burada gerçek bir depolama sağlayıcısı YOK ve olması da gerekmiyor: bu testler
/// uygulamanın AYAĞA KALKMASINI sınıyor, dosya yüklemeyi değil. Yüklemenin ve türev
/// üretiminin gerçek bir S3 uyumlu sağlayıcıya karşı kanıtı `Media.Tests` içindeki
/// entegrasyon testlerindedir.</para>
///
/// <para><b>Sırlar appsettings'te DEĞİL burada:</b> `appsettings.json` bilinçli olarak
/// `AccessKey` ve `SecretKey` taşımıyor (`docs/conventions.md` § 11) — bu yüzden onları
/// vermeyen her test fabrikası, uygulamayı doğru sebeple ayağa kaldıramaz.</para>
/// </summary>
internal static class MediaTestSettings
{
    internal static void Apply(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("Media:Storage:ServiceUrl", "http://localhost:54566");
        builder.UseSetting("Media:Storage:Bucket", "cargallery-media-test");
        builder.UseSetting("Media:Storage:AccessKey", "test");
        builder.UseSetting("Media:Storage:SecretKey", "test");
        builder.UseSetting("Media:Storage:Region", "us-east-1");
        builder.UseSetting("Media:PublicBaseUrl", "http://localhost:54566/cargallery-media-test/");
        builder.UseSetting("Media:WatermarkObjectKey", "private/brand/watermark.png");

        // AC-9 — tür kümesi de yapılandırmadan geliyor ve boş küme kuruluşta reddediliyor;
        // bu satır olmadan HER Host testi "media.allowed_formats_missing" ile düşer.
        builder.UseSetting("Media:AllowedImageFormats:0", "jpeg");
        builder.UseSetting("Media:AllowedImageFormats:1", "png");
        builder.UseSetting("Media:AllowedImageFormats:2", "webp");
    }
}
