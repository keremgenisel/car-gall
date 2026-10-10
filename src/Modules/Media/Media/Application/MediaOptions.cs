using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Setup;

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

    internal required int MaximumAttempts { get; init; }

    internal required TimeSpan RetryBaseDelay { get; init; }

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

        if (!IsReachableUrl(setup.PublicBaseUrl, out Uri? publicBaseUrl))
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

        if (setup.MaximumAttempts < 1 || setup.RetryBaseSeconds < 1)
        {
            return Result.Failure<MediaOptions>(new Error("media.invalid_retry_policy"));
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
            MaximumAttempts = setup.MaximumAttempts,
            RetryBaseDelay = TimeSpan.FromSeconds(setup.RetryBaseSeconds),
        });
    }
}
