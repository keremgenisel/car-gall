using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

internal static class ShowcaseQueryHarness
{
    internal static ShowcaseLocaleOptions Locales { get; } = new(
        Locale.Create("tr").Value,
        [Locale.Create("tr").Value, Locale.Create("ar").Value, Locale.Create("en").Value]);

    internal static async Task SeedAsync(ShowcaseDatabase database, IEnumerable<ShowcaseListing> listings)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        context.ShowcaseListings.AddRange(listings);
        await context.SaveChangesAsync();
    }

    internal static async Task<PagedResult<ShowcaseListingDto>> QueryAsync(
        ShowcaseDatabase database,
        int page = 1,
        IReadOnlyList<string>? bodyTypes = null,
        string locale = "tr",
        IVehicleModelNameResolver? modelNames = null)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        ShowcaseListingQueries queries = new(context, modelNames ?? new FakeModelNameResolver(), Locales);

        return await queries.GetAsync(
            new ShowcaseListingFilter(bodyTypes ?? [], page, locale),
            CancellationToken.None);
    }
}
