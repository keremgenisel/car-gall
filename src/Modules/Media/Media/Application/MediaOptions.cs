using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Setup;
using CarGallery.Modules.Media.Domain;

namespace CarGallery.Modules.Media.Application;

/// <summary>
/// Doğrulanmış yapılandırma (C6). Doğrulama KURULUŞTA yapılır ve eksik değerde uygulama
/// AYAĞA KALKMAZ (AC-31…33) — çalışma anında patlamaz.
///
/// <para>Gerekçe `0009`'un aynısı ve bu modülde daha keskin: filigran kaynağı
/// yapılandırılmamışsa her fotoğraf filigransız çıkar ve galeri sahibinin satın aldığı tek
/// garanti sessizce kaybolur. Sessizce. Fotoğraflar rakip sitelerde göründüğünde fark
/// edilir ve o noktada bütün türevlerin yeniden üretilmesi gerekir.</para>
/// </summary>
internal sealed class MediaOptions
{
    internal required Uri StorageServiceUrl { get; init; }

    internal required string StorageBucket { get; init; }

    internal required string StorageAccessKey { get; init; }

    internal required string StorageSecretKey { get; init; }

    internal required string StorageRegion { get; init; }

    internal required bool StorageForcePathStyle { get; init; }

    /// <summary>
    /// D12 — türevin erişim yolu bundan üretilir. MUTLAK olmalı: göreli bir taban adres
    /// vitrinde kırık görsel demektir ve hata sunucu loglarında hiç görünmez.
    /// </summary>
    internal required Uri PublicBaseUrl { get; init; }

    internal required string WatermarkObjectKey { get; init; }

    internal required long MaximumByteSize { get; init; }

    internal required long MaximumPixelCount { get; init; }

    /// <summary>
    /// AC-9 — kabul edilen tür kümesi yapılandırmadan. AC-5/D23 — küme yalnız
    /// <see cref="ImageFormatDetection.ConfigurableFormats"/> arasından seçilebilir; `svg`
    /// ya da tanınmayan bir değer kuruluşta reddedilir.
    /// </summary>
    internal required IReadOnlySet<DetectedImageFormat> AllowedFormats { get; init; }

    /// <summary>
    /// "Mutlak mı" DEĞİL, <b>çağrılabilir mi</b>. Bu ayrım ölçülerek öğrenildi: Unix'te
    /// <c>Uri.TryCreate("/goreli/yol", UriKind.Absolute, out _)</c> <b>true</b> döner ve
    /// <c>file:///goreli/yol</c> üretir. Yani yalnız mutlaklığa bakan bir kontrol, göreli
    /// yazılmış bir taban adresi kabul eder ve vitrindeki her görselin yolu
    /// <c>file://</c> olur — hata sunucu loglarında hiç görünmez, yalnız ziyaretçinin
    /// ekranında kırık bir görsel olarak görünür.
    ///
    /// <para>`0009`'un `PublicBaseUrl` kararı aynı sınıftan bir karardı ama şemayı
    /// denetlemiyordu; burada denetleniyor.</para>
    /// </summary>
    private static bool IsReachableUrl(string value, out Uri? parsed)
    {
        parsed = null;

        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? aday))
        {
            return false;
        }

        if (aday.Scheme != Uri.UriSchemeHttp && aday.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        parsed = aday;

        return true;
    }

    /// <summary>
    /// Taban adresi sondaki eğik çizgiyle tamamlar.
    ///
    /// <para><b>Bir bulgunun bedeli.</b> Erişim yolu <c>new Uri(taban, anahtar)</c> ile
    /// göreli çözülüyor ve <c>Uri</c>'nin kuralı gereği sondaki eğik çizgi olmadan taban
    /// adresin <b>son yol segmenti düşüyor</b>. Ölçüldü:
    /// <c>https://cdn.ornek.test/kova/</c> →
    /// <c>…/kova/public/assets/…/display.webp</c>, ama
    /// <c>https://cdn.ornek.test/kova</c> → <c>…/public/assets/…/display.webp</c> —
    /// <c>/kova</c> yok. Uygulama ayağa kalkıyor, log temiz, vitrindeki her görsel 404.
    /// Yapılandırmanın dört yerindeki (appsettings, ci.yml, perf.yml, dev-up.sh) değerler
    /// yol segmenti taşıyor; tek karakter eksilterek tetiklenebilir bir kurulum tuzağıydı.</para>
    ///
    /// <para><b>Reddetmek yerine normalize ediliyor</b> ve bu bir tercih: C6'nın deseni
    /// EKSİK yapılandırmayı reddetmek içindir, burada eksik bir şey yok — niyet
    /// tartışmasız ve biçim kusurlu. Eksik bir eğik çizgi için kurulumu reddetmek
    /// sürtünme ekler, güvenlik kazandırmaz. Şema denetimi (aşağıda) ayrı bir konu:
    /// orada niyet GERÇEKTEN belirsizdir.</para>
    /// </summary>
    private static string NormalizeBaseUrl(string value) =>
        string.IsNullOrWhiteSpace(value) || value.EndsWith('/') ? value : value + "/";

    internal static Result<MediaOptions> Create(MediaSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        if (!IsReachableUrl(setup.StorageServiceUrl, out Uri? serviceUrl))
        {
            return Result.Failure<MediaOptions>(new Error("media.storage_url_missing"));
        }

        if (string.IsNullOrWhiteSpace(setup.StorageBucket))
        {
            return Result.Failure<MediaOptions>(new Error("media.storage_bucket_missing"));
        }

        // AC-31 — sağlayıcı kimlik bilgileri eksikse uygulama ayağa kalkmaz. Anonim erişime
        // izin veren bir kova bu kriterin kapsamı DIŞINDADIR ve bilinçli: üretimde anonim
        // yazılabilir bir medya kovası, herkesin galerinin vitrinine dosya koyabilmesi demek.
        if (string.IsNullOrWhiteSpace(setup.StorageAccessKey)
            || string.IsNullOrWhiteSpace(setup.StorageSecretKey))
        {
            return Result.Failure<MediaOptions>(new Error("media.storage_credentials_missing"));
        }

        if (string.IsNullOrWhiteSpace(setup.StorageRegion))
        {
            return Result.Failure<MediaOptions>(new Error("media.storage_region_missing"));
        }

        if (!IsReachableUrl(NormalizeBaseUrl(setup.PublicBaseUrl), out Uri? publicBaseUrl))
        {
            return Result.Failure<MediaOptions>(new Error("media.public_base_url_missing"));
        }

        if (string.IsNullOrWhiteSpace(setup.WatermarkObjectKey))
        {
            return Result.Failure<MediaOptions>(new Error("media.watermark_missing"));
        }

        if (setup.MaximumByteSize < 1 || setup.MaximumPixelCount < 1)
        {
            return Result.Failure<MediaOptions>(new Error("media.invalid_upload_limits"));
        }

        // AC-9 / AC-5 — kabul edilen tür kümesi yapılandırmadan gelir ve küme `svg`
        // içeremez. Boş küme de reddedilir: hiçbir şeyin kabul edilmediği bir yükleme ucu,
        // sessizce kapatılmış bir yüzeydir.
        Result<IReadOnlySet<DetectedImageFormat>> formats =
            ParseAllowedFormats(setup.AllowedImageFormats);

        if (formats.IsFailure)
        {
            return Result.Failure<MediaOptions>(formats.Error);
        }

        return Result.Success(new MediaOptions
        {
            StorageServiceUrl = serviceUrl!,
            StorageBucket = setup.StorageBucket,
            StorageAccessKey = setup.StorageAccessKey,
            StorageSecretKey = setup.StorageSecretKey,
            StorageRegion = setup.StorageRegion,
            StorageForcePathStyle = setup.StorageForcePathStyle,
            PublicBaseUrl = publicBaseUrl!,
            WatermarkObjectKey = setup.WatermarkObjectKey,
            MaximumByteSize = setup.MaximumByteSize,
            MaximumPixelCount = setup.MaximumPixelCount,
            AllowedFormats = formats.Value,
        });
    }

    /// <summary>
    /// AC-9 / AC-5 — yapılandırılan tür adlarını çözer.
    ///
    /// <para><b>SVG eklenemez ve bu kodda duruyor</b> (D23): küme
    /// <c>jpeg · png · webp</c> dışına açılamaz. C14'ün istediği şey — sınırın
    /// yapılandırılabilir olması ve kriterin yapılandırılan değeri doğrulaması — korunuyor;
    /// daraltılan yalnız kümenin ÜST SINIRI. SVG aynı origin'de script çalıştırır ve bu
    /// depo o tuzağa `Program.cs`'in seed ucunda bir kez bakıp yüzeyi kapatmış. Bir
    /// yapılandırma satırıyla geri açılabilen güvenlik kararı, verilmemiş karardır.</para>
    ///
    /// <para>Tanınmayan bir ad da reddediliyor: sessizce atlanan bir ad, operatörün
    /// yapılandırdığını sandığı türün hiç kabul edilmemesi demekti.</para>
    /// </summary>
    private static Result<IReadOnlySet<DetectedImageFormat>> ParseAllowedFormats(
        IReadOnlyList<string> configured)
    {
        if (configured is null || configured.Count == 0)
        {
            return Result.Failure<IReadOnlySet<DetectedImageFormat>>(
                new Error("media.allowed_formats_missing"));
        }

        HashSet<DetectedImageFormat> cozulen = [];

        foreach (string ad in configured)
        {
            DetectedImageFormat? format = ImageFormatDetection.ParseConfigurable(ad);

            if (format is null)
            {
                return Result.Failure<IReadOnlySet<DetectedImageFormat>>(
                    Error.With("media.allowed_format_not_permitted", "format", ad.Trim().ToLowerInvariant()));
            }

            cozulen.Add(format.Value);
        }

        return Result.Success<IReadOnlySet<DetectedImageFormat>>(cozulen);
    }
}
