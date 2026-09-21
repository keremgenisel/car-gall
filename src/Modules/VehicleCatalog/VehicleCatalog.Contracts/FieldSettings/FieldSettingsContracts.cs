namespace CarGallery.Modules.VehicleCatalog.Contracts.FieldSettings;

/// <summary>
/// Frontend formu ve filtre çubuğunu buradan kurar (docs/architecture.md § 4).
/// Bu spec'te tek eksen kasa tipidir; alan ayarları eksen ÜRETMEZ, yalnızca o eksenin
/// görünürlük kapısıdır (R8).
/// </summary>
public sealed record FieldSettingDto(string FieldKey, bool IsOpen, bool IsPublic, bool IsFilterable);

public sealed record FieldSettingsDto(IReadOnlyList<FieldSettingDto> Fields);

public interface IFieldSettingsQueries
{
    Task<FieldSettingsDto> GetAsync(CancellationToken cancellationToken);
}
