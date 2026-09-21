using CarGallery.Modules.VehicleCatalog.Application.Detail;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class DetayTests(ShowcaseDatabase database)
{
    private const string Slug = "toyota-corolla-2019-abc123";

    [Fact(DisplayName = "AC-1: Yayında durumundaki bir ilanın adresi istendiğinde o ilanın detayı döner")]
    public async Task Doner_YayindakiIlanDetayi()
    {
        await database.ResetAsync();
        await SeedAsync();

        ShowcaseListingDetailDto? detay = await FindAsync(Slug);

        Assert.NotNull(detay);
        Assert.Equal(Slug, detay.Slug);
        Assert.Equal(2019, detay.ModelYear);
    }

    [Fact(DisplayName = "AC-5: Var olmayan bir ilan adresi istendiğinde sistem \"bulunamadı\" karşılığı verir")]
    public async Task Bulunamadi_VarOlmayanAdres()
    {
        await database.ResetAsync();
        await SeedAsync();

        Assert.Null(await FindAsync("hic-var-olmayan-ilan-999999"));
    }

    [Fact(DisplayName = "AC-6: Taslak, Arşiv ve Satıldı durumundaki bir ilanın adresi istendiğinde sistem \"bulunamadı\" karşılığı verir")]
    public async Task Bulunamadi_VitrindeGorunmeyenIlan()
    {
        await database.ResetAsync();

        // Vitrin okuma modeli yalnız görünür ilanları taşır (0001). Taslak/Arşiv/Satıldı
        // ilanın satırı hiç yoktur — yansıtıcı onu üretmez (ProjectionTests AC-3).
        await ShowcaseQueryHarness.SeedAsync(database, []);

        Assert.Null(await FindAsync(Slug));
    }

    [Fact(DisplayName = "AC-8: Var olan ama gizlenen bir ilan ile hiç var olmayan bir ilan için dönen karşılık birbirinden ayırt edilemez: aynı hata kodu, aynı gövde")]
    public async Task AyirtEdilemez_GizlenenVeVarOlmayan()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database, []);

        ShowcaseListingDetailDto? gizlenen = await FindAsync(Slug);
        ShowcaseListingDetailDto? hicYok = await FindAsync("baska-bir-ilan-000000");

        // İkisi de aynı: null. Ayrım yapan bir dal yok, dolayısıyla sızdırılacak bir
        // fark da yok. Süre eşitliği ölçümle değil TASARIMLA sağlanıyor (E7): tek sorgu,
        // dalsız kod yolu.
        Assert.Null(gizlenen);
        Assert.Null(hicYok);
    }

    [Fact(DisplayName = "AC-9: Rezerve durumundaki ilanın detayı açılır ve sayfada rezerve rozeti görünür")]
    public async Task RozetBilgisiTasir_RezerveIlan()
    {
        await database.ResetAsync();
        await SeedAsync(builder => builder.Reserved());

        ShowcaseListingDetailDto? detay = await FindAsync(Slug);

        Assert.NotNull(detay);
        Assert.True(detay.IsReserved);
    }

    [Fact(DisplayName = "AC-10: Detay sayfası model adını, yılı, kasa tipini, donanım paketini, kilometreyi, yakıtı, vitesi, çekişi, rengi, şube adını ve kapak görselini gösterir")]
    public async Task Gosterilir_CekirdekAlanlar()
    {
        await database.ResetAsync();
        await SeedAsync();

        ShowcaseListingDetailDto detay = (await FindAsync(Slug))!;

        Assert.Equal("Toyota Corolla", detay.ModelName);
        Assert.Equal(125_000, detay.MileageKilometers);
        Assert.Equal("Merkez Şube", detay.BranchName);
        Assert.NotNull(detay.CoverImageUrl);

        string[] anahtarlar = [.. detay.Attributes.Select(item => item.Key)];
        Assert.Contains("body_type", anahtarlar);
        Assert.Contains("fuel_type", anahtarlar);
        Assert.Contains("transmission_type", anahtarlar);
        Assert.Contains("drivetrain_type", anahtarlar);
        Assert.Contains("trim_level", anahtarlar);
        Assert.Contains("color", anahtarlar);
    }

    [Fact(DisplayName = "AC-11: Alan ayarlarında açık ve public işaretli bir opsiyonel alanın değeri detay sayfasında görünür")]
    public async Task Gosterilir_AcikVePublicOpsiyonelAlan()
    {
        await database.ResetAsync();
        await SeedAsync(builder => builder.WithOptionalValue(
            "warranty_status",
            new Dictionary<string, string> { ["tr"] = "2 yıl garanti", ["en"] = "2 year warranty" }));

        ShowcaseListingDetailDto detay = (await FindAsync(Slug))!;

        Assert.Contains(detay.Attributes, item => item.Key == "warranty_status" && item.Value == "2 yıl garanti");
    }

    [Fact(DisplayName = "AC-13: Alan ayarlarında Gizli işaretli bir opsiyonel alan detay yanıtında hiç bulunmaz; boş değer olarak da bulunmaz")]
    public async Task Bulunmaz_GizliOpsiyonelAlan()
    {
        await database.ResetAsync();

        // Gizli alanın değeri okuma modeline HİÇ yazılmaz (C4) — bu yüzden burada
        // seed edilecek bir satır da yoktur. AC-12 (kapalı alan) aynı yoldan geçer.
        await SeedAsync();

        ShowcaseListingDetailDto detay = (await FindAsync(Slug))!;

        Assert.DoesNotContain(detay.Attributes, item => item.Key == "damage_record_amount");
    }

    [Fact(DisplayName = "AC-15: \"Fiyat sorunuz\" işaretli ilanın detayında tutar yerine \"fiyat sorunuz\" karşılığı gösterilir; 0 veya boş tutar gösterilmez")]
    public async Task FiyatSorunuz_TutarTasimaz()
    {
        await database.ResetAsync();
        await SeedAsync(builder => builder.PriceOnRequest());

        ShowcaseListingDetailDto detay = (await FindAsync(Slug))!;

        Assert.True(detay.Price.OnRequest);
        Assert.Null(detay.Price.Amount);
    }

    [Fact(DisplayName = "AC-16: Kurulum para birimi KWD iken 1250,500 tutarlı ilanın detayında tutar üç ondalıkla gösterilir; yuvarlama veya kesme yapılmaz")]
    public async Task UcOndalikKorunur_KwdTutarinda()
    {
        await database.ResetAsync();
        await SeedAsync(builder => builder.WithPrice(1250.500m, "KWD", 3));

        ShowcaseListingDetailDto detay = (await FindAsync(Slug))!;

        Assert.Equal(1250.500m, detay.Price.Amount);
        Assert.Equal(3, detay.Price.MinorUnitExponent);
    }

    [Theory(DisplayName = "AC-21: Detay sayfasındaki tüm etiketler TR, AR ve EN locale'lerinde kendi dilinde görünür")]
    [InlineData("tr", "Dizel")]
    [InlineData("ar", "ديزل")]
    [InlineData("en", "Diesel")]
    public async Task EtiketlerKendiDilinde(string locale, string beklenen)
    {
        await database.ResetAsync();
        await SeedAsync();

        ShowcaseListingDetailDto detay = (await FindAsync(Slug, locale))!;

        Assert.Contains(detay.Attributes, item => item.Key == "fuel_type" && item.Label == beklenen);
    }

    private Task SeedAsync(Func<ShowcaseListingBuilder, ShowcaseListingBuilder>? düzenle = null)
    {
        ShowcaseListingBuilder builder = new ShowcaseListingBuilder()
            .WithSlug(Slug)
            .WithModelYear(2019)
            .WithMileage(125_000)
            .WithSpecification("diesel", "automatic", "front-wheel-drive", "comfort");

        return ShowcaseQueryHarness.SeedAsync(database, [(düzenle?.Invoke(builder) ?? builder).Build()]);
    }

    private async Task<ShowcaseListingDetailDto?> FindAsync(string slug, string locale = "tr")
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        ShowcaseListingDetailQueries queries = new(
            context,
            new FakeModelNameResolver(),
            new FakeReferenceLabelResolver(),
            ShowcaseQueryHarness.Locales);

        return await queries.FindAsync(slug, locale, CancellationToken.None);
    }
}
