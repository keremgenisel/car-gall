using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0004` **P9** — sitemap'in ilan adresleri vitrin okuma modelinden. Uçtan uca kriterler
/// (AC-56–AC-60) `Host.Tests/SitemapTests`'te; burada sözleşmenin kendisi.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class SitemapAdresleriTests(ShowcaseDatabase database)
{
    [Fact]
    public async Task Doner_GorunurHerIlaninSlugunu_RezerveDahil_Sirali()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder().WithSlug("toyota-corolla-2019-bbb222").Build(),
            new ShowcaseListingBuilder().WithSlug("audi-a3-2020-aaa111").Reserved().Build(),
        ]);

        Assert.Equal(["audi-a3-2020-aaa111", "toyota-corolla-2019-bbb222"], await ListAsync());
    }

    [Fact]
    public async Task BosDoner_VitrindeIlanYokken()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, []);

        Assert.Empty(await ListAsync());
    }

    private async Task<IReadOnlyList<string>> ListAsync()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await new ShowcaseSitemapQueries(context).ListVisibleSlugsAsync(CancellationToken.None);
    }
}
