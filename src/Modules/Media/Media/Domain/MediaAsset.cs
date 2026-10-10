using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;

namespace CarGallery.Modules.Media.Domain;

/// <summary>
/// Yüklenmiş bir görsel ve hat üzerindeki hâli (`docs/domain.md` § 1 "Medya Varlığı").
///
/// <para><b>Medya sahipsizdir</b> (R10, M2): bu agregatta hiçbir araç, ilan veya şube
/// kimliği YOK ve olmayacak. Hangi aracın hangi görsele sahip olduğu
/// `VehicleCatalog`'dadır (C1, Yasak Listesi 33).</para>
///
/// <para><b>Durum makinesi M1'dir ve geçişler burada korunuyor</b> — dışarıdan bir servis
/// durumu yazsaydı, `Hazır` bir varlığı yeniden `İşleniyor` yapan bir yol açık kalırdı ve
/// duyuru iki kez giderdi.</para>
/// </summary>
internal sealed class MediaAsset : AggregateRoot<Guid>
{
    private readonly List<MediaDerivative> derivatives = [];

    private MediaAsset(
        Guid id,
        string contentType,
        string originalObjectKey,
        long byteSize,
        int sourceWidth,
        int sourceHeight,
        DateTimeOffset uploadedAt)
        : base(id)
    {
        ContentType = contentType;
        OriginalObjectKey = originalObjectKey;
        ByteSize = byteSize;
        SourceWidth = sourceWidth;
        SourceHeight = sourceHeight;
        Status = MediaProcessingStatus.Uploaded;
        UploadedAt = uploadedAt;
        UpdatedAt = uploadedAt;
        NextAttemptAt = uploadedAt;
    }

    /// <summary>EF'in materyalizasyon kurucusu.</summary>
    private MediaAsset(Guid id)
        : base(id)
    {
        ContentType = string.Empty;
        OriginalObjectKey = string.Empty;
    }

    internal string ContentType { get; private set; }

    /// <summary>D16 — `private/` önekinde durur ve hiçbir yanıtta yolu dönmez.</summary>
    internal string OriginalObjectKey { get; private set; }

    internal long ByteSize { get; private set; }

    internal int SourceWidth { get; private set; }

    internal int SourceHeight { get; private set; }

    internal MediaProcessingStatus Status { get; private set; }

    /// <summary>
    /// Kaç tur denendi. Sahiplenme geçişinde artar (D14): turun kendisi başarısız olsa
    /// bile sayaç artmış olur, aksi hâlde sonsuz tekrar eden bir varlık doğardı.
    /// </summary>
    internal int AttemptCount { get; private set; }

    /// <summary>Son turun hata KODU — kullanıcı metni değil (C7).</summary>
    internal string? FailureReason { get; private set; }

    internal DateTimeOffset UploadedAt { get; private set; }

    internal DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Turun bu varlığı en erken ne zaman eline alabileceği. İlk yüklemede yükleme anıdır —
    /// yani ilk tur beklemez; yalnız BAŞARISIZ bir turdan sonra ileri atılır.
    ///
    /// <para>Alan `OutboxMessage.NextAttemptAt`'in aynısıdır ve bilinçli olarak öyle:
    /// başarısız bir turdan sonra "hemen yeniden dene" demek, bozuk bir dosyayı her beş
    /// saniyede bir çözmeye kalkmak ve turu asıl işten alıkoymak demekti.</para>
    /// </summary>
    internal DateTimeOffset NextAttemptAt { get; private set; }

    /// <summary>R9 — locale başına `alt` metni. Girişi `0012`'de gelir (D5).</summary>
    internal LocalizedText AltText { get; private set; } = LocalizedText.Empty;

    internal IReadOnlyList<MediaDerivative> Derivatives => derivatives;

    internal static MediaAsset Create(
        Guid id,
        string contentType,
        string originalObjectKey,
        long byteSize,
        int sourceWidth,
        int sourceHeight,
        DateTimeOffset uploadedAt) =>
        new(id, contentType, originalObjectKey, byteSize, sourceWidth, sourceHeight, uploadedAt);

    /// <summary>
    /// D14 — turun varlığı SAHİPLENMESİ. Yalnız <c>Yüklendi</c> durumundan geçilir ve bu
    /// geçişin kendisi kilittir: iki tur aynı varlığı alamaz, çünkü ikincisi artık
    /// <c>Yüklendi</c> görmez (AC-12).
    /// </summary>
    internal Result BeginProcessing(DateTimeOffset at)
    {
        if (Status != MediaProcessingStatus.Uploaded)
        {
            return Result.Failure(Error.With(
                MediaErrors.InvalidStatusTransition, "status", Status.ToString()));
        }

        Status = MediaProcessingStatus.Processing;
        AttemptCount++;
        UpdatedAt = at;

        return Result.Success();
    }

    internal Result MarkReady(IEnumerable<MediaDerivative> produced, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(produced);

        if (Status != MediaProcessingStatus.Processing)
        {
            return Result.Failure(Error.With(
                MediaErrors.InvalidStatusTransition, "status", Status.ToString()));
        }

        derivatives.Clear();
        derivatives.AddRange(produced);

        Status = MediaProcessingStatus.Ready;
        FailureReason = null;
        UpdatedAt = at;

        return Result.Success();
    }

    /// <summary>
    /// Tur başarısız oldu ama deneme hakkı kaldı: varlık <c>Yüklendi</c>'ye döner ve
    /// sonraki tur onu yeniden eline alır (M1).
    /// </summary>
    /// <param name="retryBaseDelay">
    /// Yapılandırmadan gelen TABAN aralık (C6). Büyüme EĞRİSİ burada, domain'de yaşıyor:
    /// taban bir operasyon ayarıdır, artan aralık ise bir iş kuralı. İkisini aynı yere
    /// koymak, eğriyi yapılandırmayla değiştirilebilir kılardı.
    /// </param>
    internal Result Reschedule(string reason, DateTimeOffset at, TimeSpan retryBaseDelay)
    {
        if (Status != MediaProcessingStatus.Processing)
        {
            return Result.Failure(Error.With(
                MediaErrors.InvalidStatusTransition, "status", Status.ToString()));
        }

        Status = MediaProcessingStatus.Uploaded;
        FailureReason = reason;
        UpdatedAt = at;
        NextAttemptAt = at.Add(BackoffFor(retryBaseDelay, AttemptCount));

        return Result.Success();
    }

    /// <summary>Artan aralık: taban, 2×taban, 4×taban… (`OutboxDispatchOptions` ile aynı eğri).</summary>
    internal static TimeSpan BackoffFor(TimeSpan baseDelay, int attemptCount) =>
        baseDelay * Math.Pow(2, Math.Max(0, attemptCount - 1));

    /// <summary>
    /// R8/AC-11 — deneme hakkı tükendi. Varlık <c>Yüklendi</c> durumunda KALMAZ: sessizce
    /// bekleyen bir varlık, personelin fotoğrafın yüklendiğini sanması demektir.
    /// </summary>
    internal Result MarkFailed(string reason, DateTimeOffset at)
    {
        if (Status != MediaProcessingStatus.Processing)
        {
            return Result.Failure(Error.With(
                MediaErrors.InvalidStatusTransition, "status", Status.ToString()));
        }

        Status = MediaProcessingStatus.Failed;
        FailureReason = reason;
        UpdatedAt = at;

        return Result.Success();
    }

    internal void SetAltText(LocalizedText altText, DateTimeOffset at)
    {
        AltText = altText;
        UpdatedAt = at;
    }

    /// <summary>
    /// Deneme hakkı kaldı mı? Politika yapılandırmadan gelir (C6) ve karar burada verilir:
    /// eşiği `Application`'a bırakmak, "kaç denemeden sonra başarısız" kuralının iki yerde
    /// yaşaması demekti.
    /// </summary>
    internal bool HasAttemptsLeft(int maximumAttempts) => AttemptCount < maximumAttempts;
}
