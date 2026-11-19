using System.Net;
using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using CarGallery.Modules.VehicleCatalog.Tests.Endpoints;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0021` R11–R14 · D9, D15, D20, D21 — vitrin marka ekseni, gerçek PostgreSQL üzerinde.
/// Okuma modeli markayı taşımaz; marka sözleşmeden model kimliği kümesine çevrilir. Marka
/// verisi `VehicleReference.Contracts`'ın sahtesinden gelir (<see cref="FakeBrandQueries"/>);
/// ölçülen şey Catalog'un o kümeyle ne yaptığı.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class MarkaEkseniTests(ShowcaseDatabase database)
{
    private static readonly Guid Corolla = Guid.Parse("0192c000-0000-7000-8000-00000000000a");
    private static readonly Guid Yaris = Guid.Parse("0192c000-0000-7000-8000-0000000000a1");
    private static readonly Guid Civic = Guid.Parse("0192c000-0000-7000-8000-0000000000a8");
    private static readonly Guid Clio = Guid.Parse("0192c000-0000-7000-8000-0000000000a4");
    private static readonly Guid Egea = Guid.CreateVersion7();

    /// <summary>Toyota ve Renault ana sayfada işaretli; Fiat işaretli ama stoksuz; Honda işaretsiz.</summary>
    private static readonly FakeBrandQueries Markalar = new(
        new FakeBrand("toyota", "Toyota", ShowOnHome: true, [Corolla, Yaris], "تويوتا"),
        new FakeBrand("honda", "Honda", ShowOnHome: false, [Civic], "هوندا"),
        new FakeBrand("renault", "Renault", ShowOnHome: true, [Clio], "رينو"),
        new FakeBrand("fiat", "Fiat", ShowOnHome: true, [Egea], "فيات"));

    [Fact(DisplayName = "0021/AC-35: Liste filtresinde marka ekseni yalnız en az bir yayındaki ilanı olan markaları sunar.")]
    public async Task Sunar_YalnizStoguOlanMarkalari()
    {
        await StokAsync();

        IReadOnlyList<ShowcaseBrandOptionDto> eksen = await Eksen().GetForListAsync("tr", [], CancellationToken.None);

        Assert.Equal(["honda", "renault", "toyota"], eksen.Select(item => item.Slug));
        Assert.DoesNotContain(eksen, item => item.Slug == "fiat");
    }

    [Fact(DisplayName = "0021/AC-36: Toyota seçildiğinde liste yalnız Toyota ilanlarını gösterir ve toplam sayı ona göre değişir.")]
    public async Task Daraltir_Toyota()
    {
        await StokAsync();

        PagedResult<ShowcaseListingDto> hepsi = await SorAsync(Filtre());
        PagedResult<ShowcaseListingDto> toyota = await SorAsync(Filtre() with { Brands = ["toyota"] });

        Assert.Equal(6, hepsi.TotalCount);
        Assert.Equal(3, toyota.TotalCount);
        Assert.All(toyota.Items, item => Assert.Contains(ModelOf(item), new[] { Corolla, Yaris }));
    }

    [Fact(DisplayName = "0021/AC-37: Toyota ve Honda birlikte seçildiğinde liste iki markanın ilanlarını gösterir.")]
    public async Task Birlestirir_ToyotaVeHonda()
    {
        await StokAsync();

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with { Brands = ["toyota", "honda"] });

        Assert.Equal(5, sayfa.TotalCount);
        Assert.All(sayfa.Items, item => Assert.Contains(ModelOf(item), new[] { Corolla, Yaris, Civic }));
    }

    [Fact(DisplayName = "0021/AC-38: Marka ve kasa tipi birlikte seçildiğinde liste ikisine de uyan ilanları gösterir.")]
    public async Task Kesistirir_MarkaVeKasa()
    {
        await StokAsync();

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(new ShowcaseListingFilter(["suv"], 1, "tr") { Brands = ["toyota"] });

        ShowcaseListingDto ilan = Assert.Single(sayfa.Items);
        Assert.Equal(Yaris, ModelOf(ilan));
    }

    [Fact(DisplayName = "0021/AC-39: Seçim adres satırında `brand=toyota` olarak görünür; bu adres yeni bir sekmede açıldığında aynı daraltma gelir.")]
    public async Task Okur_BrandParametresini_Uctan()
    {
        await StokAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database, Markalar);

        using JsonDocument belge = JsonDocument.Parse(await api.Client.GetStringAsync("/api/catalog/showcase-listings?brand=toyota&locale=tr"));

        Assert.Equal(3, belge.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Theory(DisplayName = "0021/AC-40: Bilinmeyen bir marka değeri (`brand=yok`) ve büyük harfli bir değer (`brand=Toyota`) yok sayılır ve liste hata vermez (D20).")]
    [InlineData("?brand=yok")]
    [InlineData("?brand=Toyota")]
    [InlineData("?brand=yok&brand=Toyota")]
    public async Task YokSayar_BilinmeyenVeBuyukHarfliDegeri(string sorgu)
    {
        await StokAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database, Markalar);

        HttpResponseMessage yanit = await api.Client.GetAsync($"/api/catalog/showcase-listings{sorgu}");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        using JsonDocument belge = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync());
        Assert.Equal(6, belge.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact(DisplayName = "0021/AC-41: Adresle gelen, var olan ama yayında ilanı kalmamış bir marka uygulanır, liste boş sonucu gösterir ve marka eksende işaretli görünür; işareti kaldırınca liste genişler (D21).")]
    public async Task Uygular_StoksuzSeciliMarkayi_EksendeTutar()
    {
        await StokAsync();

        PagedResult<ShowcaseListingDto> secili = await SorAsync(Filtre() with { Brands = ["fiat"] });
        IReadOnlyList<ShowcaseBrandOptionDto> eksen = await Eksen().GetForListAsync("tr", ["fiat", "yok"], CancellationToken.None);
        PagedResult<ShowcaseListingDto> kaldirilmis = await SorAsync(Filtre());

        Assert.Equal(0, secili.TotalCount);
        Assert.Equal(["fiat", "honda", "renault", "toyota"], eksen.Select(item => item.Slug));
        Assert.Equal(6, kaldirilmis.TotalCount);
    }

    [Fact(DisplayName = "0021/AC-42: Güncellemeden önce yayında olan bir Toyota ilanı marka ekseninde Toyota seçilince listelenir (D16).")]
    public async Task Listeler_GuncellemedenOnceYayindakiToyotaIlanini()
    {
        await database.ResetAsync();

        // Güncelleme öncesi satır: okuma modeli bugünkü gibi yalnız model kimliğini taşır ve
        // bu kart o satıra hiç dokunmaz (D15) — eski adresi ve kimliğiyle duruyor.
        ShowcaseListing eski = new ShowcaseListingBuilder().WithVehicleModelId(Corolla).WithSlug("toyota-corolla-2019-eski0001").Build();
        await ShowcaseQueryHarness.SeedAsync(database, [eski, new ShowcaseListingBuilder().WithVehicleModelId(Civic).Build()]);

        PagedResult<ShowcaseListingDto> sayfa = await SorAsync(Filtre() with { Brands = ["toyota"] });

        Assert.Equal(eski.ListingId, Assert.Single(sayfa.Items).ListingId);
    }

    [Fact(DisplayName = "0021/AC-44: Arapça listede marka ekseni Arapça görünen adları gösterir ve sağdan sola düzende çalışır.")]
    public async Task Doner_ArapcaGorunenAdlari()
    {
        await StokAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database, Markalar);

        using JsonDocument belge = JsonDocument.Parse(await api.Client.GetStringAsync("/api/catalog/showcase-brands?locale=ar"));

        Assert.Equal(["هوندا", "رينو", "تويوتا"], belge.RootElement.EnumerateArray().Select(item => item.GetProperty("label").GetString()));
    }

    [Fact(DisplayName = "0021/AC-45: \"Aracınızı bulun\" formu yalnız \"ana sayfada göster\" işaretli ve en az bir yayındaki ilanı olan markaları sunar.")]
    public async Task Sunar_IsaretliVeStokluMarkalari_AnaSayfada()
    {
        await StokAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database, Markalar);

        using JsonDocument belge = JsonDocument.Parse(await api.Client.GetStringAsync("/api/catalog/showcase-brands?scope=home&locale=tr"));

        Assert.Equal(["renault", "toyota"], belge.RootElement.EnumerateArray().Select(item => item.GetProperty("slug").GetString()));
        Assert.Equal(["label", "slug"], belge.RootElement[0].EnumerateObject().Select(item => item.Name).Order(StringComparer.Ordinal));
    }

    [Fact(DisplayName = "0021/AC-46: İşaretli ama yayında ilanı olmayan bir marka formda sunulmaz.")]
    public async Task Sunmaz_IsaretliStoksuzMarkayi()
    {
        await StokAsync();

        IReadOnlyList<ShowcaseBrandOptionDto> form = await Eksen().GetForHomeAsync("tr", CancellationToken.None);

        Assert.DoesNotContain(form, item => item.Slug == "fiat");
    }

    [Fact(DisplayName = "0021/AC-47: İşaretsiz bir markanın yayındaki ilanı olsa da formda sunulmaz.")]
    public async Task Sunmaz_IsaretsizStokluMarkayi()
    {
        await StokAsync();

        IReadOnlyList<ShowcaseBrandOptionDto> form = await Eksen().GetForHomeAsync("tr", CancellationToken.None);

        Assert.DoesNotContain(form, item => item.Slug == "honda");
        Assert.Contains(await Eksen().GetForListAsync("tr", [], CancellationToken.None), item => item.Slug == "honda");
    }

    [Fact]
    public async Task Doner_BosListe_VitrinBossa()
    {
        await database.ResetAsync();

        Assert.Empty(await Eksen().GetForHomeAsync("tr", CancellationToken.None));
        Assert.Empty(await Eksen().GetForListAsync("tr", [], CancellationToken.None));
    }

    /// <summary>Toyota üç (iki sedan Corolla, bir SUV Yaris), Honda iki, Renault bir ilan.</summary>
    private async Task StokAsync()
    {
        await database.ResetAsync();
        await ShowcaseQueryHarness.SeedAsync(database,
        [
            new ShowcaseListingBuilder().WithVehicleModelId(Corolla).WithBodyType("sedan").Build(),
            new ShowcaseListingBuilder().WithVehicleModelId(Corolla).WithBodyType("sedan").Build(),
            new ShowcaseListingBuilder().WithVehicleModelId(Yaris).WithBodyType("suv").Build(),
            new ShowcaseListingBuilder().WithVehicleModelId(Civic).WithBodyType("suv").Build(),
            new ShowcaseListingBuilder().WithVehicleModelId(Civic).WithBodyType("sedan").Build(),
            new ShowcaseListingBuilder().WithVehicleModelId(Clio).WithBodyType("hatchback").Build(),
        ]);
    }

    private ShowcaseBrandQueries Eksen() => new(database.CreateContext(), Markalar, ShowcaseQueryHarness.Locales);

    private Task<PagedResult<ShowcaseListingDto>> SorAsync(ShowcaseListingFilter filtre) =>
        ShowcaseQueryHarness.QueryAsync(database, filtre, brands: Markalar);

    private static ShowcaseListingFilter Filtre() => new([], 1, "tr");

    private Guid ModelOf(ShowcaseListingDto ilan)
    {
        using VehicleCatalogDbContext context = database.CreateContext();
        return context.ShowcaseListings.Single(row => row.ListingId == ilan.ListingId).VehicleModelId;
    }
}
