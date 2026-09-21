using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests;

/// <summary>Nesne kurulumu testin gövdesini boğmaz (docs/testing.md § 4).</summary>
internal sealed class VehicleBuilder
{
    private string bodyType = "sedan";
    private string fuelType = "diesel";
    private string transmission = "automatic";
    private string drivetrain = "front-wheel-drive";
    private string trimLevel = "comfort";
    private int modelYear = 2019;
    private int mileageKilometers = 125_000;
    private ListingPrice price = ListingPrice.Of(Money.Create(1_250_000m, "TRY", 2).Value);
    private Guid branchId = Guid.CreateVersion7();
    private Guid vehicleModelId = Guid.CreateVersion7();
    private int mediaCount = 1;

    internal VehicleBuilder WithBodyType(string slug)
    {
        bodyType = slug;
        return this;
    }

    internal VehicleBuilder WithModelYear(int year)
    {
        modelYear = year;
        return this;
    }

    internal VehicleBuilder WithMileage(int kilometers)
    {
        mileageKilometers = kilometers;
        return this;
    }

    internal VehicleBuilder WithPrice(ListingPrice value)
    {
        price = value;
        return this;
    }

    internal VehicleBuilder WithBranch(Guid id)
    {
        branchId = id;
        return this;
    }

    internal VehicleBuilder WithModel(Guid id)
    {
        vehicleModelId = id;
        return this;
    }

    internal VehicleBuilder WithoutMedia()
    {
        mediaCount = 0;
        return this;
    }

    internal VehicleBuilder WithSpecification(string fuel, string gearbox, string drive, string trim)
    {
        fuelType = fuel;
        transmission = gearbox;
        drivetrain = drive;
        trimLevel = trim;
        return this;
    }

    internal Vehicle Build()
    {
        VehicleSpecification specification = VehicleSpecification.Create(
            BodyTypeSlug.Create(bodyType).Value,
            fuelType,
            transmission,
            drivetrain,
            trimLevel).Value;

        LocalizedText color = LocalizedText.From(
        [
            new KeyValuePair<Locale, string>(Locale.Create("tr").Value, "Beyaz"),
            new KeyValuePair<Locale, string>(Locale.Create("ar").Value, "أبيض"),
            new KeyValuePair<Locale, string>(Locale.Create("en").Value, "White"),
        ]);

        Vehicle vehicle = Vehicle.Create(
            branchId,
            vehicleModelId,
            specification,
            color,
            modelYear,
            mileageKilometers,
            price).Value;

        for (int index = 0; index < mediaCount; index++)
        {
            vehicle.AttachMediaAsset(Guid.CreateVersion7());
        }

        return vehicle;
    }
}
