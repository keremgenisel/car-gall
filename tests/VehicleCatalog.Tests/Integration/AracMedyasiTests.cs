using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012a` — aracın medya yüzeyi, GERÇEK PostgreSQL üzerinde. Sıra `jsonb` kolonunda
/// (`vehicles.media_asset_ids`) yaşıyor ve "sıra kalıcıdır" iddiası ancak yazıp geri
/// okuyarak kanıtlanır; `0010b`'de aynı sınıftan bir eşleme hatası yalnız gerçek
/// veritabanında görünmüştü.
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class AracMedyasiTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 11, 9, 0, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0012a/AC-2: Personel yüklenmiş bir fotoğrafı araca bağlar; aracın bağlı listesi onu içerir")]
    public async Task Icerir_BagliListe_BaglananFotografi()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync();

        Result sonuc = await YazAsync(aracId, [varlik], medya);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal([varlik], await ListeAsync(aracId));
    }

    [Fact(DisplayName = "0012a/AC-3: Bağlı fotoğraflar personelin verdiği sırada durur ve sıra kalıcıdır")]
    public async Task Durur_Sira_Kalici()
    {
        FakeMediaAssetQueries medya = new();
        Guid[] varliklar =
        [
            medya.Ekle(MediaProcessingStatus.Ready),
            medya.Ekle(MediaProcessingStatus.Ready),
            medya.Ekle(MediaProcessingStatus.Processing),
        ];
        Guid aracId = await AracEkleAsync();

        await YazAsync(aracId, varliklar, medya);

        // Yeni DbContext'ten okunuyor: izlenen nesne değil, GERÇEKTEN yazılan satır.
        Assert.Equal(varliklar, await ListeAsync(aracId));

        Guid[] yeniSira = [varliklar[2], varliklar[0], varliklar[1]];

        await YazAsync(aracId, yeniSira, medya);

        Assert.Equal(yeniSira, await ListeAsync(aracId));
    }

    [Fact(DisplayName = "0012a/AC-7: Çıkarmak varlığı silmez: varlık `Media`'da durur ve türevleri okunabilir")]
    public async Task Silmez_Cikarmak_Varligi()
    {
        FakeMediaAssetQueries medya = new();
        Guid kalan = medya.Ekle(MediaProcessingStatus.Ready);
        Guid cikarilan = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync();

        await YazAsync(aracId, [kalan, cikarilan], medya);
        await YazAsync(aracId, [kalan], medya);

        Assert.Equal([kalan], await ListeAsync(aracId));

        // F1 — çıkarmak silmek değildir: varlık `Media`'da duruyor ve türevi okunabiliyor.
        MediaAssetDto? varlik = await medya.FindAsync(cikarilan, CancellationToken.None);

        Assert.NotNull(varlik);
        Assert.NotEmpty(varlik.Derivatives);
    }

    /// <summary>
    /// Boş liste geçerli: "bütün fotoğrafları çıkar" demek ve varlıkları silmiyor (R3).
    /// </summary>
    [Fact]
    public async Task Gecerlidir_BosListe()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync();

        await YazAsync(aracId, [varlik], medya);

        Assert.True((await YazAsync(aracId, [], medya)).IsSuccess);
        Assert.Empty(await ListeAsync(aracId));
        Assert.NotNull(await medya.FindAsync(varlik, CancellationToken.None));
    }

    [Fact(DisplayName = "0012a/AC-9: Var olmayan bir varlık bağlanamaz; istek çevrilmiş metin değil `errorCode` döner")]
    public async Task Baglanamaz_VarOlmayanVarlik()
    {
        FakeMediaAssetQueries medya = new();
        Guid gercek = medya.Ekle(MediaProcessingStatus.Ready);
        Guid olmayan = Guid.CreateVersion7();
        Guid aracId = await AracEkleAsync();

        Result sonuc = await YazAsync(aracId, [gercek, olmayan], medya);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Vehicle.MediaAssetNotFound, sonuc.Error.Code);
        Assert.Equal(olmayan.ToString(), sonuc.Error.Parameters!["mediaAssetId"]);

        // Reddedilen istek listeyi BOZMAMALI.
        Assert.Empty(await ListeAsync(aracId));
    }

    [Fact(DisplayName = "0012a/AC-8: Aynı varlık bir araca iki kez bağlanamaz")]
    public async Task Baglanamaz_AyniVarlik_IkiKez()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync();

        Result sonuc = await YazAsync(aracId, [varlik, varlik], medya);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Vehicle.DuplicateMediaAsset, sonuc.Error.Code);
    }

    /// <summary>
    /// D14 — bağlamanın DURUM ön koşulu yok: `işleniyor` ve `başarısız` varlıklar da
    /// bağlanabilir. R5 başarısız varlığın panelde görünmesini istiyor ve görünmesi için
    /// bağlı olması gerekiyor.
    /// </summary>
    [Theory]
    [InlineData(MediaProcessingStatus.Uploaded)]
    [InlineData(MediaProcessingStatus.Processing)]
    [InlineData(MediaProcessingStatus.Failed)]
    public async Task Baglanabilir_HazirOlmayanVarlik(MediaProcessingStatus durum)
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(durum);
        Guid aracId = await AracEkleAsync();

        Assert.True((await YazAsync(aracId, [varlik], medya)).IsSuccess);
        Assert.Equal([varlik], await ListeAsync(aracId));
    }

    [Fact(DisplayName = "0012a/AC-24: Aracın medya sorgusu sıralı listeyi `Media`'nın durum, `alt` ve türev bilgisiyle birleştirir")]
    public async Task Birlestirir_Sorgu_MedyaBilgisiyle()
    {
        FakeMediaAssetQueries medya = new();
        Guid hazir = medya.Ekle(MediaProcessingStatus.Ready, altTr: "beyaz BMW, önden");
        Guid isleniyor = medya.Ekle(MediaProcessingStatus.Processing);
        Guid basarisiz = medya.Ekle(MediaProcessingStatus.Failed);
        Guid aracId = await AracEkleAsync();

        await YazAsync(aracId, [isleniyor, hazir, basarisiz], medya);

        VehicleMediaDto? sonuc = await SorguAsync(aracId, medya);

        Assert.NotNull(sonuc);
        Assert.Equal(3, sonuc.Items.Count);

        // Sıra `VehicleCatalog`'un verisi ve yanıt onu KORUYOR.
        Assert.Equal([isleniyor, hazir, basarisiz], [.. sonuc.Items.Select(oge => oge.MediaAssetId)]);

        // Durumlar KOD olarak geçiyor, çevrilmiş ad değil.
        Assert.Equal(["processing", "ready", "failed"], [.. sonuc.Items.Select(oge => oge.Status)]);

        // Kapak TÜRETİLMİŞ: sıralı listenin ilk elemanı (D2).
        Assert.Equal(isleniyor, sonuc.CoverMediaAssetId);

        // `alt` metni ve türevler `Media`'dan geliyor.
        Assert.Equal("beyaz BMW, önden", sonuc.Items[1].AltText["tr"]);
        Assert.NotEmpty(sonuc.Items[1].Derivatives);
        Assert.Empty(sonuc.Items[0].Derivatives);

        // Başarısız varlığın sebebi KAPALI kümeden bir kod.
        Assert.Equal("watermarkunavailable", sonuc.Items[2].FailureReason);
    }

    /// <summary>
    /// D10 — birleştirme TEK toplu okumayla yapılıyor. Varlık başına çağırmak N+1 istek
    /// demekti ve bu test o regresyonun kapısı.
    /// </summary>
    [Fact]
    public async Task Okur_TekTopluCagri_UcVarlikIcin()
    {
        FakeMediaAssetQueries medya = new();
        Guid[] varliklar =
        [
            medya.Ekle(MediaProcessingStatus.Ready),
            medya.Ekle(MediaProcessingStatus.Ready),
            medya.Ekle(MediaProcessingStatus.Ready),
        ];
        Guid aracId = await AracEkleAsync();

        await YazAsync(aracId, varliklar, medya);

        int oncesi = medya.TopluOkumaSayisi;

        await SorguAsync(aracId, medya);

        Assert.Equal(oncesi + 1, medya.TopluOkumaSayisi);
    }

    [Fact]
    public async Task Dondurur_BosYuzey_HicBagliVarlikYoksa()
    {
        FakeMediaAssetQueries medya = new();
        Guid aracId = await AracEkleAsync();

        VehicleMediaDto? sonuc = await SorguAsync(aracId, medya);

        Assert.NotNull(sonuc);
        Assert.Empty(sonuc.Items);
        Assert.Null(sonuc.CoverMediaAssetId);
    }

    [Fact]
    public async Task Dondurur_Null_VarOlmayanArac()
    {
        FakeMediaAssetQueries medya = new();

        Assert.Null(await SorguAsync(Guid.CreateVersion7(), medya));
    }

    /// <summary>
    /// Her teste KENDİ aracı ve kendi VIN'i. İlk yazımda builder'ın sabit VIN'ini
    /// kullandım ve `0010a`'nın kısmi tekil indeksi (`ux_vehicles_vin`, ADR `0006`) haklı
    /// olarak reddetti — paylaşılan seed'e bağımlı test yazılmaz (`docs/testing.md` § 4) ve
    /// paylaşılan VIN de aynı kapsamda.
    /// </summary>
    private async Task<Guid> AracEkleAsync()
    {
        Vehicle vehicle = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"MEDYA{Interlocked.Increment(ref sayac):D12}")
            .Build();

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(vehicle);
        await context.SaveChangesAsync();

        return vehicle.Id.Value;
    }

    private async Task<Result> YazAsync(Guid aracId, Guid[] varliklar, FakeMediaAssetQueries medya)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        UpdateVehicleMediaCommand komut = new(context, medya, new FixedClock(Simdi));

        return await komut.ExecuteAsync(aracId, varliklar, CancellationToken.None);
    }

    private async Task<Guid[]> ListeAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Vehicle vehicle = await context.Vehicles
            .AsNoTracking()
            .SingleAsync(value => value.Id == new VehicleId(aracId));

        return [.. vehicle.MediaAssetIds];
    }

    private async Task<VehicleMediaDto?> SorguAsync(Guid aracId, FakeMediaAssetQueries medya)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await new VehicleMediaQueries(context, medya).FindAsync(aracId, CancellationToken.None);
    }
}
