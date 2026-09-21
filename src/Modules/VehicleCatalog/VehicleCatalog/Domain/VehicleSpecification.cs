using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Aracın referans veriye bağlanan çekirdek nitelikleri (docs/domain.md).
/// Değerler SLUG olarak taşınır, Guid olarak değil: kasa tipinde (0001) kurulan desen budur,
/// slug kararlıdır ve okuma modelinde insan tarafından okunabilir kalır. E8 "soft Guid"
/// demişti; slug aynı gevşek bağı kuruyor ve bir çözümleme adımını ortadan kaldırıyor.
/// </summary>
internal readonly record struct VehicleSpecification
{
    internal const string InvalidSlug = "vehicle_specification.invalid_slug";

    private VehicleSpecification(
        BodyTypeSlug bodyType,
        string fuelTypeSlug,
        string transmissionTypeSlug,
        string drivetrainTypeSlug,
        string trimLevelSlug)
    {
        BodyType = bodyType;
        FuelTypeSlug = fuelTypeSlug;
        TransmissionTypeSlug = transmissionTypeSlug;
        DrivetrainTypeSlug = drivetrainTypeSlug;
        TrimLevelSlug = trimLevelSlug;
    }

    internal BodyTypeSlug BodyType { get; }

    internal string FuelTypeSlug { get; }

    internal string TransmissionTypeSlug { get; }

    internal string DrivetrainTypeSlug { get; }

    internal string TrimLevelSlug { get; }

    internal static Result<VehicleSpecification> Create(
        BodyTypeSlug bodyType,
        string fuelTypeSlug,
        string transmissionTypeSlug,
        string drivetrainTypeSlug,
        string trimLevelSlug)
    {
        // A2: çekirdek alanlar yayındaki her araçta DOLUDUR; boş slug kabul edilmez.
        foreach (string slug in new[] { fuelTypeSlug, transmissionTypeSlug, drivetrainTypeSlug, trimLevelSlug })
        {
            if (!BodyTypeSlug.TryParse(slug, out _))
            {
                return Result.Failure<VehicleSpecification>(new Error(InvalidSlug));
            }
        }

        return Result.Success(new VehicleSpecification(
            bodyType,
            fuelTypeSlug,
            transmissionTypeSlug,
            drivetrainTypeSlug,
            trimLevelSlug));
    }
}
