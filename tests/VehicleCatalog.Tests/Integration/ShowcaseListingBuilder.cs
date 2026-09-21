using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

internal sealed class ShowcaseListingBuilder
{
    private Guid listingId = Guid.CreateVersion7();
    private bool isReserved;
    private int showcaseSortWeight;
    private DateTimeOffset publishedAt = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private string bodyTypeSlug = "sedan";
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

    internal ShowcaseListingBuilder WithBodyType(string slug)
    {
        bodyTypeSlug = slug;
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
        BodyTypeSlug = bodyTypeSlug,
        VehicleModelId = Guid.CreateVersion7(),
        ModelYear = modelYear,
        MileageKilometers = mileageKilometers,
        PriceAmount = priceAmount,
        PriceCurrencyCode = priceCurrencyCode,
        PriceMinorUnitExponent = priceMinorUnitExponent,
        PriceOnRequest = priceOnRequest,
        BranchId = Guid.CreateVersion7(),
        BranchNames = branchNames,
        CoverMediaUrl = "https://cdn.example.test/cover.webp",
    };
}
