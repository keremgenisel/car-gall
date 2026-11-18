using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// `0020` **D14** — kapatılmış şubeye araç bağlanamaz. Şubenin durumu `Dealership.Contracts`
/// üzerinden okunur ve sahtelenir (docs/testing.md § 1); sınanan şey okunan durumla ne
/// yapıldığı. Uç sözleşmesi (`422`, alan kodu) `Host.Tests`'te ölçülür.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class KapatilmisSubeTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset An = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0020/AC-55: Kapatılmış bir şubeyle yeni araç kaydı bir alan hata koduyla reddedilir ve araç oluşmaz.")]
    public async Task Reddedilir_KapatilmisSubeyleYeniKayit()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Guid kapali = Guid.CreateVersion7();

        Result<Guid> sonuc = await Kayit(context, new SahteSubeSorgusu(kapatilmis: new HashSet<Guid> { kapali }))
            .ExecuteAsync(Istek("KAPALI0000000001", kapali), CancellationToken.None);

        Assert.Equal(VehicleErrors.BranchClosed, sonuc.Error.Code);
        Assert.Equal(VehicleFieldNames.BranchId, sonuc.Error.Parameters!["field"]);
        Assert.False(await context.Vehicles.AsNoTracking().AnyAsync(item => item.BranchId == kapali));
    }

    [Fact(DisplayName = "0020/AC-56: Bir aracın şubesini kapatılmış bir şubeye değiştiren kaydetme reddedilir.")]
    public async Task Reddedilir_SubeyiKapatilmisSubeyeDegistirenKaydetme()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Guid acik = Guid.CreateVersion7();
        Guid kapali = Guid.CreateVersion7();
        Guid arac = (await Kayit(context, new SahteSubeSorgusu()).ExecuteAsync(Istek("KAPALI0000000002", acik), CancellationToken.None)).Value;

        Result sonuc = await Duzenleme(context, new SahteSubeSorgusu(kapatilmis: new HashSet<Guid> { kapali }))
            .ExecuteAsync(Guncelle(arac, "KAPALI0000000002", kapali), CancellationToken.None);

        Assert.Equal(VehicleErrors.BranchClosed, sonuc.Error.Code);
        Assert.Equal(VehicleFieldNames.BranchId, sonuc.Error.Parameters!["field"]);
        Assert.Equal(acik, (await Oku(arac)).BranchId);
    }

    [Fact(DisplayName = "0020/AC-57: Şubesi kapatılmış bir aracın şubesine dokunmadan yapılan kaydetme kabul edilir.")]
    public async Task Kabul_SubesiKapatilmisAracinSubesineDokunmadanKaydetme()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Guid sube = Guid.CreateVersion7();
        Guid arac = (await Kayit(context, new SahteSubeSorgusu()).ExecuteAsync(Istek("KAPALI0000000003", sube), CancellationToken.None)).Value;

        // Şube kayıttan SONRA kapatıldı; araç aynı şubeyle, başka bir alanı değişerek kaydediliyor.
        Result sonuc = await Duzenleme(context, new SahteSubeSorgusu(kapatilmis: new HashSet<Guid> { sube }))
            .ExecuteAsync(Guncelle(arac, "KAPALI0000000003", sube) with { MileageKilometers = 46_000 }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        Assert.Equal(46_000, (await Oku(arac)).MileageKilometers);
    }

    private async Task<Vehicle> Oku(Guid arac)
    {
        await using VehicleCatalogDbContext okuma = database.CreateContext();
        return await okuma.Vehicles.AsNoTracking().SingleAsync(item => item.Id == new VehicleId(arac));
    }

    private static CreateVehicleCommand Kayit(VehicleCatalogDbContext context, SahteSubeSorgusu sube) =>
        new(context, AracKaydiTests.Kurallar(sube), new FixedClock(An));

    private static UpdateVehicleCommand Duzenleme(VehicleCatalogDbContext context, SahteSubeSorgusu sube)
    {
        FixedClock saat = new(An.AddHours(1));

        return new UpdateVehicleCommand(
            context,
            AracKaydiTests.Kurallar(sube),
            KatalogYazicilari.Yansitma(context),
            KatalogYazicilari.Birim(context, saat),
            saat);
    }

    private static CreateVehicleRequest Istek(string vin, Guid sube) => new(
        BranchId: sube,
        VehicleModelId: Guid.CreateVersion7(),
        BodyType: "suv",
        FuelType: "diesel",
        TransmissionType: "automatic",
        DrivetrainType: "front-wheel-drive",
        TrimLevel: "comfort",
        Color: new Dictionary<string, string> { ["tr"] = "Beyaz" },
        ModelYear: 2019,
        MileageKilometers: 45_000,
        PriceAmount: 750_000m,
        PriceOnRequest: false,
        Vin: vin);

    private static UpdateVehicleRequest Guncelle(Guid arac, string vin, Guid sube) => new(
        VehicleId: arac,
        BranchId: sube,
        VehicleModelId: Guid.CreateVersion7(),
        BodyType: "suv",
        FuelType: "diesel",
        TransmissionType: "automatic",
        DrivetrainType: "front-wheel-drive",
        TrimLevel: "comfort",
        Color: new Dictionary<string, string> { ["tr"] = "Beyaz" },
        ModelYear: 2019,
        MileageKilometers: 45_000,
        PriceAmount: 750_000m,
        PriceOnRequest: false,
        Vin: vin,
        ListingTitle: new Dictionary<string, string>(),
        ListingDescription: new Dictionary<string, string>(),
        OptionalValues: []);
}
