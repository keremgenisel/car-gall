using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;

namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// `0019` — KVKK aydınlatma metni `Content`'e aittir; `Leads` talep anındaki yayın durumunu
/// KENDİ tek atlamasıyla sorar (C1, D8). Okunamayan ya da tutarsız durumda İSTİSNA atar ve
/// çağıran onu "yayında değildi"ye çevirmez: yanlış bir gerçeği kalıcı yazmaktansa talep
/// hiç yazılmaz (R10, D9, Bilinen tuzak 3).
/// </summary>
internal interface IPrivacyNoticeSource
{
    Task<PrivacyNoticeLink> FindPublishedAsync(Locale locale, CancellationToken cancellationToken);
}
