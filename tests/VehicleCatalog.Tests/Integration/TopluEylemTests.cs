using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012c` — toplu eylemin komut katmanı, GERÇEK PostgreSQL üzerinde.
///
/// <para>Neden entegrasyon: ölçülen şey **kısmi başarının gerçekten kısmi olduğu**.
/// İşlem sınırı ilan başına (**C3**/**D3**) ve bunu yalnız gerçek bir veritabanı
/// gösterebilir — bellek içi bir sahtede "üçüncü satır düştü ama ilk ikisi kaldı"
/// cümlesinin karşılığı yoktur.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class TopluEylemTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Kayit = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Eylem = new(2026, 10, 12, 11, 0, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0012c/AC-1: Oturum açmış personel birden çok aracı seçip aynı eylemi hepsine uygular; yanıt `200` ve gövdesi araç başına sonuç listesidir")]
    public async Task Uygular_AyniEylemi_SecilenHerArac()
    {
        FakeMediaAssetQueries medya = new();
        Guid birinci = await AracEkleAsync(medya, yayinda: true);
        Guid ikinci = await AracEkleAsync(medya, yayinda: true);

        IReadOnlyList<BulkListingRow> satirlar =
            await UygulaAsync(ListingOperation.Unpublish, [birinci, ikinci], medya);

        Assert.Equal(2, satirlar.Count);
        Assert.All(satirlar, satir => Assert.True(satir.Applied));
        Assert.Equal(ListingStatus.Draft, await DurumAsync(birinci));
        Assert.Equal(ListingStatus.Draft, await DurumAsync(ikinci));
    }

    [Fact(DisplayName = "0012c/AC-10: Her sonuç satırı aynı şekli taşır: araç kimliği · başarılı mı · hata kodu ve yapısal parametreleri · kaydın güncel durum kodu")]
    public async Task Tasir_AyniSekli_HerSatir()
    {
        FakeMediaAssetQueries medya = new();
        Guid gecerli = await AracEkleAsync(medya, yayinda: true);
        Guid gecersiz = await AracEkleAsync(medya);

        IReadOnlyList<BulkListingRow> satirlar =
            await UygulaAsync(ListingOperation.Unpublish, [gecerli, gecersiz], medya);

        BulkListingRow basarili = satirlar[0];
        BulkListingRow basarisiz = satirlar[1];

        Assert.Equal(gecerli, basarili.VehicleId);
        Assert.True(basarili.Applied);
        Assert.Null(basarili.ErrorCode);

        Assert.Equal(gecersiz, basarisiz.VehicleId);
        Assert.False(basarisiz.Applied);
        Assert.Equal(Listing.InvalidTransition, basarisiz.ErrorCode);

        // Şekil TEK: iki satır da durum kodunu taşıyor.
        Assert.Equal(nameof(ListingStatus.Draft), basarili.ListingStatus);
        Assert.Equal(nameof(ListingStatus.Draft), basarisiz.ListingStatus);
    }

    [Fact(DisplayName = "0012c/AC-11: **Başarılı** satır da kaydın **güncel durum kodunu** taşır")]
    public async Task Tasir_GuncelDurumu_BasariliSatirda()
    {
        FakeMediaAssetQueries medya = new();
        Guid arac = await AracEkleAsync(medya, yayinda: true);

        IReadOnlyList<BulkListingRow> satirlar =
            await UygulaAsync(ListingOperation.MarkSold, [arac], medya);

        // Durum EYLEMDEN SONRAKİ durum: panel listeyi yeniden çekmeden satırı boyayabilsin.
        Assert.True(satirlar[0].Applied);
        Assert.Equal(nameof(ListingStatus.Sold), satirlar[0].ListingStatus);
    }

    [Fact(DisplayName = "0012c/AC-12: Başarısız satır tekil ucun **aynı** gerekçesini taşır: kod **ve** yapısal parametreleri; toplu yayın reddinde eksiklerin listesi satırda durur")]
    public async Task Tasir_AyniGerekceyi_TekilUcla()
    {
        FakeMediaAssetQueries medya = new();
        Guid isleniyor = medya.Ekle(MediaProcessingStatus.Processing);
        Guid arac = await AracEkleAsync(medya, medyaVarligi: isleniyor);

        IReadOnlyList<BulkListingRow> satirlar =
            await UygulaAsync(ListingOperation.Publish, [arac], medya);

        Assert.False(satirlar[0].Applied);
        Assert.Equal(PublishErrors.CannotPublish, satirlar[0].ErrorCode);

        // Parametreler DÜŞMÜYOR: tekil uç aynı araç için "medya yok" diyor, toplu uç da.
        // Düşselerdi panel yalnızca "yayına alınamadı" diyebilirdi (**D45**).
        Assert.Equal(PublishErrors.NoMedia, satirlar[0].Parameters!["reasons"]);
    }

    [Fact(DisplayName = "0012c/AC-14: Bulunamayan araç isteği değil **o satırı** düşürür: satır `vehicle.not_found` taşır ve istek `200` kalır")]
    public async Task Dusurur_Satiri_AracBulunamadiginda()
    {
        FakeMediaAssetQueries medya = new();
        Guid gercek = await AracEkleAsync(medya, yayinda: true);
        Guid hayalet = Guid.CreateVersion7();

        IReadOnlyList<BulkListingRow> satirlar =
            await UygulaAsync(ListingOperation.Unpublish, [gercek, hayalet], medya);

        Assert.True(satirlar[0].Applied);
        Assert.False(satirlar[1].Applied);
        Assert.Equal(VehicleErrors.NotFound, satirlar[1].ErrorCode);

        // Durumu YOK ve uydurulmuyor: bulunamayan aracın ilanı da yok.
        Assert.Null(satirlar[1].ListingStatus);
    }

    [Fact(DisplayName = "0012c/AC-15: Zaten hedef **durumda** olan kayıt **başarısız** sayılır ve satır kaydın güncel durumunu taşır")]
    public async Task Sayilir_Basarisiz_ZatenHedefDurumdaysa()
    {
        FakeMediaAssetQueries medya = new();
        Guid arac = await AracEkleAsync(medya);

        // Taslak bir ilan zaten "yayında değil": yayından kaldırma reddediliyor.
        IReadOnlyList<BulkListingRow> satirlar =
            await UygulaAsync(ListingOperation.Unpublish, [arac], medya);

        Assert.False(satirlar[0].Applied);
        Assert.Equal(Listing.InvalidTransition, satirlar[0].ErrorCode);

        // Tek başına bir red kodu "zaten o durumda" ile "koşulu sağlamıyor"u ayırt
        // ettirmez; cümleyi kuran uçtur ve durumu satırdan okur (**D28**/**D45**).
        Assert.Equal(nameof(ListingStatus.Draft), satirlar[0].ListingStatus);
    }

    [Fact(DisplayName = "0012c/AC-16: Geçerli satırlar uygulanır; bir satırın reddi uygulanmış satırları **geri almaz**")]
    public async Task Almaz_Geri_BirSatirReddedildiginde()
    {
        FakeMediaAssetQueries medya = new();
        Guid birinci = await AracEkleAsync(medya, yayinda: true);
        Guid reddedilecek = await AracEkleAsync(medya);
        Guid ucuncu = await AracEkleAsync(medya, yayinda: true);

        IReadOnlyList<BulkListingRow> satirlar =
            await UygulaAsync(ListingOperation.MarkSold, [birinci, reddedilecek, ucuncu], medya);

        Assert.True(satirlar[0].Applied);
        Assert.False(satirlar[1].Applied);
        Assert.True(satirlar[2].Applied);

        // İşlem sınırı ilan başına: ortadaki red ne öncesini geri aldı ne sonrasını durdurdu.
        Assert.Equal(ListingStatus.Sold, await DurumAsync(birinci));
        Assert.Equal(ListingStatus.Draft, await DurumAsync(reddedilecek));
        Assert.Equal(ListingStatus.Sold, await DurumAsync(ucuncu));
    }

    [Theory(DisplayName = "0012c/AC-17: Altı toplu eylemin her biri toplu yüzeyden uygulanır: yayına al · yayından kaldır · satıldı işaretle · arşivle · öne çıkar · öne çıkarmayı kaldır")]
    [InlineData("Publish")]
    [InlineData("Unpublish")]
    [InlineData("MarkSold")]
    [InlineData("Archive")]
    [InlineData("Feature")]
    [InlineData("Unfeature")]
    public async Task Uygulanir_AltiEylemin_HerBiri(string islemAdi)
    {
        ListingOperation islem = Enum.Parse<ListingOperation>(islemAdi);

        FakeMediaAssetQueries medya = new();
        Guid hazir = medya.Ekle(MediaProcessingStatus.Ready);

        // Yayına alma taslak ister, diğer dördü yayında bir ilan; öne çıkarma ikisinde de
        // çalışır (**AC-29**) ve taslakla koşuyor.
        bool yayinda = islem is ListingOperation.Unpublish or ListingOperation.MarkSold;
        Guid arac = await AracEkleAsync(medya, medyaVarligi: hazir, yayinda: yayinda);

        IReadOnlyList<BulkListingRow> satirlar = await UygulaAsync(islem, [arac], medya);

        Assert.True(
            satirlar[0].Applied,
            $"{islemAdi}: {satirlar[0].ErrorCode} {satirlar[0].Parameters?.GetValueOrDefault("reasons")}");
    }

    /// <summary>
    /// **AC-17**'nin `Theory`'si boşalırsa kapı sessizce açılır (`docs/testing.md` § 3).
    /// Vaka listesi toplu rota tablosundan geliyor ve sayısı orada yazılı.
    /// </summary>
    [Fact(DisplayName = "0012c/AC-17: Toplu yüzeyin eylem listesi boşalmaz ve altı eylem taşır")]
    public void Tasir_AltiEylem_TopluYuzey()
    {
        Assert.Equal(6, ListingOperations.Bulk.Length);
        Assert.DoesNotContain(ListingOperations.Bulk, item => item.Operation is ListingOperation.Reserve);
        Assert.DoesNotContain(ListingOperations.Bulk, item => item.Operation is ListingOperation.Unreserve);
    }

    [Fact(DisplayName = "0012c/AC-18: Satırlar **gönderildiği sırada** işlenir ve sonuç listesi aynı sırayı taşır")]
    public async Task Isler_GonderilenSirada()
    {
        FakeMediaAssetQueries medya = new();
        List<Guid> secim = [];

        for (int index = 0; index < 5; index++)
        {
            secim.Add(await AracEkleAsync(medya, yayinda: true));
        }

        // Ters sırada gönderiliyor: dönen sıra gönderilen sırayla aynı olmalı, veritabanı
        // sırasıyla değil.
        List<Guid> tersine = [.. Enumerable.Reverse(secim)];

        IReadOnlyList<BulkListingRow> satirlar =
            await UygulaAsync(ListingOperation.Archive, tersine, medya);

        Assert.Equal(tersine, [.. satirlar.Select(satir => satir.VehicleId)]);
    }

    [Fact(DisplayName = "0012c/AC-19: Bir satırın eşzamanlılık kaybı **satır düzeyinde** kalır: `concurrency.lost` o satıra yazılır, diğer satırlar uygulanır")]
    public async Task Kalir_SatirDuzeyinde_EszamanlilikKaybi()
    {
        FakeMediaAssetQueries medya = new();
        Guid birinci = await AracEkleAsync(medya, yayinda: true);
        Guid kilitli = await AracEkleAsync(medya, yayinda: true);
        Guid ucuncu = await AracEkleAsync(medya, yayinda: true);

        // Ortadaki ilanın satırı BAŞKA bir bağlantıda kilitli tutuluyor: komut onu
        // alamayacak ve `lock_timeout` dolduğunda `concurrency.lost` alacak.
        await using NpgsqlConnection baskaIstek = new(database.ConnectionString);
        await baskaIstek.OpenAsync();

        await using NpgsqlTransaction tutulan = await baskaIstek.BeginTransactionAsync();

        await using (NpgsqlCommand kilit = baskaIstek.CreateCommand())
        {
            kilit.Transaction = tutulan;
            kilit.CommandText =
                $"SELECT 1 FROM {VehicleCatalogDbContext.Schema}.listings WHERE vehicle_id = @id FOR UPDATE";
            kilit.Parameters.AddWithValue("id", kilitli);

            await kilit.ExecuteNonQueryAsync();
        }

        IReadOnlyList<BulkListingRow> satirlar = await UygulaAsync(
            ListingOperation.Archive,
            [birinci, kilitli, ucuncu],
            medya,
            kilitSuresi: TimeSpan.FromMilliseconds(400));

        await tutulan.RollbackAsync();

        // Kayıp İSTEĞİ düşürmüyor: tek bir kilit kaybında iptal etmek D3'ün kısmi başarı
        // kararını silerdi. Önceki ve sonraki satır uygulandı.
        Assert.True(satirlar[0].Applied);
        Assert.False(satirlar[1].Applied);
        Assert.Equal(ConcurrencyErrors.Lost, satirlar[1].ErrorCode);
        Assert.True(satirlar[2].Applied);

        Assert.Equal(ListingStatus.Archived, await DurumAsync(birinci));
        Assert.Equal(ListingStatus.Published, await DurumAsync(kilitli));
        Assert.Equal(ListingStatus.Archived, await DurumAsync(ucuncu));
    }

    [Fact(DisplayName = "0012c/AC-21: Toplu eylem aracın **son güncellenme** zamanını ilerletir")]
    public async Task Ilerletir_SonGuncellenmeyi()
    {
        FakeMediaAssetQueries medya = new();
        Guid arac = await AracEkleAsync(medya, yayinda: true);

        await UygulaAsync(ListingOperation.Archive, [arac], medya);

        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle okunan = await context.Vehicles.AsNoTracking()
            .SingleAsync(item => item.Id == new VehicleId(arac));

        Assert.Equal(Eylem, okunan.UpdatedAt);
    }

    [Fact(DisplayName = "0012c/AC-22: Toplu eylem vitrin okuma modelini **aynı işlemde** günceller")]
    public async Task Gunceller_VitrinOkumaModelini()
    {
        FakeMediaAssetQueries medya = new();
        Guid birinci = await AracEkleAsync(medya, yayinda: true);
        Guid ikinci = await AracEkleAsync(medya, yayinda: true);

        Assert.Equal(2, await VitrinSatiriSayisiAsync([birinci, ikinci]));

        await UygulaAsync(ListingOperation.Unpublish, [birinci, ikinci], medya);

        // Yayından kalkan ilanın vitrin satırı AYNI işlemde siliniyor; ayrı bir turda
        // silinseydi ziyaretçi yayından kalkmış aracı görmeye devam ederdi.
        Assert.Equal(0, await VitrinSatiriSayisiAsync([birinci, ikinci]));
    }

    [Fact(DisplayName = "0012c/AC-23: Toplu geçişler **duyurulur**: başarılı her geçiş kendi entegrasyon olayını yazar")]
    public async Task Yazar_Duyuruyu_HerBasariliGecis()
    {
        FakeMediaAssetQueries medya = new();
        Guid birinci = await AracEkleAsync(medya, yayinda: true);
        Guid reddedilecek = await AracEkleAsync(medya);
        Guid ucuncu = await AracEkleAsync(medya, yayinda: true);

        await OutboxuTemizleAsync();

        await UygulaAsync(ListingOperation.MarkSold, [birinci, reddedilecek, ucuncu], medya);

        // Tablo SAYILIYOR, kod okunmuyor: `0006`'nın kaçan hatası ve `#52`'nin tuzağı
        // ikisi de "yazıldı sanılan ama yazılmayan" duyuruydu. İki başarılı geçiş =
        // iki satır; reddedilen satır duyuru üretmiyor.
        await using VehicleCatalogDbContext context = database.CreateContext();
        List<string> tipler = await context.OutboxMessages.AsNoTracking()
            .Select(message => message.Type)
            .ToListAsync();

        Assert.Equal(2, tipler.Count);
        Assert.All(tipler, tip => Assert.EndsWith("VehicleSold", tip, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0012c/AC-34: Beş okuma istek başına **bir kez** yapılır: zorunlu alan kümesi · public alan anahtarları · şube başına ad haritası · kanonik model adları · seçimdeki araçların kapak görseli adresleri")]
    public async Task Okur_BirKez_AyniYanitiVerenOkumalari()
    {
        FakeMediaAssetQueries medya = new();
        Guid sube = Guid.CreateVersion7();
        List<Guid> secim = [];

        for (int index = 0; index < 4; index++)
        {
            secim.Add(await AracEkleAsync(medya, yayinda: true, sube: sube));
        }

        SayanSubeSorgusu subeler = new();
        SayanModelAdCozucu modeller = new();

        // Kurulum sırasında yansıtma kayıt başına çağrıldı; ölçülen şey TOPLU yolun kendisi.
        int kapakOncesi = medya.TopluOkumaSayisi;

        await UygulaAsync(ListingOperation.Archive, secim, medya, subeler, modeller);

        // Dört araç, TEK şube: `Dealership` bir kez okundu. Araç başına okunsaydı dört.
        Assert.Equal(1, subeler.AdOkumaSayisi);

        // Kanonik adlar da bir kez: küme hâlinde soruluyor.
        Assert.Equal(1, modeller.KanonikOkumaSayisi);

        // **D56** — kapak adresleri de tek `FindManyAsync` ile. Bu sayı bir dış bulgunun
        // bedeli: yansıtma kapağı KAYIT BAŞINA okuyordu ve hiçbir kriter onu saymıyordu,
        // yani 25 satırlık bir yayın 25 hazırlık + 25 kapak çağrısı yapıyordu.
        Assert.Equal(1, medya.TopluOkumaSayisi - kapakOncesi);
    }

    /// <summary>
    /// **D56**'nın bedel satırı: bağlam kapak adresini çözemediyse yansıtma kayıt başına
    /// okumaya DÜŞER ve satır görselsiz kalmaz.
    ///
    /// <para>Ölçüm şuydu: `MediaAssetReady` yalnız VAR OLAN vitrin satırlarını tazeliyor
    /// (`MediaAssetReadyHandler` `ExecuteUpdate` ile). Bağlam kurulurken henüz hazır olmayan
    /// bir kapak, satır o istekte doğduktan sonra hiçbir olayla tazelenmez — "fotoğrafları
    /// yükle, hemen yayına al" sırası tam bu pencereye düşüyor ve araç vitrine
    /// <b>görselsiz</b> girerdi. Bayat kapağı kabul eden karar var olan satır için geçerli,
    /// yeni doğan satır için değil.</para>
    /// </summary>
    [Fact(DisplayName = "0012c/D56: kapak bağlamda çözülemediyse yansıtma kayıt başına okur ve satır görselsiz kalmaz")]
    public async Task Okur_KayitBasina_BaglamdaCozulemeyenKapagi()
    {
        FakeMediaAssetQueries medya = new();
        Guid aracId = await AracEkleAsync(medya, yayinda: true);

        // Kapak haritası BOŞ: bağlam kurulduğunda varlık henüz hazır değildi.
        ListingReadContext bos = new(
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new Dictionary<Guid, IReadOnlyDictionary<string, string>>(),
            new Dictionary<Guid, string>(),
            new Dictionary<Guid, string>());

        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle arac = await context.Vehicles.SingleAsync(item => item.Id == new VehicleId(aracId));
        Listing ilan = await context.Listings.SingleAsync(item => item.VehicleId == arac.Id);

        int oncesi = medya.TopluOkumaSayisi;

        await KatalogYazicilari.Yansitma(context, medya).ProjectAsync(arac, ilan, bos, CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(1, medya.TopluOkumaSayisi - oncesi);

        ShowcaseListing satir = await context.ShowcaseListings.AsNoTracking()
            .SingleAsync(row => row.VehicleId == aracId);

        Assert.False(string.IsNullOrEmpty(satir.CoverMediaUrl));
    }

    [Fact(DisplayName = "0012c/AC-35: Medya hazırlığı **kayıt başına** ve **kilidin altında** okunur")]
    public async Task Okur_KayitBasina_MedyaHazirligini()
    {
        FakeMediaAssetQueries medya = new();
        Guid hazir = medya.Ekle(MediaProcessingStatus.Ready);
        List<Guid> secim = [];

        for (int index = 0; index < 3; index++)
        {
            secim.Add(await AracEkleAsync(medya, medyaVarligi: hazir));
        }

        int oncesi = medya.HazirlikOkumaSayisi;

        await UygulaAsync(ListingOperation.Publish, secim, medya);

        // Üç yayın isteği = üç hazırlık okuması. Bunu ön okuma bağlamına almak (**D46**)
        // `0012b` **D8**'in önlediği bayat-okuma hatasını geri getirirdi: fotoğrafı o
        // saniye düşen bir araç yayına alınırdı.
        Assert.Equal(3, medya.HazirlikOkumaSayisi - oncesi);
    }

    private async Task<int> VitrinSatiriSayisiAsync(IReadOnlyList<Guid> araclar)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.ShowcaseListings.AsNoTracking()
            .CountAsync(row => araclar.Contains(row.VehicleId));
    }

    private async Task OutboxuTemizleAsync()
    {
        await using VehicleCatalogDbContext context = database.CreateContext();
        await context.OutboxMessages.ExecuteDeleteAsync();
    }

    private async Task<Guid> AracEkleAsync(
        FakeMediaAssetQueries medya,
        Guid? medyaVarligi = null,
        bool yayinda = false,
        Guid? sube = null)
    {
        Guid varlik = medyaVarligi ?? medya.Ekle(MediaProcessingStatus.Ready);

        VehicleBuilder kurucu = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"TOPLU{Interlocked.Increment(ref sayac):D12}")
            .CreatedAt(Kayit);

        if (sube is not null)
        {
            kurucu = kurucu.WithBranch(sube.Value);
        }

        Vehicle arac = kurucu.Build();
        arac.AttachMediaAsset(varlik);

        Listing ilan = Listing.CreateDraft(arac.Id, Listing.NormalWeight);

        if (yayinda)
        {
            Assert.True(ilan.Publish(
                ListingSlug.Create("Toyota Corolla", arac.ModelYear, ilan.Id).Value,
                new FixedClock(Kayit)).IsSuccess);
        }

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(arac);
        context.Listings.Add(ilan);
        await context.SaveChangesAsync();

        if (yayinda)
        {
            // Vitrin satırı yazma yolundan geliyor: AC-22 silinmeyi ölçebilsin.
            await using VehicleCatalogDbContext yansitma = database.CreateContext();
            Vehicle okunan = await yansitma.Vehicles.SingleAsync(item => item.Id == arac.Id);
            Listing okunanIlan = await yansitma.Listings.SingleAsync(item => item.VehicleId == arac.Id);

            await KatalogYazicilari.Yansitma(yansitma, medya)
                .ProjectAsync(okunan, okunanIlan, read: null, CancellationToken.None);

            await yansitma.SaveChangesAsync();
        }

        return arac.Id.Value;
    }

    private async Task<IReadOnlyList<BulkListingRow>> UygulaAsync(
        ListingOperation islem,
        IReadOnlyList<Guid> secim,
        FakeMediaAssetQueries medya,
        SayanSubeSorgusu? subeler = null,
        SayanModelAdCozucu? modeller = null,
        TimeSpan? kilitSuresi = null)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(Eylem);
        SayanSubeSorgusu subeSorgusu = subeler ?? new SayanSubeSorgusu();
        SayanModelAdCozucu modelCozucu = modeller ?? new SayanModelAdCozucu();
        InvariantGuardOptions<VehicleCatalogDbContext> kilit = kilitSuresi is null
            ? InvariantGuardOptions<VehicleCatalogDbContext>.Default
            : new InvariantGuardOptions<VehicleCatalogDbContext>(kilitSuresi.Value);

        ListingBulkCommands komut = new(
            context,
            new ListingTransitionCommands(
                context,
                KatalogYazicilari.Birim(context, saat),
                new ShowcaseProjectionWriter(context, subeSorgusu, medya),
                medya,
                modelCozucu,
                kilit,
                saat),
            new ListingFeatureCommands(
                context,
                KatalogYazicilari.Birim(context, saat),
                new ShowcaseProjectionWriter(context, subeSorgusu, medya),
                kilit,
                saat),
            subeSorgusu,
            modelCozucu,
            medya,
            new CatalogAdminPagingOptions(25, 100));

        Result<IReadOnlyList<BulkListingRow>> sonuc =
            await komut.ExecuteAsync(islem, secim, CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : string.Empty);

        return sonuc.Value;
    }

    private async Task<ListingStatus> DurumAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.Listings.AsNoTracking()
            .Where(listing => listing.VehicleId == new VehicleId(aracId))
            .Select(listing => listing.Status)
            .SingleAsync();
    }
}
