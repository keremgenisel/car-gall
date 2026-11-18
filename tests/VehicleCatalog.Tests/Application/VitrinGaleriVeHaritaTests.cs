using System.Text.Json;
using System.Text.Json.Nodes;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.Detail;
using CarGallery.Modules.VehicleCatalog.Application.EventHandlers;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// `0016b` — vitrin detayının galerisi ve parça haritası, sunucu yarısı: okuma modelinin
/// galeri tablosu (D25), medya olay işleyicileri, detay sözleşmesi (C12) ve haritanın
/// görünürlüğü (C1). Gerçek PostgreSQL, gerçek komutlar, gerçek yansıtıcı ve gerçek detay
/// sorgusu; `Media` yalnız sözleşmesi üzerinden sahtelenir.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class VitrinGaleriVeHaritaTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Simdi = new(2026, 11, 18, 14, 0, 0, TimeSpan.Zero);

    /// <summary>Endpoint'in serileştirmesi: ASP.NET'in web varsayılanları (camelCase).</summary>
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>`0016a` AC-5'in haritası: dört durumun hepsi.</summary>
    private static readonly IReadOnlyDictionary<string, string> Ac5 = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["front_bumper"] = "painted",
        ["trunk_lid"] = "replaced",
        ["left_rear_door"] = "local_painted",
    };

    // ── Galeri — içerik ve sıra ───────────────────────────────────────────────

    [Fact(DisplayName = "0016b/AC-1: Medya sırası A (hazır), B (işleniyor), C (hazır), D (başarısız), E (hazır) olan ilanın detay galerisi yalnız A, C ve E'yi bu sırayla gösterir")]
    public async Task Gosterir_YalnizHazirlari_Sirayla()
    {
        FakeMediaAssetQueries medya = new();
        Guid[] s = Ac1Sirasi(medya);
        string slug = await YayindaIlanAsync(medya, s);

        Assert.Equal([s[0], s[2], s[4]], Kimlikler(await DetayAsync(slug)));
    }

    [Fact(DisplayName = "0016b/AC-2: AC-1'in ilanında sayaç ilk görselde \"1 / 3\" gösterir; toplam 5 değildir")]
    public async Task Sayar_YalnizHazirlari()
    {
        FakeMediaAssetQueries medya = new();
        string slug = await YayindaIlanAsync(medya, Ac1Sirasi(medya));

        // Sayacın toplamı ön yüzde görsel listesinin uzunluğudur (`Gallery.test.tsx`); sunucu
        // yarısı listenin 5 değil 3 eleman taşımasıdır.
        Assert.Equal(3, (await DetayAsync(slug)).Images.Count);
    }

    [Fact(DisplayName = "0016b/AC-3: AC-1'in B varlığı için MediaAssetReady işlendikten sonra, ilan yeniden yayımlanmadan, detay galerisi A, B, C, E'yi gösterir ve sayaç \"1 / 4\" olur")]
    public async Task Ekler_HazirOlani_YenidenYayinlamadan()
    {
        FakeMediaAssetQueries medya = new();
        Guid[] s = Ac1Sirasi(medya);
        string slug = await YayindaIlanAsync(medya, s);

        await HazirAsync(s[1]);

        ShowcaseListingDetailDto detay = await DetayAsync(slug);
        Assert.Equal([s[0], s[1], s[2], s[4]], Kimlikler(detay));
        Assert.Equal(4, detay.Images.Count);
    }

    [Fact(DisplayName = "0016b/AC-4: AC-1'in B varlığı için MediaAssetFailed işlendiğinde detay galerisi A, C, E olarak kalır ve sayaç \"1 / 3\" olarak kalır")]
    public async Task Gostermez_BasarisizOlani()
    {
        FakeMediaAssetQueries medya = new();
        Guid[] s = Ac1Sirasi(medya);
        string slug = await YayindaIlanAsync(medya, s);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            await new MediaAssetFailedHandler(context, new FixedClock(Simdi)).HandleAsync(
                new MediaAssetFailed(Guid.CreateVersion7(), Simdi, s[1], MediaFailureReason.Undecodable),
                CancellationToken.None);
        }

        Assert.Equal([s[0], s[2], s[4]], Kimlikler(await DetayAsync(slug)));
    }

    [Fact(DisplayName = "0016b/AC-5: Personel aracın medya sırasını E, C, A olarak değiştirdikten sonra detay galerisi görselleri E, C, A sırasıyla gösterir")]
    public async Task Izler_YeniSirayi()
    {
        FakeMediaAssetQueries medya = new();
        Guid[] s = Ac1Sirasi(medya);
        (Guid aracId, string slug) = await YayindaAracAsync(medya, s);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            Result sonuc = await new UpdateVehicleMediaCommand(context, medya, KatalogYazicilari.Yansitma(context, medya), new FixedClock(Simdi))
                .ExecuteAsync(aracId, [s[4], s[2], s[0]], CancellationToken.None);
            Assert.True(sonuc.IsSuccess);
        }

        Assert.Equal([s[4], s[2], s[0]], Kimlikler(await DetayAsync(slug)));
    }

    [Fact(DisplayName = "0016b/AC-6: Medya sırası A (başarısız), B (hazır), C (hazır) olan ilanın detayı B ile açılır ve sayaç \"1 / 2\" gösterir")]
    public async Task AcilirIlkHazirla()
    {
        FakeMediaAssetQueries medya = new();
        Guid[] s = Ac6Sirasi(medya);
        string slug = await YayindaIlanAsync(medya, s);

        ShowcaseListingDetailDto detay = await DetayAsync(slug);
        Assert.Equal(s[1], Kimlik(detay.Images[0]));
        Assert.Equal(2, detay.Images.Count);
    }

    [Fact(DisplayName = "0016b/AC-7: AC-6'nın ilanının liste kartı yanıtındaki kapak değeri bu spec'ten önceki davranışla aynıdır (bugün: kapak yok)")]
    public async Task DegistirmezKartKapagini()
    {
        FakeMediaAssetQueries medya = new();
        string slug = await YayindaIlanAsync(medya, Ac6Sirasi(medya));

        ShowcaseListingDto kart = await KartAsync(slug);

        // `0012a`'nın kuralı: kapak sıranın İLK elemanı; o başarısız olduğu için kapak yok.
        // R7 detayın ilk görselini değiştirir, kartın kapağını değiştirmez (X10).
        Assert.Null(kart.CoverImageUrl);
    }

    [Fact(DisplayName = "0016b/AC-8: Detay yanıtındaki her görsel adresi thumbnail, display veya large türevlerinden birine aittir; varlığın orijinal dosyasının adresi yanıtta hiçbir yerde geçmez")]
    public async Task TasirYalnizTurevleri()
    {
        FakeMediaAssetQueries medya = new();
        string slug = await YayindaIlanAsync(medya, Ac1Sirasi(medya));

        ShowcaseListingDetailDto detay = await DetayAsync(slug);
        string json = JsonSerializer.Serialize(detay, Web);

        Assert.All(
            detay.Images.SelectMany(image => new[] { image.Thumbnail.Url, image.Display.Url, image.Large.Url }),
            url => Assert.Matches("/(thumbnail|display|large)\\.webp$", url));
        Assert.DoesNotContain("original", json, StringComparison.OrdinalIgnoreCase);
    }

    // ── Detay sözleşmesi ──────────────────────────────────────────────────────

    [Fact(DisplayName = "0016b/AC-9: Detay yanıtında coverImageUrl anahtarı yoktur; yerine sıralı bir görsel listesi vardır")]
    public async Task KaldirirKapakAnahtarini()
    {
        FakeMediaAssetQueries medya = new();
        string slug = await YayindaIlanAsync(medya, Ac1Sirasi(medya));

        JsonObject govde = Govde(await DetayAsync(slug));

        Assert.False(govde.ContainsKey("coverImageUrl"));
        Assert.Equal(3, govde["images"]!.AsArray().Count);
    }

    [Fact(DisplayName = "0016b/AC-10: Görsel listesinin her elemanı üç türevin adresini ve gerçek boyutunu taşır: 1200×900 kaynak için large 1200×900, display 800×600, thumbnail 400×300")]
    public async Task TasirGercekBoyutlari()
    {
        FakeMediaAssetQueries medya = new();
        string slug = await YayindaIlanAsync(medya, [medya.EkleTurevli(MediaProcessingStatus.Ready, 1200, 900)]);

        ShowcaseImageDto gorsel = (await DetayAsync(slug)).Images.Single();

        Assert.Equal((1200, 900), (gorsel.Large.Width, gorsel.Large.Height));
        Assert.Equal((800, 600), (gorsel.Display.Width, gorsel.Display.Height));
        Assert.Equal((400, 300), (gorsel.Thumbnail.Width, gorsel.Thumbnail.Height));
    }

    [Fact(DisplayName = "0016b/AC-11: Vitrin liste (kart) yanıtının şekli değişmez: kart kapak adresini taşımaya devam eder ve görsel listesi taşımaz")]
    public async Task DegistirmezKartSozlesmesini()
    {
        FakeMediaAssetQueries medya = new();
        string slug = await YayindaIlanAsync(medya, Ac1Sirasi(medya));

        JsonObject kart = JsonSerializer.SerializeToNode(await KartAsync(slug), Web)!.AsObject();

        Assert.True(kart.ContainsKey("coverImageUrl"));
        Assert.NotNull(kart["coverImageUrl"]);
        Assert.False(kart.ContainsKey("images"));
        Assert.False(kart.ContainsKey("paintChangeMap"));
    }

    [Fact(DisplayName = "0016b/AC-12: Alan Opsiyonel + Görünür ve araçta 0016a AC-5'in haritası kayıtlıyken detay yanıtı haritayı ayrı bir özellik olarak taşır: 13 {part, state} elemanı, C10 sırasında, yalnız kodla")]
    public async Task TasirHaritayi_AyriOzellikOlarak()
    {
        string slug = await YayindaIlanAsync(new FakeMediaAssetQueries(), [], Ac5);

        IReadOnlyList<ShowcasePaintChangeDto> harita = (await DetayAsync(slug)).PaintChangeMap!;

        Assert.Equal(Beklenen(Ac5), harita.Select(item => (item.Part, item.State)));
    }

    [Fact(DisplayName = "0016b/AC-13: Alan Zorunlu + Görünür ve değerliyken de detay yanıtı haritayı taşır")]
    public async Task TasirHaritayi_ZorunluIken()
    {
        string slug = await YayindaIlanAsync(new FakeMediaAssetQueries(), [], Ac5);

        await AyarlaAsync(FieldFormState.Required, FieldPublicVisibility.Visible, async () =>
            Assert.Equal(13, (await DetayAsync(slug)).PaintChangeMap!.Count));
    }

    [Fact(DisplayName = "0016b/AC-14: Aynı ilanın ar ve tr locale'li detay yanıtlarında parça haritası özelliği aynıdır; ikisi de çevrilmiş parça ya da durum adı taşımaz")]
    public async Task AyniHarita_IkiLocaleDa()
    {
        string slug = await YayindaIlanAsync(new FakeMediaAssetQueries(), [], Ac5);

        JsonNode tr = Govde(await DetayAsync(slug, "tr"))["paintChangeMap"]!;
        JsonNode ar = Govde(await DetayAsync(slug, "ar"))["paintChangeMap"]!;

        Assert.Equal(tr.ToJsonString(), ar.ToJsonString());
        Assert.Matches("^[\\[\\]{}\":,a-z_]+$", tr.ToJsonString());
    }

    [Fact(DisplayName = "0016b/AC-15: Parça haritası Attributes içinde tekrar edilmez: paint_change_map anahtarlı ya da parça adı içeren bir nitelik satırı yoktur")]
    public async Task TekrarEtmez_Niteliklerde()
    {
        string slug = await YayindaIlanAsync(new FakeMediaAssetQueries(), [], Ac5);

        ShowcaseListingDetailDto detay = await DetayAsync(slug);

        Assert.DoesNotContain(detay.Attributes, item => item.Key == VehicleFieldKeys.PaintChangeMap);
        Assert.DoesNotContain(detay.Attributes, item =>
            PaintChangeParts.Ordered.Any(part => item.Value.Contains(part, StringComparison.Ordinal))
            || item.Value.Contains("Ön tampon", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0016b/AC-16: Alan Kapalı iken detay yanıtının JSON'unda parça haritası özelliğinin anahtarı yoktur — null, boş liste veya boş nesne olarak da yazılmaz")]
    public async Task AnahtarYok_KapaliIken()
    {
        (Guid aracId, string slug) = await YayindaAracAsync(new FakeMediaAssetQueries(), [], Ac5);

        await AyarlaAsync(FieldFormState.Closed, FieldPublicVisibility.Visible, async () =>
        {
            // Ayar değişti, satır HENÜZ yeniden yansıtılmadı: okuma tarafının elemesi.
            Assert.False(Govde(await DetayAsync(slug)).ContainsKey("paintChangeMap"));

            // Yeniden yansıtıldıktan sonra: yansıtma tarafının elemesi.
            await YansitAsync(aracId, new FakeMediaAssetQueries());
            Assert.False(Govde(await DetayAsync(slug)).ContainsKey("paintChangeMap"));
        });
    }

    [Fact(DisplayName = "0016b/AC-17: Alan Opsiyonel + Gizli iken detay yanıtının JSON'unda parça haritası özelliğinin anahtarı yoktur — null veya boş olarak da yazılmaz")]
    public async Task AnahtarYok_GizliIken()
    {
        (Guid aracId, string slug) = await YayindaAracAsync(new FakeMediaAssetQueries(), [], Ac5);

        await AyarlaAsync(FieldFormState.Optional, FieldPublicVisibility.Hidden, async () =>
        {
            Assert.False(Govde(await DetayAsync(slug)).ContainsKey("paintChangeMap"));

            await YansitAsync(aracId, new FakeMediaAssetQueries());
            Assert.False(Govde(await DetayAsync(slug)).ContainsKey("paintChangeMap"));
        });
    }

    [Fact(DisplayName = "0016b/AC-18: Alan Opsiyonel + Görünür ama araçta değer yokken detay yanıtının JSON'unda parça haritası özelliğinin anahtarı yoktur — null veya boş olarak da yazılmaz")]
    public async Task AnahtarYok_DegerYokken()
    {
        string slug = await YayindaIlanAsync(new FakeMediaAssetQueries(), []);

        string json = JsonSerializer.Serialize(await DetayAsync(slug), Web);

        Assert.DoesNotContain("paintChangeMap", json, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0016b/AC-19: Alan Kapalı'ya çekilip yeniden Opsiyonel + Görünür yapıldığında detay yanıtı aynı 13 çifti değişmeden yeniden taşır")]
    public async Task GeriGetirir_YenidenAcilinca()
    {
        (Guid aracId, string slug) = await YayindaAracAsync(new FakeMediaAssetQueries(), [], Ac5);

        await AyarlaAsync(FieldFormState.Closed, FieldPublicVisibility.Visible, async () =>
        {
            await YansitAsync(aracId, new FakeMediaAssetQueries());
            Assert.Null((await DetayAsync(slug)).PaintChangeMap);
        });

        // Kapatmak silmek değildir (F1): yazma modelindeki değer yeniden yansıtılır.
        await YansitAsync(aracId, new FakeMediaAssetQueries());

        Assert.Equal(Beklenen(Ac5), (await DetayAsync(slug)).PaintChangeMap!.Select(item => (item.Part, item.State)));
    }

    [Fact(DisplayName = "0016b/AC-20: Panelde haritada hood original'dan painted'a değiştirilip kaydedildikten sonra detay yanıtı hood için painted taşır")]
    public async Task Izler_PaneldekiDegisikligi()
    {
        FakeMediaAssetQueries medya = new();
        (Guid aracId, string slug) = await YayindaAracAsync(medya, [], Ac5);

        Dictionary<string, string> yeni = new(Ac5, StringComparer.Ordinal) { ["hood"] = "painted" };
        await HaritaKaydetAsync(aracId, Tam(yeni), medya);

        Assert.Equal("painted", (await DetayAsync(slug)).PaintChangeMap!.Single(item => item.Part == "hood").State);
    }

    [Fact(DisplayName = "0016b/AC-21: Panelde harita temizlendikten sonra detay yanıtında parça haritası özelliğinin anahtarı yoktur — vitrin tarafında eski harita kalmaz")]
    public async Task Temizler_VitrinKopyasini()
    {
        FakeMediaAssetQueries medya = new();
        (Guid aracId, string slug) = await YayindaAracAsync(medya, [], Ac5);

        await HaritaKaydetAsync(aracId, [], medya);

        Assert.False(Govde(await DetayAsync(slug)).ContainsKey("paintChangeMap"));
    }

    [Fact(DisplayName = "0016b/AC-78: 0002 AC-12 ve AC-13 yeni özellik için de geçerlidir: tramer (Gizli) detay yanıtında hiçbir biçimde bulunmaz ve parça haritasının eklenmesi bunu değiştirmez")]
    public async Task TramerYok_HaritaVarken()
    {
        string slug = await YayindaIlanAsync(new FakeMediaAssetQueries(), [], Ac5, tramer: true);

        string json = JsonSerializer.Serialize(await DetayAsync(slug), Web);

        Assert.Contains("paintChangeMap", json, StringComparison.Ordinal);
        Assert.DoesNotContain(VehicleFieldKeys.DamageRecordAmount, json, StringComparison.Ordinal);
        Assert.DoesNotContain("9900", json, StringComparison.Ordinal);
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────────

    /// <summary>AC-1'in sırası: A hazır, B işleniyor, C hazır, D başarısız, E hazır.</summary>
    private static Guid[] Ac1Sirasi(FakeMediaAssetQueries medya) =>
    [
        medya.EkleTurevli(MediaProcessingStatus.Ready),
        medya.EkleTurevli(MediaProcessingStatus.Processing),
        medya.EkleTurevli(MediaProcessingStatus.Ready),
        medya.EkleTurevli(MediaProcessingStatus.Failed),
        medya.EkleTurevli(MediaProcessingStatus.Ready),
    ];

    /// <summary>AC-6'nın sırası: A başarısız, B hazır, C hazır.</summary>
    private static Guid[] Ac6Sirasi(FakeMediaAssetQueries medya) =>
    [
        medya.EkleTurevli(MediaProcessingStatus.Failed),
        medya.EkleTurevli(MediaProcessingStatus.Ready),
        medya.EkleTurevli(MediaProcessingStatus.Ready),
    ];

    /// <summary>Görselin varlık kimliği; sahte türev adresleri onu taşır.</summary>
    private static Guid Kimlik(ShowcaseImageDto gorsel) => Guid.Parse(gorsel.Display.Url.Split('/')[3]);

    private static List<Guid> Kimlikler(ShowcaseListingDetailDto detay) => [.. detay.Images.Select(Kimlik)];

    private static JsonObject Govde(ShowcaseListingDetailDto detay) => JsonSerializer.SerializeToNode(detay, Web)!.AsObject();

    private static List<PaintChangeEntry> Tam(IReadOnlyDictionary<string, string> farklar) =>
        [.. PaintChangeParts.Ordered.Select(part => new PaintChangeEntry(part, farklar.TryGetValue(part, out string? durum) ? durum : "original"))];

    private static IEnumerable<(string, string)> Beklenen(IReadOnlyDictionary<string, string> farklar) =>
        Tam(farklar).Select(entry => (entry.Part, entry.State));

    /// <summary>`MediaAssetReady`'yi gerçek işleyiciden geçirir; küme `Media`'nın üç türevi.</summary>
    private async Task HazirAsync(Guid varlik)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        await new MediaAssetReadyHandler(context, new FixedClock(Simdi)).HandleAsync(
            new MediaAssetReady(
                Guid.CreateVersion7(),
                Simdi,
                varlik,
                [.. FakeMediaAssetQueries.Turevler(varlik, 1200, 900).Select(item => new MediaDerivativePayload(item.Variant, item.Url, item.Width, item.Height))]),
            CancellationToken.None);
    }

    private async Task<string> YayindaIlanAsync(
        FakeMediaAssetQueries medya,
        IReadOnlyList<Guid> sira,
        IReadOnlyDictionary<string, string>? harita = null,
        bool tramer = false) =>
        (await YayindaAracAsync(medya, sira, harita, tramer)).Slug;

    /// <summary>
    /// Aracı gerçek komutlarla kurar: kayıt, medya sırası, (varsa) harita, yayın ve yansıtma.
    /// Yayının "en az bir hazır görsel" ön koşulu burada aranmaz — kriterler sıfır hazır
    /// görselli yayında ilanı da kapsar (`0016b` K3).
    /// </summary>
    private async Task<(Guid AracId, string Slug)> YayindaAracAsync(
        FakeMediaAssetQueries medya,
        IReadOnlyList<Guid> sira,
        IReadOnlyDictionary<string, string>? harita = null,
        bool tramer = false)
    {
        Guid aracId;

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            string vin = $"GAL{Guid.CreateVersion7():N}"[..24].ToUpperInvariant();
            Result<Guid> kayit = await new CreateVehicleCommand(context, AracKaydiTests.Kurallar(), new FixedClock(Simdi))
                .ExecuteAsync(
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
            Assert.True(kayit.IsSuccess);
            aracId = kayit.Value;

            if (sira.Count > 0)
            {
                Result baglama = await new UpdateVehicleMediaCommand(context, medya, KatalogYazicilari.Yansitma(context, medya), new FixedClock(Simdi))
                    .ExecuteAsync(aracId, sira, CancellationToken.None);
                Assert.True(baglama.IsSuccess);
            }
        }

        if (harita is not null)
        {
            await HaritaKaydetAsync(aracId, Tam(harita), medya);
        }

        string slug;

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            Vehicle arac = await context.Vehicles.Include(item => item.OptionalFieldValues).SingleAsync(item => item.Id == new VehicleId(aracId));
            Listing ilan = await context.Listings.SingleAsync(item => item.VehicleId == arac.Id);

            if (tramer)
            {
                arac.SetOptionalFieldValue(
                    VehicleFieldKeys.DamageRecordAmount,
                    OptionalFieldValue.OfAmount(Money.Create(9_900m, "TRY", 2).Value));
            }

            ListingSlug adres = ListingSlug.Create("Toyota Corolla", arac.ModelYear, ilan.Id).Value;
            Assert.True(ilan.Publish(adres, new FixedClock(Simdi)).IsSuccess);
            await KatalogYazicilari.Yansitma(context, medya).ProjectAsync(arac, ilan, read: null, CancellationToken.None);
            await context.SaveChangesAsync();
            slug = adres.Value;
        }

        return (aracId, slug);
    }

    private async Task YansitAsync(Guid aracId, FakeMediaAssetQueries medya)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle arac = await context.Vehicles.Include(item => item.OptionalFieldValues).SingleAsync(item => item.Id == new VehicleId(aracId));
        await KatalogYazicilari.Yansitma(context, medya).ProjectAsync(arac, CancellationToken.None);
        await context.SaveChangesAsync();
    }

    /// <summary>Haritayı GERÇEK yönetim komutuyla yazar; komut aynı işlemde yansıtır.</summary>
    private async Task HaritaKaydetAsync(Guid aracId, IReadOnlyList<PaintChangeEntry> harita, FakeMediaAssetQueries medya)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle arac = await context.Vehicles.AsNoTracking().SingleAsync(item => item.Id == new VehicleId(aracId));
        FixedClock saat = new(Simdi.AddMinutes(5));

        Result sonuc = await new UpdateVehicleCommand(
                context,
                AracKaydiTests.Kurallar(),
                KatalogYazicilari.Yansitma(context, medya),
                KatalogYazicilari.Birim(context, saat),
                saat)
            .ExecuteAsync(
                new UpdateVehicleRequest(
                    VehicleId: aracId,
                    BranchId: arac.BranchId,
                    VehicleModelId: arac.VehicleModelId,
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
                    Vin: arac.Vin.Value,
                    ListingTitle: new Dictionary<string, string>(),
                    ListingDescription: new Dictionary<string, string>(),
                    OptionalValues: [new OptionalFieldInput(VehicleFieldKeys.PaintChangeMap, null, null, harita)]),
                CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
    }

    /// <summary>Ayarı değiştirir, gövdeyi koşturur ve ayarı HER durumda migration'ın hâline döndürür.</summary>
    private async Task AyarlaAsync(FieldFormState durum, FieldPublicVisibility gorunurluk, Func<Task> govde)
    {
        await AyarAsync(durum, gorunurluk);

        try
        {
            await govde();
        }
        finally
        {
            await AyarAsync(FieldFormState.Optional, FieldPublicVisibility.Visible);
        }
    }

    private async Task AyarAsync(FieldFormState durum, FieldPublicVisibility gorunurluk)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        VehicleFieldSetting ayar = await context.VehicleFieldSettings.SingleAsync(item => item.FieldKey == VehicleFieldKeys.PaintChangeMap);
        ayar.FormState = durum;
        ayar.PublicVisibility = gorunurluk;
        await context.SaveChangesAsync();
    }

    private async Task<ShowcaseListingDetailDto> DetayAsync(string slug, string locale = "tr")
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        ShowcaseListingDetailDto? detay = await new ShowcaseListingDetailQueries(
                context,
                new FakeModelNameResolver(),
                new FakeReferenceLabelResolver(),
                ShowcaseQueryHarness.Locales)
            .FindAsync(slug, locale, CancellationToken.None);

        Assert.NotNull(detay);
        return detay;
    }

    private async Task<ShowcaseListingDto> KartAsync(string slug)
    {
        PagedResult<ShowcaseListingDto> sayfa = await ShowcaseQueryHarness.QueryAsync(
            database,
            new ShowcaseListingFilter([], 1, "tr") with { PageSize = 100 });

        return sayfa.Items.Single(item => item.Slug == slug);
    }
}
