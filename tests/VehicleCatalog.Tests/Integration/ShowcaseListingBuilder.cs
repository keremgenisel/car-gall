using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

internal sealed class ShowcaseListingBuilder
{
    private Guid listingId = Guid.CreateVersion7();
    private bool isReserved;
    private int showcaseSortWeight;
    private DateTimeOffset publishedAt = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private string? slug;
    private string bodyTypeSlug = "sedan";
    private string fuelTypeSlug = "diesel";
    private string transmissionTypeSlug = "automatic";
    private string drivetrainTypeSlug = "front-wheel-drive";
    private string trimLevelSlug = "comfort";
    private Dictionary<string, string> colorNames = new()
    {
        ["tr"] = "Beyaz",
        ["ar"] = "أبيض",
        ["en"] = "White",
    };
    private List<ShowcaseListingOptionalValue> optionalValues = [];
    private int modelYear = 2019;
    private int mileageKilometers = 125_000;
    private decimal? priceAmount = 1_250_000m;
    private string? priceCurrencyCode = "TRY";
    private int? priceMinorUnitExponent = 2;
    private bool priceOnRequest;
    private Dictionary<string, string> branchNames = new()
    {
        ["tr"] = "Merkez Şube",
        ["en"] = "Main Branch",
        ["ar"] = "الفرع الرئيسي",
    };

    internal ShowcaseListingBuilder WithId(Guid id)
    {
        listingId = id;
        return this;
    }

    internal ShowcaseListingBuilder Reserved()
    {
        isReserved = true;
        return this;
    }

    internal ShowcaseListingBuilder WithSortWeight(int weight)
    {
        showcaseSortWeight = weight;
        return this;
    }

    internal ShowcaseListingBuilder PublishedAt(DateTimeOffset moment)
    {
        publishedAt = moment;
        return this;
    }

    internal ShowcaseListingBuilder WithBodyType(string value)
    {
        bodyTypeSlug = value;
        return this;
    }

    internal ShowcaseListingBuilder WithSlug(string value)
    {
        slug = value;
        return this;
    }

    internal ShowcaseListingBuilder WithSpecification(string fuel, string gearbox, string drive, string trim)
    {
        fuelTypeSlug = fuel;
        transmissionTypeSlug = gearbox;
        drivetrainTypeSlug = drive;
        trimLevelSlug = trim;
        return this;
    }

    internal ShowcaseListingBuilder WithColorNames(Dictionary<string, string> names)
    {
        colorNames = names;
        return this;
    }

    /// <summary>Yalnız açık ve public alanlar okuma modeline girer (C4).</summary>
    internal ShowcaseListingBuilder WithOptionalValue(string fieldKey, Dictionary<string, string> values)
    {
        optionalValues.Add(new ShowcaseListingOptionalValue
        {
            ListingId = listingId,
            FieldKey = fieldKey,
            Values = values,
        });
        return this;
    }

    internal ShowcaseListingBuilder WithModelYear(int year)
    {
        modelYear = year;
        return this;
    }

    internal ShowcaseListingBuilder WithMileage(int kilometers)
    {
        mileageKilometers = kilometers;
        return this;
    }

    internal ShowcaseListingBuilder WithPrice(decimal amount, string currencyCode, int minorUnitExponent)
    {
        priceAmount = amount;
        priceCurrencyCode = currencyCode;
        priceMinorUnitExponent = minorUnitExponent;
        priceOnRequest = false;
        return this;
    }

    internal ShowcaseListingBuilder PriceOnRequest()
    {
        priceAmount = null;
        priceCurrencyCode = null;
        priceMinorUnitExponent = null;
        priceOnRequest = true;
        return this;
    }

    internal ShowcaseListingBuilder WithBranchNames(Dictionary<string, string> names)
    {
        branchNames = names;
        return this;
    }

    internal ShowcaseListing Build() => new()
    {
        ListingId = listingId,
        VehicleId = Guid.CreateVersion7(),
        IsReserved = isReserved,
        ShowcaseSortWeight = showcaseSortWeight,
        PublishedAt = publishedAt,
        // Slug tekildir (ux_showcase_listings_slug); varsayılan değer kimlikten türer,
        // yoksa çoklu seed aynı adresi paylaşır ve indeks kırılır.
        Slug = slug ?? $"toyota-corolla-2019-{listingId:N}",
        BodyTypeSlug = bodyTypeSlug,
        FuelTypeSlug = fuelTypeSlug,
        TransmissionTypeSlug = transmissionTypeSlug,
        DrivetrainTypeSlug = drivetrainTypeSlug,
        TrimLevelSlug = trimLevelSlug,
        ColorNames = colorNames,
        OptionalValues = optionalValues,
        VehicleModelId = Guid.CreateVersion7(),
        ModelYear = modelYear,
        MileageKilometers = mileageKilometers,
        PriceAmount = priceAmount,
        PriceCurrencyCode = priceCurrencyCode,
        PriceMinorUnitExponent = priceMinorUnitExponent,
        PriceOnRequest = priceOnRequest,
        BranchId = Guid.CreateVersion7(),
        BranchNames = branchNames,
        CoverAssetId = Guid.CreateVersion7(),
        CoverMediaUrl = "https://cdn.example.test/cover.webp",
    };
}
