namespace CarGallery.Modules.Media.Contracts.Setup;

/// <summary>
/// Modülün kuruluş ayarları. Hepsi dağıtım yapılandırmasından gelir (C6); hiçbiri koda
/// gömülü değildir.
///
/// `Contracts` içinde yaşamasının nedeni erişimdir: Host'un bunu kurabilmesi gerekir ve
/// modülün iç projesinde hiçbir şey public değildir (architecture.md § 5).
///
/// <para><b>Eksik yapılandırmayla uygulama AYAĞA KALKMAZ</b> (C6, AC-31…33). Üçünün de
/// sessiz bir varsayılanı olamaz: depolama kimliği eksikse her yükleme çalışma anında
/// patlar; taban adres eksikse türevin erişim yolu üretilemez; filigran kaynağı eksikse
/// galerinin satın aldığı tek garanti — fotoğrafın logosuyla çıkması — sessizce kaybolur.</para>
/// </summary>
/// <param name="StorageAccessKey">
/// C6 — sır repoda DURMAZ; geliştirmede user-secrets, üretimde ortam değişkeni
/// (`docs/conventions.md` § 11).
/// </param>
/// <param name="StorageForcePathStyle">
/// R2 sanal-host adreslemesi kullanır, MinIO ve LocalStack yol tabanlı. Yanlış seçim
/// "kova bulunamadı" olarak görünür ve kimlik hatası sanılır.
/// </param>
/// <param name="WatermarkObjectKey">
/// D4 — filigran kaynağı `Media`'nın KENDİ kovasındaki bir nesne anahtarıdır, bir medya
/// varlığı kimliği değil. Varlık kimliği olsaydı çözülemeyen bir kurulum döngüsü doğardı:
/// yapılandırma zorunlu → uygulama kalkmaz → logo yüklenemez → kimlik yok.
/// </param>
/// <param name="MaximumPixelCount">
/// D10(c) — boyut sınırı bunu GÖRMEZ. 2 MB'lık bir PNG gigapiksele açılabilir ve tek
/// istekle sunucu CPU'sunu tüketir.
/// </param>
public sealed record MediaSetup(
    string ConnectionString,
    string StorageServiceUrl,
    string StorageBucket,
    string StorageAccessKey,
    string StorageSecretKey,
    string StorageRegion,
    bool StorageForcePathStyle,
    string PublicBaseUrl,
    string WatermarkObjectKey,
    long MaximumByteSize,
    long MaximumPixelCount,
    int MaximumAttempts,
    int RetryBaseSeconds);
