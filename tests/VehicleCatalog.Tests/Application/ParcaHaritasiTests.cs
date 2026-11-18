using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// `0016a` — parça haritası: alan, değer kuralları, kalıcılık ve yönetim sözleşmesi.
/// Gerçek PostgreSQL, gerçek komut ve gerçek yönetim sorgusu.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class ParcaHaritasiTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Kayit = new(2026, 11, 18, 9, 0, 0, TimeSpan.Zero);

    /// <summary>AC-5'in haritası: dört durumun hepsi.</summary>
    private static readonly IReadOnlyDictionary<string, string> Ac5 = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["front_bumper"] = "painted",
        ["trunk_lid"] = "replaced",
        ["left_rear_door"] = "local_painted",
    };

    [Fact(DisplayName = "0016a/AC-1: Migration'ları sıfırdan uygulanmış bir kurulumda paint_change_map alanının ayarı form durumu Opsiyonel, public görünürlüğü Görünür'dür")]
    public async Task Seedlenir_OpsiyonelGorunur_SifirdanKurulumda()
    {
        await using VehicleCatalogDbContext context = await TazeVeritabaniAsync("ac1", hedef: null);

        VehicleFieldSetting ayar = await context.VehicleFieldSettings.AsNoTracking()
            .SingleAsync(item => item.FieldKey == VehicleFieldKeys.PaintChangeMap);

        Assert.Equal(FieldFormState.Optional, ayar.FormState);
        Assert.Equal(FieldPublicVisibility.Visible, ayar.PublicVisibility);
        Assert.False(ayar.IsCore);
    }

    [Fact(DisplayName = "0016a/AC-2: Tramer'in ayarı Opsiyonel + Gizli olan mevcut bir kurulumda migration uygulandıktan sonra paint_change_map ayarı Opsiyonel + Görünür olarak eklenmiştir ve tramer'in ayarı Opsiyonel + Gizli kalır")]
    public async Task Eklenir_MevcutKurulumda_TramerDegismez()
    {
        // Önceki migration'a kadar kurulmuş "mevcut" bir kurulum.
        await using VehicleCatalogDbContext context = await TazeVeritabaniAsync("ac2", hedef: "20261011115335_AddListingSlugAndOutbox");

        Assert.False(await context.VehicleFieldSettings.AnyAsync(item => item.FieldKey == VehicleFieldKeys.PaintChangeMap));
        VehicleFieldSetting oncekiTramer = await context.VehicleFieldSettings.AsNoTracking()
            .SingleAsync(item => item.FieldKey == VehicleFieldKeys.DamageRecordAmount);
        Assert.Equal((FieldFormState.Optional, FieldPublicVisibility.Hidden), (oncekiTramer.FormState, oncekiTramer.PublicVisibility));

        await context.Database.MigrateAsync();

        Dictionary<string, (FieldFormState, FieldPublicVisibility)> ayarlar = await context.VehicleFieldSettings.AsNoTracking()
            .ToDictionaryAsync(item => item.FieldKey, item => (item.FormState, item.PublicVisibility));
        Assert.Equal((FieldFormState.Optional, FieldPublicVisibility.Visible), ayarlar[VehicleFieldKeys.PaintChangeMap]);
        Assert.Equal((FieldFormState.Optional, FieldPublicVisibility.Hidden), ayarlar[VehicleFieldKeys.DamageRecordAmount]);
    }

    [Fact(DisplayName = "0016a/AC-3: Yönetim formunun alan tanımı listesinde paint_change_map tek bir alan olarak, parça haritası tipiyle döner; parça başına ayrı alan yoktur ve tramer Amount tipiyle kalır")]
    public async Task Doner_TekAlan_ParcaHaritasiTipiyle()
    {
        await database.ResetAsync();
        await using VehicleCatalogDbContext context = database.CreateContext();

        IReadOnlyList<VehicleAdminFieldDto> alanlar = await Sorgu(context).GetFormFieldsAsync(CancellationToken.None);

        Assert.Single(alanlar, alan => alan.FieldKey == VehicleFieldKeys.PaintChangeMap);
        Assert.Equal(nameof(FieldValueKind.PartMap), alanlar.Single(alan => alan.FieldKey == VehicleFieldKeys.PaintChangeMap).Kind);
        Assert.DoesNotContain(alanlar, alan => alan.FieldKey.StartsWith("paint_change_map.", StringComparison.Ordinal));
        Assert.Equal(nameof(FieldValueKind.Amount), alanlar.Single(alan => alan.FieldKey == VehicleFieldKeys.DamageRecordAmount).Kind);
    }

    [Fact(DisplayName = "0016a/AC-4: paint_change_map ayarı Zorunlu iken haritası olmayan bir aracın ilanı yayına alınamaz; aynı araca tam harita girildikten sonra ilan yayına alınır")]
    public async Task Gerektirir_Harita_ZorunluIken()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC4");
        HashSet<string> zorunlu = new([VehicleFieldKeys.PaintChangeMap], StringComparer.Ordinal);
        FakeMediaAssetQueries medya = new();
        HashSet<Guid> hazir = [medya.Ekle(MediaProcessingStatus.Ready)];

        Vehicle once = await Oku(arac.Id);
        once.AttachMediaAsset(hazir.Single());
        IReadOnlyList<Error> eksikOnce = PublishEligibility.Evaluate(once, hazir, zorunlu);
        Assert.Contains(eksikOnce, error => error.Code == PublishErrors.RequiredField && error.Parameters!["fields"] == VehicleFieldKeys.PaintChangeMap);

        Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);

        Vehicle sonra = await Oku(arac.Id);
        sonra.AttachMediaAsset(hazir.Single());
        Assert.Empty(PublishEligibility.Evaluate(sonra, hazir, zorunlu));
    }

    [Fact(DisplayName = "0016a/AC-5: Tam harita kaydedilir ve yönetim detay yanıtı aynı 13 parça–durum çiftini döner")]
    public async Task Kaydedilir_TamHarita()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC5");

        Result sonuc = await KaydetAsync(context, arac, Tam(Ac5));

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        Assert.Equal(Beklenen(Ac5), (await YonetimHaritasiAsync(arac.Id))!.Select(item => (item.Part, item.State)));
    }

    [Fact(DisplayName = "0016a/AC-6: AC-5'in haritası C10'un ters sırasıyla gönderildiğinde de yönetim detay yanıtı parçaları C10 sırasıyla döner")]
    public async Task Doner_C10Sirasiyla_TersGonderilse()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC6");

        Assert.True((await KaydetAsync(context, arac, [.. Enumerable.Reverse(Tam(Ac5))])).IsSuccess);

        IReadOnlyList<VehicleAdminPaintChangeDto> harita = (await YonetimHaritasiAsync(arac.Id))!;
        Assert.Equal("front_bumper", harita[0].Part);
        Assert.Equal("right_rear_fender", harita[^1].Part);
        Assert.Equal(PaintChangeParts.Ordered, harita.Select(item => item.Part));
    }

    [Fact(DisplayName = "0016a/AC-7: trunk_lid eksik, 12 parçalı bir harita reddedilir ve araçta önceden kayıtlı tam harita değişmeden kalır")]
    public async Task Reddeder_Eksik_OncekiKalir()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC7");
        Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);

        Result sonuc = await KaydetAsync(context, arac, Hepsi("original").Where(item => item.Part != "trunk_lid").ToList());

        Assert.True(sonuc.IsFailure);
        Assert.Equal(PaintChangeMap.Incomplete, sonuc.Error.Code);
        Assert.Equal(Beklenen(Ac5), (await YonetimHaritasiAsync(arac.Id))!.Select(item => (item.Part, item.State)));
    }

    [Fact(DisplayName = "0016a/AC-8: roof ve right_rear_door eksik, 11 parçalı bir haritanın hata yanıtı eksik parçaları kod olarak ve C10 sırasıyla taşır; çevrilmiş parça adı yoktur")]
    public async Task Tasir_EksikParcalar_KodOlarak()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC8");

        Result sonuc = await KaydetAsync(context, arac, Hepsi("original").Where(item => item.Part is not ("roof" or "right_rear_door")).Reverse().ToList());

        Assert.Equal(PaintChangeMap.Incomplete, sonuc.Error.Code);
        Assert.Equal("roof,right_rear_door", sonuc.Error.Parameters!["parts"]);
        Assert.Equal(VehicleFieldKeys.PaintChangeMap, sonuc.Error.Parameters!["field"]);
        Assert.DoesNotContain(sonuc.Error.Parameters!.Values, value => value.Contains("Tavan", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0016a/AC-9: 13 geçerli parçaya ek olarak tanınmayan bir parça kodu (windshield) taşıyan harita reddedilir")]
    public async Task Reddeder_TaninmayanParca()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC9");

        Result sonuc = await KaydetAsync(context, arac, [.. Hepsi("original"), new PaintChangeEntry("windshield", "original")]);

        Assert.Equal(VehicleErrors.InvalidFieldValue, sonuc.Error.Code);
        Assert.Null(await YonetimHaritasiAsync(arac.Id));
    }

    [Theory(DisplayName = "0016a/AC-10: Bir parçanın durumu dört koddan biri değilse harita reddedilir: scratched ve unspecified ayrı ayrı reddedilir")]
    [InlineData("scratched")]
    [InlineData("unspecified")]
    public async Task Reddeder_TaninmayanDurum(string durum)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC10");

        Result sonuc = await KaydetAsync(context, arac, Tam(new Dictionary<string, string> { ["hood"] = durum }));

        Assert.Equal(VehicleErrors.InvalidFieldValue, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0016a/AC-11: hood iki kez geçen 14 elemanlı harita ve hood iki kez geçip roof hiç geçmeyen 13 elemanlı harita reddedilir; ikincisinin hata yanıtı eksik parça olarak roof'u taşır")]
    public async Task Reddeder_TekrarlananParca()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC11");

        Result ondort = await KaydetAsync(context, arac, [.. Hepsi("original"), new PaintChangeEntry("hood", "painted")]);
        Result onuc = await KaydetAsync(context, arac, [.. Hepsi("original").Where(item => item.Part != "roof"), new PaintChangeEntry("hood", "painted")]);

        Assert.Equal(VehicleErrors.InvalidFieldValue, ondort.Error.Code);
        Assert.Equal(PaintChangeMap.Incomplete, onuc.Error.Code);
        Assert.Equal("roof", onuc.Error.Parameters!["parts"]);
    }

    [Fact(DisplayName = "0016a/AC-12: Kodlar tam ve harfe duyarlı eşleşir: durumu Painted olan veya parçası Front_Bumper olan harita reddedilir")]
    public async Task Reddeder_HarfeDuyarli()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC12");

        Result durum = await KaydetAsync(context, arac, Tam(new Dictionary<string, string> { ["hood"] = "Painted" }));
        Result parca = await KaydetAsync(context, arac, [.. Hepsi("original").Where(item => item.Part != "front_bumper"), new PaintChangeEntry("Front_Bumper", "original")]);

        Assert.Equal(VehicleErrors.InvalidFieldValue, durum.Error.Code);
        Assert.Equal(VehicleErrors.InvalidFieldValue, parca.Error.Code);
    }

    [Fact(DisplayName = "0016a/AC-13: Parça haritası opsiyonel alanların metin değeri yoluyla gönderildiğinde reddedilir; harita yalnız yapılandırılmış alanıyla kabul edilir")]
    public async Task Reddeder_MetinYolu()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC13");
        string json = """[{"part":"front_bumper","state":"original"}]""";

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with { OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.PaintChangeMap, json, null)] },
            CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidFieldValue, sonuc.Error.Code);
        Assert.Null(await YonetimHaritasiAsync(arac.Id));
    }

    /// <summary>
    /// Triyaj BULGU-4 — sözleşmenin yazılı tarifi (`UpdateVehicleOptionalValue` özeti) ile
    /// davranış aynı şeyi söyler: öğe listede HİÇ yoksa kayıtlı haritaya dokunulmaz; öğe
    /// gönderilip <c>partMap</c> taşımıyorsa (<c>null</c>) istek reddedilir (AC-13) ve
    /// kayıtlı harita değişmez.
    /// </summary>
    [Fact]
    public async Task Ayirir_GonderilmeyenOgeyi_PartMapsizOgeden()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHB4");
        Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);

        // Öğe hiç yok: dokunulmaz.
        Assert.True((await Komut(context).ExecuteAsync(Istek(arac) with { OptionalValues = [] }, CancellationToken.None)).IsSuccess);
        Assert.Equal(Beklenen(Ac5), (await YonetimHaritasiAsync(arac.Id))!.Select(item => (item.Part, item.State)));

        // Öğe var, partMap null: reddedilir.
        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with { OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.PaintChangeMap, null, null, null)] },
            CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidFieldValue, sonuc.Error.Code);
        Assert.Equal(Beklenen(Ac5), (await YonetimHaritasiAsync(arac.Id))!.Select(item => (item.Part, item.State)));
    }

    [Fact(DisplayName = "0016a/AC-14: Tramer için yapılandırılmış harita gönderilmesi reddedilir ve tramer'in kayıtlı tutarı değişmez")]
    public async Task Reddeder_TramereHarita()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC14");
        Assert.True((await Komut(context).ExecuteAsync(
            Istek(arac) with { OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.DamageRecordAmount, "12500.00", null)] },
            CancellationToken.None)).IsSuccess);

        Result sonuc = await Komut(context).ExecuteAsync(
            Istek(arac) with { OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.DamageRecordAmount, null, null, Hepsi("original"))] },
            CancellationToken.None);

        Assert.Equal(VehicleErrors.InvalidFieldValue, sonuc.Error.Code);
        Assert.Equal(12_500.00m, (await Oku(arac.Id)).OptionalFieldValues.Single(item => item.FieldKey == VehicleFieldKeys.DamageRecordAmount).Value.Amount!.Value.Amount);
    }

    [Fact(DisplayName = "0016a/AC-15: paint_change_map ayarı Kapalı iken tam harita gönderilmesi vehicle.field_closed ile reddedilir")]
    public async Task Reddeder_KapaliIken()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC15");
        await AyarAsync(FieldFormState.Closed, FieldPublicVisibility.Visible);

        try
        {
            Result sonuc = await KaydetAsync(context, arac, Tam(Ac5));
            Assert.Equal(VehicleErrors.FieldClosed, sonuc.Error.Code);
        }
        finally
        {
            await AyarAsync(FieldFormState.Optional, FieldPublicVisibility.Visible);
        }
    }

    [Fact(DisplayName = "0016a/AC-16: Saklanan değer yalnız parça ve durum kodlarını taşır; Türkçe, İngilizce veya Arapça parça/durum adı saklanan değerde geçmez")]
    public async Task Saklar_YalnizKod()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC16");
        Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);

        await using NpgsqlConnection baglanti = new(database.ConnectionString);
        await baglanti.OpenAsync();
        await using NpgsqlCommand komut = new(
            $"SELECT value_part_map::text FROM {VehicleCatalogDbContext.Schema}.vehicle_optional_field_values WHERE vehicle_id = @id AND field_key = 'paint_change_map'",
            baglanti);
        komut.Parameters.AddWithValue("id", arac.Id);
        string saklanan = (string)(await komut.ExecuteScalarAsync())!;

        Assert.Contains("\"front_bumper\"", saklanan, StringComparison.Ordinal);
        Assert.Contains("\"local_painted\"", saklanan, StringComparison.Ordinal);
        foreach (string ad in new[] { "Ön tampon", "boyalı", "Front bumper", "Painted", "مصد" })
        {
            Assert.DoesNotContain(ad, saklanan, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(@"[؀-ۿ]", saklanan);
    }

    [Fact(DisplayName = "0016a/AC-17: Yönetim detay yanıtı haritayı kod listesi olarak taşır ve ar ile tr locale'li oturumlarda aynıdır")]
    public async Task Tasir_KodListesi_LocaledenBagimsiz()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC17");
        Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);

        // Yönetim detay sorgusu locale ALMAZ: yanıtı bir oturumun diline bağlayan bir girdi
        // yoktur. İki okuma ve iki farklı locale seçeneğiyle kurulmuş sorgu aynı kodları döner.
        IReadOnlyList<VehicleAdminPaintChangeDto> tr = (await YonetimHaritasiAsync(arac.Id, "tr"))!;
        IReadOnlyList<VehicleAdminPaintChangeDto> ar = (await YonetimHaritasiAsync(arac.Id, "ar"))!;

        Assert.Equal(tr, ar);
        Assert.All(tr, item => Assert.Matches("^[a-z_]+$", item.Part + item.State));
    }

    [Fact(DisplayName = "0016a/AC-18: Alan Kapalı'ya çekildiğinde yönetim detay yanıtında değer bulunmaz; alan yeniden Opsiyonel yapıldığında 13 çift değişmeden geri döner")]
    public async Task Gizler_KapaliIken_GeriGetirir()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC18");
        Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);

        try
        {
            await AyarAsync(FieldFormState.Closed, FieldPublicVisibility.Visible);
            Assert.Null(await YonetimHaritasiAsync(arac.Id));
        }
        finally
        {
            await AyarAsync(FieldFormState.Optional, FieldPublicVisibility.Visible);
        }

        Assert.Equal(Beklenen(Ac5), (await YonetimHaritasiAsync(arac.Id))!.Select(item => (item.Part, item.State)));
    }

    [Fact(DisplayName = "0016a/AC-19: Alan Opsiyonel + Gizli iken tam harita kaydedilebilir ve yönetim detay yanıtında döner")]
    public async Task Kaydeder_GizliIken()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC19");

        try
        {
            await AyarAsync(FieldFormState.Optional, FieldPublicVisibility.Hidden);
            Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);
            Assert.Equal(Beklenen(Ac5), (await YonetimHaritasiAsync(arac.Id))!.Select(item => (item.Part, item.State)));
        }
        finally
        {
            await AyarAsync(FieldFormState.Optional, FieldPublicVisibility.Visible);
        }
    }

    [Fact(DisplayName = "0016a/AC-20: Tam haritası kayıtlı bir araçta harita temizlenip kaydedildiğinde araçta değer kalmaz ve yönetim detay yanıtında alanın değeri bulunmaz")]
    public async Task Temizler_BosHarita()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC20");
        Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);

        Assert.True((await KaydetAsync(context, arac, [])).IsSuccess);

        Assert.DoesNotContain((await Oku(arac.Id)).OptionalFieldValues, item => item.FieldKey == VehicleFieldKeys.PaintChangeMap);
        Assert.Null(await YonetimHaritasiAsync(arac.Id));
    }

    [Fact(DisplayName = "0016a/AC-21: AC-20'deki temizleme, aynı aracın tramer değerini değiştirmez")]
    public async Task Korur_Tramer_Temizlemede()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC21");
        Assert.True((await Komut(context).ExecuteAsync(
            Istek(arac) with
            {
                OptionalValues =
                [
                    new OptionalFieldInput(VehicleFieldKeys.DamageRecordAmount, "12500.00", null),
                    new OptionalFieldInput(VehicleFieldKeys.PaintChangeMap, null, null, Tam(Ac5)),
                ],
            },
            CancellationToken.None)).IsSuccess);

        Assert.True((await KaydetAsync(context, arac, [])).IsSuccess);

        Vehicle okunan = await Oku(arac.Id);
        Assert.Equal(12_500.00m, okunan.OptionalFieldValues.Single(item => item.FieldKey == VehicleFieldKeys.DamageRecordAmount).Value.Amount!.Value.Amount);
        Assert.DoesNotContain(okunan.OptionalFieldValues, item => item.FieldKey == VehicleFieldKeys.PaintChangeMap);
    }

    [Fact(DisplayName = "0016a/AC-22: Haritası olmayan bir araç, harita için hiçbir seçim yapılmadan kaydedildiğinde değer oluşmaz; '13 parçası orijinal' bir değer de yazılmaz")]
    public async Task Olusturmaz_SecimYokken()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHAC22");

        Assert.True((await KaydetAsync(context, arac, [])).IsSuccess);

        Assert.DoesNotContain((await Oku(arac.Id)).OptionalFieldValues, item => item.FieldKey == VehicleFieldKeys.PaintChangeMap);
    }

    /// <summary>`0016a` D26 — tipli kanal: alan public ve değerliyken okuma modeline yalnız kod iner; gizliyken inmez.</summary>
    [Fact]
    public async Task Yansitir_YalnizPublicIken_TipliKanala()
    {
        await database.ResetAsync();
        FakeMediaAssetQueries medya = new();
        await using VehicleCatalogDbContext context = database.CreateContext();
        (Guid Id, string Vin) arac = await AracKaydetAsync(context, "PHYAN");
        Assert.True((await KaydetAsync(context, arac, Tam(Ac5))).IsSuccess);
        await YayinlaVeYansitAsync(arac.Id, medya);

        List<(string, string)>? gorunur = await OkumaModeliAsync(arac.Id);
        Assert.Equal(Beklenen(Ac5), gorunur);

        try
        {
            await AyarAsync(FieldFormState.Optional, FieldPublicVisibility.Hidden);
            await YayinlaVeYansitAsync(arac.Id, medya, yayinla: false);
            Assert.Null(await OkumaModeliAsync(arac.Id));
        }
        finally
        {
            await AyarAsync(FieldFormState.Optional, FieldPublicVisibility.Visible);
        }
    }

    // ── yardımcılar ──────────────────────────────────────────────────────────────

    private static List<PaintChangeEntry> Hepsi(string durum) =>
        [.. PaintChangeParts.Ordered.Select(part => new PaintChangeEntry(part, durum))];

    private static List<PaintChangeEntry> Tam(IReadOnlyDictionary<string, string> farklar) =>
        [.. PaintChangeParts.Ordered.Select(part => new PaintChangeEntry(part, farklar.TryGetValue(part, out string? durum) ? durum : "original"))];

    private static IEnumerable<(string, string)> Beklenen(IReadOnlyDictionary<string, string> farklar) =>
        Tam(farklar).Select(entry => (entry.Part, entry.State));

    private static Task<Result> KaydetAsync(VehicleCatalogDbContext context, (Guid Id, string Vin) arac, IReadOnlyList<PaintChangeEntry> harita) =>
        Komut(context).ExecuteAsync(
            Istek(arac) with { OptionalValues = [new OptionalFieldInput(VehicleFieldKeys.PaintChangeMap, null, null, harita)] },
            CancellationToken.None);

    private async Task<IReadOnlyList<VehicleAdminPaintChangeDto>?> YonetimHaritasiAsync(Guid id, string varsayilan = "tr")
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        VehicleAdminDetailDto? detay = await Sorgu(context, varsayilan).FindAsync(id, CancellationToken.None);
        return detay!.OptionalValues.SingleOrDefault(item => item.FieldKey == VehicleFieldKeys.PaintChangeMap)?.PartMap;
    }

    private static VehicleAdminQueries Sorgu(VehicleCatalogDbContext context, string varsayilan = "tr") => new(
        context,
        new SahteModelSorgusu(),
        new ShowcaseLocaleOptions(Locale.Create(varsayilan).Value, [Locale.Create("tr").Value, Locale.Create("ar").Value]),
        new CatalogAdminPagingOptions(25, 100));

    private async Task AyarAsync(FieldFormState durum, FieldPublicVisibility gorunurluk)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        VehicleFieldSetting ayar = await context.VehicleFieldSettings.SingleAsync(item => item.FieldKey == VehicleFieldKeys.PaintChangeMap);
        ayar.FormState = durum;
        ayar.PublicVisibility = gorunurluk;
        await context.SaveChangesAsync();
    }

    private async Task<Vehicle> Oku(Guid id)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        return await context.Vehicles.AsNoTracking()
            .Include(vehicle => vehicle.OptionalFieldValues)
            .SingleAsync(vehicle => vehicle.Id == new VehicleId(id));
    }

    private async Task<List<(string, string)>?> OkumaModeliAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        var satir = await context.ShowcaseListings.AsNoTracking().SingleAsync(row => row.VehicleId == aracId);
        return satir.PaintChangeMap?.Select(row => (row.Part, row.State)).ToList();
    }

    private async Task YayinlaVeYansitAsync(Guid aracId, FakeMediaAssetQueries medya, bool yayinla = true)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle arac = await context.Vehicles.Include(vehicle => vehicle.OptionalFieldValues).SingleAsync(vehicle => vehicle.Id == new VehicleId(aracId));
        Listing ilan = await context.Listings.SingleAsync(item => item.VehicleId == new VehicleId(aracId));

        if (yayinla)
        {
            arac.AttachMediaAsset(medya.Ekle(MediaProcessingStatus.Ready));
            Assert.True(ilan.Publish(ListingSlug.Create("Toyota Corolla", arac.ModelYear, ilan.Id).Value, new FixedClock(Kayit)).IsSuccess);
            await context.SaveChangesAsync();
        }

        await KatalogYazicilari.Yansitma(context, medya).ProjectAsync(arac, ilan, read: null, CancellationToken.None);
        await context.SaveChangesAsync();
    }

    private async Task<VehicleCatalogDbContext> TazeVeritabaniAsync(string ad, string? hedef)
    {
        NpgsqlConnectionStringBuilder baglanti = new(database.ConnectionString) { Database = $"cargallery_0016a_{ad}_{Guid.NewGuid():N}"[..48] };
        VehicleCatalogDbContext context = new(new DbContextOptionsBuilder<VehicleCatalogDbContext>()
            .UseNpgsql(baglanti.ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleCatalogDbContext.Schema))
            .Options);

        if (hedef is null)
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.GetService<IMigrator>().MigrateAsync(hedef);
        }

        return context;
    }

    private static async Task<(Guid Id, string Vin)> AracKaydetAsync(VehicleCatalogDbContext context, string sasePrefix)
    {
        string vin = $"{sasePrefix}{Guid.CreateVersion7():N}"[..24].ToUpperInvariant();
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
                PriceAmount: 750_000m,
                PriceOnRequest: false,
                Vin: vin),
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        return (sonuc.Value, vin);
    }

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

    private static UpdateVehicleCommand Komut(VehicleCatalogDbContext context)
    {
        FixedClock saat = new(Kayit.AddHours(1));
        return new UpdateVehicleCommand(
            context,
            AracKaydiTests.Kurallar(),
            KatalogYazicilari.Yansitma(context),
            KatalogYazicilari.Birim(context, saat),
            saat);
    }
}
