using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

public sealed class ListingTests
{
    private static readonly DateTimeOffset IlkYayin = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Yayinlanir_TaslakDurumundan()
    {
        FixedClock clock = new(IlkYayin);
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        Result result = listing.Publish(Adres(listing), clock);

        Assert.True(result.IsSuccess);
        Assert.Equal(ListingStatus.Published, listing.Status);
        Assert.Equal(IlkYayin, listing.PublishedAt);
    }

    // Parametre tipi dışarıdan görünür olmak zorunda: test metodu public, ListingStatus ise
    // internal (Yasak Listesi — modül içi tipler public yapılmaz).
    [Theory]
    [InlineData(nameof(ListingStatus.Reserved))]
    [InlineData(nameof(ListingStatus.Sold))]
    [InlineData(nameof(ListingStatus.Archived))]
    public void Yayinlanamaz_TaslakDisindakiDurumlardan(string statusName)
    {
        FixedClock clock = new(IlkYayin);
        Listing listing = ListingIn(Enum.Parse<ListingStatus>(statusName), clock);

        Result result = listing.Publish(Adres(listing), clock);

        Assert.True(result.IsFailure);
        Assert.Equal(Listing.InvalidTransition, result.Error.Code);
    }

    [Fact]
    public void YayinTarihiDegismez_RezerveDonusunde()
    {
        FixedClock clock = new(IlkYayin);
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        listing.Publish(Adres(listing), clock);
        listing.Reserve();

        clock.Advance(TimeSpan.FromDays(30));
        listing.ReturnToShowcase();

        Assert.Equal(IlkYayin, listing.PublishedAt);
    }

    [Fact]
    public void YayinTarihiDegismez_TaslagaDonupYenidenYayinlandiginda()
    {
        FixedClock clock = new(IlkYayin);
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        listing.Publish(Adres(listing), clock);
        listing.Unpublish();

        clock.Advance(TimeSpan.FromDays(7));
        listing.Publish(Adres(listing), clock);

        Assert.Equal(IlkYayin, listing.PublishedAt);
    }

    [Fact]
    public void YenidenYayinlanamaz_SatildiktanSonra()
    {
        FixedClock clock = new(IlkYayin);
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);
        listing.Publish(Adres(listing), clock);
        listing.MarkSold();

        Result result = listing.Publish(Adres(listing), clock);

        Assert.True(result.IsFailure);
        Assert.Equal(ListingStatus.Sold, listing.Status);
    }

    [Fact]
    public void ArsivTerminaldir_HicbirGecisKabulEtmez()
    {
        FixedClock clock = new(IlkYayin);
        Listing listing = ListingIn(ListingStatus.Archived, clock);

        Assert.True(listing.Publish(Adres(listing), clock).IsFailure);
        Assert.True(listing.Reserve().IsFailure);
        Assert.True(listing.MarkSold().IsFailure);
        Assert.True(listing.Archive().IsFailure);
        Assert.Equal(ListingStatus.Archived, listing.Status);
    }

    [Fact]
    public void RezerveEdilemez_YayindaDegilse()
    {
        FixedClock clock = new(IlkYayin);
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        Result result = listing.Reserve();

        Assert.True(result.IsFailure);
        Assert.Equal(ListingStatus.Draft, listing.Status);
    }

    private static Listing ListingIn(ListingStatus status, IClock clock)
    {
        Listing listing = Listing.CreateDraft(VehicleId.New(), showcaseSortWeight: 0);

        if (status is ListingStatus.Draft)
        {
            return listing;
        }

        listing.Publish(Adres(listing), clock);

        switch (status)
        {
            case ListingStatus.Reserved:
                listing.Reserve();
                break;
            case ListingStatus.Sold:
                listing.MarkSold();
                break;
            case ListingStatus.Archived:
                listing.Archive();
                break;
            case ListingStatus.Draft:
            case ListingStatus.Published:
            default:
                break;
        }

        return listing;
    }
    /// <summary>
    /// `0012b` **D13** — adres artık yayının parametresi: ilk yayında bir kez yazılıyor ve
    /// bir daha değişmiyor. Bu testlerin ilgilendiği şey adres DEĞİL; yardımcı, imza
    /// değişikliğini testin gövdesine taşımamak için var.
    /// </summary>
    private static ListingSlug Adres(Listing listing) =>
        ListingSlug.Create("Toyota Corolla", 2020, listing.Id).Value;

}
