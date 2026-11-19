using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>Abone olma isteği. Dil gövdede açıkça gelir (**D14**, `0005` deseni).</summary>
internal sealed record SubscribeCommand(
    string? Email,
    string? Locale,
    /// <summary>Geçerli istek bütçesini bölen OPAK anahtar (bugün IP). Kalıcı kayda girmez (AC-15).</summary>
    string ClientKey);

/// <summary>
/// `0018` R1 · R7 · R13 · R19 — abone olma.
///
/// <para><b>Dört durum, TEK yazma yolu</b> (**D5**, Bilinen tuzak 7). Hiç kaydı olmayan,
/// teyit bekleyen, etkin ve çıkmış adresin isteği aynı sonucu döner; arkadaki davranış farklı
/// ama hepsi aynı transaction'dan, aynı sırayla geçer ve hiçbiri iletinin gönderilmesini
/// beklemez — ileti yalnız KUYRUĞA girer (R10, AC-24).</para>
///
/// <para><b>Sıra:</b> önce ziyaretçinin kendi girdisi (adres, dil), sonra geçerli istek
/// bütçesi, en sonda yazma. Biçimi geçersiz istek bütçeyi tüketmez (AC-13).</para>
///
/// <para><b>Adres sınırı</b> (**D9**, **D29**): pencere içindeki teyit iletileri aboneliğin
/// gönderim kayıtlarından, satır kilidi altında sayılır (`InvariantGuard`). Sınırdayken kayıtta HİÇBİR ŞEY
/// değişmez — anahtar, dil, durum ve izin olayı (AC-23).</para>
/// </summary>
internal sealed class SubscribeToNewsletter(
    NotificationsDbContext context,
    INewsletterRequestQuota quota,
    ConfirmationTokens tokens,
    NewsletterOptions options,
    InvariantGuardOptions<NotificationsDbContext> guardOptions,
    IClock clock)
{
    internal async Task<Result> HandleAsync(SubscribeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!SubscriberEmail.TryParse(command.Email, out SubscriberEmail email))
        {
            return Result.Failure(Error.With(NotificationErrors.SubscriptionEmailInvalid, "field", "email"));
        }

        if (command.Locale is null || !options.IsEnabled(command.Locale))
        {
            return Result.Failure(Error.With(NotificationErrors.SubscriptionInvalidLocale, "field", "locale"));
        }

        if (!quota.TryAcquire(command.ClientKey))
        {
            return Result.Failure(new Error(NotificationErrors.SubscriptionRateLimited));
        }

        try
        {
            return await WriteAsync(email, command.Locale, cancellationToken);
        }
        catch (DbUpdateException exception) when (IsAddressConflict(exception))
        {
            // AC-22 — aynı YENİ adrese eşzamanlı ikinci istek okumayı geçmiş ama tekil
            // indekse takılmış. Kayıt artık var: istek "var olan adres" yolundan bir kez daha
            // geçer. Yarışı okuma değil indeks kapatır.
            context.ChangeTracker.Clear();
            return await WriteAsync(email, command.Locale, cancellationToken);
        }
    }

    /// <summary>
    /// Kilit töreni ortak desendedir (`InvariantGuard`, `0007` R3): aynı adresin satırı kilitlenir,
    /// kayıt KİLİTTEN SONRA okunur, karar verilir ve yazılır. Kilit, adres sınırının sayımını
    /// ve yazmayı aynı isteğe bağlar: aynı adrese eşzamanlı iki istek sınırı birlikte aşamaz.
    /// Satır yoksa kilitlenecek bir şey yoktur; o yarışı tekil indeks kapatır (AC-22).
    /// </summary>
    private async Task<Result> WriteAsync(SubscriberEmail email, string locale, CancellationToken cancellationToken) =>
        await InvariantGuard.ExecuteAsync(
            context,
            new InvariantLock(
                NotificationsDbContext.Schema,
                "newsletter_subscriptions",
                "normalized_email = {0}",
                [email.Normalized]),
            Guid.Empty,
            async (_, token) =>
            {
                await DecideAndWriteAsync(email, locale, token);
                return Result.Success();
            },
            guardOptions,
            cancellationToken);

    private async Task DecideAndWriteAsync(SubscriberEmail email, string locale, CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.UtcNow;

        NewsletterSubscription? subscription = await context.Subscriptions
            .SingleOrDefaultAsync(item => item.NormalizedEmail == email.Normalized, cancellationToken);

        if (subscription is null)
        {
            subscription = NewsletterSubscription.Request(email.Address, email.Normalized, locale, now);
            context.Subscriptions.Add(subscription);
            Queue(subscription, locale, now);
        }
        else if (subscription.Status == SubscriptionStatus.Active)
        {
            // D5 — etkin abonelik değişmez ve ileti gitmez; istek yine kayda geçer (D34).
            context.ConsentEvents.Add(ConsentEvent.Record(subscription.Id, ConsentEventKind.Requested, now));
        }
        else if (await ConfirmationsInWindowAsync(subscription.Id, now, cancellationToken) >= options.AddressConfirmationLimit)
        {
            // D9 — adres sınırı: cevap aynı, kayıtta hiçbir şey değişmez, olay yazılmaz (D27).
            return;
        }
        else
        {
            subscription.RequestAgain(locale);
            Queue(subscription, locale, now);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// **D28** — teyit anahtarı gönderim kaydı OLUŞURKEN türetilir ve özeti aynı anda aboneliğe
    /// yazılır: önceki kaydın bağlantısı bu anda geçersizleşir (**D7**) ve süre istekten başlar.
    /// </summary>
    private void Queue(NewsletterSubscription subscription, string locale, DateTimeOffset now)
    {
        NotificationMessage message = NotificationMessage.QueueConfirmation(subscription.Id, locale, now);

        subscription.IssueConfirmationToken(tokens.For(message.Id).Hash, now + options.ConfirmationLinkLifetime);
        context.ConsentEvents.Add(ConsentEvent.Record(subscription.Id, ConsentEventKind.Requested, now));
        context.Messages.Add(message);
    }

    private Task<int> ConfirmationsInWindowAsync(Guid subscriptionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        DateTimeOffset since = now - options.AddressWindow;

        return context.Messages.CountAsync(
            message => message.SubscriptionId == subscriptionId
                && message.TemplateKey == TemplateKeys.NewsletterConfirmation
                && message.QueuedAt > since,
            cancellationToken);
    }

    private static bool IsAddressConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
        && postgres.ConstraintName == "ux_newsletter_subscriptions_normalized_email";
}
