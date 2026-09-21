namespace CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

/// <summary>
/// Vitrinde gösterilmesine izin verilen opsiyonel alanın değeri, locale başına.
/// Buraya yazılmış olması, alanın o an açık VE public olduğunun kanıtıdır.
/// </summary>
internal sealed class ShowcaseListingOptionalValue
{
    internal required Guid ListingId { get; init; }

    internal required string FieldKey { get; init; }

    internal Dictionary<string, string> Values { get; set; } = [];
}
