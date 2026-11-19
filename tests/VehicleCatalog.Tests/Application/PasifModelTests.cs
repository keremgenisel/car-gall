using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// `0021` **D23** — pasif model (ya da pasif markanın modeli) yeni seçim olarak reddedilir;
/// aracın zaten kayıtlı olduğu pasif modelle kaydetmek kabul edilir. Modelin seçilebilirliği
/// `VehicleReference.Contracts` üzerinden okunur ve sahtelenir; seçilebilirliğin kendisi
/// `VehicleReference.Tests`'te gerçek PostgreSQL üzerinde ölçülür.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class PasifModelTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset An = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0021/AC-26: Pasif bir modelle kayıtlı aracın düzenleme formu markayı ve modeli seçili gösterir ve araç model değiştirilmeden kaydedilebilir.")]
    public async Task Kabul_PasifModeliyleKaydetme()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Guid model = Guid.CreateVersion7();
        Guid arac = (await Kayit(context, new SahteModelSorgusu()).ExecuteAsync(Istek("PASIFM0000000001", model), CancellationToken.None)).Value;

        // Model kayıttan SONRA pasif yapıldı; araç aynı modelle, başka bir alanı değişerek kaydediliyor.
        Result sonuc = await Duzenleme(context, new SahteModelSorgusu(pasifler: new HashSet<Guid> { model }))
            .ExecuteAsync(Guncelle(arac, "PASIFM0000000001", model) with { MileageKilometers = 47_000 }, CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        Vehicle kayit = await Oku(arac);
        Assert.Equal(model, kayit.VehicleModelId);
        Assert.Equal(47_000, kayit.MileageKilometers);
    }

    [Fact(DisplayName = "0021/AC-27: Yeni kayıtta ve modeli değiştiren düzenlemede pasif bir model sunucuda reddedilir (D23).")]
    public async Task Reddedilir_YeniKayittaPasifModel()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Guid pasif = Guid.CreateVersion7();

        Result<Guid> sonuc = await Kayit(context, new SahteModelSorgusu(pasifler: new HashSet<Guid> { pasif }))
            .ExecuteAsync(Istek("PASIFM0000000002", pasif), CancellationToken.None);

        Assert.Equal(VehicleErrors.ModelInactive, sonuc.Error.Code);
        Assert.Equal(VehicleFieldNames.VehicleModelId, sonuc.Error.Parameters!["field"]);
        Assert.False(await context.Vehicles.AsNoTracking().AnyAsync(item => item.VehicleModelId == pasif));
    }

    [Fact(DisplayName = "0021/AC-27: Yeni kayıtta ve modeli değiştiren düzenlemede pasif bir model sunucuda reddedilir (D23).")]
    public async Task Reddedilir_ModeliPasifModeleDegistirenDuzenleme()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Guid aktif = Guid.CreateVersion7();
        Guid pasif = Guid.CreateVersion7();
        Guid arac = (await Kayit(context, new SahteModelSorgusu()).ExecuteAsync(Istek("PASIFM0000000003", aktif), CancellationToken.None)).Value;

        Result sonuc = await Duzenleme(context, new SahteModelSorgusu(pasifler: new HashSet<Guid> { pasif }))
            .ExecuteAsync(Guncelle(arac, "PASIFM0000000003", pasif), CancellationToken.None);

        Assert.Equal(VehicleErrors.ModelInactive, sonuc.Error.Code);
        Assert.Equal(aktif, (await Oku(arac)).VehicleModelId);
    }

    [Fact]
    public async Task Uygular_AktifModeleDegistirenDuzenlemeyi()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Guid ilk = Guid.CreateVersion7();
        Guid yeni = Guid.CreateVersion7();
        Guid arac = (await Kayit(context, new SahteModelSorgusu()).ExecuteAsync(Istek("PASIFM0000000004", ilk), CancellationToken.None)).Value;

        Result sonuc = await Duzenleme(context, new SahteModelSorgusu())
            .ExecuteAsync(Guncelle(arac, "PASIFM0000000004", yeni), CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        Assert.Equal(yeni, (await Oku(arac)).VehicleModelId);
    }

    private async Task<Vehicle> Oku(Guid arac)
    {
        await using VehicleCatalogDbContext okuma = database.CreateContext();
        return await okuma.Vehicles.AsNoTracking().SingleAsync(item => item.Id == new VehicleId(arac));
    }

    private static CreateVehicleCommand Kayit(VehicleCatalogDbContext context, SahteModelSorgusu model) =>
        new(context, AracKaydiTests.Kurallar(model: model), new FixedClock(An));

    private static UpdateVehicleCommand Duzenleme(VehicleCatalogDbContext context, SahteModelSorgusu model)
    {
        FixedClock saat = new(An.AddHours(1));

        return new UpdateVehicleCommand(
            context,
            AracKaydiTests.Kurallar(model: model),
            KatalogYazicilari.Yansitma(context),
            KatalogYazicilari.Birim(context, saat),
            saat);
    }

    private static CreateVehicleRequest Istek(string vin, Guid model) => new(
        BranchId: Guid.CreateVersion7(),
        VehicleModelId: model,
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

    private static UpdateVehicleRequest Guncelle(Guid arac, string vin, Guid model) => new(
        VehicleId: arac,
        BranchId: Guid.CreateVersion7(),
        VehicleModelId: model,
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
