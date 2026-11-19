using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using CarGallery.Modules.VehicleCatalog.Infrastructure;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0018` **D11** · **D30** — bülten satırının kaynağı. Burada sınanan şey Catalog'un payı:
/// hangi ilanın "şu an Yayında" sayıldığı ve satırın hangi alanları taşıdığı. Bültenin
/// kendisi `Notifications.Tests`'te.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class IlanOzetiSorgusuTests(ShowcaseDatabase database)
{
    [Fact(DisplayName = "0018/AC-74 (okuma yarısı): Bültendeki her araç satırı model adını abonenin dilinde, model yılını, km'yi, fiyatı, şube adını ve ilanın abonenin dilindeki mutlak bağlantısını taşır")]
    public async Task Doner_ModelKimligiYilKmFiyatSubeVeAdres()
    {
        Guid ilanId = Guid.CreateVersion7();
        Guid modelId = Guid.CreateVersion7();
        Guid subeId = Guid.CreateVersion7();
        DateTimeOffset yayin = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder()
                .WithId(ilanId)
                .WithSlug("toyota-corolla-2019-abc123")
                .WithVehicleModelId(modelId)
                .WithBranchId(subeId)
                .WithPrice(12500.750m, "KWD", 3)
                .PublishedAt(yayin)
                .Build()
        ]);

        ListingSummaryDto ozet = Assert.Single(await BulAsync(ilanId));

        Assert.Equal("toyota-corolla-2019-abc123", ozet.Slug);
        Assert.Equal(modelId, ozet.VehicleModelId);
        Assert.Equal(subeId, ozet.BranchId);
        Assert.Equal(2019, ozet.ModelYear);
        Assert.Equal(125_000, ozet.MileageKilometers);
        Assert.Equal(12500.750m, ozet.Price.Amount);
        Assert.Equal("KWD", ozet.Price.CurrencyCode);
        Assert.Equal(3, ozet.Price.MinorUnitExponent);
        Assert.Equal(yayin, ozet.PublishedAt);
    }

    [Fact(DisplayName = "0018/AC-61 (okuma yarısı): Dönem işlenirken **satılmış**, **arşivlenmiş**, **yayından kaldırılmış** ya da **rezerve** olan aday o dönemin bülteninde yer almaz (dört ayrı test)")]
    public async Task Donmez_RezerveVeVitrindeOlmayanIlan()
    {
        Guid yayinda = Guid.CreateVersion7();
        Guid rezerve = Guid.CreateVersion7();
        Guid vitrindeYok = Guid.CreateVersion7();

        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder().WithId(yayinda).WithSlug("a-2019-aaa111").Build(),
            new ShowcaseListingBuilder().WithId(rezerve).WithSlug("b-2019-bbb222").Reserved().Build(),
        ]);

        IReadOnlyList<ListingSummaryDto> sonuc = await BulAsync(yayinda, rezerve, vitrindeYok);

        Assert.Equal(yayinda, Assert.Single(sonuc).ListingId);
    }

    [Fact(DisplayName = "0018/AC-75 (okuma yarısı): Fiyatı \"sorunuz\" olan ilanın satırı tutar yerine şablon kataloğundaki \"Fiyat sorunuz\" karşılığını taşır")]
    public async Task Doner_FiyatSorunuzDurumu()
    {
        Guid ilanId = Guid.CreateVersion7();

        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder().WithId(ilanId).WithSlug("c-2019-ccc333").PriceOnRequest().Build(),
        ]);

        ListingSummaryDto ozet = Assert.Single(await BulAsync(ilanId));

        Assert.True(ozet.Price.OnRequest);
        Assert.Null(ozet.Price.Amount);
    }

    private async Task<IReadOnlyList<ListingSummaryDto>> BulAsync(params Guid[] kimlikler)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await new ListingSummaryQueries(context).FindPublishedAsync(kimlikler, CancellationToken.None);
    }
}
