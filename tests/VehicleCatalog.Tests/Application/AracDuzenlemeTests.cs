using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// Araç düzenleme komutu (0010b B, C, D grupları).
///
/// <para>Gerçek PostgreSQL: düzenleme var olan bir satırı okuyup yazıyor ve opsiyonel
/// alan ayarları veritabanından geliyor (C24 — ayarları yazan uç yok, ön koşul veri
/// katmanından kuruluyor).</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class AracDuzenlemeTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Kayit = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Duzenleme = new(2026, 10, 10, 15, 30, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0010b/AC-2: Kaydedilmiş bir araç düzenlenir ve değişiklik listede görünür")]
    public async Task Duzenlenir_KaydedilmisArac()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ02");

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with { MileageKilometers = 99_000, ModelYear = 2021 },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);

        Vehicle okunan = await Oku(arac.Id);

        Assert.Equal(99_000, okunan.MileageKilometers);
        Assert.Equal(2021, okunan.ModelYear);
        Assert.Equal(Duzenleme, okunan.UpdatedAt);
    }

    [Theory(DisplayName = "0010b/AC-3: Düzenlemede de çekirdek alanlardan biri boşaltılarak kaydedilemez")]
    [InlineData("bodyType", VehicleFieldNames.BodyType)]
    [InlineData("color", VehicleFieldNames.Color)]
    [InlineData("price", VehicleFieldNames.Price)]
    public async Task Kaydedilemez_CekirdekAlanBosaltilinca(string alan, string beklenenAlan)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, $"DUZ03{alan[..3]}");

        UpdateVehicleRequest istek = alan switch
        {
            "bodyType" => Istek(arac) with { BodyType = "  " },
            "color" => Istek(arac) with { Color = new Dictionary<string, string>() },
            _ => Istek(arac) with { PriceAmount = null, PriceOnRequest = false },
        };

        Result sonuc = await Komut(context).ExecuteAsync(istek, CancellationToken.None);

        Assert.Equal(VehicleErrors.CoreFieldMissing, sonuc.Error.Code);
        Assert.Equal(beklenenAlan, sonuc.Error.Parameters!["field"]);
    }

    /// <summary>
    /// Dört slug AYRI AYRI işaretlenmeli. `VehicleSpecification.Create` dördünü bir
    /// döngüde geziyor ve hangisinin düştüğünü söylemiyor; eşleme koşulsuz `fuelType`
    /// olduğunda vitesi boş bırakan personele YAKIT alanı kırmızı gösteriliyordu.
    /// </summary>
    [Theory(DisplayName = "0010b/AC-3: Düzenlemede de çekirdek alanlardan biri boşaltılarak kaydedilemez")]
    [InlineData("fuel", VehicleFieldNames.FuelType)]
    [InlineData("transmission", VehicleFieldNames.TransmissionType)]
    [InlineData("drivetrain", VehicleFieldNames.DrivetrainType)]
    [InlineData("trim", VehicleFieldNames.TrimLevel)]
    public async Task Isaretler_BosBirakilanSlugAlanini(string alan, string beklenenAlan)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, $"DUZS{alan[..3]}");

        UpdateVehicleRequest istek = alan switch
        {
            "fuel" => Istek(arac) with { FuelType = "  " },
            "transmission" => Istek(arac) with { TransmissionType = "  " },
            "drivetrain" => Istek(arac) with { DrivetrainType = "  " },
            _ => Istek(arac) with { TrimLevel = "  " },
        };

        Result sonuc = await Komut(context).ExecuteAsync(istek, CancellationToken.None);

        Assert.Equal(VehicleErrors.CoreFieldMissing, sonuc.Error.Code);
        Assert.Equal(beklenenAlan, sonuc.Error.Parameters!["field"]);
    }

    /// <summary>
    /// C25/AC-5'in komut katmanındaki karşılığı. Uç testi tel üzerinde ölçüyor; burada
    /// ölçülen şey kuralın KOMUTTA yaşadığı — bağlayıcıya değil.
    /// </summary>
    [Theory]
    [InlineData("mileage")]
    [InlineData("modelYear")]
    public async Task Reddedilir_DegerTipliCekirdekAlanGonderilmediginde(string alan)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, $"DUZN{alan[..3]}");

        UpdateVehicleRequest istek = alan == "mileage"
            ? Istek(arac) with { MileageKilometers = null }
            : Istek(arac) with { ModelYear = null };

        Result sonuc = await Komut(context).ExecuteAsync(istek, CancellationToken.None);

        Assert.Equal(VehicleErrors.CoreFieldMissing, sonuc.Error.Code);
        Assert.Equal(
            alan == "mileage" ? VehicleFieldNames.Mileage : VehicleFieldNames.ModelYear,
            sonuc.Error.Parameters!["field"]);

        // Reddedilen istek araca DOKUNMAMALI.
        Assert.Equal(45_000, (await Oku(arac.Id)).MileageKilometers);
    }

    [Fact(DisplayName = "0010b/AC-4: Var olmayan bir araç kimliğiyle gelen düzenleme isteği `404` ve kararlı bir hata koduyla reddedilir")]
    public async Task Reddedilir_VarOlmayanKimlik()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek((Guid.CreateVersion7(), "DUZ04SASE00000001")),
            CancellationToken.None);

        Assert.Equal(VehicleErrors.NotFound, sonuc.Error.Code);
        Assert.Equal(VehicleFieldNames.VehicleId, sonuc.Error.Parameters!["field"]);
    }

    [Fact(DisplayName = "0010b/AC-8: Şase numarası düzenlenemez; değiştirme girişimi reddedilir")]
    public async Task Reddedilir_SaseDegistirmeGirisimi()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ08");

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with { Vin = "BASKASASE00000001" },
            CancellationToken.None);

        Assert.Equal(VehicleErrors.VinImmutable, sonuc.Error.Code);
        Assert.Equal(VehicleFieldNames.Vin, sonuc.Error.Parameters!["field"]);

        // Reddedilen istek HİÇBİR alanı değiştirmemiş olmalı.
        Assert.Equal(45_000, (await Oku(arac.Id)).MileageKilometers);
    }

    [Fact(DisplayName = "0010b/AC-6: Düzenlemede fiyat değiştirildiğinde fiyat geçmişine yeni bir kayıt yazılır ve eski kayıt olduğu gibi kalır")]
    public async Task Yazilir_FiyatGecmisineYeniKayit()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ06");

        await Komut(context).ExecuteAsync(
            Istek(arac) with { PriceAmount = 820_000m },
            CancellationToken.None);

        Vehicle okunan = await Oku(arac.Id);
        List<PriceChange> gecmis = [.. okunan.PriceChanges.OrderBy(change => change.ChangedAt)];

        Assert.Equal(2, gecmis.Count);
        Assert.Equal(750_000m, gecmis[0].Price.Amount!.Value.Amount);
        Assert.Equal(820_000m, gecmis[1].Price.Amount!.Value.Amount);
        Assert.Equal(820_000m, okunan.Price.Amount!.Value.Amount);
    }

    /// <summary>
    /// Değişmemiş fiyatı geçmişe yazmak, her kaydetmede bir "değişiklik" uydururdu ve
    /// geçmiş anlamını yitirirdi.
    /// </summary>
    [Fact]
    public async Task Yazmaz_FiyatDegismediginde()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZFIY");

        await Komut(context).ExecuteAsync(Istek(arac), CancellationToken.None);

        Assert.Single((await Oku(arac.Id)).PriceChanges);
    }

    [Fact(DisplayName = "0010b/AC-7: Düzenlemede \"fiyat sorunuz\"dan belirli bir fiyata geçiş de fiyat geçmişine yazılır")]
    public async Task Yazilir_FiyatSorunuzdanFiyataGecis()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ07", fiyatSorunuz: true);

        await Komut(context).ExecuteAsync(
            Istek(arac) with { PriceAmount = 500_000m, PriceOnRequest = false },
            CancellationToken.None);

        Vehicle okunan = await Oku(arac.Id);
        List<PriceChange> gecmis = [.. okunan.PriceChanges.OrderBy(change => change.ChangedAt)];

        Assert.Equal(2, gecmis.Count);
        Assert.True(gecmis[0].Price.IsOnRequest);
        Assert.Equal(500_000m, gecmis[1].Price.Amount!.Value.Amount);
    }

    [Fact(DisplayName = "0010b/AC-19: Yalnızca bir dilde metni olan araç kaydedilebilir")]
    public async Task Kaydedilir_TekDildeMetinle()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ19");

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with
            {
                ListingTitle = new Dictionary<string, string> { ["tr"] = "Temiz Corolla" },
                ListingDescription = new Dictionary<string, string> { ["tr"] = "Bakımlı" },
            },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);

        Listing ilan = await IlanOku(arac.Id);
        Locale tr = Locale.Create("tr").Value;
        Locale en = Locale.Create("en").Value;

        Assert.Equal("Temiz Corolla", ilan.Title.Resolve(tr, tr));
        Assert.Contains(en, ilan.MissingLocales([tr, en]));
    }

    /// <summary>
    /// Hata HANGİ alanın düştüğünü de söylemeli: ikisi tek koda eşlendiğinde açıklamayı
    /// uzun yazan personele BAŞLIK alanı kırmızı gösteriliyordu. Önceki hâli yalnız hata
    /// kodunu doğruluyordu ve bu yüzden görmedi.
    /// </summary>
    [Theory(DisplayName = "0010b/AC-20: İlan başlığı veya açıklaması locale başına üst uzunluk sınırını aştığında istek reddedilir")]
    [InlineData("title", VehicleFieldNames.ListingTitle)]
    [InlineData("description", VehicleFieldNames.ListingDescription)]
    public async Task Reddedilir_IlanMetniUzunlugu(string alan, string beklenenAlan)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, $"DUZ20{alan[..3]}");

        UpdateVehicleRequest istek = alan == "title"
            ? Istek(arac) with
            {
                ListingTitle = new Dictionary<string, string>
                {
                    ["tr"] = new('B', Listing.MaximumTitleLength + 1),
                },
            }
            : Istek(arac) with
            {
                ListingDescription = new Dictionary<string, string>
                {
                    ["tr"] = new('A', Listing.MaximumDescriptionLength + 1),
                },
            };

        Result sonuc = await Komut(context).ExecuteAsync(istek, CancellationToken.None);

        Assert.Equal(VehicleErrors.TextTooLong, sonuc.Error.Code);
        Assert.Equal(beklenenAlan, sonuc.Error.Parameters!["field"]);
    }

    [Fact]
    public async Task Reddedilir_EtkinOlmayanLocaledeIlanMetni()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZLOC");

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with
            {
                ListingTitle = new Dictionary<string, string> { ["de"] = "Sauberer Corolla" },
            },
            CancellationToken.None);

        Assert.Equal(VehicleErrors.LocaleNotEnabled, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0010b/AC-13: Alan ayarlarında açık bir opsiyonel alan formda görünür ve değeri kaydedilir")]
    public async Task Kaydedilir_AcikOpsiyonelAlaninDegeri()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ13");

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with
            {
                OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.DamageRecordAmount, "12500.75", null)],
            },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);

        OptionalFieldValue deger = (await Oku(arac.Id)).OptionalFieldValues.Single().Value;

        Assert.Equal(FieldValueKind.Amount, deger.Kind);
        Assert.Equal(12_500.75m, deger.Amount!.Value.Amount);
    }

    [Fact(DisplayName = "0010b/AC-9: Her opsiyonel alan R6'daki kapalı kümeden bir tipe sahiptir; tipi beyan edilmemiş bir alana değer kaydedilemez")]
    public async Task Kaydedilemez_TipiBeyanEdilmemisAlan()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ09");

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with
            {
                OptionalValues = [new OptionalFieldInput("uydurma_alan", "değer", null)],
            },
            CancellationToken.None);

        Assert.Equal(VehicleErrors.FieldNotDeclared, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0010b/AC-11: Tutar tipli bir alana sayı olmayan bir değer verildiğinde istek reddedilir")]
    public async Task Reddedilir_TutarAlaninaSayiOlmayanDeger()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ11");

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with
            {
                OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.DamageRecordAmount, "on iki bin", null)],
            },
            CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidFieldValue, sonuc.Error.Code);
        Assert.Equal(VehicleFieldKeys.DamageRecordAmount, sonuc.Error.Parameters!["field"]);
    }

    /// <summary>
    /// D27 — kapalı alana gelen değer sessizce YUTULMAZ. Ön koşul veri katmanından
    /// kuruluyor: ayarları yazan uç yok (X21, C24).
    /// </summary>
    [Fact]
    public async Task Reddedilir_KapaliAlanaGelenDeger()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZKAP");

        await AyarDegistirAsync(context, FieldFormState.Closed);

        try
        {
            Result sonuc = await Komut(context).ExecuteAsync(
                Istek(arac) with
                {
                    OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.DamageRecordAmount, "1000", null)],
                },
                CancellationToken.None);

            Assert.Equal(VehicleErrors.FieldClosed, sonuc.Error.Code);
        }
        finally
        {
            await AyarDegistirAsync(context, FieldFormState.Optional);
        }
    }

    /// <summary>
    /// AC-15 — zorunluluk yalnız YAYINA ALIRKEN bağlayıcı (R8, A2/F2). Taslak kayıt,
    /// zorunlu bir opsiyonel alan boşken de kaydedilebilir.
    /// </summary>
    [Fact(DisplayName = "0010b/AC-15: Alan ayarlarında zorunlu işaretli bir opsiyonel alan boş bırakılmış araç, taslak olarak kaydedilebilir")]
    public async Task Kaydedilir_ZorunluAlanBosken()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ15");

        await AyarDegistirAsync(context, FieldFormState.Required);

        try
        {
            Result sonuc = await Komut(context).ExecuteAsync(Istek(arac), CancellationToken.None);

            Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
            Assert.Empty((await Oku(arac.Id)).OptionalFieldValues);
        }
        finally
        {
            await AyarDegistirAsync(context, FieldFormState.Optional);
        }
    }

    /// <summary>
    /// D23 — opsiyonel alanlar BİRLEŞTİRİLİR. Kapalı alan formda görünmez (R7), istekte
    /// taşınmaz; "gelmeyen = silindi" demek kapatmayı silmeye çevirirdi (F1, AC-17).
    /// </summary>
    [Fact(DisplayName = "0010b/AC-17: Kapatılan bir alanın daha önce girilmiş değeri silinmez; alan yeniden açıldığında eski değer görünür")]
    public async Task Silinmez_IstekteTasinmayanOpsiyonelDeger()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZ17");

        await Komut(context).ExecuteAsync(
            Istek(arac) with
            {
                OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.DamageRecordAmount, "7500", null)],
            },
            CancellationToken.None);

        // Alan kapatılıyor ve form onu artık göndermiyor.
        await AyarDegistirAsync(context, FieldFormState.Closed);

        try
        {
            await Komut(context).ExecuteAsync(Istek(arac), CancellationToken.None);

            OptionalFieldValue deger = (await Oku(arac.Id)).OptionalFieldValues.Single().Value;

            Assert.Equal(7_500m, deger.Amount!.Value.Amount);
        }
        finally
        {
            await AyarDegistirAsync(context, FieldFormState.Optional);
        }
    }

    private static async Task AyarDegistirAsync(VehicleCatalogDbContext context, FieldFormState durum)
    {
        await using VehicleCatalogDbContext ayarContext = new(
            new DbContextOptionsBuilder<VehicleCatalogDbContext>()
                .UseNpgsql(context.Database.GetConnectionString())
                .Options);

        VehicleFieldSetting ayar = await ayarContext.VehicleFieldSettings
            .SingleAsync(item => item.FieldKey == VehicleFieldKeys.DamageRecordAmount);

        ayar.FormState = durum;
        await ayarContext.SaveChangesAsync();
    }

    private async Task<Vehicle> Oku(Guid id)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.Vehicles
            .AsNoTracking()
            .Include(vehicle => vehicle.OptionalFieldValues)
            .Include(vehicle => vehicle.PriceChanges)
            .SingleAsync(vehicle => vehicle.Id == new VehicleId(id));
    }

    private async Task<Listing> IlanOku(Guid vehicleId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.Listings
            .AsNoTracking()
            .SingleAsync(listing => listing.VehicleId == new VehicleId(vehicleId));
    }

    /// <summary>
    /// Kayıt yardımcısı ŞASEYİ de döndürüyor: düzenleme isteği şaseyi taşımak zorunda
    /// (D24) ve saklanandan farklı olması AC-8 ile reddediliyor.
    /// </summary>
    private static async Task<(Guid Id, string Vin)> AracKaydetAsync(
        VehicleCatalogDbContext context,
        string sasePrefix,
        bool fiyatSorunuz = false)
    {
        string vin = Sase(sasePrefix);

        CreateVehicleCommand komut = new(context, AracKaydiTests.Kurallar(), new FixedClock(Kayit));

        Result<Guid> sonuc = await komut.ExecuteAsync(
            new CreateVehicleRequest(
                BranchId: Guid.CreateVersion7(),
                VehicleModelId: Guid.CreateVersion7(),
                BodyType: "suv",
                FuelType: "diesel",
                TransmissionType: "automatic",
                DrivetrainType: "front-wheel-drive",
                TrimLevel: "comfort",
                Color: new Dictionary<string, string> { ["tr"] = "Beyaz" },
                ModelYear: 2019,
                MileageKilometers: 45_000,
                PriceAmount: fiyatSorunuz ? null : 750_000m,
                PriceOnRequest: fiyatSorunuz,
                Vin: vin),
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);

        return (sonuc.Value, vin);
    }

    private static string Sase(string prefix) => $"{prefix}{Guid.CreateVersion7():N}"[..24].ToUpperInvariant();

    private static UpdateVehicleRequest Istek((Guid Id, string Vin) arac) => new(
        VehicleId: arac.Id,
        BranchId: Guid.CreateVersion7(),
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
        Vin: arac.Vin,
        ListingTitle: new Dictionary<string, string>(),
        ListingDescription: new Dictionary<string, string>(),
        OptionalValues: []);

    /// <summary>
    /// **D37** — ölçüldü: `Touch`'ın çağıranları yalnız `UpdateVehicleMediaCommand` ve
    /// `ChangePrice`'tı. Başlık, kilometre veya renk düzeltmesi "son güncellenen"
    /// sıralamasında GÖRÜNMÜYORDU; personel bir aracı düzeltiyor ve araç listede yerinden
    /// kıpırdamıyordu.
    /// </summary>
    /// <summary>
    /// **D37**'nin kriteri. <b>Kararın ölçümü tutmadı ve bu PR'da yazılı:</b> karar
    /// *"`Touch`'ın çağıranları yalnız `UpdateVehicleMediaCommand` ve `ChangePrice`"*
    /// diyordu, oysa `Vehicle.ChangeCoreDetails` de `Touch` çağırıyor (`:205`) ve
    /// düzenleme komutu her zaman ondan geçiyor. Yani davranış zaten vardı; eksik olan
    /// ETİKETLİ testti.
    ///
    /// <para>Fiyat bilerek DEĞİŞTİRİLMİYOR: `ChangePrice`'ın kendi `Touch` çağrısı sonucu
    /// gizler ve test yanlış sebeple yeşil kalırdı.</para>
    /// </summary>
    [Fact(DisplayName = "0012b/AC-21: Araç düzenlemesi de aracın son güncellenme zamanını ilerletir")]
    public async Task Ilerletir_SonGuncellenme_AracDuzenlemesi()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "DUZTOUCH");

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with { MileageKilometers = 61_250 },
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : string.Empty);

        Vehicle okunan = await Oku(arac.Id);

        Assert.Equal(Duzenleme, okunan.UpdatedAt);
        Assert.Equal(61_250, okunan.MileageKilometers);
    }

    private static UpdateVehicleCommand Komut(VehicleCatalogDbContext context)
    {
        FixedClock saat = new(Duzenleme);

        return new UpdateVehicleCommand(
            context,
            AracKaydiTests.Kurallar(),
            Integration.KatalogYazicilari.Yansitma(context),
            Integration.KatalogYazicilari.Birim(context, saat),
            saat);
    }
}
