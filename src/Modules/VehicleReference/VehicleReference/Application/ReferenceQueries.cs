using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Domain;
using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleReference.Application;

internal sealed class BodyTypeQueries(VehicleReferenceDbContext context) : IBodyTypeQueries
{
    public async Task<IReadOnlyList<BodyTypeDto>> GetAllAsync(string locale, CancellationToken cancellationToken)
    {
        List<BodyType> types = await context.BodyTypes
            .AsNoTracking()
            .Include(type => type.Translations)
            .OrderBy(type => type.Slug)
            .ToListAsync(cancellationToken);

        return
        [
            .. types.Select(type => new BodyTypeDto(
                type.Slug,
                // Referans veride çeviri zorunludur; eksikse kanonik Latin ada düşülür
                // ve ham çeviri anahtarı ASLA gösterilmez (AC-29).
                type.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name
                    ?? type.CanonicalName))
        ];
    }
}

internal sealed class VehicleModelQueries(VehicleReferenceDbContext context) : IVehicleModelQueries
{
    public async Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (vehicleModelIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        List<VehicleModel> models = await context.VehicleModels
            .AsNoTracking()
            .Include(model => model.Translations)
            .Where(model => vehicleModelIds.Contains(model.Id))
            .ToListAsync(cancellationToken);

        return models.ToDictionary(
            model => model.Id,
            model => model.Translations.FirstOrDefault(translation => translation.Locale == locale)?.Name
                ?? model.CanonicalName);
    }
}
