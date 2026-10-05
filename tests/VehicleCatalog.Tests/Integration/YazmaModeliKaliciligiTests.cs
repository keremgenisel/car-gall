using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Yazma modelinin kalıcılığı — bu spec'e kadar HİÇ YOKTU.
///
/// <para>`Vehicle` ve `Listing` için ne `DbSet`, ne EF yapılandırması, ne tablo vardı;
/// `ShowcaseSeeder` agregatları bellekte kurup projekte ediyor ve atıyordu. Buradaki
/// testler eşlemenin gerçekten gidip geldiğini ölçüyor — özellikle değer tiplerinin,
/// çünkü hiçbiri daha önce bir veritabanı görmedi.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class YazmaModeliKaliciligiTests(ShowcaseDatabase database)
{
    [Fact]
    public async Task GidipGelir_AracinButunDegerTipleri()
    {
        Vehicle yazilan = new VehicleBuilder().WithVin("ROUNDTRIP00000001").Build();
        yazilan.SetOptionalFieldValue(
            VehicleFieldKeys.DamageRecordAmount,
            LocalizedText.From([new KeyValuePair<Locale, string>(Locale.Create("tr").Value, "12.500 TL")]));
        yazilan.AttachMediaAsset(Guid.CreateVersion7());

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(yazilan);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();

        Vehicle okunan = await okuma.Vehicles
            .Include(vehicle => vehicle.OptionalFieldValues)
            .Include(vehicle => vehicle.PriceChanges)
            .SingleAsync(vehicle => vehicle.Id == yazilan.Id);

        Assert.Equal(yazilan.Vin.Value, okunan.Vin.Value);
        Assert.Equal(yazilan.BranchId, okunan.BranchId);
        Assert.Equal(yazilan.VehicleModelId, okunan.VehicleModelId);
        Assert.Equal(yazilan.ModelYear, okunan.ModelYear);
        Assert.Equal(yazilan.MileageKilometers, okunan.MileageKilometers);

        // Beş slug tek tek: biri yanlış kolona düşse diğerleri doğru kalır ve toplu bir
        // karşılaştırma bunu gizlerdi.
        Assert.Equal(yazilan.Specification.BodyType.Value, okunan.Specification.BodyType.Value);
        Assert.Equal(yazilan.Specification.FuelTypeSlug, okunan.Specification.FuelTypeSlug);
        Assert.Equal(yazilan.Specification.TransmissionTypeSlug, okunan.Specification.TransmissionTypeSlug);
        Assert.Equal(yazilan.Specification.DrivetrainTypeSlug, okunan.Specification.DrivetrainTypeSlug);
        Assert.Equal(yazilan.Specification.TrimLevelSlug, okunan.Specification.TrimLevelSlug);

        Assert.Equal(
            yazilan.Color.Resolve(Locale.Create("tr").Value, Locale.Create("tr").Value),
            okunan.Color.Resolve(Locale.Create("tr").Value, Locale.Create("tr").Value));

        Assert.Single(okunan.OptionalFieldValues);

        // Builder zaten bir medya varlığı ekliyor; sayı YAZILANLA karşılaştırılıyor,
        // sabitle değil — sabit, builder değiştiği gün yanlış sebeple kırılırdı.
        Assert.Equal(yazilan.MediaAssetIds.Count, okunan.MediaAssetIds.Count);
        Assert.Equal(yazilan.MediaAssetIds, okunan.MediaAssetIds);
    }

    /// <summary>
    /// AC-22'nin kalıcılık yarısı. Fiyat tek kolona çevriliyor (EF iç içe karmaşık tipi
    /// kurucu parametresine bağlayamıyor) ve asıl risk tam da burada: ondalık ÖLÇEK
    /// kaybolursa `1250,500` sessizce `1250,50` gibi okunur.
    /// </summary>
    [Fact(DisplayName = "0010a/AC-22: Kurulum para birimi KWD iken `1250,500` tutarı üç ondalıkla saklanır; yuvarlama veya kesme yapılmaz")]
    public async Task Saklar_UcOndalikla_KWD()
    {
        Money kwd = Money.Create(1250.500m, "KWD", 3).Value;
        Vehicle yazilan = new VehicleBuilder()
            .WithVin("KWD0000000000001")
            .WithPrice(ListingPrice.Of(kwd))
            .Build();

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(yazilan);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();
        Vehicle okunan = await okuma.Vehicles.SingleAsync(vehicle => vehicle.Id == yazilan.Id);

        Assert.False(okunan.Price.IsOnRequest);
        Assert.Equal("KWD", okunan.Price.Amount!.Value.CurrencyCode);
        Assert.Equal(3, okunan.Price.Amount!.Value.MinorUnitExponent);

        // `Equal` decimal'de ölçeği GÖRMEZ: 1250.5m == 1250.500m. Ölçeğin korunduğunu
        // ancak metin gösterimi söyler.
        Assert.Equal("1250.500", okunan.Price.Amount!.Value.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task GidipGelir_FiyatSorunuz()
    {
        Vehicle yazilan = new VehicleBuilder()
            .WithVin("ONREQUEST0000001")
            .WithPrice(ListingPrice.OnRequest())
            .Build();

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(yazilan);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();
        Vehicle okunan = await okuma.Vehicles.SingleAsync(vehicle => vehicle.Id == yazilan.Id);

        // P4 — "fiyat sorunuz" `0` DEĞİLDİR; tutar hiç yoktur.
        Assert.True(okunan.Price.IsOnRequest);
        Assert.Null(okunan.Price.Amount);
    }

    [Fact]
    public async Task GidipGelir_TaslakIlan()
    {
        Vehicle vehicle = new VehicleBuilder().WithVin("LISTING000000001").Build();
        Listing listing = Listing.CreateDraft(vehicle.Id, showcaseSortWeight: 42);

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(vehicle);
            yazma.Listings.Add(listing);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();
        Listing okunan = await okuma.Listings.SingleAsync(item => item.Id == listing.Id);

        Assert.Equal(ListingStatus.Draft, okunan.Status);
        Assert.Equal(42, okunan.ShowcaseSortWeight);
        Assert.Null(okunan.PublishedAt);
        Assert.Equal(vehicle.Id, okunan.VehicleId);
    }

    /// <summary>
    /// A6 — ilk fiyat da geçmişe yazılır. "Eski kayıt olduğu gibi kalır" (AC-23) ancak
    /// karşılaştırılacak bir ilk kayıt varsa ölçülebilir.
    /// </summary>
    [Fact]
    public async Task Yazar_IlkFiyati_Gecmise()
    {
        Vehicle vehicle = new VehicleBuilder().WithVin("HISTORY000000001").Build();

        await using (VehicleCatalogDbContext yazma = database.CreateContext())
        {
            yazma.Vehicles.Add(vehicle);
            await yazma.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();

        Vehicle okunan = await okuma.Vehicles
            .Include(item => item.PriceChanges)
            .SingleAsync(item => item.Id == vehicle.Id);

        Assert.Single(okunan.PriceChanges);
    }
}
