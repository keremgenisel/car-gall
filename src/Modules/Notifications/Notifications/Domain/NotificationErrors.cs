namespace CarGallery.Modules.Notifications.Domain;

/// <summary>
/// Kararlı hata kodları: `&lt;alan&gt;.&lt;durum&gt;`, snake_case
/// (docs/conventions.md § 5). Burada yalnız KOD vardır, metin yoktur.
/// </summary>
internal static class NotificationErrors
{
    internal const string TemplateMissing = "notification.template_missing";
    internal const string NoChannel = "notification.no_channel";
    internal const string SendFailed = "notification.send_failed";

    /// <summary>Sonucu zaten yazılmış bir mesajı yeniden işaretleme girişimi.</summary>
    internal const string AlreadyCompleted = "notification.already_completed";

    /// <summary>Yapılandırılan gönderim zaman aşımı aşıldı — GEÇİCİ hata (AC-18).</summary>
    internal const string SendTimedOut = "notification.send_timed_out";
}
