using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using CarGallery.Modules.VehicleCatalog.Tests.Application;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>`0015` — daraltma eksenleri (R1–R4, R15, R16), sorgu tarafında.</summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class VitrinEksenleriTests(ShowcaseDatabase database)
{
    private static int sayac;

    [Fact(DisplayName = "0015/AC-1: 3 diesel ve 2 petrol görünür ilan varken ?fuel=diesel yalnız 3 dizel ilanı listeler; toplam sayı 3'tür")]
    public async Task Daraltir_Yakit()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, [.. Yakit("diesel", 3), .. Yakit("petrol", 2)]);

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with { FuelTypes = ["diesel"] });

        Assert.Equal(3, sayfa.TotalCount);
        Assert.Equal(3, sayfa.Items.Count);
    }

    [Fact(DisplayName = "0015/AC-2: Aynı veriyle ?fuel=diesel&fuel=petrol 5 ilanı listeler (eksen içinde veya)")]
    public async Task Birlestirir_EksenIcindeVeya()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, [.. Yakit("diesel", 3), .. Yakit("petrol", 2), .. Yakit("hybrid", 1)]);

        Assert.Equal(5, (await SorAsync(Filtre() with { FuelTypes = ["diesel", "petrol"] })).TotalCount);
    }

    [Fact(DisplayName = "0015/AC-3: 2 manual ve 4 automatic görünür ilan varken ?transmission=manual yalnız 2 ilanı listeler")]
    public async Task Daraltir_Vites()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, [.. Vites("manual", 2), .. Vites("automatic", 4)]);

        Assert.Equal(2, (await SorAsync(Filtre() with { TransmissionTypes = ["manual"] })).TotalCount);
    }

    [Fact(DisplayName = "0015/AC-4: Model yılları 2017–2021 olan beş ilan varken ?yearMin=2018&yearMax=2020 2018, 2019 ve 2020'yi listeler; sınırlar dahildir")]
    public async Task Daraltir_YilAraligi_SinirlarDahil()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, new[] { 2017, 2018, 2019, 2020, 2021 }.Select(yil => new ShowcaseListingBuilder().WithModelYear(yil).Build()));

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with { Year = new("2018", "2020") });

        Assert.Equal([2018, 2019, 2020], sayfa.Items.Select(item => item.ModelYear).Order());
    }

    [Fact(DisplayName = "0015/AC-5: Kilometreleri 49.999, 50.000 ve 50.001 olan üç ilan varken ?mileageMax=50000 ilk ikisini listeler")]
    public async Task Daraltir_KilometreUstSiniri_Dahil()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, new[] { 49_999, 50_000, 50_001 }.Select(km => new ShowcaseListingBuilder().WithMileage(km).Build()));

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with { Mileage = new(null, "50000") });

        Assert.Equal([49_999, 50_000], sayfa.Items.Select(item => item.MileageKilometers).Order());
    }

    [Fact(DisplayName = "0015/AC-6: Fiyatları 499.999, 500.000, 1.000.000 ve 1.000.001 olan dört ilan varken ?priceMin=500000&priceMax=1000000 ortadaki ikisini listeler")]
    public async Task Daraltir_FiyatAraligi_SinirlarDahil()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, Fiyatlar(499_999m, 500_000m, 1_000_000m, 1_000_001m));

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with { Price = new("500000", "1000000") });

        Assert.Equal([500_000m, 1_000_000m], sayfa.Items.Select(item => item.Price.Amount!.Value).Order());
    }

    [Fact(DisplayName = "0015/AC-7: Tek sınır geçerlidir: ?yearMin=2020 yalnız 2020 ve sonrasını, ?priceMax=500000 yalnız 500.000 ve altını listeler")]
    public async Task Uygular_TekSinir()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(
            database,
            [
                new ShowcaseListingBuilder().WithModelYear(2019).WithPrice(400_000m, "TRY", 2).Build(),
                new ShowcaseListingBuilder().WithModelYear(2020).WithPrice(500_000m, "TRY", 2).Build(),
                new ShowcaseListingBuilder().WithModelYear(2022).WithPrice(600_000m, "TRY", 2).Build(),
            ]);

        Assert.Equal([2020, 2022], (await SorAsync(Filtre() with { Year = new("2020", null) })).Items.Select(item => item.ModelYear).Order());
        Assert.Equal([400_000m, 500_000m], (await SorAsync(Filtre() with { Price = new(null, "500000") })).Items.Select(item => item.Price.Amount!.Value).Order());
    }

    [Fact(DisplayName = "0015/AC-8: Eksenler ve ile birleşir: ?bodyType=suv&fuel=diesel&yearMin=2019 yalnız üç koşulun hepsini sağlayan ilanları listeler")]
    public async Task Birlestirir_EksenlerArasiVe()
    {
        await database.ResetAsync();
        ShowcaseListing hepsi = Ilan("suv", "diesel", 2020);
        await ShowcaseQueryHarness.SeedAsync(
            database,
            [hepsi, Ilan("sedan", "diesel", 2020), Ilan("suv", "petrol", 2020), Ilan("suv", "diesel", 2018)]);

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(
            new ShowcaseListingFilter(["suv"], 1, "tr") { FuelTypes = ["diesel"], Year = new("2019", null) });

        Assert.Equal([hepsi.ListingId], sayfa.Items.Select(item => item.ListingId));
    }

    [Fact(DisplayName = "0015/AC-10: \"Fiyat sorunuz\" bir ilan varken ?priceMax=1000000 ve ayrıca yalnız ?priceMin=1 o ilanı listelemez")]
    public async Task Listelemez_FiyatSorunuz_FiyatSiniriVarken()
    {
        await database.ResetAsync();
        ShowcaseListing sorunuz = new ShowcaseListingBuilder().PriceOnRequest().Build();
        await ShowcaseQueryHarness.SeedAsync(database, [sorunuz, .. Fiyatlar(500_000m)]);

        Assert.DoesNotContain((await SorAsync(Filtre() with { Price = new(null, "1000000") })).Items, item => item.ListingId == sorunuz.ListingId);
        Assert.DoesNotContain((await SorAsync(Filtre() with { Price = new("1", null) })).Items, item => item.ListingId == sorunuz.ListingId);
    }

    [Fact(DisplayName = "0015/AC-11: Fiyat sınırı verilmemişken (?fuel=diesel) \"fiyat sorunuz\" dizel ilan listede görünür")]
    public async Task Listeler_FiyatSorunuz_FiyatSiniriYokken()
    {
        await database.ResetAsync();
        ShowcaseListing sorunuz = new ShowcaseListingBuilder().WithSpecification("diesel", "automatic", "front-wheel-drive", "comfort").PriceOnRequest().Build();
        await ShowcaseQueryHarness.SeedAsync(database, [sorunuz]);

        Assert.Contains((await SorAsync(Filtre() with { FuelTypes = ["diesel"] })).Items, item => item.ListingId == sorunuz.ListingId);
    }

    [Fact(DisplayName = "0015/AC-12: ?yearMin=2021&yearMax=2018 (alt > üst) yıl eksenini yok sayar, diğer eksenleri uygular")]
    public async Task YokSayar_AltUsttenBuyukYilAraligi()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, [Ilan("suv", "diesel", 2015), Ilan("suv", "petrol", 2019), Ilan("suv", "diesel", 2023)]);

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with { FuelTypes = ["diesel"], Year = new("2021", "2018") });

        // Yıl ekseni uygulansaydı (veya sınırlar yer değiştirseydi) liste boş/tek olurdu.
        Assert.Equal([2015, 2023], sayfa.Items.Select(item => item.ModelYear).Order());
    }

    [Theory(DisplayName = "0015/AC-13: ?mileageMax=abc, ?mileageMax=-5 ve ?priceMax=500000.5 ilgili ekseni yok sayar")]
    [InlineData("abc", null)]
    [InlineData("-5", null)]
    [InlineData(null, "500000.5")]
    public async Task YokSayar_TamSayiOlmayanVeyaNegatifSinir(string? kilometre, string? fiyat)
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(
            database,
            [
                new ShowcaseListingBuilder().WithMileage(10_000).WithPrice(100_000m, "TRY", 2).Build(),
                new ShowcaseListingBuilder().WithMileage(900_000).WithPrice(9_000_000m, "TRY", 2).Build(),
            ]);

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with
        {
            Mileage = new(null, kilometre),
            Price = new(null, fiyat),
        });

        Assert.Equal(2, sayfa.TotalCount);
    }

    [Fact(DisplayName = "0015/AC-16: ?fuel=kerosene (tanınmayan slug) sunucu tarafında yok sayılır ve liste filtresiz döner")]
    public async Task YokSayar_TaninmayanYakit()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, [.. Yakit("diesel", 2), .. Yakit("petrol", 1)]);

        Assert.Equal(3, (await SorAsync(Filtre() with { FuelTypes = ["kerosene"] })).TotalCount);
    }

    [Fact(DisplayName = "0015/AC-17: Eşleştirme tam ve harfe duyarlıdır: ?fuel=Diesel tanınmaz ve yok sayılır")]
    public async Task YokSayar_HarfeDuyarli()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, [.. Yakit("diesel", 2), .. Yakit("petrol", 1)]);

        // "Diesel" tanınsaydı 2 dönerdi; tanınmadığı için eksen yok sayılır, 3 döner.
        Assert.Equal(3, (await SorAsync(Filtre() with { FuelTypes = ["Diesel"] })).TotalCount);
    }

    [Fact(DisplayName = "0015/AC-42: Taslak, Satıldı ve Arşiv ilanlar hiçbir eksen ve sıralama birleşiminde listelenmez")]
    public async Task Listelemez_GorunmeyenDurumlar_EksenVeSiralamaIle()
    {
        await database.ResetAsync();
        FakeMediaAssetQueries medya = new();
        Guid yayinda = await IlanAsync(medya, ilan => Result.Success());
        await IlanAsync(medya, ilan => ilan.MarkSold());
        await IlanAsync(medya, ilan => ilan.Archive());
        await IlanAsync(medya, ilan => ilan.Unpublish());

        foreach (string? siralama in new[] { null, "newest", "price-asc", "price-desc", "mileage-asc", "year-desc", "year-asc" })
        {
            PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with
            {
                FuelTypes = ["diesel", "petrol", "hybrid", "electric"],
                TransmissionTypes = ["manual", "automatic"],
                Year = new("1900", "2100"),
                Mileage = new("0", "9999999"),
                Price = new("0", "999999999"),
                Sort = siralama,
            });

            Assert.Equal([yayinda], sayfa.Items.Select(item => item.ListingId));
        }
    }

    private Task<PagedResult<ShowcaseListingDto>> SorAsync(ShowcaseListingFilter filtre) =>
        ShowcaseQueryHarness.QueryAsync(database, filtre);

    private static ShowcaseListingFilter Filtre() => new([], 1, "tr");

    private static IEnumerable<ShowcaseListing> Yakit(string yakit, int adet) =>
        Enumerable.Range(0, adet).Select(_ => new ShowcaseListingBuilder().WithSpecification(yakit, "automatic", "front-wheel-drive", "comfort").Build());

    private static IEnumerable<ShowcaseListing> Vites(string vites, int adet) =>
        Enumerable.Range(0, adet).Select(_ => new ShowcaseListingBuilder().WithSpecification("diesel", vites, "front-wheel-drive", "comfort").Build());

    private static IEnumerable<ShowcaseListing> Fiyatlar(params decimal[] tutarlar) =>
        tutarlar.Select(tutar => new ShowcaseListingBuilder().WithPrice(tutar, "TRY", 2).Build());

    private static ShowcaseListing Ilan(string kasa, string yakit, int yil) =>
        new ShowcaseListingBuilder().WithBodyType(kasa).WithSpecification(yakit, "automatic", "front-wheel-drive", "comfort").WithModelYear(yil).Build();

    /// <summary>Gerçek yazma yolu: agregat, durum geçişi ve yansıtıcı (`0014` AC-23 deseni).</summary>
    private async Task<Guid> IlanAsync(FakeMediaAssetQueries medya, Func<Listing, Result> gecis)
    {
        Vehicle arac = new VehicleBuilder().WithoutMedia().WithVin($"EKSEN{Interlocked.Increment(ref sayac):D12}").Build();
        arac.AttachMediaAsset(medya.Ekle(MediaProcessingStatus.Ready));
        Listing ilan = Listing.CreateDraft(arac.Id, Listing.NormalWeight);
        Assert.True(ilan.Publish(ListingSlug.Create("Toyota Corolla", arac.ModelYear, ilan.Id).Value, new FixedClock(DateTimeOffset.UtcNow)).IsSuccess);

        await using (VehicleCatalogDbContext kayit = database.CreateContext())
        {
            kayit.Vehicles.Add(arac);
            kayit.Listings.Add(ilan);
            await kayit.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle okunan = await context.Vehicles.SingleAsync(item => item.Id == arac.Id);
        Listing okunanIlan = await context.Listings.SingleAsync(item => item.VehicleId == arac.Id);
        await KatalogYazicilari.Yansitma(context, medya).ProjectAsync(okunan, okunanIlan, read: null, CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.True(gecis(okunanIlan).IsSuccess);
        await KatalogYazicilari.Yansitma(context, medya).ProjectAsync(okunan, okunanIlan, read: null, CancellationToken.None);
        await context.SaveChangesAsync();

        return okunanIlan.Id.Value;
    }
}
