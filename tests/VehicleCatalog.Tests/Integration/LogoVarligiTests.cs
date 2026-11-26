using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0026` **D16** — araca yalnız `photo` türündeki varlık bağlanır ve yayın kapısı logo türündeki
/// hazır varlığı "hazır görsel" saymaz (tuzak 6). Gerçek veritabanı; `Media` sahte okumayla.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class LogoVarligiTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Simdi = new(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0026/AC-69: Logo türündeki bir varlıkla araç medyası kaydetmesi `vehicle.media_asset_wrong_kind` döner; aracın medyası değişmez.")]
    public async Task Reddeder_LogoTuru()
    {
        FakeMediaAssetQueries medya = new();
        Guid fotograf = medya.Ekle(MediaProcessingStatus.Ready);
        Guid logo = medya.EkleLogo();
        Guid aracId = await AracEkleAsync();
        Assert.True((await YazAsync(aracId, [fotograf], medya)).IsSuccess);

        Result sonuc = await YazAsync(aracId, [fotograf, logo], medya);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Vehicle.MediaAssetWrongKind, sonuc.Error.Code);
        Assert.Equal(logo.ToString(), sonuc.Error.Parameters!["mediaAssetId"]);
        Assert.Equal([fotograf], await ListeAsync(aracId));
    }

    [Fact(DisplayName = "0026/AC-70: Medyasında yalnız logo türünde hazır varlık olan araç \"en az bir hazır görsel\" eksiğini taşır; fotoğraf eklenince eksik kalkar.")]
    public async Task Saymaz_HazirLogo_YayinKapisinda()
    {
        FakeMediaAssetQueries medya = new();
        Guid logo = medya.EkleLogo();
        Guid fotograf = medya.Ekle(MediaProcessingStatus.Ready);

        IReadOnlySet<Guid> yalnizLogo = await PublishEligibility.ReadyPhotosAsync(medya, [logo], CancellationToken.None);
        Assert.Empty(yalnizLogo);

        Vehicle vehicle = new VehicleBuilder().WithoutMedia().Build();
        Assert.Contains(PublishEligibility.Evaluate(vehicle, yalnizLogo, new HashSet<string>()), hata => hata.Code == PublishErrors.NoMedia);

        IReadOnlySet<Guid> ikisi = await PublishEligibility.ReadyPhotosAsync(medya, [logo, fotograf], CancellationToken.None);
        Assert.Equal([fotograf], ikisi);
    }

    private async Task<Guid> AracEkleAsync()
    {
        Vehicle vehicle = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"LOGO{Interlocked.Increment(ref sayac):D13}")
            .Build();

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        return vehicle.Id.Value;
    }

    private async Task<Result> YazAsync(Guid aracId, Guid[] varliklar, FakeMediaAssetQueries medya)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        UpdateVehicleMediaCommand komut = new(
            context, medya, KatalogYazicilari.Yansitma(context, medya), new FixedClock(Simdi));

        return await komut.ExecuteAsync(aracId, varliklar, CancellationToken.None);
    }

    private async Task<Guid[]> ListeAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Vehicle vehicle = await context.Vehicles
            .AsNoTracking()
            .SingleAsync(value => value.Id == new VehicleId(aracId));

        return [.. vehicle.MediaAssetIds];
    }
}
