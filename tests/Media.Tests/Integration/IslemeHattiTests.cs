using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Media.Tests.Fixtures;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Contracts.Events;
using CarGallery.Modules.Media.Contracts.Setup;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// Hattın tamamı: sahiplenme, türev üretimi, depoya yazma, duyuru ve başarısızlık.
/// Gerçek PostgreSQL + gerçek S3 uyumlu depo.
/// </summary>
[Collection(MedyaOrtamiKoleksiyonu.Name)]
public sealed class IslemeHattiTests(MedyaOrtami ortam)
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0011/AC-9: İşleme başladığında durum `İşleniyor`, başarıyla bittiğinde `Hazır` olur")]
    public async Task Olur_Durum_Hazir_IslemeBittiginde()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Ready, kayit.Status);
        Assert.Equal(1, kayit.AttemptCount);
        Assert.Null(kayit.FailureReason);
    }

    [Fact(DisplayName = "0011/AC-15: Her türev kendi depolama yolunda durur ve yol varlığın kimliğinden türer")]
    public async Task Durur_HerTurev_KendiYolunda()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(3, kayit.Derivatives.Count);

        foreach (MediaDerivative turev in kayit.Derivatives)
        {
            Assert.Equal($"public/assets/{id:D}/{turev.VariantCode}.webp", turev.ObjectKey);

            // Yolun KULLANILABİLİR olduğu da burada görülüyor; AC-30'un kendi testi
            // ayrıca baytların ve boyutun doğruluğunu ölçüyor.
            byte[]? govde = await ortam.NesneOkuAsync(turev.ObjectKey);

            Assert.NotNull(govde);
            Assert.Equal(
                DetectedImageFormat.Webp,
                ImageFormatDetection.Detect(govde.AsSpan(0, 32)));
        }

        Assert.Equal(
            kayit.Derivatives.Count,
            kayit.Derivatives.Select(turev => turev.ObjectKey).Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// AC-30'un KENDİ testi. Önceki hâli AC-15'in testinin içinde bir yorum satırıydı ve
    /// etiket taramasıyla kurulan kriter → test eşlemesi onu hiç görmüyordu: etiketsiz
    /// kriter, testi bir gün dar bir refactor'da düştüğünde kimsenin fark etmeyeceği
    /// kriterdir.
    /// </summary>
    [Fact(DisplayName = "0011/AC-30: Türev, S3 uyumlu sağlayıcıya yazılır ve oradan okunabilir")]
    public async Task Okunabilir_Turev_S3UyumluSaglayicidan()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(3, kayit.Derivatives.Count);

        foreach (MediaDerivative turev in kayit.Derivatives)
        {
            byte[]? govde = await ortam.NesneOkuAsync(turev.ObjectKey);

            Assert.NotNull(govde);
            Assert.NotEmpty(govde);

            // Yazılan şey GERÇEKTEN o türev: boyutu kayıtla uyuşuyor.
            using SkiaSharp.SKBitmap cozulen = SkiaSharp.SKBitmap.Decode(govde);

            Assert.Equal(turev.Width, cozulen.Width);
            Assert.Equal(turev.Height, cozulen.Height);
        }
    }

    [Fact(DisplayName = "0011/AC-19: Yüklenen görselin konum ve cihaz bilgisi türevlerde bulunmaz")]
    public async Task Bulunmaz_KonumVeCihazBilgisi_DepodakiTurevlerde()
    {
        // Hattın UCUNDA ölçüyor: işlemcinin çıktısı değil, depoya GERÇEKTEN yazılmış bayt.
        Guid id = await HazirlaAsync(GorselFixture.ExifliJpeg());

        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);

        foreach (MediaDerivative turev in kayit.Derivatives)
        {
            byte[]? govde = await ortam.NesneOkuAsync(turev.ObjectKey);

            Assert.NotNull(govde);
            Assert.Empty(ExifOkuyucu.KonumVeCihazEtiketleri(govde));
        }
    }

    [Fact(DisplayName = "0011/AC-21: Orijinal dosya işleme bittikten sonra da depoda durur ve public yolda bulunmaz")]
    public async Task Durur_Orijinal_IslemeBittiktenSonra()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Ready, kayit.Status);
        Assert.NotNull(await ortam.NesneOkuAsync(kayit.OriginalObjectKey));
        Assert.False(MediaObjectKeys.IsPubliclyReachable(kayit.OriginalObjectKey));
    }

    [Fact(DisplayName = "0011/AC-22: Varlık hazır olduğunda `MediaAssetReady` yayılır; duyuru varlığın kimliğini ve erişim yolunu taşır")]
    public async Task Yayilir_MediaAssetReady_VarlikHazirOldugunda()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        OutboxMessage duyuru = await TekDuyuruAsync();

        Assert.Equal(typeof(MediaAssetReady).FullName, duyuru.Type);

        MediaAssetReady govde = System.Text.Json.JsonSerializer
            .Deserialize<MediaAssetReady>(duyuru.Payload)!;

        Assert.Equal(id, govde.MediaAssetId);

        // D11 — `Url` GÖSTERİM türevidir (800 px) ve yapılandırılan taban adresten üretilir.
        Assert.Equal($"https://cdn.ornek.test/{MedyaOrtami.Bucket}/public/assets/{id:D}/display.webp", govde.Url);
    }

    /// <summary>
    /// AC-29 — erişim yolu YAPILANDIRILAN taban adresten üretiliyor, koda gömülü bir alan
    /// adından değil. Taban adres değiştirilip aynı varlık yeniden çözülüyor.
    /// </summary>
    [Fact(DisplayName = "0011/AC-29: Türevin erişim yolu yapılandırılan taban adresten üretilir")]
    public async Task Uretilir_ErisimYolu_YapilandirilanTabandan()
    {
        Guid id = await HazirlaAsync();
        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);
        string anahtar = kayit.Derivatives.Single(t => t.VariantCode == "display").ObjectKey;

        MediaOptions birinci = ortam.Options();
        Assert.Equal(
            $"https://cdn.ornek.test/{MedyaOrtami.Bucket}/{anahtar}",
            new MediaUrlResolver(birinci).Resolve(anahtar).ToString());

        MediaSetup ikinciSetup = ortam.Setup() with { PublicBaseUrl = "https://baska-cdn.ornek.test/kok/" };
        MediaOptions ikinci = MediaOptions.Create(ikinciSetup).Value;

        Assert.Equal(
            $"https://baska-cdn.ornek.test/kok/{anahtar}",
            new MediaUrlResolver(ikinci).Resolve(anahtar).ToString());
    }

    /// <summary>
    /// AC-29'un eksik yarısı: taban adres sondaki eğik çizgi OLMADAN yazıldığında yol
    /// segmenti korunmalı.
    ///
    /// <para>Önceki testin iki vakası da eğik çizgiyle bitiyordu ve bu yüzden kusuru
    /// görmüyordu: <c>new Uri(taban, anahtar)</c> göreli çözümünde eğik çizgi yoksa son
    /// segment (<c>/kova</c>) <b>sessizce düşüyor</b> ve vitrindeki her görsel 404 olurdu.</para>
    /// </summary>
    [Theory(DisplayName = "0011/AC-29: Türevin erişim yolu yapılandırılan taban adresten üretilir")]
    [InlineData("https://cdn.ornek.test/kova/")]
    [InlineData("https://cdn.ornek.test/kova")]
    public void Korunur_YolSegmenti_EgikCizgiOlmasaDa(string tabanAdres)
    {
        MediaOptions ayarlar = MediaOptions.Create(
            ortam.Setup() with { PublicBaseUrl = tabanAdres }).Value;

        const string anahtar = "public/assets/abc/display.webp";

        Assert.Equal(
            $"https://cdn.ornek.test/kova/{anahtar}",
            new MediaUrlResolver(ayarlar).Resolve(anahtar).ToString());
    }

    /// <summary>
    /// D16 — orijinalin erişim yolu ÜRETİLEMEZ. Üretilebilse, ham EXIF taşıyan dosya
    /// yayınlanabilir olurdu ve R5'in kapattığı delik arka kapıdan açılırdı.
    /// </summary>
    [Fact]
    public void Uretilemez_ErisimYolu_PrivateAnahtarIcin()
    {
        MediaUrlResolver cozucu = new(ortam.Options());

        Assert.Throws<InvalidOperationException>(
            () => cozucu.Resolve("private/originals/abc.jpg"));
    }

    [Fact(DisplayName = "0011/AC-24: Aynı varlık için ikinci bir duyuru oluşmaz")]
    public async Task Olusmaz_IkinciDuyuru_AyniVarlikIcin()
    {
        await HazirlaAsync();

        // Üç tur üst üste. İkinci ve üçüncü turlar varlığı artık `Yüklendi` görmez (D14):
        // sahiplenme geçişinin kendisi kilittir.
        await TurKosAsync();
        await TurKosAsync();
        await TurKosAsync();

        await using MediaDbContext oku = ortam.CreateContext();
        List<OutboxMessage> duyurular = await oku.OutboxMessages.AsNoTracking().ToListAsync();

        Assert.Single(duyurular);
    }

    [Fact(DisplayName = "0011/AC-12: Bir işleme turu, başka bir turun sahiplendiği varlığı yeniden işlemez")]
    public async Task Islemez_Tur_BaskaTurunSahiplendigiVarligi()
    {
        Guid id = await HazirlaAsync();

        // Birinci tur varlığı sahipleniyor ve ORADA duruyor (işi bitirmiyor): gerçek
        // hayatta süreç tam burada düşebilir.
        await using (MediaDbContext context = ortam.CreateContext())
        {
            MediaAsset asset = await context.Assets.SingleAsync(a => a.Id == id);
            Assert.True(asset.BeginProcessing(Simdi).IsSuccess);
            await context.SaveChangesAsync();
        }

        // İkinci tur: alacak bir şey bulamaz.
        int islenen = await TurKosAsync();

        Assert.Equal(0, islenen);

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Processing, kayit.Status);
        Assert.Equal(1, kayit.AttemptCount);
    }

    /// <summary>
    /// AC-12'nin ASIL riski: iki turun sahiplenme okuması <b>kesişiyor</b>. Sıralı vaka
    /// (yukarıdaki test) sorguyu durum filtresiyle koruyor ve kilidin var olup olmadığını
    /// hiç sormuyor — bu test onu soruyor.
    ///
    /// <para>Kilit kaldırıldığında kırmızı veriyor: kilitsiz bir <c>SELECT</c> + <c>UPDATE</c>
    /// çiftinde ikinci tur da varlığı sahipleniyor ve <c>attempt_count</c> kaybolan
    /// güncellemeyle <b>1</b>'de kalıyor.</para>
    /// </summary>
    [Fact(DisplayName = "0011/AC-12: Bir işleme turu, başka bir turun sahiplendiği varlığı yeniden işlemez")]
    public async Task Islemez_Tur_KesisenOkumadaBileAyniVarligi()
    {
        Guid id = await HazirlaAsync();

        // İki ayrı bağlantı, iki ayrı tur — ve ikisi birlikte koşuyor. `Task.WhenAll`
        // olmadan sıralama testin kendi kurgusundan gelirdi.
        Task<int> birinci = TurKosAsync();
        Task<int> ikinci = TurKosAsync();

        int[] islenen = await Task.WhenAll(birinci, ikinci);

        // Varlık BİR kez işlendi: turlardan biri onu aldı, diğeri kilitli satırı atladı.
        Assert.Equal(1, islenen.Sum());

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Ready, kayit.Status);
        Assert.Equal(1, kayit.AttemptCount);

        // Ve tek duyuru: ikinci tur işi tekrar yapıp çakışmaya düşmedi.
        await using MediaDbContext oku = ortam.CreateContext();

        Assert.Single(await oku.OutboxMessages.AsNoTracking().ToListAsync());

        // Türevler de bir kez yazıldı — çift yazma `PK_asset_derivatives` çakışmasıyla
        // patlıyordu ve son hâl "tutarlı" görünüyordu; bu iddia o tesadüfü kapatıyor.
        Assert.Equal(3, kayit.Derivatives.Count);
    }

    /// <summary>
    /// Kilit, hattı SERİ hâle getirmemeli: ikinci tur kilitli satırı beklemek yerine
    /// atlayıp sıradaki varlığı almalı (`SKIP LOCKED`). İki varlık, iki paralel tur →
    /// ikisi de işlenmiş olmalı.
    /// </summary>
    [Fact]
    public async Task Atlar_Tur_KilitliSatiri_VeSiradakiniAlir()
    {
        await ortam.SifirlaAsync();
        await ortam.FiligraniYerlestirAsync(GorselFixture.FiligranPng());

        Guid birinciId = await YukleAsync();
        Guid ikinciId = await YukleAsync();

        int[] islenen = await Task.WhenAll(TurKosAsync(), TurKosAsync());

        Assert.Equal(2, islenen.Sum());
        Assert.Equal(MediaProcessingStatus.Ready, (await VarlikAsync(birinciId)).Status);
        Assert.Equal(MediaProcessingStatus.Ready, (await VarlikAsync(ikinciId)).Status);
    }

    [Fact(DisplayName = "0011/AC-18: Filigran kaynağı erişilemezse varlık `Başarısız` olur; filigransız türev üretilmez")]
    public async Task Olur_Varlik_Basarisiz_FiligranYoksa()
    {
        Guid id = await HazirlaAsync();

        // Yapılandırma var olmayan bir anahtara işaret ediyor — C6 yapılandırmanın DOLU
        // olmasını garanti eder, kovada GERÇEKTEN bulunduğunu edemez.
        MediaOptions eksik = ortam.Options(
            maximumAttempts: 1, watermarkObjectKey: "private/brand/yok.png");

        await TurKosAsync(eksik);

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Failed, kayit.Status);
        Assert.Equal(MediaErrors.WatermarkUnavailable, kayit.FailureReason);
        Assert.Empty(kayit.Derivatives);

        // Filigransız türev ÜRETİLMEDİ: depoda hiçbir varyant yok.
        foreach (MediaVariant variant in MediaVariant.All)
        {
            Assert.Null(await ortam.NesneOkuAsync(MediaObjectKeys.Derivative(id, variant)));
        }
    }

    /// <summary>
    /// Filigran TUR BAŞINA bir kez indirilir, varlık başına değil. Kriter ihlali değil —
    /// ölçülmüş bir maliyet: önceki hâli 20'lik bir partide aynı nesneyi 20 kez çekiyordu.
    /// </summary>
    [Fact]
    public async Task Indirilir_Filigran_TurBasinaBirKez()
    {
        await ortam.SifirlaAsync();
        await ortam.FiligraniYerlestirAsync(GorselFixture.FiligranPng());

        await YukleAsync();
        await YukleAsync();
        await YukleAsync();

        MediaOptions ayarlar = ortam.Options();
        SayanDepo depo = new(ortam.Store(ayarlar));

        await using MediaDbContext context = ortam.CreateContext();

        MediaProcessingService servis = new(
            context,
            depo,
            new SkiaImageProcessor(),
            new MediaUrlResolver(ayarlar),
            new OutboxWriter<MediaDbContext>(context, new SabitSaat(Simdi)),
            ayarlar,
            new SabitSaat(Simdi),
            NullLogger<MediaProcessingService>.Instance);

        int islenen = await servis.ProcessDueAsync(CancellationToken.None);

        Assert.Equal(3, islenen);
        Assert.Equal(1, depo.OkumaSayisi(MedyaOrtami.WatermarkKey));
    }

    [Fact(DisplayName = "0011/AC-25: İşleme başarısız olduğunda `MediaAssetFailed` yayılır")]
    public async Task Yayilir_MediaAssetFailed_IslemeBasarisizOldugunda()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync(ortam.Options(maximumAttempts: 1, watermarkObjectKey: "private/brand/yok.png"));

        OutboxMessage duyuru = await TekDuyuruAsync();

        Assert.Equal(typeof(MediaAssetFailed).FullName, duyuru.Type);

        MediaAssetFailed govde = System.Text.Json.JsonSerializer
            .Deserialize<MediaAssetFailed>(duyuru.Payload)!;

        Assert.Equal(id, govde.MediaAssetId);

        // Sebep bir HATA KODU, kullanıcı metni değil (Yasak Listesi 23).
        Assert.Equal(MediaErrors.WatermarkUnavailable, govde.Reason);
    }

    [Fact(DisplayName = "0011/AC-11: Denemeler tükendiğinde durum `Başarısız` olur; varlık `Yüklendi` durumunda kalmaz")]
    public async Task Olur_Durum_Basarisiz_UcuncuTurdanSonra()
    {
        Guid id = await HazirlaAsync();

        MediaOptions bozuk = ortam.Options(
            maximumAttempts: 3, watermarkObjectKey: "private/brand/yok.png");

        SabitSaat saat = new(Simdi);

        // Birinci ve ikinci tur: hak var, varlık `Yüklendi`'ye döner. Saat ilerletiliyor
        // çünkü geri çekilme aralığı `NextAttemptAt`'i ileri atıyor.
        for (int tur = 1; tur <= 2; tur++)
        {
            Assert.Equal(1, await TurKosAsync(bozuk, saat));

            MediaAsset arada = await VarlikAsync(id);
            Assert.Equal(MediaProcessingStatus.Uploaded, arada.Status);
            Assert.Equal(tur, arada.AttemptCount);

            saat.Ilerle(TimeSpan.FromMinutes(10));
        }

        Assert.Equal(1, await TurKosAsync(bozuk, saat));

        MediaAsset son = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Failed, son.Status);
        Assert.Equal(3, son.AttemptCount);
    }

    /// <summary>
    /// AC-23 — duyuru iş verisiyle AYNI `SaveChanges`'te yazılıyor. Ayrı yazılsaydı ya
    /// duyurulmayan bir hazır varlık ya da olmayan bir varlığın duyurusu kalırdı.
    ///
    /// <para>Ölçüm: `SaveChanges` sayılıyor. Hazır işaretleme + duyuru tek çağrıda
    /// gidiyorsa, varlığın `Hazır` olduğu ve duyurunun bulunduğu AYNI anda doğrudur —
    /// birini görüp diğerini görmeyen bir okuma mümkün değil.</para>
    /// </summary>
    [Fact(DisplayName = "0011/AC-23: Duyuru, iş verisiyle aynı transaction'da outbox'a yazılır")]
    public async Task Yazilir_Duyuru_IsVerisiyleAyniTransactionda()
    {
        Guid id = await HazirlaAsync();

        KayitYakalayici yakalayici = new();

        await TurKosAsync(yakalayici: yakalayici);

        // Tur iki kez kaydediyor: (1) sahiplenme — tek başına, bilinçli olarak (D14, o
        // geçiş kilidin kendisi); (2) hazır işaretleme + duyuru — BİRLİKTE.
        IReadOnlyList<string> sonTur = yakalayici.Turlar[^1];

        Assert.Contains("MediaAsset:Modified", sonTur);
        Assert.Contains("OutboxMessage:Added", sonTur);

        // Ve hiçbir tur duyuruyu TEK BAŞINA yazmıyor: yazsaydı, varlık güncellemesi
        // olmadan bir duyuru kalıcı olabilirdi.
        Assert.DoesNotContain(
            yakalayici.Turlar,
            tur => tur.Contains("OutboxMessage:Added") && !tur.Contains("MediaAsset:Modified"));

        await using MediaDbContext oku = ortam.CreateContext();
        MediaAsset kayit = await oku.Assets.AsNoTracking().SingleAsync(a => a.Id == id);

        Assert.Equal(MediaProcessingStatus.Ready, kayit.Status);
        Assert.Single(await oku.OutboxMessages.AsNoTracking().ToListAsync());
    }

    private async Task<Guid> HazirlaAsync(byte[]? govde = null)
    {
        await ortam.SifirlaAsync();
        await ortam.FiligraniYerlestirAsync(GorselFixture.FiligranPng());

        return await YukleAsync(govde);
    }

    /// <summary>
    /// Sıfırlamadan yükler. Birden çok varlık gereken testler bunu kullanıyor:
    /// `HazirlaAsync` her çağrıda tabloyu boşaltıyor.
    /// </summary>
    private async Task<Guid> YukleAsync(byte[]? govde = null)
    {
        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        UploadMediaAssetCommand komut = new(
            context,
            ortam.Store(ayarlar),
            new SkiaImageProcessor(),
            ayarlar,
            new SabitSaat(Simdi));

        Result<UploadMediaAssetResult> sonuc = await komut.ExecuteAsync(
            govde ?? GorselFixture.DikeyJpeg(genislik: 2400, yukseklik: 1600),
            CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : string.Empty);

        return sonuc.Value.Id;
    }

    private async Task<int> TurKosAsync(
        MediaOptions? options = null,
        IClock? clock = null,
        KayitYakalayici? yakalayici = null)
    {
        MediaOptions ayarlar = options ?? ortam.Options();

        await using MediaDbContext context = ortam.CreateContext(yakalayici);

        MediaProcessingService servis = new(
            context,
            ortam.Store(ayarlar),
            new SkiaImageProcessor(),
            new MediaUrlResolver(ayarlar),
            new OutboxWriter<MediaDbContext>(context, clock ?? new SabitSaat(Simdi)),
            ayarlar,
            clock ?? new SabitSaat(Simdi),
            NullLogger<MediaProcessingService>.Instance);

        return await servis.ProcessDueAsync(CancellationToken.None);
    }

    private async Task<MediaAsset> VarlikAsync(Guid id)
    {
        await using MediaDbContext oku = ortam.CreateContext();

        return await oku.Assets.AsNoTracking().Include(a => a.Derivatives).SingleAsync(a => a.Id == id);
    }

    private async Task<OutboxMessage> TekDuyuruAsync()
    {
        await using MediaDbContext oku = ortam.CreateContext();

        return await oku.OutboxMessages.AsNoTracking().SingleAsync();
    }
}
