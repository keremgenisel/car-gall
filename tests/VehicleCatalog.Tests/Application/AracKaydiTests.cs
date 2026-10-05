using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// Araç kaydı komutu — C17 gereği agregatın ÜZERİNE kurulu bir katman; `Vehicle.Create`
/// ve `Listing.CreateDraft` olduğu gibi çağrılıyor.
///
/// <para>Gerçek PostgreSQL kullanılıyor çünkü AC-16'nın kapısı bir veritabanı kısıtı:
/// tekillik sahte bir depoyla sınanamaz.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class AracKaydiTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset An = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0010a/AC-10: Yeni oluşturulan araç kaydının ilanı taslak durumundadır ve varsayılan vitrin sıralama ağırlığıyla başlar")]
    public async Task Baslar_TaslakVeVarsayilanAgirlikla()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context).ExecuteAsync(Istek("AC10000000000001"), CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);

        Listing listing = await context.Listings.AsNoTracking()
            .SingleAsync(item => item.VehicleId == new VehicleId(sonuc.Value));

        Assert.Equal(ListingStatus.Draft, listing.Status);
        Assert.Equal(CreateVehicleCommand.DefaultShowcaseSortWeight, listing.ShowcaseSortWeight);
        Assert.Null(listing.PublishedAt);
    }

    [Fact(DisplayName = "0010a/AC-3: Şubesi seçilmemiş bir araç kaydedilemez")]
    public async Task Kaydedilemez_SubesizArac()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context)
            .ExecuteAsync(Istek("AC03000000000001") with { BranchId = Guid.Empty }, CancellationToken.None);

        Assert.Equal(VehicleErrors.BranchRequired, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0010a/AC-4: Var olmayan bir şube kimliğiyle araç kaydedilemez")]
    public async Task Kaydedilemez_VarOlmayanSubeyle()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context, sube: new SahteSubeSorgusu(bulunur: false))
            .ExecuteAsync(Istek("AC04000000000001"), CancellationToken.None);

        Assert.Equal(VehicleErrors.BranchNotFound, sonuc.Error.Code);
    }

    /// <summary>
    /// Altı alan AYRI AYRI: hata hangi alanın düştüğünü söylemeli, yoksa personel hangisini
    /// düzelteceğini bilemez (AC-42).
    /// </summary>
    [Theory(DisplayName = "0010a/AC-5: Model, kasa tipi, donanım paketi, yakıt, vites ve çekiş altısı da referans veride bulunmayan bir değerle kaydedilemez; altı alan ayrı ayrı doğrulanır")]
    [InlineData("model", VehicleFieldNames.VehicleModelId)]
    [InlineData("kasa", VehicleFieldNames.BodyType)]
    [InlineData(ReferenceTermKinds.FuelType, VehicleFieldNames.FuelType)]
    [InlineData(ReferenceTermKinds.TransmissionType, VehicleFieldNames.TransmissionType)]
    [InlineData(ReferenceTermKinds.DrivetrainType, VehicleFieldNames.DrivetrainType)]
    [InlineData(ReferenceTermKinds.TrimLevel, VehicleFieldNames.TrimLevel)]
    public async Task Kaydedilemez_ReferansDisiDegerle(string eksik, string beklenenAlan)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        CreateVehicleCommand komut = eksik switch
        {
            "model" => Komut(context, model: new SahteModelSorgusu(bulunur: false)),
            "kasa" => Komut(context, kasa: new SahteKasaSorgusu(bulunur: false)),
            _ => Komut(context, terim: new SahteTerimSorgusu(eksik)),
        };

        Result<Guid> sonuc = await komut.ExecuteAsync(Istek($"AC05{eksik[..4]}00000000"), CancellationToken.None);

        Assert.Equal(VehicleErrors.ReferenceNotFound, sonuc.Error.Code);
        Assert.Equal(beklenenAlan, sonuc.Error.Parameters!["field"]);
    }

    [Theory(DisplayName = "0010a/AC-6: Çekirdek alanlardan biri eksik bırakılan araç kaydedilemez (D8)")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Kaydedilemez_CekirdekAlanEksikken(string bos)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context)
            .ExecuteAsync(Istek("AC06000000000001") with { BodyType = bos }, CancellationToken.None);

        Assert.Equal(VehicleErrors.CoreFieldMissing, sonuc.Error.Code);
        Assert.Equal(VehicleFieldNames.BodyType, sonuc.Error.Parameters!["field"]);
    }

    [Theory(DisplayName = "0010a/AC-7: Model yılı 1900'den küçük veya 2200'den büyük bir değerle araç kaydedilemez")]
    [InlineData(1899)]
    [InlineData(2201)]
    public async Task Kaydedilemez_ImkansizModelYiliyla(int yil)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context)
            .ExecuteAsync(Istek($"AC07{yil}0000000") with { ModelYear = yil }, CancellationToken.None);

        Assert.Equal(Vehicle.ImplausibleModelYear, sonuc.Error.Code);

        // AC-42 — hata HANGİ alanın düştüğünü söyler; agregatın hatası alan adı taşımaz
        // ve eşleme komut katmanında yapılır.
        Assert.Equal(VehicleFieldNames.ModelYear, sonuc.Error.Parameters!["field"]);
    }

    [Fact(DisplayName = "0010a/AC-8: Kilometre negatif bir değerle kaydedilemez")]
    public async Task Kaydedilemez_NegatifKilometreyle()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context)
            .ExecuteAsync(Istek("AC08000000000001") with { MileageKilometers = -1 }, CancellationToken.None);

        Assert.Equal(Vehicle.NegativeMileage, sonuc.Error.Code);
        Assert.Equal(VehicleFieldNames.Mileage, sonuc.Error.Parameters!["field"]);
    }

    [Theory(DisplayName = "0010a/AC-9: İlan başlığı, ilan açıklaması, renk veya şase numarası üst uzunluk sınırını aştığında araç kaydedilemez")]
    [InlineData("vin")]
    [InlineData("color")]
    public async Task Kaydedilemez_UzunlukSinirlariAsildiginda(string alan)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        CreateVehicleRequest istek = alan == "vin"
            ? Istek(new string('V', Vin.MaximumLength + 1))
            : Istek("AC09000000000001") with
            {
                Color = new Dictionary<string, string>
                {
                    ["tr"] = new string('B', CreateVehicleCommand.MaximumColorLength + 1),
                },
            };

        Result<Guid> sonuc = await Komut(context).ExecuteAsync(istek, CancellationToken.None);

        Assert.Equal(VehicleErrors.TextTooLong, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0010a/AC-15: Araç kaydı şase numarasıyla kaydedilir")]
    public async Task Kaydedilir_SaseNumarasiyla()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context).ExecuteAsync(Istek("AC15000000000001"), CancellationToken.None);

        Vehicle vehicle = await context.Vehicles.AsNoTracking()
            .SingleAsync(item => item.Id == new VehicleId(sonuc.Value));

        Assert.Equal("AC15000000000001", vehicle.Vin.Value);
    }

    [Fact(DisplayName = "0010a/AC-16: Galeride aynı şase numarasına sahip ikinci bir araç kaydedilemez")]
    public async Task Kaydedilemez_AyniSaseyleIkinciArac()
    {
        await using VehicleCatalogDbContext ilk = database.CreateContext();
        Assert.True((await Komut(ilk).ExecuteAsync(Istek("DUPLICATE0000001"), CancellationToken.None)).IsSuccess);

        await using VehicleCatalogDbContext ikinci = database.CreateContext();
        Result<Guid> sonuc = await Komut(ikinci).ExecuteAsync(Istek("DUPLICATE0000001"), CancellationToken.None);

        Assert.Equal(VehicleErrors.DuplicateVin, sonuc.Error.Code);
    }

    /// <summary>
    /// `0010a`'da hiçbir şey arşivlemez (X2), bu yüzden muafiyet agregat üzerinden
    /// kuruluyor. Kapı veritabanı indeksinde: `WHERE NOT is_archived`.
    /// </summary>
    [Fact(DisplayName = "0010a/AC-17: `Arşiv` durumundaki bir aracın şase numarası, yeni bir araç kaydında yeniden kullanılabilir")]
    public async Task Kullanilabilir_ArsivlenmisAracinSasesi()
    {
        await using VehicleCatalogDbContext ilk = database.CreateContext();
        Result<Guid> once = await Komut(ilk).ExecuteAsync(Istek("ARCHIVED00000001"), CancellationToken.None);

        Vehicle arsivlenen = await ilk.Vehicles.SingleAsync(item => item.Id == new VehicleId(once.Value));
        arsivlenen.MarkArchived();
        await ilk.SaveChangesAsync();

        await using VehicleCatalogDbContext ikinci = database.CreateContext();
        Result<Guid> sonra = await Komut(ikinci).ExecuteAsync(Istek("ARCHIVED00000001"), CancellationToken.None);

        Assert.True(sonra.IsSuccess, sonra.IsFailure ? sonra.Error.Code : null);
    }

    [Fact(DisplayName = "0010a/AC-20: Fiyatı belirlenmiş bir araç kaydedilir ve fiyatı listede görünür")]
    public async Task Kaydedilir_BelirliFiyatla()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context).ExecuteAsync(Istek("AC20000000000001"), CancellationToken.None);

        Vehicle vehicle = await context.Vehicles.AsNoTracking()
            .SingleAsync(item => item.Id == new VehicleId(sonuc.Value));

        Assert.False(vehicle.Price.IsOnRequest);
        Assert.Equal(750_000m, vehicle.Price.Amount!.Value.Amount);
    }

    [Fact(DisplayName = "0010a/AC-21: \"Fiyat sorunuz\" işaretli bir araç kaydedilir; tutar `0` olarak saklanmaz")]
    public async Task Kaydedilir_FiyatSorunuz_SifirDegil()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context).ExecuteAsync(
            Istek("AC21000000000001") with { PriceAmount = null, PriceOnRequest = true },
            CancellationToken.None);

        Vehicle vehicle = await context.Vehicles.AsNoTracking()
            .SingleAsync(item => item.Id == new VehicleId(sonuc.Value));

        Assert.True(vehicle.Price.IsOnRequest);
        Assert.Null(vehicle.Price.Amount);
    }

    /// <summary>P4 — ikisi birlikte verilirse hangisinin kazandığını sessizce seçmeyiz.</summary>
    [Fact]
    public async Task Reddedilir_FiyatVeSorunuzBirlikte()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result<Guid> sonuc = await Komut(context).ExecuteAsync(
            Istek("BOTH000000000001") with { PriceOnRequest = true },
            CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidPrice, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0010a/AC-23: Fiyat değiştirildiğinde fiyat geçmişine yeni bir kayıt yazılır ve eski kayıt olduğu gibi kalır")]
    public async Task Yazar_YeniKayit_FiyatDegistiginde()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Result<Guid> sonuc = await Komut(context).ExecuteAsync(Istek("AC23000000000001"), CancellationToken.None);

        decimal ilkTutar;

        await using (VehicleCatalogDbContext degistirme = database.CreateContext())
        {
            Vehicle vehicle = await degistirme.Vehicles
                .Include(item => item.PriceChanges)
                .SingleAsync(item => item.Id == new VehicleId(sonuc.Value));

            ilkTutar = vehicle.PriceChanges[0].Price.Amount!.Value.Amount;

            vehicle.ChangePrice(ListingPrice.Of(Money.Create(900_000m, "TRY", 2).Value), An.AddHours(1));

            await degistirme.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();
        Vehicle okunan = await okuma.Vehicles
            .Include(item => item.PriceChanges)
            .SingleAsync(item => item.Id == new VehicleId(sonuc.Value));

        Assert.Equal(2, okunan.PriceChanges.Count);
        Assert.Equal(ilkTutar, okunan.PriceChanges.OrderBy(change => change.ChangedAt).First().Price.Amount!.Value.Amount);
        Assert.Equal(900_000m, okunan.Price.Amount!.Value.Amount);
    }

    [Fact(DisplayName = "0010a/AC-25: \"Fiyat sorunuz\"dan belirli bir fiyata geçiş de fiyat geçmişine yazılır")]
    public async Task Yazar_SorunuzdanBelirliFiyataGecisi()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Result<Guid> sonuc = await Komut(context).ExecuteAsync(
            Istek("AC25000000000001") with { PriceAmount = null, PriceOnRequest = true },
            CancellationToken.None);

        await using (VehicleCatalogDbContext degistirme = database.CreateContext())
        {
            Vehicle vehicle = await degistirme.Vehicles
                .Include(item => item.PriceChanges)
                .SingleAsync(item => item.Id == new VehicleId(sonuc.Value));

            vehicle.ChangePrice(ListingPrice.Of(Money.Create(500_000m, "TRY", 2).Value), An.AddHours(1));
            await degistirme.SaveChangesAsync();
        }

        await using VehicleCatalogDbContext okuma = database.CreateContext();
        Vehicle okunan = await okuma.Vehicles
            .Include(item => item.PriceChanges)
            .SingleAsync(item => item.Id == new VehicleId(sonuc.Value));

        Assert.Equal(2, okunan.PriceChanges.Count);
        Assert.True(okunan.PriceChanges.OrderBy(change => change.ChangedAt).First().Price.IsOnRequest);
    }

    private static CreateVehicleRequest Istek(string vin) => new(
        BranchId: Guid.CreateVersion7(),
        VehicleModelId: Guid.CreateVersion7(),
        BodyType: "suv",
        FuelType: "diesel",
        TransmissionType: "automatic",
        DrivetrainType: "front-wheel-drive",
        TrimLevel: "comfort",
        Color: new Dictionary<string, string> { ["tr"] = "Beyaz", ["en"] = "White" },
        ModelYear: 2019,
        MileageKilometers: 45_000,
        PriceAmount: 750_000m,
        PriceOnRequest: false,
        Vin: vin);

    private static CreateVehicleCommand Komut(
        VehicleCatalogDbContext context,
        SahteSubeSorgusu? sube = null,
        SahteKasaSorgusu? kasa = null,
        SahteModelSorgusu? model = null,
        SahteTerimSorgusu? terim = null) =>
        new(
            context,
            sube ?? new SahteSubeSorgusu(),
            kasa ?? new SahteKasaSorgusu(),
            model ?? new SahteModelSorgusu(),
            terim ?? new SahteTerimSorgusu(),
            new CatalogCurrencyOptions("TRY", 2),
            new ShowcaseLocaleOptions(
                Locale.Create("tr").Value,
                [Locale.Create("tr").Value, Locale.Create("ar").Value, Locale.Create("en").Value]),
            new FixedClock(An));
}
