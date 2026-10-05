using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Application;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Yönetim listesi sorgusu. Gerçek PostgreSQL kullanılıyor çünkü sınanan şey EF'in
/// ürettiği sorgu: durum eşlemesi `listings` tablosunun TAMAMINI değil, elde olan
/// araçları okuyor.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class YonetimListesiTests(ShowcaseDatabase database)
{
    [Fact]
    public async Task Esler_HerAracinKendiDurumunu()
    {
        Vehicle taslak = new VehicleBuilder().WithVin($"ADMINLIST{Guid.CreateVersion7():N}"[..32]).Build();
        Vehicle yayinda = new VehicleBuilder().WithVin($"ADMINLIST{Guid.CreateVersion7():N}"[..32]).Build();

        Listing taslakIlan = Listing.CreateDraft(taslak.Id, showcaseSortWeight: 0);
        Listing yayindaIlan = Listing.CreateDraft(yayinda.Id, showcaseSortWeight: 0);
        yayindaIlan.Publish(new FixedClock(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero)));

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.AddRange(taslak, yayinda);
            yazma.Listings.AddRange(taslakIlan, yayindaIlan);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();

        IReadOnlyList<VehicleAdminListItemDto> liste =
            await new VehicleAdminQueries(okuma, new SahteModelSorgusu()).ListAsync("tr", CancellationToken.None);

        // Durum sorgusu araçlarla SINIRLI; sınırlama yanlış yazılırsa (boş küme, yanlış
        // anahtar) her satır varsayılan `Draft`'a düşer ve bu assert kırılır.
        Assert.Equal(
            nameof(ListingStatus.Published),
            liste.Single(item => item.VehicleId == yayinda.Id.Value).ListingStatus);

        Assert.Equal(
            nameof(ListingStatus.Draft),
            liste.Single(item => item.VehicleId == taslak.Id.Value).ListingStatus);
    }

    /// <summary>Şase YÖNETİM yanıtında bulunur (AC-19); vitrinde hiç bulunmaz (AC-18).</summary>
    [Fact]
    public async Task Tasir_SaseNumarasini()
    {
        string sase = $"ADMINVIN{Guid.CreateVersion7():N}"[..32];
        Vehicle arac = new VehicleBuilder().WithVin(sase).Build();

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(arac);
            yazma.Listings.Add(Listing.CreateDraft(arac.Id, showcaseSortWeight: 0));
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();

        IReadOnlyList<VehicleAdminListItemDto> liste =
            await new VehicleAdminQueries(okuma, new SahteModelSorgusu()).ListAsync("tr", CancellationToken.None);

        Assert.Equal(sase, liste.Single(item => item.VehicleId == arac.Id.Value).Vin);
    }
}
