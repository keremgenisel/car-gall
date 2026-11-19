using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Notifications.Domain;

/// <summary>
/// `0018` — Bülten Aboneliği: adres + dil + SON durum (docs/domain.md § Bildirim, **D19**).
///
/// <para><b>Geçmiş burada değil.</b> Aynı adresin ikinci aboneliği bu kaydı yeniden kullanır
/// (**D5**); istek, teyit ve çıkış zamanlarının hepsi <see cref="ConsentEvent"/>'te kalır
/// (**D27**). Buradaki <see cref="ConfirmedAt"/> ve <see cref="UnsubscribedAt"/> yalnız son
/// geçişi söyler.</para>
///
/// <para><b>Adres kimlik değildir, ayrıştırılmış adres kimliktir</b> (**D10**): kayıt
/// MimeKit'in çözdüğü adresi ve onun küçük harfli eşini taşır; tekillik eşin üzerindedir.</para>
///
/// <para><b>Teyit anahtarının yalnız SHA-256 özeti</b> durur (**D25**). Düz anahtar dağıtımda
/// üretilir ve yalnız iletide yaşar (**D28**).</para>
/// </summary>
internal sealed class NewsletterSubscription
{
    internal Guid Id { get; init; }

    /// <summary>Gönderim anında kaynaktan okunan adres (C5); başka hiçbir kayda kopyalanmaz.</summary>
    internal required string Email { get; init; }

    /// <summary>**D10** — büyük/küçük harf duyarsız tekil kuralın anahtarı.</summary>
    internal required string NormalizedEmail { get; init; }

    internal required string Locale { get; set; }

    internal SubscriptionStatus Status { get; private set; }

    internal byte[]? ConfirmationTokenHash { get; private set; }

    internal DateTimeOffset? ConfirmationExpiresAt { get; private set; }

    internal DateTimeOffset CreatedAt { get; init; }

    internal DateTimeOffset? ConfirmedAt { get; private set; }

    internal DateTimeOffset? UnsubscribedAt { get; private set; }

    /// <summary>
    /// Eşzamanlılık belirteci. Tek kullanımlık teyit bağlantısıyla aynı anda gelen iki istekten
    /// yalnız biri yazabilir (AC-33); ardışık test bunu göremez (Bilinen tuzak 9).
    /// </summary>
    internal int Version { get; private set; }

    internal static NewsletterSubscription Request(
        string email,
        string normalizedEmail,
        string locale,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            Locale = locale,
            Status = SubscriptionStatus.PendingConfirmation,
            CreatedAt = now,
        };

    /// <summary>
    /// **D5** — teyit bekleyen ya da çıkmış kayda yeni istek: dil güncellenir (**D14**) ve
    /// kayıt teyit bekliyor'a döner. Etkin kayıt değişmez; çağıran bu metodu çağırmaz.
    /// </summary>
    internal void RequestAgain(string locale)
    {
        Locale = locale;
        Status = SubscriptionStatus.PendingConfirmation;
        Touch();
    }

    /// <summary>**D28** — dağıtıcı yeni anahtarın özetini yazar; önceki bağlantı geçersizleşir (**D7**).</summary>
    internal void IssueConfirmationToken(byte[] tokenHash, DateTimeOffset expiresAt)
    {
        ConfirmationTokenHash = tokenHash;
        ConfirmationExpiresAt = expiresAt;
        Touch();
    }

    /// <summary>
    /// **D7** — süresi dolmuş, kullanılmış ve hiç verilmemiş anahtar AYNI sonucu üretir;
    /// çağıran ayırt etmez. Başarılı teyitte anahtar silinir: ikinci kullanım geçersizdir.
    /// </summary>
    internal Result Confirm(DateTimeOffset now)
    {
        if (Status != SubscriptionStatus.PendingConfirmation
            || ConfirmationExpiresAt is null
            || ConfirmationExpiresAt <= now)
        {
            return Result.Failure(new Error(NotificationErrors.SubscriptionLinkInvalid));
        }

        Status = SubscriptionStatus.Active;
        ConfirmedAt = now;
        ConfirmationTokenHash = null;
        ConfirmationExpiresAt = null;
        Touch();

        return Result.Success();
    }

    /// <summary>
    /// **D8** — çıkış döndürülmez: çıkmış kayıtta ikinci çağrı hiçbir şeyi değiştirmez ve
    /// <c>false</c> döner (AC-41). Kayıt silinmez (R6).
    /// </summary>
    internal bool Unsubscribe(DateTimeOffset now)
    {
        if (Status == SubscriptionStatus.Unsubscribed)
        {
            return false;
        }

        Status = SubscriptionStatus.Unsubscribed;
        UnsubscribedAt = now;
        ConfirmationTokenHash = null;
        ConfirmationExpiresAt = null;
        Touch();

        return true;
    }

    private void Touch() => Version++;
}

/// <summary>Abonelik Durumu (**D19**). Kod olarak saklanır (Yasak Listesi 22).</summary>
internal enum SubscriptionStatus
{
    PendingConfirmation = 1,
    Active = 2,
    Unsubscribed = 3,
}
