using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` R5 · R6 · **D6** · **D8** — çıkış: bağlantı açıldığı anda çalışır, giriş ya da onay
/// istemez (AC-38). Kayıt silinmez, çıkmış işaretlenir (AC-40). Çıkmış kayıtta ikinci açılış
/// aynı sonucu verir ve hiçbir şeyi değiştirmez (AC-41).
///
/// <para>Geçersiz imza ile var olmayan abonelik AYNI kodu döner ve hiçbir kaydı değiştirmez
/// (AC-44).</para>
/// </summary>
internal sealed class UnsubscribeFromNewsletter(
    NotificationsDbContext context,
    UnsubscribeTokens tokens,
    IClock clock)
{
    internal async Task<Result> HandleAsync(string? token, CancellationToken cancellationToken)
    {
        if (!tokens.TryVerify(token, out Guid subscriptionId))
        {
            return Invalid();
        }

        NewsletterSubscription? subscription = await context.Subscriptions
            .SingleOrDefaultAsync(item => item.Id == subscriptionId, cancellationToken);

        if (subscription is null)
        {
            return Invalid();
        }

        DateTimeOffset now = clock.UtcNow;

        if (!subscription.Unsubscribe(now))
        {
            return Result.Success();
        }

        context.ConsentEvents.Add(ConsentEvent.Record(subscription.Id, ConsentEventKind.Unsubscribed, now));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Eşzamanlı ikinci açılış ya da aynı anda gelen istek: kazanan yazdı. Abone
            // çıkmışsa sonuç aynıdır; değilse (araya yeni bir istek girdiyse) bir kez daha dene.
            context.ChangeTracker.Clear();
            return await HandleAsync(token, cancellationToken);
        }

        return Result.Success();
    }

    private static Result Invalid() => Result.Failure(new Error(NotificationErrors.SubscriptionLinkInvalid));
}
