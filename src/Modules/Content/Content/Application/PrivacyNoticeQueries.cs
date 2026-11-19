using CarGallery.Modules.Content.Contracts.PrivacyNotice;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Content.Application;

/// <summary>
/// `0019` — sürüm okuması. TEK ATLAMA: bu dosya başka bir modülün sözleşmesine uzanmaz;
/// `Leads` buraya sorar ve zincir burada biter (C1, D19, Yasak Listesi 13).
/// </summary>
internal sealed class PrivacyNoticeQueries(ContentDbContext context, ContentLocaleOptions locales) : IPrivacyNoticeQueries
{
    /// <inheritdoc />
    public async Task<PrivacyNoticeLookup> FindPublishedAsync(string locale, CancellationToken cancellationToken)
    {
        // Etkin olmayan dilde `Content` hiçbir metni yayına alamaz: "yayında değil" gerçektir.
        if (!locales.IsEnabled(locale))
        {
            return PrivacyNoticeLookup.NotPublished;
        }

        // P4/C7 — sayfa satırı, dilin durumu, işaretçinin gösterdiği sürüm satırının
        // varlığı ve (P13) o satırın metninin canlı metinden farklı olup olmadığı TEK
        // komutta okunur. İki ayrı sorgu, aralarına giren bir yayınla iki farklı
        // anın karışımını okuyabilirdi (R8).
        var satir = await context.Pages
            .AsNoTracking()
            .Where(page => page.SystemKey == SystemPageKeys.Privacy)
            .Select(page => new
            {
                Dil = page.Localizations
                    .Where(item => item.Locale == locale)
                    .Select(item => new
                    {
                        item.Status,
                        item.PublishedVersionId,
                        SurumVar = context.PrivacyNoticeVersions.Any(surum => surum.Id == item.PublishedVersionId),
                        MetinFarkli = context.PrivacyNoticeVersions.Any(surum =>
                            surum.Id == item.PublishedVersionId
                            && (surum.Title != item.Title || surum.Body != item.Body)),
                    })
                    .FirstOrDefault(),
            })
            .SingleOrDefaultAsync(cancellationToken);

        // D14 — sayfa satırının yokluğu "yayında değil" DEĞİLDİR: kurulum bozuktur.
        if (satir is null)
        {
            throw new InvalidOperationException("KVKK aydınlatma metni sistem sayfası (privacy) bulunamadı.");
        }

        if (satir.Dil is null || satir.Dil.Status != PublicationStatus.Published)
        {
            return PrivacyNoticeLookup.NotPublished;
        }

        // D14 — dil yayında ama sürümü yok: canlı metnin kanıtı yok (R13 kırık).
        if (satir.Dil.PublishedVersionId is not Guid surumKimligi || !satir.Dil.SurumVar)
        {
            throw new InvalidOperationException(
                $"KVKK aydınlatma metni '{locale}' dilinde yayında ama yayınlanmış sürümü yok.");
        }

        // P13 — R13'ün değişmezi çalışma anında da uygulanır: işaretçinin gösterdiği sürüm
        // canlı metinle birebir aynı değilse (ör. eski uygulama migration'dan sonra yayındaki
        // metni düzenledi — C10) talep o sürüme BAĞLANMAZ; kanıt canlı metni söylemezdi.
        // PostgreSQL'in deterministik harmanlamasında `=` bayt eşitliğidir: ordinal.
        if (satir.Dil.MetinFarkli)
        {
            throw new InvalidOperationException(
                $"KVKK aydınlatma metninin '{locale}' dilindeki en son sürümü canlı metinle aynı değil.");
        }

        return PrivacyNoticeLookup.Published(surumKimligi);
    }

    /// <inheritdoc />
    public async Task<PrivacyNoticeVersionDto?> FindVersionAsync(Guid versionId, CancellationToken cancellationToken) =>
        await context.PrivacyNoticeVersions
            .AsNoTracking()
            .Where(surum => surum.Id == versionId)
            .Select(surum => new PrivacyNoticeVersionDto(
                surum.Id,
                surum.Locale,
                surum.Title,
                surum.Body,
                surum.RecordedAt,
                surum.IsInitial))
            .SingleOrDefaultAsync(cancellationToken);
}
