using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

internal sealed class PublicGalleryQueries(DealershipDbContext context, Locale defaultLocale)
    : IPublicGalleryQueries, IGalleryDisplayNameQueries, IGalleryBrandingQueries
{
    /// <summary>
    /// `0026` D17 · AC-76 — ad ve logo kimliği TEK projeksiyon sorgusunda; ticari unvan, vergi ve
    /// sicil numarası süreç belleğine inmez (`0020` AC-16).
    /// </summary>
    public async Task<GalleryBrandingDto?> FindBrandingAsync(string locale, CancellationToken cancellationToken)
    {
        var row = await context.Galleries
            .AsNoTracking()
            .Select(gallery => new { gallery.DisplayNames, gallery.LogoMediaAssetId })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new GalleryBrandingDto(row.DisplayNames.Resolve(Resolve(locale), defaultLocale), row.LogoMediaAssetId);
    }

    /// <summary>
    /// `0020` D16 — Host'un `index.html`'e bastığı ad. Projeksiyon sorguda: ticari unvan,
    /// vergi ve sicil numarası süreç belleğine hiç inmez (AC-16).
    /// </summary>
    public async Task<string?> FindDisplayNameAsync(string locale, CancellationToken cancellationToken)
    {
        var row = await context.Galleries
            .AsNoTracking()
            .Select(gallery => new { gallery.DisplayNames })
            .SingleOrDefaultAsync(cancellationToken);

        return row?.DisplayNames.Resolve(Resolve(locale), defaultLocale);
    }

    public async Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken)
    {
        // `0020` D15 — ticari unvan DEĞİL görünen ad (AC-13, AC-16).
        // `0024` D7 — sosyal hesaplar aynı okumada; vergi ve sicil numarası yine okunmaz.
        var gallery = await context.Galleries
            .AsNoTracking()
            .Select(item => new { item.DisplayNames, item.SocialLinks })
            .SingleOrDefaultAsync(cancellationToken);

        if (gallery is null)
        {
            return null;
        }

        Locale resolved = Resolve(locale);

        // AC-10 — ziyaretçinin dilinde yoksa varsayılan dildeki ad. O da yoksa (kurulum adımı
        // atlanmış bir veritabanı) boş: R5a "adın yeri boş kalır" diyor, unvana düşmek değil.
        string displayName = gallery.DisplayNames.Resolve(resolved, defaultLocale) ?? string.Empty;

        // AC-48 — süzgeç SORGUDA: geçici kapalı ve kapatılmış şube süreç belleğine hiç
        // inmez. Projeksiyon da sorguda (D15): e-posta, WhatsApp, koordinat ve slug okunmaz.
        var rows = await context.Branches
            .AsNoTracking()
            .Where(branch => branch.Status == BranchStatus.Active)
            .Select(branch => new { branch.Names, branch.Addresses, branch.Phone, branch.OpeningHours })
            .ToListAsync(cancellationToken);

        List<PublicBranchDto> branches =
        [
            .. rows
                // D13 — varsayılan dildeki adın ORDINAL sırası: kültürden bağımsız ve
                // deterministik. Ziyaretçinin dilindeki ada göre sıralamak aynı şubeleri
                // her dilde başka sırada gösterirdi.
                .OrderBy(row => row.Names.Resolve(defaultLocale, defaultLocale) ?? string.Empty, StringComparer.Ordinal)
                .Select(row => new PublicBranchDto(
                    // D8 — ziyaretçinin dilinde yoksa varsayılan dildeki metin (AC-44, AC-68).
                    // Varsayılan dildeki ad ve adres zorunlu (0003 AC-6), satır boş kalmaz.
                    row.Names.Resolve(resolved, defaultLocale) ?? string.Empty,
                    row.Addresses.Resolve(resolved, defaultLocale) ?? string.Empty,
                    row.Phone.Value,
                    // `0025` D8 — programsız şubede null; dilden bağımsız.
                    row.OpeningHours?.ToDto()))
        ];

        // D3 — D1'de olmayan kodla saklanmış satır düşer; sıra D1'inkidir, girilme sırası değil.
        List<PublicSocialLinkDto> socialLinks =
        [
            .. SocialPlatform.InOrder(gallery.SocialLinks).Select(link => new PublicSocialLinkDto(link.Platform, link.Url)),
        ];

        return new PublicGalleryDto(displayName, branches, socialLinks);
    }

    private Locale Resolve(string locale)
    {
        Result<Locale> requested = Locale.Create(locale);
        return requested.IsSuccess ? requested.Value : defaultLocale;
    }
}
