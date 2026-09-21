using CarGallery.Modules.VehicleCatalog.Contracts.FieldSettings;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.FieldSettings;

internal sealed class FieldSettingsQueries(VehicleCatalogDbContext context) : IFieldSettingsQueries
{
    public async Task<FieldSettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        List<VehicleFieldSetting> settings = await context.VehicleFieldSettings
            .AsNoTracking()
            .OrderBy(setting => setting.FieldKey)
            .ToListAsync(cancellationToken);

        return new FieldSettingsDto(
        [
            .. settings.Select(setting => new FieldSettingDto(
                setting.FieldKey,
                setting.IsOpen,
                setting.IsPublic,
                setting.IsFilterable))
        ]);
    }
}
