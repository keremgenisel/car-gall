using CarGallery.Modules.VehicleReference.Domain;
using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleReference.Application;

/// <summary>`0021` AC-6 — yönetim listesinin satırı: aktif ve pasif bütün markalar.</summary>
internal sealed record AdminBrandDto(
    Guid Id,
    string CanonicalName,
    string Slug,
    bool IsActive,
    bool ShowOnHome,
    int Version,
    IReadOnlyDictionary<string, string> DisplayNames,
    int ModelCount);

/// <summary>`0021` — markanın ayrıntısı ve bütün modelleri (pasifler dâhil).</summary>
internal sealed record AdminBrandDetailDto(AdminBrandDto Brand, IReadOnlyList<AdminModelDto> Models);

internal sealed record AdminModelDto(
    Guid Id,
    Guid BrandId,
    string CanonicalName,
    bool IsActive,
    int Version,
    IReadOnlyDictionary<string, string> DisplayNames);

/// <summary>
/// `0021` D24 — pasifleri ve "ana sayfada göster" işaretini gösteren YÖNETİM okuması. Uçları
/// Yönetici politikalı grupta; oturumsuz referans uçları bu bilgiyi hiç döndürmez (AC-55).
/// </summary>
internal sealed class ReferenceAdminQueries(VehicleReferenceDbContext context)
{
    internal async Task<IReadOnlyList<AdminBrandDto>> ListBrandsAsync(CancellationToken cancellationToken)
    {
        List<VehicleBrand> brands = await context.VehicleBrands
            .AsNoTracking()
            .Include(brand => brand.Translations)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, int> counts = await context.VehicleModels
            .AsNoTracking()
            .GroupBy(model => model.BrandId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Key, row => row.Count, cancellationToken);

        return [.. VehicleBrandQueries.Alphabetical(brands).Select(brand => Brand(brand, counts.GetValueOrDefault(brand.Id)))];
    }

    internal async Task<AdminBrandDetailDto?> FindBrandAsync(Guid id, CancellationToken cancellationToken)
    {
        VehicleBrand? brand = await context.VehicleBrands
            .AsNoTracking()
            .Include(item => item.Translations)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (brand is null)
        {
            return null;
        }

        List<VehicleModel> models = await context.VehicleModels
            .AsNoTracking()
            .Include(model => model.Translations)
            .Where(model => model.BrandId == id)
            .ToListAsync(cancellationToken);

        return new AdminBrandDetailDto(
            Brand(brand, models.Count),
            [
                .. models
                    .OrderBy(model => model.CanonicalName, StringComparer.OrdinalIgnoreCase)
                    .Select(model => new AdminModelDto(
                        model.Id,
                        model.BrandId,
                        model.CanonicalName,
                        model.IsActive,
                        model.Version,
                        model.Translations.ToDictionary(item => item.Locale, item => item.Name, StringComparer.Ordinal)))
            ]);
    }

    private static AdminBrandDto Brand(VehicleBrand brand, int modelCount) => new(
        brand.Id,
        brand.CanonicalName,
        brand.Slug,
        brand.IsActive,
        brand.ShowOnHome,
        brand.Version,
        brand.Translations.ToDictionary(item => item.Locale, item => item.Name, StringComparer.Ordinal),
        modelCount);
}
