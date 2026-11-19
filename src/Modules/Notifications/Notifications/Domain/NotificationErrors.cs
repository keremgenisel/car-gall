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

    /// <summary>Sağlayıcı geçici bir koşul bildirdi (`4xx`), bağlantı veya ağ düştü.</summary>
    internal const string ProviderUnavailable = "notification.provider_unavailable";

    /// <summary>Sağlayıcı mesajı kalıcı olarak reddetti (`5xx`) veya adres geçersiz.</summary>
    internal const string RecipientRejected = "notification.recipient_rejected";

    /// <summary>`0018` AC-7–AC-9 — biçimi geçersiz adres (**D10**).</summary>
    internal const string SubscriptionEmailInvalid = "notification.subscription_email_invalid";

    /// <summary>`0018` AC-10 — kurulumda etkin olmayan ya da tanınmayan dil (**D14**).</summary>
    internal const string SubscriptionInvalidLocale = "notification.subscription_invalid_locale";

    /// <summary>`0018` AC-12, AC-14 — üç hız sınırının kaynak başına ikisi (**D9**).</summary>
    internal const string SubscriptionRateLimited = "notification.subscription_rate_limited";

    /// <summary>
    /// `0018` **D7** · **D8** — süresi dolmuş, kullanılmış, yerine yenisi gelmiş, bozuk ve hiç
    /// var olmamış bağlantı: TEK kod (AC-35, AC-44).
    /// </summary>
    internal const string SubscriptionLinkInvalid = "notification.subscription_link_invalid";

    /// <summary>`0018` **D22** — dağıtım anında abonelik etkin değil (çıkmış, kayıt yok, teyit bekliyor).</summary>
    internal const string SubscriptionInactive = "notification.subscription_inactive";

    /// <summary>`0018` **D7** — teyit iletisinin bağlantısı dağıtım anında süresini doldurmuş; gönderilmez.</summary>
    internal const string ConfirmationLinkExpired = "notification.confirmation_link_expired";

    /// <summary>`0018` **D21** — dağıtım anında dönemin hiçbir ilanı Yayında değil; boş bülten çıkmaz (R9).</summary>
    internal const string NewsletterEmpty = "notification.newsletter_empty";
}
