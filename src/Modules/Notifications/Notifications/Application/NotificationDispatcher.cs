using System.Globalization;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications.Application;

/// <summary>
/// Kuyruğu işler: sırası gelmiş her mesaj için alıcıyı ve şablonu çözer, gönderir, sonucu
/// işaretler (R10, R11).
///
/// <para><b>Sıralama C10'dur:</b> mesaj ÖNCE kaydedilmiştir (işleyici yazdı), burada
/// gönderilir, SONRA sonucu işaretlenir. Gönderim ile işaretleme arasında süreç düşerse
/// mesaj tekrar gönderilebilir ve bu bilerek kabul edilir — en-az-bir-kez teslim ile dış
/// yan etki birlikteyken tam-bir-kez imkânsızdır.</para>
///
/// <para><b>Mesajlar birbirinden bağımsızdır</b> (R7, AC-12): biri başarısız olursa döngü
/// devam eder. Galeri bildiriminin düşmesi ziyaretçi karşılığını engellemez ve tersi.</para>
///
/// <para><b>Alıcı adresi kayıttan değil KAYNAKTAN okunur</b> (C5): gönderim anında
/// çözülür, kayda hiç yazılmaz.</para>
/// </summary>
internal sealed class NotificationDispatcher(
    NotificationsDbContext context,
    INotificationTransport transport,
    TemplateResolver templates,
    ILeadNotificationQueries leads,
    IBranchContactQueries branches,
    IListingSlugQueries listings,
    NotificationsOptions options,
    IClock clock,
    ILogger<NotificationDispatcher> logger)
{
    /// <summary>Bir turda işlenecek üst sınır; kuyruk uzunsa tur tur ilerlenir.</summary>
    private const int BatchSize = 50;

    internal async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset now = clock.UtcNow;

        List<NotificationMessage> due = await context.Messages
            .Where(message => message.Status == NotificationStatus.Queued
                && message.NextAttemptAt != null
                && message.NextAttemptAt <= now)
            .OrderBy(message => message.NextAttemptAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (NotificationMessage message in due)
        {
            await DispatchOneAsync(message, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        return due.Count;
    }

    private async Task DispatchOneAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        LeadNotificationDto? lead = await leads.FindAsync(message.LeadId, cancellationToken);

        if (lead is null)
        {
            // Talep silinmiş veya anonimleştirilmiş: gönderilecek bir şey kalmadı ve bu
            // yeniden denemeyle düzelmez.
            Finish(message, SendFailureKind.Permanent, NotificationErrors.NoChannel);
            return;
        }

        BranchNotificationContactDto? branch =
            await branches.FindContactAsync(lead.BranchId, options.DefaultLocale, cancellationToken);

        string? recipient = ResolveRecipient(message, lead, branch);

        if (recipient is null)
        {
            Finish(message, SendFailureKind.Permanent, NotificationErrors.NoChannel);
            return;
        }

        Result<ResolvedTemplate> template = await templates.ResolveAsync(
            message.TemplateKey,
            message.Locale,
            options.DefaultLocale,
            cancellationToken);

        if (template.IsFailure)
        {
            // AC-21 — ham anahtar veya boş gövde GÖNDERİLMEZ; yeniden denemek de düzeltmez.
            Finish(message, SendFailureKind.Permanent, NotificationErrors.TemplateMissing);
            return;
        }

        Dictionary<string, string> values = new(StringComparer.Ordinal)
        {
            ["modelAdi"] = lead.CanonicalModelName,
            ["subeAdi"] = branch?.Name ?? string.Empty,
            ["ilanBaglantisi"] = await BuildListingLinkAsync(lead, message.Locale, cancellationToken),
        };

        OutboundMessage outbound = new(
            recipient,
            TemplateRenderer.Render(template.Value.Subject, values),
            TemplateRenderer.Render(template.Value.Body, values));

        SendResult result = await SendAsync(outbound, cancellationToken);

        if (result.Succeeded)
        {
            message.MarkSent(clock.UtcNow);
            return;
        }

        Finish(message, result.Failure!.Value, result.Code ?? NotificationErrors.SendFailed);
    }

    /// <summary>
    /// AC-18 — yapılandırılan zaman aşımı GEÇİCİ hatadır. Taşıyıcının kendi beklemesine
    /// güvenilmez: sağlayıcı bağlantıyı açık tutup hiç cevap vermeyebilir ve o hâlde
    /// kuyruk tek bir mesajda süresiz asılı kalırdı.
    /// </summary>
    private async Task<SendResult> SendAsync(OutboundMessage outbound, CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.SendTimeout);

        try
        {
            return await transport.SendAsync(outbound, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SendResult.Transient(NotificationErrors.SendTimedOut);
        }
    }

    /// <summary>
    /// AC-4/AC-5 — galeri bildirimi şubenin adresine, yoksa kurulum adresine gider.
    /// Ziyaretçi karşılığının tek kanalı talebin e-postasıdır.
    /// </summary>
    private string? ResolveRecipient(
        NotificationMessage message,
        LeadNotificationDto lead,
        BranchNotificationContactDto? branch)
    {
        if (message.TemplateKey == TemplateKeys.LeadReceivedGallery)
        {
            return string.IsNullOrWhiteSpace(branch?.Email)
                ? options.SetupNotificationAddress
                : branch.Email;
        }

        return string.IsNullOrWhiteSpace(lead.Email) ? null : lead.Email;
    }

    /// <summary>
    /// AC-3 — bağlantı MUTLAK olmalı; göreli bağlantı e-postada tıklanamaz.
    /// AC-32 — slug çözülemezse boş döner ve şablonun o satırı tamamen düşer; gönderim
    /// başarısız SAYILMAZ, çünkü galeri yine de talepten haberdar olmalı.
    /// </summary>
    private async Task<string> BuildListingLinkAsync(
        LeadNotificationDto lead,
        string locale,
        CancellationToken cancellationToken)
    {
        string? slug = await listings.FindSlugAsync(lead.ListingId, cancellationToken);

        if (string.IsNullOrWhiteSpace(slug))
        {
            logger.LogInformation(
                "İlan slug'ı çözülemedi; bildirim bağlantısız gidiyor: {ListingId}",
                lead.ListingId);

            return string.Empty;
        }

        return new Uri(
            options.PublicBaseUrl,
            string.Create(CultureInfo.InvariantCulture, $"/{locale}/cars/{Uri.EscapeDataString(slug)}"))
            .ToString();
    }

    private void Finish(NotificationMessage message, SendFailureKind kind, string code)
    {
        Result marked = message.Fail(
            kind,
            code,
            options.MaximumAttempts,
            options.RetryBaseDelay,
            clock.UtcNow);

        if (marked.IsFailure)
        {
            return;
        }

        // AC-16 — denemeler tükendiğinde sessizce düşmez: işaretlenir VE loglanır.
        if (message.Status == NotificationStatus.Failed)
        {
            logger.LogError(
                "Bildirim gönderilemedi ve denemeler tükendi: {MessageId} {TemplateKey} {FailureCode} {AttemptCount}",
                message.Id,
                message.TemplateKey,
                code,
                message.AttemptCount);
        }
    }
}
