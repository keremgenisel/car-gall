using CarGallery.BuildingBlocks;
using CarGallery.Modules.Content.Contracts.PrivacyNotice;
using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Domain;

namespace CarGallery.Modules.Leads.Infrastructure;

/// <summary>
/// Application'daki portun dışa bakan adaptörü. `Content`'e yalnız `Contracts` üzerinden,
/// salt okuma, tek atlama ve aynı istek içinde konuşur (docs/architecture.md § 2.A). Bu
/// sözleşmenin gerçeklemesini `Content` kaydeder; `Leads` bir varsayılan KAYDETMEZ — kap
/// `Content`'siz kurulduysa istek sessiz bir "yayında değildi"ye değil hataya düşer (AC-49).
/// </summary>
internal sealed class PrivacyNoticeSource(IPrivacyNoticeQueries queries) : IPrivacyNoticeSource
{
    public async Task<PrivacyNoticeLink> FindPublishedAsync(Locale locale, CancellationToken cancellationToken)
    {
        PrivacyNoticeLookup lookup = await queries.FindPublishedAsync(locale.Code, cancellationToken);

        return lookup.VersionId is Guid versionId
            ? PrivacyNoticeLink.Published(versionId)
            : PrivacyNoticeLink.NotPublished();
    }
}
