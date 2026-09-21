using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

public sealed class VehicleTests
{
    [Fact]
    public void KapakGorseli_SiraliListeninIlkElemanidir()
    {
        Vehicle vehicle = new VehicleBuilder().WithoutMedia().Build();
        Guid ilk = Guid.CreateVersion7();
        Guid ikinci = Guid.CreateVersion7();

        vehicle.AttachMediaAsset(ilk);
        vehicle.AttachMediaAsset(ikinci);

        Assert.Equal(ilk, vehicle.CoverAssetId);
    }

    [Fact]
    public void Yayinlanamaz_MedyaVarligiYoksa()
    {
        Vehicle vehicle = new VehicleBuilder().WithoutMedia().Build();

        Result result = vehicle.EnsurePublishable();

        Assert.True(result.IsFailure);
        Assert.Equal(Vehicle.MediaRequired, result.Error.Code);
    }

    [Fact]
    public void LokasyonAtamasiBekler_SubeKapandiginda()
    {
        Vehicle vehicle = new VehicleBuilder().Build();

        vehicle.MarkAwaitingBranchAssignment();

        Assert.True(vehicle.AwaitingBranchAssignment);
    }

    [Fact]
    public void BeklemeBiter_YeniSubeAtandiginda()
    {
        Vehicle vehicle = new VehicleBuilder().Build();
        vehicle.MarkAwaitingBranchAssignment();
        Guid yeniSube = Guid.CreateVersion7();

        vehicle.AssignBranch(yeniSube);

        Assert.False(vehicle.AwaitingBranchAssignment);
        Assert.Equal(yeniSube, vehicle.BranchId);
    }

    [Fact]
    public void Olusturulamaz_NegatifKilometreyle()
    {
        Result<Vehicle> result = Vehicle.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            VehicleSpecification.Create(
                BodyTypeSlug.Create("suv").Value,
                "diesel",
                "automatic",
                "front-wheel-drive",
                "base").Value,
            LocalizedText.Empty,
            2020,
            -1,
            ListingPrice.OnRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(Vehicle.NegativeMileage, result.Error.Code);
    }

    [Theory]
    [InlineData("SUV")]
    [InlineData("station wagon")]
    [InlineData("-sedan")]
    [InlineData("")]
    public void Reddedilir_KanonikOlmayanKasaTipiSlugu(string slug)
    {
        Result<BodyTypeSlug> result = BodyTypeSlug.Create(slug);

        Assert.True(result.IsFailure);
        Assert.Equal(BodyTypeSlug.InvalidSlug, result.Error.Code);
    }
}
