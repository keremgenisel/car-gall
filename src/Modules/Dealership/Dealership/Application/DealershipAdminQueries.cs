using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// `0020` R1 — panelin galeri profili. Görünen ad dil → metin haritasıdır: panel her etkin
/// dil için bir alan gösterir ve eksik dili BOŞ gösterir, varsayılana düşmez (AC-1, AC-7).
/// </summary>
internal sealed record AdminGalleryDto(
    string CommercialName,
    string? TaxNumber,
    string? TradeRegistryNumber,
    IReadOnlyDictionary<string, string> DisplayNames,
    int Version);

/// <summary>`0020` R10 — panelin şube düzenleme formu; bütün alanlar ham hâliyle.</summary>
internal sealed record AdminBranchDto(
    Guid BranchId,
    IReadOnlyDictionary<string, string> Names,
    IReadOnlyDictionary<string, string> Addresses,
    string Phone,
    string? WhatsApp,
    string? Email,
    string TimeZone,
    string Slug,
    decimal? Latitude,
    decimal? Longitude,
    string Status,
    int Version);

/// <summary>
/// `0020` — panelin okumaları. Uç modülün dışına çıkmaz; `Contracts`'a girmez, çünkü başka
/// bir modülün bu veriye ihtiyacı yok (C1).
/// </summary>
internal sealed class DealershipAdminQueries(DealershipDbContext context)
{
    internal async Task<AdminGalleryDto?> GetGalleryAsync(CancellationToken cancellationToken)
    {
        Gallery? gallery = await context.Galleries.AsNoTracking().SingleOrDefaultAsync(cancellationToken);

        return gallery is null
            ? null
            : new AdminGalleryDto(
                gallery.CommercialName,
                gallery.TaxNumber,
                gallery.TradeRegistryNumber,
                gallery.DisplayNames.ToDictionary(),
                gallery.Version);
    }

    internal async Task<AdminBranchDto?> FindBranchAsync(Guid branchId, CancellationToken cancellationToken)
    {
        Branch? branch = await context.Branches
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == new BranchId(branchId), cancellationToken);

        return branch is null
            ? null
            : new AdminBranchDto(
                branch.Id.Value,
                branch.Names.ToDictionary(),
                branch.Addresses.ToDictionary(),
                branch.Phone.Value,
                branch.WhatsApp?.Value,
                branch.Email?.Value,
                branch.TimeZone.Value,
                branch.Slug.Value,
                branch.Coordinate?.Latitude,
                branch.Coordinate?.Longitude,
                // C13 — durum KODU; panel çevirir.
                branch.Status.ToString(),
                branch.Version);
    }
}
