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

    [Fact(DisplayName = "0011/AC-11: İşleme başladığında durum `işleniyor`, başarıyla bittiğinde `hazır` olur")]
    public async Task Olur_Durum_Hazir_IslemeBittiginde()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Ready, kayit.Status);
        Assert.Equal(1, kayit.AttemptCount);
        Assert.Null(kayit.FailureReason);
    }

    [Fact(DisplayName = "0011/AC-19: Her türev kendi depolama yolunda durur ve yol `(varlık kimliği, etiket)`'ten deterministik üretilir")]
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
    [Fact(DisplayName = "0011/AC-41: Türev, S3 uyumlu sağlayıcıya yazılır ve oradan okunabilir")]
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

    [Fact(DisplayName = "0011/AC-24: Yüklenen görselin konum ve cihaz bilgisi türevlerde bulunmaz")]
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

    [Fact(DisplayName = "0011/AC-26: Orijinal dosya işleme bittikten sonra da depoda durur")]
    public async Task Durur_Orijinal_IslemeBittiktenSonra()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Ready, kayit.Status);
        Assert.NotNull(await ortam.NesneOkuAsync(kayit.OriginalObjectKey));
        Assert.False(MediaObjectKeys.IsPubliclyReachable(kayit.OriginalObjectKey));
    }

    [Fact(DisplayName = "0011/AC-28: Varlık hazır olduğunda `MediaAssetReady` yayılır; duyuru varlığın kimliğini taşır")]
    public async Task Yayilir_MediaAssetReady_VarlikHazirOldugunda()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        OutboxMessage duyuru = await TekDuyuruAsync();

        Assert.Equal(typeof(MediaAssetReady).FullName, duyuru.Type);
        Assert.Equal(id, GovdeAsync(duyuru).MediaAssetId);
    }

    /// <summary>
    /// AC-29 — <b>tek bir erişim yolu yeterli değildir.</b> Duyuru üç türevi birlikte
    /// taşıyor ve her biri etiket, yol, genişlik ve yükseklik veriyor.
    ///
    /// <para>Genişlik/yükseklik GERÇEKLEŞEN boyut: AC-18 gereği kaynak hedeften küçükse
    /// büyütme yapılmıyor ve hedefi taşımak "1600" yazıp 900 piksel servis etmek olurdu
    /// (`docs/frontend.md` § 7'nin CLS borcu).</para>
    /// </summary>
    [Fact(DisplayName = "0011/AC-29: Hazır duyurusu **türev kümesini** taşır: her türev için etiket, erişim yolu, genişlik ve yükseklik")]
    public async Task Tasir_Duyuru_TurevKumesini()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        MediaAssetReady govde = GovdeAsync(await TekDuyuruAsync());
        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(3, govde.Derivatives.Count);
        Assert.Equal(
            ["large", "display", "thumbnail"],
            [.. govde.Derivatives.Select(turev => turev.Variant)]);

        foreach (MediaDerivativePayload turev in govde.Derivatives)
        {
            MediaDerivative kayitli = kayit.Derivatives
                .Single(deger => deger.VariantCode == turev.Variant);

            // Boyutlar kayıttaki GERÇEKLEŞEN boyutla birebir.
            Assert.Equal(kayitli.Width, turev.Width);
            Assert.Equal(kayitli.Height, turev.Height);
            Assert.True(turev.Width > 0 && turev.Height > 0);

            // Yol yapılandırılan taban adresten üretilmiş.
            Assert.Equal(
                $"https://cdn.ornek.test/{MedyaOrtami.Bucket}/public/assets/{id:D}/{turev.Variant}.webp",
                turev.Url);
        }
    }

    /// <summary>
    /// AC-29 — erişim yolu YAPILANDIRILAN taban adresten üretiliyor, koda gömülü bir alan
    /// adından değil. Taban adres değiştirilip aynı varlık yeniden çözülüyor.
    /// </summary>
    [Fact(DisplayName = "0011/AC-40: Türevin erişim yolu yapılandırılan taban adresten üretilir")]
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
    [Theory(DisplayName = "0011/AC-40: Türevin erişim yolu yapılandırılan taban adresten üretilir")]
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

    [Fact(DisplayName = "0011/AC-31: Aynı varlık için ikinci bir duyuru oluşmaz")]
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

    [Fact(DisplayName = "0011/AC-16: Bir işleme turu, başka bir turun sahiplendiği varlığı yeniden işlemez")]
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
    [Fact(DisplayName = "0011/AC-16: Bir işleme turu, başka bir turun sahiplendiği varlığı yeniden işlemez")]
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

    [Fact(DisplayName = "0011/AC-23: Filigran kaynağı erişilemezse varlık `başarısız` olur; filigransız türev üretilmez")]
    public async Task Olur_Varlik_Basarisiz_FiligranYoksa()
    {
        Guid id = await HazirlaAsync();

        // Yapılandırma var olmayan bir anahtara işaret ediyor — C6 yapılandırmanın DOLU
        // olmasını garanti eder, kovada GERÇEKTEN bulunduğunu edemez.
        MediaOptions eksik = ortam.Options(watermarkObjectKey: "private/brand/yok.png");

        await TurKosAsync(eksik);

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Failed, kayit.Status);
        Assert.Equal(MediaFailureReason.WatermarkUnavailable, kayit.FailureReason);
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

    [Fact(DisplayName = "0011/AC-32: İşleme başarısız olduğunda `MediaAssetFailed` yayılır")]
    public async Task Yayilir_MediaAssetFailed_IslemeBasarisizOldugunda()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync(ortam.Options(watermarkObjectKey: "private/brand/yok.png"));

        OutboxMessage duyuru = await TekDuyuruAsync();

        Assert.Equal(typeof(MediaAssetFailed).FullName, duyuru.Type);

        MediaAssetFailed govde = System.Text.Json.JsonSerializer
            .Deserialize<MediaAssetFailed>(duyuru.Payload)!;

        Assert.Equal(id, govde.MediaAssetId);
        Assert.Equal(MediaFailureReason.WatermarkUnavailable, govde.Reason);
    }

    /// <summary>
    /// AC-33 — sebep KAPALI bir kümedendir. Gövdede serbest metin yok: değer enum'un
    /// adıdır ve uç onu kendi kataloğundan çevirir.
    /// </summary>
    [Fact(DisplayName = "0011/AC-33: Başarısızlık duyurusunun sebebi kapalı bir kümedendir; serbest metin veya çevrilmiş metin taşımaz")]
    public async Task Tasimaz_SerbestMetin_BasarisizlikDuyurusu()
    {
        await HazirlaAsync();

        await TurKosAsync(ortam.Options(watermarkObjectKey: "private/brand/yok.png"));

        OutboxMessage duyuru = await TekDuyuruAsync();

        // JSON'daki değer kapalı kümenin bir üyesi — serbest bir cümle değil.
        System.Text.Json.JsonElement kok = System.Text.Json.JsonDocument
            .Parse(duyuru.Payload).RootElement;

        string sebep = kok.GetProperty("Reason").ToString();

        Assert.Contains(
            sebep,
            Enum.GetValues<MediaFailureReason>()
                .SelectMany(deger => new[] { deger.ToString(), ((int)deger).ToString(
                    System.Globalization.CultureInfo.InvariantCulture) }));

        // Gövdede boşluk içeren bir metin alanı yok: cümle taşımıyor.
        Assert.DoesNotContain(" ", sebep, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0011/AC-13: İşleme başarısız olduğunda durum `başarısız` olur; varlık `yüklendi` durumunda kalmaz")]
    public async Task Olur_Durum_Basarisiz_TurDustugunde()
    {
        Guid id = await HazirlaAsync();

        Assert.Equal(1, await TurKosAsync(ortam.Options(watermarkObjectKey: "private/brand/yok.png")));

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Failed, kayit.Status);
        Assert.NotEqual(MediaProcessingStatus.Uploaded, kayit.Status);
        Assert.Equal(1, kayit.AttemptCount);
    }

    /// <summary>
    /// AC-15 — otomatik tekrar YOK. Önceki hâlde varlık deneme bütçesi tükenene kadar
    /// `yüklendi`'ye dönüyordu ve sonraki tur onu kendiliğinden yeniden eline alıyordu.
    /// </summary>
    [Fact(DisplayName = "0011/AC-15: Tekrar deneme otomatik tetiklenmez; komut çağrılmadan durum kendiliğinden değişmez")]
    public async Task Tetiklenmez_TekrarDeneme_Otomatik()
    {
        Guid id = await HazirlaAsync();
        MediaOptions bozuk = ortam.Options(watermarkObjectKey: "private/brand/yok.png");

        Assert.Equal(1, await TurKosAsync(bozuk));

        // Filigran geri konuyor: tek engel o olsaydı sonraki tur varlığı işleyebilirdi.
        await ortam.FiligraniYerlestirAsync(GorselFixture.FiligranPng());

        // İki tur daha — hiçbiri varlığı almıyor, çünkü `başarısız`dan kendiliğinden
        // çıkış yok.
        Assert.Equal(0, await TurKosAsync());
        Assert.Equal(0, await TurKosAsync());

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Failed, kayit.Status);
        Assert.Equal(1, kayit.AttemptCount);
    }

    /// <summary>
    /// AC-14 — açık komut varlığı yeniden kuyruğa alıyor ve bir sonraki tur onu işliyor;
    /// kimlik değişmiyor.
    /// </summary>
    [Fact(DisplayName = "0011/AC-14: `başarısız` terminal değildir: tekrar deneme ayrı bir komutla tetiklenir ve aynı varlık kimliğini korur")]
    public async Task Tetiklenir_TekrarDeneme_AcikKomutla()
    {
        Guid id = await HazirlaAsync();

        Assert.Equal(1, await TurKosAsync(ortam.Options(watermarkObjectKey: "private/brand/yok.png")));
        Assert.Equal(MediaProcessingStatus.Failed, (await VarlikAsync(id)).Status);

        await ortam.FiligraniYerlestirAsync(GorselFixture.FiligranPng());

        await using (MediaDbContext context = ortam.CreateContext())
        {
            RetryMediaAssetCommand komut = new(context, new SabitSaat(Simdi));

            Assert.True((await komut.ExecuteAsync(id, CancellationToken.None)).IsSuccess);
        }

        Assert.Equal(MediaProcessingStatus.Uploaded, (await VarlikAsync(id)).Status);
        Assert.Equal(1, await TurKosAsync());

        MediaAsset kayit = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Ready, kayit.Status);
        Assert.Equal(id, kayit.Id);
        Assert.Equal(3, kayit.Derivatives.Count);
    }

    /// <summary>
    /// AC-14'ün diğer yarısı: komut yalnız `başarısız` durumdan çalışır. `hazır` bir
    /// varlığı tekrar kuyruğa almak, türevlerini silip yeniden üretmek olurdu.
    /// </summary>
    [Fact]
    public async Task Reddedilir_TekrarDeneme_BasarisizOlmayanVarlikta()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();
        Assert.Equal(MediaProcessingStatus.Ready, (await VarlikAsync(id)).Status);

        await using MediaDbContext context = ortam.CreateContext();
        RetryMediaAssetCommand komut = new(context, new SabitSaat(Simdi));

        Result sonuc = await komut.ExecuteAsync(id, CancellationToken.None);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(MediaErrors.NotRetryable, sonuc.Error.Code);
        Assert.Equal("ready", sonuc.Error.Parameters!["status"]);
    }

    /// <summary>
    /// AC-23 — duyuru iş verisiyle AYNI `SaveChanges`'te yazılıyor. Ayrı yazılsaydı ya
    /// duyurulmayan bir hazır varlık ya da olmayan bir varlığın duyurusu kalırdı.
    ///
    /// <para>Ölçüm: `SaveChanges` sayılıyor. Hazır işaretleme + duyuru tek çağrıda
    /// gidiyorsa, varlığın `Hazır` olduğu ve duyurunun bulunduğu AYNI anda doğrudur —
    /// birini görüp diğerini görmeyen bir okuma mümkün değil.</para>
    /// </summary>
    [Fact(DisplayName = "0011/AC-30: Duyuru, iş verisiyle aynı transaction'da outbox'a yazılır")]
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
        KayitYakalayici? yakalayici = null,
        IObjectStore? depo = null,
        MediaDbContext? context = null)
    {
        MediaOptions ayarlar = options ?? ortam.Options();
        MediaDbContext kendi = context ?? ortam.CreateContext(yakalayici);

        try
        {
            MediaProcessingService servis = new(
                kendi,
                depo ?? ortam.Store(ayarlar),
                new SkiaImageProcessor(),
                new MediaUrlResolver(ayarlar),
                new OutboxWriter<MediaDbContext>(kendi, clock ?? new SabitSaat(Simdi)),
                ayarlar,
                clock ?? new SabitSaat(Simdi),
                NullLogger<MediaProcessingService>.Instance);

            return await servis.ProcessDueAsync(CancellationToken.None);
        }
        finally
        {
            if (context is null)
            {
                await kendi.DisposeAsync();
            }
        }
    }

    private async Task<MediaAsset> VarlikAsync(Guid id)
    {
        await using MediaDbContext oku = ortam.CreateContext();

        return await oku.Assets.AsNoTracking().Include(a => a.Derivatives).SingleAsync(a => a.Id == id);
    }

    private async Task<MediaAssetDto?> SorguAsync(Guid id)
    {
        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        return await new MediaAssetQueries(context, new MediaUrlResolver(ayarlar))
            .FindAsync(id, CancellationToken.None);
    }

    private static MediaAssetReady GovdeAsync(OutboxMessage duyuru) =>
        System.Text.Json.JsonSerializer.Deserialize<MediaAssetReady>(duyuru.Payload)!;

    private async Task<OutboxMessage> TekDuyuruAsync()
    {
        await using MediaDbContext oku = ortam.CreateContext();

        return await oku.OutboxMessages.AsNoTracking().SingleAsync();
    }

    /// <summary>
    /// AC-20 — "hazır" damgasından ÖNCE kesilen bir koşu, ikinci turda kopya dosya
    /// üretmez. C12/D10: türev yolları `(kimlik, etiket)`'ten deterministik ve durum
    /// geçişi tek commit noktası.
    ///
    /// <para>Kesinti GERÇEK: depo son türevin yazımını reddediyor, yani ilk iki türev
    /// kovaya çoktan yazılmış ve varlık `başarısız` oluyor. Sonra AÇIK komutla (AC-14)
    /// yeniden kuyruğa alınıyor ve ikinci tur aynı anahtarların ÜZERİNE yazıyor —
    /// `PK_asset_derivatives` çakışması ya da sahipsiz kopya dosya üretmiyor.</para>
    /// </summary>
    [Fact(DisplayName = "0011/AC-20: \"Hazır\" damgasından önce kesilen bir koşu, ikinci turda kopya dosya üretmez")]
    public async Task Uretmez_KopyaDosya_KesilenKosudanSonra()
    {
        Guid id = await HazirlaAsync();

        MediaOptions ayarlar = ortam.Options();
        ReddedenDepo reddeden = new(ortam.Store(ayarlar), "thumbnail");

        await using (MediaDbContext context = ortam.CreateContext())
        {
            await TurKosAsync(ayarlar, depo: reddeden, context: context);
        }

        MediaAsset kesilen = await VarlikAsync(id);

        // Koşu kesildi: varlık hazır DEĞİL ve türev kaydı yok.
        Assert.Equal(MediaProcessingStatus.Failed, kesilen.Status);
        Assert.Empty(kesilen.Derivatives);

        // Ama ilk iki türev kovaya YAZILDI — ikinci turun üzerine yazacağı şey bu.
        Assert.NotNull(await ortam.NesneOkuAsync(MediaObjectKeys.Derivative(id, MediaVariant.Large)));
        Assert.NotNull(await ortam.NesneOkuAsync(MediaObjectKeys.Derivative(id, MediaVariant.Display)));

        await using (MediaDbContext context = ortam.CreateContext())
        {
            RetryMediaAssetCommand komut = new(context, new SabitSaat(Simdi));
            Assert.True((await komut.ExecuteAsync(id, CancellationToken.None)).IsSuccess);
        }

        Assert.Equal(1, await TurKosAsync());

        MediaAsset ikinci = await VarlikAsync(id);

        Assert.Equal(MediaProcessingStatus.Ready, ikinci.Status);

        // Kopya YOK: üç türev, üç ayrı anahtar — ve anahtarlar birinci turdakilerle aynı.
        Assert.Equal(3, ikinci.Derivatives.Count);
        Assert.Equal(
            3,
            ikinci.Derivatives.Select(turev => turev.ObjectKey).Distinct(StringComparer.Ordinal).Count());

        foreach (MediaVariant variant in MediaVariant.All)
        {
            string beklenen = MediaObjectKeys.Derivative(id, variant);

            Assert.Contains(beklenen, ikinci.Derivatives.Select(turev => turev.ObjectKey));
            Assert.NotNull(await ortam.NesneOkuAsync(beklenen));
        }
    }

    /// <summary>
    /// AC-27 — orijinal herkese açık SUNULMAZ. Yol üretimi public olmayan bir anahtarı
    /// reddediyor; sessiz bir <c>null</c>, çağıranın onu bir yere yazmasına izin verirdi.
    /// </summary>
    [Fact(DisplayName = "0011/AC-27: Orijinal dosya herkese açık sunulmaz; onun için erişim yolu üretilemez")]
    public async Task Uretilemez_ErisimYolu_OrijinalIcin()
    {
        Guid id = await HazirlaAsync();

        await TurKosAsync();

        MediaAsset kayit = await VarlikAsync(id);
        MediaUrlResolver cozucu = new(ortam.Options());

        Assert.False(MediaObjectKeys.IsPubliclyReachable(kayit.OriginalObjectKey));
        Assert.Throws<InvalidOperationException>(() => cozucu.Resolve(kayit.OriginalObjectKey));

        // Okuma yüzeyi de orijinali hiç taşımıyor: gizlenmiyor, bulunmuyor
        // (Yasak Listesi 41'in aynı mantığı).
        MediaAssetDto? okunan = await SorguAsync(id);

        Assert.NotNull(okunan);
        Assert.DoesNotContain(
            okunan.Derivatives,
            turev => turev.Url.Contains("private/", StringComparison.Ordinal));
    }

    /// <summary>
    /// AC-42 — bir varlık KÜMESİNİN hazır olup olmadığı. `0012` bunu A2'nin yayın şartına
    /// bağlayacak (M3); bu spec yüzeyi sunar, tüketmez (X7).
    /// </summary>
    [Fact(DisplayName = "0011/AC-42: `Media`, bir varlık kümesinin hazır olup olmadığını yanıtlayan bir okuma yüzeyi sunar")]
    public async Task Yanitlar_KumeHazirMi_OkumaYuzeyi()
    {
        await ortam.SifirlaAsync();
        await ortam.FiligraniYerlestirAsync(GorselFixture.FiligranPng());

        Guid hazirId = await YukleAsync();

        Assert.Equal(1, await TurKosAsync());

        // İkinci varlık turdan SONRA yükleniyor: ilk denememde ikisini birden yükleyip
        // "yalnız birini işle" diye bir kanca koymuştum, ama tur bir partide HEPSİNİ alıyor
        // ve ikincisi de hazır oluyordu — test kendi kurgusuyla yanlış ölçüyordu.
        Guid bekleyenId = await YukleAsync();

        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();
        MediaAssetQueries sorgular = new(context, new MediaUrlResolver(ayarlar));

        Assert.True(await sorgular.AreAllReadyAsync([hazirId], CancellationToken.None));
        Assert.False(await sorgular.AreAllReadyAsync([bekleyenId], CancellationToken.None));
        Assert.False(await sorgular.AreAllReadyAsync([hazirId, bekleyenId], CancellationToken.None));

        // Aynı kimlik iki kez verildiğinde eşitlik bozulmuyor.
        Assert.True(await sorgular.AreAllReadyAsync([hazirId, hazirId], CancellationToken.None));

        // Boş küme `false`: A2 en az bir varlık istiyor, yani "hiç varlık yok" sorusunun
        // doğru cevabı "hazır değil". `true` dönmek, fotoğrafı olmayan aracı yayına uygun
        // göstermekti.
        Assert.False(await sorgular.AreAllReadyAsync([], CancellationToken.None));

        // Var olmayan kimlik de hazır değil.
        Assert.False(await sorgular.AreAllReadyAsync([Guid.CreateVersion7()], CancellationToken.None));
    }
}
