using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Domain;
using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static CarGallery.Modules.VehicleReference.Tests.MarkaYonetimiTests;

namespace CarGallery.Modules.VehicleReference.Tests;

/// <summary>`0021` R2, R5, R6 · D11, D14 — model yönetimi ve araç formunun okuması, gerçek PostgreSQL üzerinde.</summary>
[Collection(ReferenceDatabaseCollection.Name)]
public sealed class ModelYonetimiTests(ReferenceDatabase database)
{
    [Fact(DisplayName = "0021/AC-17: Yönetici \"Fiat\" markasına \"Egea\" modelini eklediğinde model o markanın altında aktif görünür.")]
    public async Task Ekler_Egea_FiatAltindaAktif()
    {
        string db = await database.NewDatabaseAsync();
        Guid fiat = await EkleAsync(db, "Fiat");

        Guid egea = await ModelEkleAsync(db, fiat, "Egea");

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        AdminBrandDetailDto detay = (await new ReferenceAdminQueries(context).FindBrandAsync(fiat, CancellationToken.None))!;
        AdminModelDto model = Assert.Single(detay.Models);
        Assert.Equal(egea, model.Id);
        Assert.Equal("Egea", model.CanonicalName);
        Assert.True(model.IsActive);
        Assert.Contains(await new VehicleModelQueries(context).GetAllAsync("tr", CancellationToken.None), item => item.Id == egea && item.BrandId == fiat && item.Label == "Egea");
    }

    [Fact(DisplayName = "0021/AC-18: Aynı markada aynı kanonik adla ikinci bir model reddedilir.")]
    public async Task Reddeder_AyniMarkadaAyniAdi()
    {
        string db = await database.NewDatabaseAsync();
        Guid toyota = await KimlikAsync(db, "toyota");

        Result<(Guid, int)> sonuc = await ModelKomut(db).CreateAsync(toyota, new ReferenceNameInput("COROLLA", null), CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ReferenceErrors.ModelNameTaken, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0021/AC-19: Farklı markalarda aynı kanonik adlı modeller eklenebilir.")]
    public async Task Ekler_AyniAdi_FarkliMarkalarda()
    {
        string db = await database.NewDatabaseAsync();
        Guid honda = await KimlikAsync(db, "honda");
        Guid fiat = await EkleAsync(db, "Fiat");

        Guid ikinci = await ModelEkleAsync(db, fiat, "Civic");

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        List<VehicleModel> civicler = await context.VehicleModels.Where(item => item.CanonicalName == "Civic").ToListAsync();
        Assert.Equal(2, civicler.Count);
        Assert.Contains(civicler, item => item.BrandId == honda);
        Assert.Contains(civicler, item => item.Id == ikinci && item.BrandId == fiat);
    }

    [Fact(DisplayName = "0021/AC-20: Modelin markası değiştirilemez: düzenleme ekranında marka seçimi sunulmaz ve uç markayı değiştiren isteği reddeder.")]
    public async Task Reddeder_MarkaDegistirenIstegi()
    {
        string db = await database.NewDatabaseAsync();
        Guid honda = await KimlikAsync(db, "honda");

        Result<int> sonuc = await ModelKomut(db).UpdateAsync(MarkaYonetimiTests.Corolla, 1, honda, new ReferenceNameInput("Corolla", null), CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ReferenceErrors.ModelBrandImmutable, sonuc.Error.Code);
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        VehicleModel model = await context.VehicleModels.SingleAsync(item => item.Id == MarkaYonetimiTests.Corolla);
        Assert.Equal(await KimlikAsync(db, "toyota"), model.BrandId);
        Assert.Equal(1, model.Version);
    }

    [Fact(DisplayName = "0021/AC-21: Pasif yapılan model araç formunda yeni seçim olarak sunulmaz; o modelle kayıtlı araçlar ve ilanlar değişmez.")]
    public async Task Sunmaz_PasifModeli_AdiCozulmeyeDevamEder()
    {
        string db = await database.NewDatabaseAsync();

        Result<int> sonuc = await ModelKomut(db).SetActiveAsync(MarkaYonetimiTests.Corolla, 1, active: false, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        VehicleModelQueries sorgu = new(context);
        Assert.DoesNotContain(await sorgu.GetAllAsync("tr", CancellationToken.None), item => item.Id == MarkaYonetimiTests.Corolla);
        Assert.False(await sorgu.FindSelectableAsync(MarkaYonetimiTests.Corolla, CancellationToken.None));

        // Kayıtlı araçların ve ilanların adı aynı kimlikten çözülmeye devam eder.
        Assert.Equal("Toyota Corolla", (await sorgu.ResolveNamesAsync([MarkaYonetimiTests.Corolla], "tr", CancellationToken.None))[MarkaYonetimiTests.Corolla]);
        Assert.Equal("Toyota Corolla", (await sorgu.ResolveCanonicalNamesAsync([MarkaYonetimiTests.Corolla], CancellationToken.None))[MarkaYonetimiTests.Corolla]);
    }

    [Fact(DisplayName = "0021/AC-22: Pasif bir markanın aktif modelleri de araç formunda yeni seçim olarak sunulmaz.")]
    public async Task Sunmaz_PasifMarkaninAktifModellerini()
    {
        string db = await database.NewDatabaseAsync();
        Guid toyota = await KimlikAsync(db, "toyota");

        await PasifYapAsync(db, toyota);

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        VehicleModelQueries sorgu = new(context);
        IReadOnlyList<VehicleModelDto> modeller = await sorgu.GetAllAsync("tr", CancellationToken.None);
        Assert.DoesNotContain(modeller, item => item.BrandId == toyota);
        Assert.Equal(10, modeller.Count);
        Assert.False(await sorgu.FindSelectableAsync(MarkaYonetimiTests.Corolla, CancellationToken.None));
    }

    [Fact(DisplayName = "0021/AC-30: Görünen adı olmayan bir dilde marka ve model kanonik adlarıyla gösterilir.")]
    public async Task Gosterir_KanonikAdlari_GorunenAdYoksa()
    {
        string db = await database.NewDatabaseAsync();
        Guid fiat = await EkleAsync(db, "Fiat");
        Guid egea = await ModelEkleAsync(db, fiat, "Egea");

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        VehicleModelQueries sorgu = new(context);

        foreach (string dil in new[] { "tr", "ar", "en" })
        {
            Assert.Equal("Fiat Egea", (await sorgu.ResolveNamesAsync([egea], dil, CancellationToken.None))[egea]);
        }
    }

    [Fact(DisplayName = "0021/AC-31: Yönetici bir markanın görünen adını değiştirdikten sonra vitrin yeni adı gösterir.")]
    public async Task Gosterir_YeniGorunenAdi()
    {
        string db = await database.NewDatabaseAsync();
        Guid toyota = await KimlikAsync(db, "toyota");

        Result<int> sonuc = await Komut(db).UpdateAsync(
            toyota,
            1,
            new ReferenceNameInput("Toyota", new Dictionary<string, string?> { ["ar"] = "تويوتا الجديدة" }),
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        Assert.Equal("تويوتا الجديدة كورولا", (await new VehicleModelQueries(context).ResolveNamesAsync([MarkaYonetimiTests.Corolla], "ar", CancellationToken.None))[MarkaYonetimiTests.Corolla]);
    }

    [Fact(DisplayName = "0021/AC-32: Yeni yayına alınan bir Fiat Egea ilanının adresi `fiat-egea-<yıl>-…` biçimindedir.")]
    public async Task Doner_TamKanonikAdi_AdresKaynagi()
    {
        string db = await database.NewDatabaseAsync();
        Guid fiat = await EkleAsync(db, "Fiat");
        Guid egea = await ModelEkleAsync(db, fiat, "Egea");

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);

        // İlan adresi bu addan üretilir (`ListingSlug.Create`); adresin kendisi `Host.Tests`'te.
        Assert.Equal("Fiat Egea", (await new VehicleModelQueries(context).ResolveCanonicalNamesAsync([egea], CancellationToken.None))[egea]);
    }

    [Fact]
    public async Task Gunceller_AdlariVeSurumu()
    {
        string db = await database.NewDatabaseAsync();
        Guid fiat = await EkleAsync(db, "Fiat");
        Guid egea = await ModelEkleAsync(db, fiat, "Egea");

        Result<int> sonuc = await ModelKomut(db).UpdateAsync(
            egea,
            1,
            fiat,
            new ReferenceNameInput("Egea Cross", new Dictionary<string, string?> { ["ar"] = "إيجيا", ["en"] = "" }),
            CancellationToken.None);

        Assert.Equal(2, sonuc.Value);
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        VehicleModel model = await context.VehicleModels.Include(item => item.Translations).SingleAsync(item => item.Id == egea);
        Assert.Equal("Egea Cross", model.CanonicalName);
        Assert.Equal("إيجيا", Assert.Single(model.Translations).Name);
    }
}
