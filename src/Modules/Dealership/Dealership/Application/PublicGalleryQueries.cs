using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>`0014` — ana sayfanın galeri adı ve altlığın şube bloğu (R1, R7).</summary>
internal interface IPublicGalleryQueries
{
    /// <summary>Galeri profili yoksa <c>null</c>.</summary>
    Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken);
}

internal sealed class PublicGalleryQueries(DealershipDbContext context, Locale defaultLocale) : IPublicGalleryQueries
{
    public async Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken)
    {
        string? commercialName = await context.Galleries
            .AsNoTracking()
            .Select(gallery => gallery.CommercialName)
            .SingleOrDefaultAsync(cancellationToken);

        if (commercialName is null)
        {
            return null;
        }

        Result<Locale> requested = Locale.Create(locale);
        Locale resolved = requested.IsSuccess ? requested.Value : defaultLocale;

        // AC-48 — süzgeç SORGUDA: geçici kapalı ve kapatılmış şube süreç belleğine hiç
        // inmez. Projeksiyon da sorguda (D15): e-posta, WhatsApp, koordinat ve slug okunmaz.
        var rows = await context.Branches
            .AsNoTracking()
            .Where(branch => branch.Status == BranchStatus.Active)
            .Select(branch => new { branch.Names, branch.Addresses, branch.Phone })
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
                    row.Phone.Value))
        ];

        return new PublicGalleryDto(commercialName, branches);
    }
}
