namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// Doğrulama hatalarının işaret ettiği alan adları. Uç bunları `errorCode` parametresi
/// olarak döner ve panel hangi alanı kırmızıya boyayacağını buradan bilir (AC-42).
///
/// <para>Sabit olarak tutuluyor çünkü aynı adlar üç yerde geçiyor: doğrulama, sözleşme ve
/// arayüz. Üçü ayrı ayrı yazılsaydı biri değiştiğinde diğer ikisi sessizce bayatlardı.</para>
/// </summary>
internal static class VehicleFieldNames
{
    internal const string BranchId = "branchId";
    internal const string VehicleModelId = "vehicleModelId";
    internal const string BodyType = "bodyType";
    internal const string FuelType = "fuelType";
    internal const string TransmissionType = "transmissionType";
    internal const string DrivetrainType = "drivetrainType";
    internal const string TrimLevel = "trimLevel";
    internal const string Color = "color";
    internal const string ModelYear = "modelYear";
    internal const string Mileage = "mileageKilometers";
    internal const string Price = "price";
    internal const string Vin = "vin";
}
