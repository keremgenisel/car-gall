using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

public sealed class ProjectionTests
{
    private static readonly DateTimeOffset Yayin = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly ListingSlug Slug =
        ListingSlug.Create("Toyota Corolla", 2019, new ListingId(Guid.CreateVersion7())).Value;

    /// <summary>Bu testlerde hiçbir opsiyonel alan public değildir; AC-12/13 ayrıca sınanır.</summary>
    private static readonly HashSet<string> PublicAlanYok = [];

    private static readonly Dictionary<string, string> SubeAdlari = new()
    {
        ["tr"] = "Merkez Şube",
        ["ar"] = "الفرع الرئيسي",
        ["en"] = "Main Branch",
    };

    [Fact(DisplayName = "0001/AC-2: Rezerve durumundaki bir ilan listede görünür ve üzerinde rezerve rozeti bulunur")]
    public void Listelenir_RezerveIlanRozetBilgisiyle()
    {
        Vehicle vehicle = new VehicleBuilder().Build();
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 3);
        listing.Publish(Adres(listing), new FixedClock(Yayin));
        listing.Reserve();

        ShowcaseListing? row = ShowcaseListingProjector.Project(vehicle, listing, Slug, SubeAdlari, "https://cdn.example.test/a.webp", PublicAlanYok);

        Assert.NotNull(row);
        Assert.True(row.IsReserved);
    }

    [Theory(DisplayName = "0001/AC-3: Satıldı, Taslak, Arşiv durumundaki ve \"lokasyon ataması bekliyor\" durumundaki ilanlar listede yer almaz")]
    [InlineData("Draft")]
    [InlineData("Sold")]
    [InlineData("Archived")]
    [InlineData("AwaitingBranchAssignment")]
    public void Listelenmez_VitrindeGorunmeyenDurumlar(string durum)
    {
        Vehicle vehicle = new VehicleBuilder().Build();
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 3);
        FixedClock clock = new(Yayin);

        switch (durum)
        {
            case "Draft":
                break;
            case "Sold":
                listing.Publish(Adres(listing), clock);
                listing.MarkSold();
                break;
            case "Archived":
                listing.Publish(Adres(listing), clock);
                listing.Archive();
                break;
            case "AwaitingBranchAssignment":
                listing.Publish(Adres(listing), clock);
                vehicle.MarkAwaitingBranchAssignment();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(durum), durum, "Bilinmeyen durum.");
        }

        ShowcaseListing? row = ShowcaseListingProjector.Project(vehicle, listing, Slug, SubeAdlari, null, PublicAlanYok);

        Assert.Null(row);
    }

    [Fact]
    public void Listelenir_YayindaIlan()
    {
        Vehicle vehicle = new VehicleBuilder().Build();
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 3);
        listing.Publish(Adres(listing), new FixedClock(Yayin));

        ShowcaseListing? row = ShowcaseListingProjector.Project(vehicle, listing, Slug, SubeAdlari, null, PublicAlanYok);

        Assert.NotNull(row);
        Assert.False(row.IsReserved);
        Assert.Equal(Yayin, row.PublishedAt);
    }

    [Fact]
    public void TutarTasimaz_FiyatSorunuzIlaninYansimasinda()
    {
        Vehicle vehicle = new VehicleBuilder().WithPrice(ListingPrice.OnRequest()).Build();
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        listing.Publish(Adres(listing), new FixedClock(Yayin));

        ShowcaseListing row = ShowcaseListingProjector.Project(vehicle, listing, Slug, SubeAdlari, null, PublicAlanYok)!;

        Assert.True(row.PriceOnRequest);
        Assert.Null(row.PriceAmount);
        Assert.Null(row.PriceCurrencyCode);
    }

    [Fact]
    public void UcOndalikTasinir_KwdFiyatliIlaninYansimasinda()
    {
        Money kwd = Money.Create(1250.500m, "KWD", 3).Value;
        Vehicle vehicle = new VehicleBuilder().WithPrice(ListingPrice.Of(kwd)).Build();
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        listing.Publish(Adres(listing), new FixedClock(Yayin));

        ShowcaseListing row = ShowcaseListingProjector.Project(vehicle, listing, Slug, SubeAdlari, null, PublicAlanYok)!;

        Assert.Equal(1250.500m, row.PriceAmount);
        Assert.Equal(3, row.PriceMinorUnitExponent);
    }
    /// <summary>
    /// `0012b` **D13** — adres artık yayının parametresi: ilk yayında bir kez yazılıyor ve
    /// bir daha değişmiyor. Bu testlerin ilgilendiği şey adres DEĞİL; yardımcı, imza
    /// değişikliğini testin gövdesine taşımamak için var.
    /// </summary>
    private static ListingSlug Adres(Listing listing) =>
        ListingSlug.Create("Toyota Corolla", 2020, listing.Id).Value;

}
