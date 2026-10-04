using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Notifications.Domain;

/// <summary>
/// Kuyruk satırı **ve** gönderim geçmişi — tek tablo (R10, R12).
///
/// <para><b>Neden tek tablo.</b> İki tablo aynı gerçeği iki yere yazıp senkron tutma borcu
/// üretir ve "gönderildi ama geçmişe yazılamadı" diye yeni bir tutarsızlık sınıfı doğar.
/// Durum alanı ikisini birden karşılıyor.</para>
///
/// <para><b>ALICI ADRESİ BURADA YOKTUR</b> (C5, AC-24). Adres gönderim anında kaynaktan
/// okunur ve kayda yazılmaz. Tutulsaydı <c>Lead.Anonymize()</c> çalıştığı gün kişisel veri
/// <c>notifications</c> şemasında durmaya devam ederdi ve L6 kâğıt üstünde kalırdı —
/// <c>LeadCaptured</c>'ın doc comment'inin uyardığı hatanın başka bir modüldeki kopyası.
/// Bedeli bilerek ödeniyor: "hangi adrese gitti" sorusu kayıttan değil, yalnız sağlayıcı
/// günlüğünden cevaplanır.</para>
///
/// <para><b>Tam-bir-kez teslim vaat edilmez</b> (C10). Mesaj önce kaydedilir, sonra
/// gönderilir, sonra sonucu işaretlenir; gönderim ile işaretleme arasında süreç düşerse
/// mesaj tekrar gönderilebilir. Garanti edilen şey, aynı olayın ikinci işlenmesinde ikinci
/// bir KAYIT oluşmamasıdır.</para>
/// </summary>
internal sealed class NotificationMessage
{
    internal Guid Id { get; init; }

    /// <summary>Hangi talebin bildirimi (R12). Kişisel veri değil, kimliktir.</summary>
    internal Guid LeadId { get; init; }

    internal required string TemplateKey { get; init; }

    /// <summary>
    /// Talebin bırakıldığı locale (L1). Şablon çözümü bunun üzerinden yapılır ve istenen
    /// dilde şablon yoksa varsayılana düşülür (R9) — düşüş LOGLANIR (AC-20).
    ///
    /// `init`: kuyruk satırının dili kuyruğa girerken bellidir ve sonradan değişmez. Açık
    /// bir setter bırakmak, onu kimin ne zaman değiştirebileceğini belirsiz yapardı; diğer
    /// değişken alanların hepsi zaten `private set`.
    /// </summary>
    internal required string Locale { get; init; }

    internal NotificationChannel Channel { get; init; }

    internal NotificationStatus Status { get; private set; }

    internal int AttemptCount { get; private set; }

    /// <summary>Artan aralık (R11); `null` ise sırada bekleyen bir deneme yok.</summary>
    internal DateTimeOffset? NextAttemptAt { get; private set; }

    /// <summary>Kararlı hata kodu — çevrilmiş metin DEĞİL (Yasak Listesi 23).</summary>
    internal string? FailureCode { get; private set; }

    internal DateTimeOffset QueuedAt { get; init; }

    internal DateTimeOffset? CompletedAt { get; private set; }

    internal static NotificationMessage Queue(
        Guid leadId,
        string templateKey,
        string locale,
        NotificationChannel channel,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            LeadId = leadId,
            TemplateKey = templateKey,
            Locale = locale,
            Channel = channel,
            Status = NotificationStatus.Queued,
            NextAttemptAt = now,
            QueuedAt = now,
        };

    /// <summary>
    /// R6/AC-11 — kanal yoksa gönderim denenmez ama kayıt DURUR. Bu bir hata değil, eksik
    /// kanaldır; `Failed` yazmak galeriye yanlış bir şey söylerdi.
    /// </summary>
    internal static NotificationMessage NoChannel(
        Guid leadId,
        string templateKey,
        string locale,
        NotificationChannel channel,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            LeadId = leadId,
            TemplateKey = templateKey,
            Locale = locale,
            Channel = channel,
            Status = NotificationStatus.NoChannel,
            NextAttemptAt = null,
            QueuedAt = now,
            CompletedAt = now,
        };

    /// <summary>
    /// Tamamlanmış bir mesaj yeniden işaretlenemez. Koruma olmasaydı geç gelen bir sonuç
    /// `CompletedAt`'ı ezer ve geçmiş sessizce yalan söylerdi.
    /// </summary>
    internal Result MarkSent(DateTimeOffset now)
    {
        if (IsCompleted)
        {
            return Result.Failure(new Error(NotificationErrors.AlreadyCompleted));
        }

        Status = NotificationStatus.Sent;
        NextAttemptAt = null;
        FailureCode = null;
        CompletedAt = now;

        return Result.Success();
    }

    /// <summary>Sonucu yazılmış mesaj: `Sent`, `Failed` veya `NoChannel`.</summary>
    private bool IsCompleted => Status is not NotificationStatus.Queued;

    /// <summary>
    /// R11 — geçici hata yapılandırılan sayıda, ARTAN aralıkla yeniden denenir; denemeler
    /// tükendiğinde kayıt başarısız işaretlenir ve sessizce düşmez (AC-15, AC-16).
    /// Kalıcı hata hiç denenmez (AC-17).
    /// </summary>
    internal Result Fail(
        SendFailureKind kind,
        string failureCode,
        int maximumAttempts,
        TimeSpan baseDelay,
        DateTimeOffset now)
    {
        if (IsCompleted)
        {
            return Result.Failure(new Error(NotificationErrors.AlreadyCompleted));
        }

        AttemptCount++;
        FailureCode = failureCode;

        if (kind == SendFailureKind.Permanent || AttemptCount >= maximumAttempts)
        {
            Status = NotificationStatus.Failed;
            NextAttemptAt = null;
            CompletedAt = now;

            return Result.Success();
        }

        // Artan aralık: 1×, 2×, 4× … Sabit aralık, geçici bir kesintide bütün kuyruğu aynı
        // anda sağlayıcıya yığar.
        NextAttemptAt = now + baseDelay * Math.Pow(2, AttemptCount - 1);

        return Result.Success();
    }
}
