using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Infrastructure;

namespace CarGallery.Modules.VehicleReference.Tests;

/// <summary>
/// 0010a/R4 — panelin seçtirmesi gereken ALTI kümenin okuma yüzeyi.
///
/// <para>Bugüne kadar yalnız kasa tipi açıktı; model ve dört terim türü için okuma yüzeyi
/// hiç yoktu ve panel onları hiçbir yerden okuyamazdı.</para>
///
/// <para>Varlık doğrulaması da burada: `VehicleSpecification` slug'ın yalnız KALIBINI
/// denetliyor, referans veride var olup olmadığına bakmıyor (D14).</para>
/// </summary>
[Collection(ReferenceDatabaseCollection.Name)]
public sealed class ReferansOkumaYuzeyiTests(ReferenceDatabase database)
{
    [Fact(DisplayName = "0010a/AC-12: Panel; model, kasa tipi, donanım paketi, yakıt, vites ve çekiş kümelerini referans veriden listeler")]
    public async Task Listeler_AltiKumeyi()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();

        Assert.NotEmpty(await new VehicleModelQueries(context).GetAllAsync("tr", CancellationToken.None));
        Assert.NotEmpty(await new BodyTypeQueries(context).GetAllAsync("tr", CancellationToken.None));

        ReferenceTermQueries terms = new(context);

        foreach (string kind in Turler)
        {
            Assert.NotEmpty(await terms.GetByKindAsync(kind, "tr", CancellationToken.None));
        }
    }

    /// <summary>
    /// AC-13'ün ölçülebilir hâli: etiket **slug'a eşit olamaz**. Ham slug göstermek, bir
    /// locale'de çeviri bulunamadığında olurdu; kanonik ada düşmek onu engelliyor.
    /// </summary>
    [Theory(DisplayName = "0010a/AC-13: Referans listeleri personelin locale'indeki etiketlerle gelir; hiçbir yerde ham slug gösterilmez")]
    [InlineData("tr")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task Gelir_LocaleEtiketiyle_HamSlugYok(string locale)
    {
        await using VehicleReferenceDbContext context = database.CreateContext();

        IReadOnlyList<BodyTypeDto> kasalar = await new BodyTypeQueries(context).GetAllAsync(locale, CancellationToken.None);
        Assert.All(kasalar, kasa => Assert.False(string.IsNullOrWhiteSpace(kasa.Label)));
        Assert.All(kasalar, kasa => Assert.NotEqual(kasa.Slug, kasa.Label));

        ReferenceTermQueries terms = new(context);

        foreach (string kind in Turler)
        {
            IReadOnlyList<ReferenceTermDto> terimler = await terms.GetByKindAsync(kind, locale, CancellationToken.None);

            Assert.All(terimler, terim => Assert.False(string.IsNullOrWhiteSpace(terim.Label)));
            Assert.All(terimler, terim => Assert.NotEqual(terim.Slug, terim.Label));
            Assert.All(terimler, terim => Assert.Equal(kind, terim.Kind));
        }
    }

    /// <summary>
    /// Model etiketleri bugün üç locale'de de aynı Latin adı taşıyabilir (marka adları
    /// çevrilmez), bu yüzden "etiket ≠ slug" ölçüsü modelde işlemez. Ölçülen şey, modelin
    /// locale'de bir ETİKETİ olduğu ve kimliğin ham hâliyle gösterilmediği.
    /// </summary>
    [Theory]
    [InlineData("tr")]
    [InlineData("ar")]
    [InlineData("en")]
    public async Task Tasir_ModelEtiketi_HerLocaleDe(string locale)
    {
        await using VehicleReferenceDbContext context = database.CreateContext();

        IReadOnlyList<VehicleModelDto> modeller =
            await new VehicleModelQueries(context).GetAllAsync(locale, CancellationToken.None);

        Assert.All(modeller, model => Assert.False(string.IsNullOrWhiteSpace(model.Label)));
        Assert.All(modeller, model => Assert.DoesNotContain(model.Id.ToString(), model.Label, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0010a/AC-14: Başlangıç model kümesi kurulum verisiyle gelir; taze bir kurulumda model listesi boş değildir")]
    public async Task BosDegil_TazeKurulumdaModelListesi()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();

        // Fikstür yalnız `MigrateAsync` koşuyor — hiçbir test seed'i çalışmıyor. Yani bu
        // liste doluysa, dolduran şey KURULUM VERİSİDİR.
        IReadOnlyList<VehicleModelDto> modeller =
            await new VehicleModelQueries(context).GetAllAsync("tr", CancellationToken.None);

        Assert.NotEmpty(modeller);
    }

    [Fact]
    public async Task Dondurur_EksikTerimleri_VarOlanlariDegil()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        ReferenceTermQueries terms = new(context);

        IReadOnlyList<ReferenceTermDto> yakitlar =
            await terms.GetByKindAsync(ReferenceTermKinds.FuelType, "tr", CancellationToken.None);

        IReadOnlyList<(string Kind, string Slug)> eksikler = await terms.FindMissingAsync(
            [
                (ReferenceTermKinds.FuelType, yakitlar[0].Slug),
                (ReferenceTermKinds.FuelType, "olmayan-yakit"),
            ],
            CancellationToken.None);

        Assert.Equal([(ReferenceTermKinds.FuelType, "olmayan-yakit")], eksikler);
    }

    /// <summary>
    /// Tür AYRACI önemli: aynı slug başka bir türde var olabilir ve eşleşme türü de
    /// denetlemezse yanlış kümeden bir değer geçerli sayılırdı.
    /// </summary>
    [Fact]
    public async Task Ayirir_Turleri_EksikAramasinda()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();
        ReferenceTermQueries terms = new(context);

        IReadOnlyList<ReferenceTermDto> yakitlar =
            await terms.GetByKindAsync(ReferenceTermKinds.FuelType, "tr", CancellationToken.None);

        IReadOnlyList<(string Kind, string Slug)> eksikler = await terms.FindMissingAsync(
            [(ReferenceTermKinds.TrimLevel, yakitlar[0].Slug)],
            CancellationToken.None);

        Assert.Single(eksikler);
    }

    [Fact]
    public async Task Dogrular_ModelVeKasaVarligini()
    {
        await using VehicleReferenceDbContext context = database.CreateContext();

        IReadOnlyList<VehicleModelDto> modeller =
            await new VehicleModelQueries(context).GetAllAsync("tr", CancellationToken.None);

        Assert.True(await new VehicleModelQueries(context).ExistsAsync(modeller[0].Id, CancellationToken.None));
        Assert.False(await new VehicleModelQueries(context).ExistsAsync(Guid.NewGuid(), CancellationToken.None));

        IReadOnlyList<BodyTypeDto> kasalar =
            await new BodyTypeQueries(context).GetAllAsync("tr", CancellationToken.None);

        Assert.True(await new BodyTypeQueries(context).ExistsAsync(kasalar[0].Slug, CancellationToken.None));
        Assert.False(await new BodyTypeQueries(context).ExistsAsync("olmayan-kasa", CancellationToken.None));
    }

    private static string[] Turler =>
    [
        ReferenceTermKinds.FuelType,
        ReferenceTermKinds.TransmissionType,
        ReferenceTermKinds.DrivetrainType,
        ReferenceTermKinds.TrimLevel,
    ];
}
