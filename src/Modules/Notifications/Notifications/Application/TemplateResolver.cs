using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications.Application;

/// <summary>Çözülen şablon ve GERÇEKTEN kullanılan locale.</summary>
internal sealed record ResolvedTemplate(string Locale, string Subject, string Body);

/// <summary>
/// R9 — şablonlar (anahtar × locale) olarak tutulur.
///
/// <para><b>Üç yol:</b> istenen dilde şablon varsa o; yoksa varsayılan dile düşülür ve
/// bu durum LOGLANIR (AC-20); varsayılan dilde de yoksa gönderim BAŞARISIZ işaretlenir
/// (AC-21). Ham şablon anahtarı veya boş gövde asla gönderilmez — `lead.received` yazan
/// bir e-posta, gönderilmemiş olmaktan kötüdür.</para>
///
/// <para><b>Boş gövde eksik sayılır.</b> Satırın var olması yetmez: konusu veya gövdesi
/// boş bir şablon, ziyaretçiye boş bir e-posta göndermek demektir ve R9'un yasakladığı
/// sonucu üretir.</para>
/// </summary>
internal sealed class TemplateResolver(NotificationsDbContext context, ILogger<TemplateResolver> logger)
{
    internal async Task<Result<ResolvedTemplate>> ResolveAsync(
        string templateKey,
        string requestedLocale,
        string defaultLocale,
        CancellationToken cancellationToken)
    {
        ResolvedTemplate? istenen = await BulAsync(templateKey, requestedLocale, cancellationToken);

        if (istenen is not null)
        {
            return Result.Success(istenen);
        }

        if (!string.Equals(requestedLocale, defaultLocale, StringComparison.Ordinal))
        {
            ResolvedTemplate? varsayilan = await BulAsync(templateKey, defaultLocale, cancellationToken);

            if (varsayilan is not null)
            {
                // AC-20 — düşüş sessiz olmaz: eksik şablon bir eksikliktir ve görünmeli.
                logger.LogWarning(
                    "Şablon istenen dilde yok, varsayılana düşüldü: {TemplateKey} {Requested} -> {Default}",
                    templateKey,
                    requestedLocale,
                    defaultLocale);

                return Result.Success(varsayilan);
            }
        }

        logger.LogError(
            "Şablon varsayılan dilde de yok; gönderim yapılmayacak: {TemplateKey} {Locale}",
            templateKey,
            defaultLocale);

        return Result.Failure<ResolvedTemplate>(new Error(NotificationErrors.TemplateMissing));
    }

    private async Task<ResolvedTemplate?> BulAsync(
        string templateKey,
        string locale,
        CancellationToken cancellationToken)
    {
        NotificationTemplate? sablon = await context.Templates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                template => template.TemplateKey == templateKey && template.Locale == locale,
                cancellationToken);

        if (sablon is null
            || string.IsNullOrWhiteSpace(sablon.Subject)
            || string.IsNullOrWhiteSpace(sablon.Body))
        {
            return null;
        }

        return new ResolvedTemplate(locale, sablon.Subject, sablon.Body);
    }
}
