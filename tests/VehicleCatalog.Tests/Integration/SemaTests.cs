using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class SemaTests(ShowcaseDatabase database)
{
    [Fact]
    public async Task UcOndalikKorunur_KwdTutariVeritabaninaYazilipOkundugunda()
    {
        await database.ResetAsync();
        Guid id = Guid.CreateVersion7();

        await using (VehicleCatalogDbContext write = database.CreateContext())
        {
            write.ShowcaseListings.Add(new ShowcaseListingBuilder()
                .WithId(id)
                .WithPrice(1250.500m, "KWD", 3)
                .Build());
            await write.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext read = database.CreateContext();
        ShowcaseListing? stored = await read.ShowcaseListings
            .AsNoTracking()
            .SingleOrDefaultAsync(listing => listing.ListingId == id);

        Assert.NotNull(stored);
        Assert.Equal(1250.500m, stored.PriceAmount);
        Assert.Equal(3, stored.PriceMinorUnitExponent);
    }

    [Fact]
    public async Task JsonbOlarakSaklanir_LocaleBasinaSubeAdi()
    {
        await database.ResetAsync();
        Guid id = Guid.CreateVersion7();
        Dictionary<string, string> names = new()
        {
            ["tr"] = "Kadıköy Şubesi",
            ["ar"] = "فرع كاديكوي",
            ["en"] = "Kadikoy Branch",
        };

        await using (VehicleCatalogDbContext write = database.CreateContext())
        {
            write.ShowcaseListings.Add(new ShowcaseListingBuilder().WithId(id).WithBranchNames(names).Build());
            await write.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext read = database.CreateContext();
        ShowcaseListing stored = await read.ShowcaseListings.AsNoTracking()
            .SingleAsync(listing => listing.ListingId == id);

        Assert.Equal("Kadıköy Şubesi", stored.BranchNames["tr"]);
        Assert.Equal("فرع كاديكوي", stored.BranchNames["ar"]);
    }

    [Fact]
    public async Task KendiSemasindaYasar_VitrinOkumaModeli()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        int tableCount = await context.Database
            .SqlQuery<int>($"""
                SELECT count(*)::int AS "Value" FROM information_schema.tables
                WHERE table_schema = 'vehicle_catalog' AND table_name = 'showcase_listings'
                """)
            .SingleAsync();

        Assert.Equal(1, tableCount);
    }
}
