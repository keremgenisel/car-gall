using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application.Newsletter;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// Abonelik uçlarının arkasındaki üç işleyiciyi gerçek veritabanıyla kurar. Her çağrı kendi
/// bağlamını açar — istekler gibi. Geçerli istek bütçesi düzen ömrü boyunca tekildir, Host'taki
/// gibi.
/// </summary>
internal sealed class AbonelikDuzeni(NotificationsDatabase database, IClock saat, NewsletterOptions? secenekler = null)
{
    internal const string Ip = "198.51.100.23";

    internal NewsletterOptions Secenekler { get; } = secenekler ?? Bulten.BultenSecenekleri();

    private NewsletterRequestQuota? kota;

    private NewsletterRequestQuota Kota => kota ??= new NewsletterRequestQuota(saat, Secenekler);

    internal UnsubscribeTokens Cikis => new(Secenekler);

    internal async Task<Result> AboneOlAsync(string? adres, string? dil = "tr", string ip = Ip, params IInterceptor[] araya)
    {
        await using NotificationsDbContext context = database.CreateContext(araya);

        return await new SubscribeToNewsletter(context, Kota, Secenekler, CarGallery.BuildingBlocks.Concurrency.InvariantGuardOptions<NotificationsDbContext>.Default, saat)
            .HandleAsync(new SubscribeCommand(adres, dil, ip), CancellationToken.None);
    }

    internal async Task<Result> TeyitEtAsync(string? anahtar, params IInterceptor[] araya)
    {
        await using NotificationsDbContext context = database.CreateContext(araya);

        return await new ConfirmSubscription(context, saat).HandleAsync(anahtar, CancellationToken.None);
    }

    internal async Task<Result> CikAsync(string? anahtar)
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await new UnsubscribeFromNewsletter(context, Cikis, saat).HandleAsync(anahtar, CancellationToken.None);
    }

    /// <summary>
    /// **D28** — dağıtıcının teyit iletisini kurarken yaptığı şey: anahtar üretilir, özeti ve
    /// süresi aboneliğe yazılır. Teyit testleri iletiyi göndermeden bağlantıyı elde eder.
    /// </summary>
    internal async Task<string> AnahtarVerAsync(string adres)
    {
        await using NotificationsDbContext context = database.CreateContext();

        NewsletterSubscription abonelik = await context.Subscriptions
            .SingleAsync(item => item.NormalizedEmail == adres.Trim().ToLowerInvariant());

        (string anahtar, byte[] ozet) = ConfirmationTokens.Generate();
        abonelik.IssueConfirmationToken(ozet, saat.UtcNow + Secenekler.ConfirmationLinkLifetime);
        await context.SaveChangesAsync();

        return anahtar;
    }

    internal async Task<NewsletterSubscription> AbonelikAsync(string adres)
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Subscriptions.AsNoTracking()
            .SingleAsync(item => item.NormalizedEmail == adres.Trim().ToLowerInvariant());
    }

    internal async Task<int> AbonelikSayisiAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Subscriptions.CountAsync();
    }

    internal async Task<List<ConsentEvent>> OlaylarAsync(Guid abonelikId)
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.ConsentEvents.AsNoTracking()
            .Where(item => item.SubscriptionId == abonelikId)
            .OrderBy(item => item.OccurredAt)
            .ThenBy(item => item.Id)
            .ToListAsync();
    }

    internal async Task<List<NotificationMessage>> TeyitIletileriAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Messages.AsNoTracking()
            .Where(item => item.TemplateKey == TemplateKeys.NewsletterConfirmation)
            .OrderBy(item => item.QueuedAt)
            .ToListAsync();
    }

    /// <summary>`notifications` şemasındaki BÜTÜN satırlar metin olarak — yarın eklenen bir kolon da taranır.</summary>
    internal async Task<List<string>> TumSatirlarAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Database.SqlQuery<string>($"""
            SELECT t::text AS "Value" FROM notifications.newsletter_subscriptions t
            UNION ALL SELECT t::text FROM notifications.consent_events t
            UNION ALL SELECT t::text FROM notifications.messages t
            UNION ALL SELECT t::text FROM notifications.newsletter_candidates t
            UNION ALL SELECT t::text FROM notifications.newsletter_periods t
            UNION ALL SELECT t::text FROM notifications.newsletter_period_listings t
            UNION ALL SELECT t::text FROM notifications.inbox_entries t
            """).ToListAsync();
    }
}
