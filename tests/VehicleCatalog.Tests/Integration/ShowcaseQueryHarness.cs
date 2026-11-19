using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>`0016b` — adresi verilen vitrin satırına hazır bir galeri yuvası ekler.</summary>
    internal static async Task SeedReadyImageAsync(ShowcaseDatabase database, string slug)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Guid ilan = await context.ShowcaseListings.Where(row => row.Slug == slug).Select(row => row.ListingId).SingleAsync();
        Guid varlik = Guid.CreateVersion7();

        context.ShowcaseListingMedia.Add(new ShowcaseListingMedia
        {
            ListingId = ilan,
            Position = 0,
            MediaAssetId = varlik,
            Status = ShowcaseMediaStatus.Ready,
            Variants =
            [
                new ShowcaseMediaVariant("thumbnail", $"https://cdn.example.test/{varlik:N}/thumbnail.webp", 400, 300),
                new ShowcaseMediaVariant("display", $"https://cdn.example.test/{varlik:N}/display.webp", 800, 600),
                new ShowcaseMediaVariant("large", $"https://cdn.example.test/{varlik:N}/large.webp", 1200, 900),
            ],
        });
        await context.SaveChangesAsync();
    }

    internal static async Task<PagedResult<ShowcaseListingDto>> QueryAsync(
        ShowcaseDatabase database,
        int page = 1,
        IReadOnlyList<string>? bodyTypes = null,
        string locale = "tr",
        IVehicleModelNameResolver? modelNames = null)
    {
        return await QueryAsync(database, new ShowcaseListingFilter(bodyTypes ?? [], page, locale), modelNames);
    }

    /// <summary>`0015` — bütün eksenler ve sıralama.</summary>
    internal static async Task<PagedResult<ShowcaseListingDto>> QueryAsync(
        ShowcaseDatabase database,
        ShowcaseListingFilter filter,
        IVehicleModelNameResolver? modelNames = null,
        IVehicleBrandQueries? brands = null)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        ShowcaseListingQueries queries = new(
            context,
            modelNames ?? new FakeModelNameResolver(),
            new FakeBodyTypeCatalog(),
            new FakeReferenceTermCatalog(),
            brands ?? new FakeBrandQueries(),
            Locales);

        return await queries.GetAsync(filter, CancellationToken.None);
    }
}
