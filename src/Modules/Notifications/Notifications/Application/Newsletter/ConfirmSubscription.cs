using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` R2 · R14 · **D6** · **D7** — teyit. Sayfanın AÇILMASI teyit değildir; bu yol
/// yalnız sayfadaki düğmeyle çağrılır (AC-27).
///
/// <para><b>Tek sonuç kodu.</b> Süresi dolmuş, kullanılmış, yerine yenisi gönderilmiş, bozuk
/// ve hiç var olmamış anahtar AYNI kodu döner (AC-35): hangisi olduğu bağlantıyı elinde tutana
/// hiçbir şey kazandırmaz.</para>
///
/// <para><b>Tek kullanım eşzamanlılıkta da tek</b> (AC-33, Bilinen tuzak 9): aynı anda gelen
/// iki istekten yalnız biri yazar — kayıt sürüm belirteci taşır ve ikinci yazma
/// <see cref="DbUpdateConcurrencyException"/> alır.</para>
/// </summary>
internal sealed class ConfirmSubscription(NotificationsDbContext context, IClock clock)
{
    internal async Task<Result> HandleAsync(string? token, CancellationToken cancellationToken)
    {
        byte[]? hash = ConfirmationTokens.HashOf(token);

        if (hash is null)
        {
            return Invalid();
        }

        NewsletterSubscription? subscription = await context.Subscriptions
            .SingleOrDefaultAsync(item => item.ConfirmationTokenHash == hash, cancellationToken);

        if (subscription is null)
        {
            return Invalid();
        }

        DateTimeOffset now = clock.UtcNow;

        if (subscription.Confirm(now).IsFailure)
        {
            return Invalid();
        }

        context.ConsentEvents.Add(ConsentEvent.Record(subscription.Id, ConsentEventKind.Confirmed, now));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // İkinci eşzamanlı kullanım: kazanan zaten yazdı; bu istek "kullanılmış" sayılır.
            context.ChangeTracker.Clear();
            return Invalid();
        }

        return Result.Success();
    }

    private static Result Invalid() => Result.Failure(new Error(NotificationErrors.SubscriptionLinkInvalid));
}
