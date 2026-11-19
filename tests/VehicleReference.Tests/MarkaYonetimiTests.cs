using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Domain;
using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleReference.Tests;

/// <summary>
/// `0021` R1, R5–R7 · D4–D7, D10, D19 — marka yönetimi, gerçek PostgreSQL üzerinde. Her test
/// kendi veritabanını kurar (<see cref="ReferenceDatabase.NewDatabaseAsync"/>): marka adı galeri
/// genelinde tekildir ve kriterler belirli adlar istiyor.
/// </summary>
[Collection(ReferenceDatabaseCollection.Name)]
public sealed class MarkaYonetimiTests(ReferenceDatabase database)
{
    internal static readonly ReferenceLocales Diller = new([Dil("tr"), Dil("ar"), Dil("en")]);

    [Fact(DisplayName = "0021/AC-6: Yönetici marka ekranında bütün markaları — aktif ve pasif — kanonik adları, durumları ve \"ana sayfada göster\" işaretleriyle görür.")]
    public async Task Listeler_AktifVePasifMarkalari()
    {
        string db = await database.NewDatabaseAsync();
        Guid fiat = await EkleAsync(db, "Fiat");
        await PasifYapAsync(db, fiat);
        Guid toyota = await KimlikAsync(db, "toyota");
        await AnaSayfadaGosterAsync(db, toyota);

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        IReadOnlyList<AdminBrandDto> markalar = await new ReferenceAdminQueries(context).ListBrandsAsync(CancellationToken.None);

        Assert.Equal(10, markalar.Count);
        AdminBrandDto pasif = Assert.Single(markalar, item => item.CanonicalName == "Fiat");
        Assert.False(pasif.IsActive);
        AdminBrandDto isaretli = Assert.Single(markalar, item => item.CanonicalName == "Toyota");
        Assert.True(isaretli.IsActive);
        Assert.True(isaretli.ShowOnHome);
        Assert.Equal(2, isaretli.ModelCount);
        Assert.Equal([.. markalar.Select(item => item.CanonicalName).Order(StringComparer.OrdinalIgnoreCase)], [.. markalar.Select(item => item.CanonicalName)]);
    }

    [Fact(DisplayName = "0021/AC-7: Yönetici kanonik adı \"Fiat\" olan bir marka eklediğinde marka listede aktif görünür ve kısaltması `fiat` olur.")]
    public async Task Ekler_Fiat_AktifVeKisaltmasiFiat()
    {
        string db = await database.NewDatabaseAsync();

        Guid fiat = await EkleAsync(db, "Fiat");

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        AdminBrandDto marka = Assert.Single(
            await new ReferenceAdminQueries(context).ListBrandsAsync(CancellationToken.None),
            item => item.Id == fiat);
        Assert.True(marka.IsActive);
        Assert.Equal("fiat", marka.Slug);
        Assert.False(marka.ShowOnHome);
    }

    [Fact(DisplayName = "0021/AC-8: Var olan bir markanın kanonik adıyla — büyük-küçük harf farkı gözetmeden (\"toyota\") — marka eklemek reddedilir ve hata adın zaten kullanıldığını söyler.")]
    public async Task Reddeder_AyniAd_HarfFarkiylaBile()
    {
        string db = await database.NewDatabaseAsync();

        Result<(Guid, int)> sonuc = await Komut(db).CreateAsync(new ReferenceNameInput("toyota", null), CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ReferenceErrors.BrandNameTaken, sonuc.Error.Code);
        Assert.Equal("canonicalName", sonuc.Error.Parameters!["field"]);
    }

    [Fact(DisplayName = "0021/AC-8: Var olan bir markanın kanonik adıyla — büyük-küçük harf farkı gözetmeden (\"toyota\") — marka eklemek reddedilir ve hata adın zaten kullanıldığını söyler.")]
    public async Task Reddeder_EszamanliIkinciEklemeyi_Indeksle()
    {
        string db = await database.NewDatabaseAsync();

        // Yarış: iki istek ön denetimi birlikte geçer; ikincisini tekil indeks düşürür ve
        // hata yine adın kullanıldığını söyler, 500 değil.
        Task<Result<(Guid Id, int Version)>>[] istekler =
        [
            Komut(db).CreateAsync(new ReferenceNameInput("Lancia", null), CancellationToken.None),
            Komut(db).CreateAsync(new ReferenceNameInput("LANCIA", null), CancellationToken.None),
        ];
        Result<(Guid Id, int Version)>[] sonuclar = await Task.WhenAll(istekler);

        Assert.Single(sonuclar, sonuc => sonuc.IsSuccess);
        Result<(Guid Id, int Version)> red = Assert.Single(sonuclar, sonuc => sonuc.IsFailure);
        Assert.Contains(red.Error.Code, new[] { ReferenceErrors.BrandNameTaken, ReferenceErrors.BrandSlugTaken });
    }

    [Fact(DisplayName = "0021/AC-9: \"Alfa Romeo\" varken \"Alfa-Romeo\" eklemek, ad farklı olsa da kısaltma çakıştığı için ayrı bir hata koduyla reddedilir (D19).")]
    public async Task Reddeder_KisaltmaCakismasini_AyriKodla()
    {
        string db = await database.NewDatabaseAsync();
        await EkleAsync(db, "Alfa Romeo");

        Result<(Guid, int)> sonuc = await Komut(db).CreateAsync(new ReferenceNameInput("Alfa-Romeo", null), CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(ReferenceErrors.BrandSlugTaken, sonuc.Error.Code);
        Assert.NotEqual(ReferenceErrors.BrandNameTaken, sonuc.Error.Code);
    }

    [Theory(DisplayName = "0021/AC-10: ASCII olmayan ya da boş bir kanonik ad (\"Škoda\", \"\") reddedilir.")]
    [InlineData("Škoda", "brand.canonical_name_invalid")]
    [InlineData("", "brand.canonical_name_required")]
    [InlineData("   ", "brand.canonical_name_required")]
    public async Task Reddeder_AsciiOlmayanVeBosAdi(string ad, string kod)
    {
        string db = await database.NewDatabaseAsync();

        Result<(Guid, int)> sonuc = await Komut(db).CreateAsync(new ReferenceNameInput(ad, null), CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(kod, sonuc.Error.Code);
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        Assert.Equal(9, await context.VehicleBrands.CountAsync());
    }

    [Fact(DisplayName = "0021/AC-11: \"Alfa Romeo\" kanonik adlı markanın kısaltması `alfa-romeo`, \"Rolls-Royce & Co.\" kanonik adlınınki `rolls-royce-co` olur (D10).")]
    public async Task Uretir_Kisaltmayi_KuralaGore()
    {
        string db = await database.NewDatabaseAsync();

        Guid alfa = await EkleAsync(db, "Alfa Romeo");
        Guid rolls = await EkleAsync(db, "Rolls-Royce & Co.");

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        Assert.Equal("alfa-romeo", (await context.VehicleBrands.SingleAsync(item => item.Id == alfa)).Slug);
        Assert.Equal("rolls-royce-co", (await context.VehicleBrands.SingleAsync(item => item.Id == rolls)).Slug);
        Assert.Equal("a-b", ReferenceNames.Slug("--A  &  B--"));
    }

    [Fact(DisplayName = "0021/AC-12: Markanın kanonik adı düzeltildiğinde kısaltması değişmez.")]
    public async Task Korur_Kisaltmayi_AdDuzeltilince()
    {
        string db = await database.NewDatabaseAsync();
        Guid marka = await EkleAsync(db, "Alfa Romeo");

        Result<int> sonuc = await Komut(db).UpdateAsync(marka, 1, new ReferenceNameInput("Alfa Romeo SpA", null), CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        VehicleBrand kayit = await context.VehicleBrands.SingleAsync(item => item.Id == marka);
        Assert.Equal("Alfa Romeo SpA", kayit.CanonicalName);
        Assert.Equal("alfa-romeo", kayit.Slug);
    }

    [Fact(DisplayName = "0021/AC-13: Yönetici bir markaya Arapça görünen ad (\"فيات\") verdiğinde Arapça vitrin onu gösterir; Türkçe vitrin kanonik adı gösterir.")]
    public async Task Gosterir_ArapcaAdi_ArapcaVitrinde()
    {
        string db = await database.NewDatabaseAsync();
        Guid fiat = await EkleAsync(db, "Fiat");
        Guid egea = await ModelEkleAsync(db, fiat, "Egea");

        Result<int> sonuc = await Komut(db).UpdateAsync(
            fiat,
            1,
            new ReferenceNameInput("Fiat", new Dictionary<string, string?> { ["ar"] = "فيات" }),
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        VehicleModelQueries sorgu = new(context);
        Assert.Equal("فيات Egea", (await sorgu.ResolveNamesAsync([egea], "ar", CancellationToken.None))[egea]);
        Assert.Equal("Fiat Egea", (await sorgu.ResolveNamesAsync([egea], "tr", CancellationToken.None))[egea]);
        ShowcaseBrandDto eksen = Assert.Single(await new VehicleBrandQueries(context).GetBrandsOfModelsAsync([egea], "ar", CancellationToken.None));
        Assert.Equal("فيات", eksen.Label);
    }

    [Fact(DisplayName = "0021/AC-14: Pasif yapılan marka araç formunun marka seçicisinde yeni seçim olarak görünmez.")]
    public async Task Sunmaz_PasifMarkayi_AracFormunda()
    {
        string db = await database.NewDatabaseAsync();
        Guid fiat = await EkleAsync(db, "Fiat");

        await PasifYapAsync(db, fiat);

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        IReadOnlyList<VehicleBrandDto> aktif = await new VehicleBrandQueries(context).GetActiveAsync("tr", CancellationToken.None);
        Assert.DoesNotContain(aktif, item => item.Id == fiat);
        Assert.Equal(9, aktif.Count);
    }

    [Fact(DisplayName = "0021/AC-15: Pasif yapılan markanın ilanları vitrinde kalır ve marka ekseninde süzülebilir.")]
    public async Task Suzer_PasifMarkayi_Eksende()
    {
        string db = await database.NewDatabaseAsync();
        Guid toyota = await KimlikAsync(db, "toyota");

        await PasifYapAsync(db, toyota);

        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        VehicleBrandQueries sorgu = new(context);
        ShowcaseBrandDto eksen = Assert.Single(await sorgu.GetBrandsOfModelsAsync([Corolla], "tr", CancellationToken.None));
        Assert.Equal("toyota", eksen.Slug);
        BrandModelSetDto kume = await sorgu.GetModelSetAsync(["toyota"], CancellationToken.None);
        Assert.Equal(["toyota"], kume.KnownSlugs);
        Assert.Contains(Corolla, kume.ModelIds);
        Assert.Equal("Toyota Corolla", (await new VehicleModelQueries(context).ResolveNamesAsync([Corolla], "tr", CancellationToken.None))[Corolla]);
    }

    [Fact(DisplayName = "0021/AC-16: Pasif marka yeniden etkinleştirildiğinde araç formunda yeniden seçilebilir.")]
    public async Task Sunar_YenidenEtkinlesenMarkayi()
    {
        string db = await database.NewDatabaseAsync();
        Guid toyota = await KimlikAsync(db, "toyota");
        await PasifYapAsync(db, toyota);

        Result<int> sonuc = await Komut(db).SetActiveAsync(toyota, 2, active: true, CancellationToken.None);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(3, sonuc.Value);
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        Assert.Contains(await new VehicleBrandQueries(context).GetActiveAsync("tr", CancellationToken.None), item => item.Id == toyota);
        Assert.True(await new VehicleModelQueries(context).FindSelectableAsync(Corolla, CancellationToken.None));
    }

    [Fact]
    public async Task Reddeder_EskiSurumle_Yazmayi()
    {
        string db = await database.NewDatabaseAsync();
        Guid fiat = await EkleAsync(db, "Fiat");
        Assert.True((await Komut(db).SetShowOnHomeAsync(fiat, 1, true, CancellationToken.None)).IsSuccess);

        Result<int> sonuc = await Komut(db).SetActiveAsync(fiat, 1, active: false, CancellationToken.None);

        Assert.Equal(ConcurrencyErrors.Lost, sonuc.Error.Code);
    }

    [Fact]
    public async Task Reddeder_EtkinOlmayanDili()
    {
        string db = await database.NewDatabaseAsync();

        Result<(Guid, int)> sonuc = await Komut(db).CreateAsync(
            new ReferenceNameInput("Fiat", new Dictionary<string, string?> { ["de"] = "Fiat" }),
            CancellationToken.None);

        Assert.Equal(ReferenceErrors.LocaleNotEnabled, sonuc.Error.Code);
        Assert.Equal("displayNames.de", sonuc.Error.Parameters!["field"]);
    }

    internal static readonly Guid Corolla = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    internal static BrandCommands Komut(string db) => new(ReferenceDatabase.CreateContext(db), Diller);

    internal static ModelCommands ModelKomut(string db) => new(ReferenceDatabase.CreateContext(db), Diller);

    internal static async Task<Guid> EkleAsync(string db, string ad)
    {
        Result<(Guid Id, int Version)> sonuc = await Komut(db).CreateAsync(new ReferenceNameInput(ad, null), CancellationToken.None);
        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        return sonuc.Value.Id;
    }

    internal static async Task<Guid> ModelEkleAsync(string db, Guid marka, string ad)
    {
        Result<(Guid Id, int Version)> sonuc = await ModelKomut(db).CreateAsync(marka, new ReferenceNameInput(ad, null), CancellationToken.None);
        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        return sonuc.Value.Id;
    }

    internal static async Task<Guid> KimlikAsync(string db, string kisaltma)
    {
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        return (await context.VehicleBrands.SingleAsync(item => item.Slug == kisaltma)).Id;
    }

    internal static async Task PasifYapAsync(string db, Guid marka)
    {
        int surum = await SurumAsync(db, marka);
        Assert.True((await Komut(db).SetActiveAsync(marka, surum, active: false, CancellationToken.None)).IsSuccess);
    }

    private static async Task AnaSayfadaGosterAsync(string db, Guid marka)
    {
        int surum = await SurumAsync(db, marka);
        Assert.True((await Komut(db).SetShowOnHomeAsync(marka, surum, true, CancellationToken.None)).IsSuccess);
    }

    private static async Task<int> SurumAsync(string db, Guid marka)
    {
        await using VehicleReferenceDbContext context = ReferenceDatabase.CreateContext(db);
        return (await context.VehicleBrands.SingleAsync(item => item.Id == marka)).Version;
    }

    private static Locale Dil(string kod) => Locale.Create(kod).Value;
}
