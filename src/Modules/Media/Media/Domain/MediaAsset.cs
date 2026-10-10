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

    /// <summary>
    /// AC-33 — sebep KAPALI bir kümedendir; serbest metin veya çevrilmiş metin değil.
    /// Önceki hâli <c>string</c>'di ve küme yalnızca sabitlerle temsil ediliyordu; tip
    /// onu zorlamıyordu.
    /// </summary>
    internal MediaFailureReason? FailureReason { get; private set; }

    internal DateTimeOffset UploadedAt { get; private set; }

    internal DateTimeOffset UpdatedAt { get; private set; }


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
    /// D14 — turun varlığı SAHİPLENMESİ. Yalnız <c>yüklendi</c> durumundan geçilir.
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
    /// R8/AC-13 — tur başarısız oldu. Varlık <c>yüklendi</c> durumunda KALMAZ: sessizce
    /// bekleyen bir varlık, personelin fotoğrafın yüklendiğini sanması demektir.
    ///
    /// <para><b>Otomatik yeniden kuyruğa alma YOK</b> (AC-15, M1). Önceki hâli deneme
    /// bütçesi tüketilene kadar <c>işleniyor → yüklendi</c> geçişi yapıyordu; M1 o geçişi
    /// hiç saymıyor ve gerekçe kayıtlı: `0009`'un kaçan hatası otomatik tekrarın kuyruğu
    /// sessizce tıkamasıydı. Tekrar deneme artık <see cref="Retry"/> ile, AÇIK bir
    /// komutla.</para>
    /// </summary>
    internal Result MarkFailed(MediaFailureReason reason, DateTimeOffset at)
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

    /// <summary>
    /// AC-14 — <c>başarısız</c> TERMİNAL DEĞİLDİR. Tekrar deneme ayrı bir komutla
    /// tetiklenir ve <b>aynı varlık kimliğini korur</b>: yeni bir kimlik,
    /// `VehicleCatalog`'un tuttuğu referansı öksüz bırakırdı.
    ///
    /// <para>Yalnız <c>başarısız</c>'dan geçilir (M1). <c>işleniyor</c>'dan çağrılması,
    /// koşan bir turun altından varlığı çekmek olurdu.</para>
    /// </summary>
    internal Result Retry(DateTimeOffset at)
    {
        if (Status != MediaProcessingStatus.Failed)
        {
            return Result.Failure(Error.With(
                MediaErrors.InvalidStatusTransition, "status", Status.ToString()));
        }

        Status = MediaProcessingStatus.Uploaded;
        UpdatedAt = at;

        return Result.Success();
    }

    internal void SetAltText(LocalizedText altText, DateTimeOffset at)
    {
        AltText = altText;
        UpdatedAt = at;
    }
}
