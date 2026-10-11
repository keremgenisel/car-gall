using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Infrastructure;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// `0012a` D5/D10 — `Media`'nın iki yeni okuması.
///
/// <para><b>Neden `AreAllReadyAsync` yetmiyor:</b> o "hepsi hazır mı" diye soruyor, **A2**
/// ise "en az bir hazır" istiyor. Bu testler ikisinin ZIT cevap verdiği durumu adıyla
/// ölçüyor — CLARIFY turunun Tuzak 1'i tam buydu.</para>
/// </summary>
[Collection(MedyaOrtamiKoleksiyonu.Name)]
public sealed class HazirKumeTests(MedyaOrtami ortam)
{
    [Fact(DisplayName = "0012a/AC-23: `Media`'nın okuma yüzeyi verilen kümeden **hazır olanların** kümesini döndürür")]
    public async Task Dondurur_HazirOlanlarinKumesini()
    {
        (Guid hazir, Guid bekleyen) = await IkiVarlikAsync();

        IReadOnlySet<Guid> sonuc = await SorgularAsync(
            sorgular => sorgular.ReadyAmongAsync([hazir, bekleyen], CancellationToken.None));

        Assert.Single(sonuc);
        Assert.Contains(hazir, sonuc);
        Assert.DoesNotContain(bekleyen, sonuc);
    }

    /// <summary>
    /// Tuzak 1'in kendisi: aynı küme için iki okuma **zıt** cevap veriyor ve A2'nin doğru
    /// cevabı "en az bir hazır var" olan. `AreAllReadyAsync` ile A2'yi ölçen bir uygulama
    /// bu aracı reddederdi.
    /// </summary>
    [Fact]
    public async Task Verir_ZitCevap_IkiOkuma()
    {
        (Guid hazir, Guid bekleyen) = await IkiVarlikAsync();
        Guid[] kume = [hazir, bekleyen];

        Assert.False(await SorgularAsync(
            sorgular => sorgular.AreAllReadyAsync(kume, CancellationToken.None)));

        Assert.NotEmpty(await SorgularAsync(
            sorgular => sorgular.ReadyAmongAsync(kume, CancellationToken.None)));
    }

    [Fact]
    public async Task Dondurur_BosKume_BosGirdide()
    {
        await ortam.SifirlaAsync();

        Assert.Empty(await SorgularAsync(
            sorgular => sorgular.ReadyAmongAsync([], CancellationToken.None)));
    }

    [Fact]
    public async Task Duser_BilinmeyenKimlik_Sessizce()
    {
        (Guid hazir, _) = await IkiVarlikAsync();
        Guid olmayan = Guid.CreateVersion7();

        IReadOnlySet<Guid> sonuc = await SorgularAsync(
            sorgular => sorgular.ReadyAmongAsync([hazir, olmayan], CancellationToken.None));

        Assert.Single(sonuc);
        Assert.DoesNotContain(olmayan, sonuc);
    }

    [Fact(DisplayName = "0012a/AC-24: Aracın medya sorgusu sıralı listeyi `Media`'nın durum, `alt` ve türev bilgisiyle birleştirir")]
    public async Task Okur_TopluDetay_TekCagrida()
    {
        (Guid hazir, Guid bekleyen) = await IkiVarlikAsync();

        IReadOnlyList<MediaAssetDto> sonuc = await SorgularAsync(
            sorgular => sorgular.FindManyAsync([hazir, bekleyen], CancellationToken.None));

        Assert.Equal(2, sonuc.Count);

        MediaAssetDto hazirDto = sonuc.Single(dto => dto.Id == hazir);
        MediaAssetDto bekleyenDto = sonuc.Single(dto => dto.Id == bekleyen);

        Assert.Equal(MediaProcessingStatus.Ready, hazirDto.Status);
        Assert.Equal(3, hazirDto.Derivatives.Count);

        // Henüz işlenmemiş varlık da DÖNER — ekran onu "işleniyor" hâliyle gösteriyor (D8)
        // ve listeden düşürmek onu görünmez kılardı.
        Assert.Equal(MediaProcessingStatus.Uploaded, bekleyenDto.Status);
        Assert.Empty(bekleyenDto.Derivatives);
    }

    /// <summary>
    /// Toplu okuma ile tek okuma AYNI DTO'yu üretmek zorunda: iki ayrı dönüştürme, biri
    /// alan eklendiğinde diğerinin sessizce eksik kalması demekti.
    /// </summary>
    [Fact]
    public async Task Uretir_AyniDto_TekVeTopluOkuma()
    {
        (Guid hazir, _) = await IkiVarlikAsync();

        MediaAssetDto? tek = await SorgularAsync(
            sorgular => sorgular.FindAsync(hazir, CancellationToken.None));
        IReadOnlyList<MediaAssetDto> toplu = await SorgularAsync(
            sorgular => sorgular.FindManyAsync([hazir], CancellationToken.None));

        Assert.NotNull(tek);
        Assert.Equal(tek.Status, toplu.Single().Status);
        Assert.Equal(
            [.. tek.Derivatives.Select(turev => turev.Url)],
            [.. toplu.Single().Derivatives.Select(turev => turev.Url)]);
    }

    /// <summary>Biri hazır, biri yükleme sırasını bekleyen iki varlık.</summary>
    private async Task<(Guid Hazir, Guid Bekleyen)> IkiVarlikAsync()
    {
        await ortam.SifirlaAsync();
        await ortam.FiligraniYerlestirAsync(Fixtures.GorselFixture.FiligranPng());

        Guid hazir = await YuklemeYardimcisi.YukleAsync(ortam);

        await TurKosAsync();

        // İkinci varlık turdan SONRA yükleniyor: tur bir partide hepsini alıyor.
        Guid bekleyen = await YuklemeYardimcisi.YukleAsync(ortam);

        return (hazir, bekleyen);
    }

    private async Task TurKosAsync()
    {
        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        MediaProcessingService servis = new(
            context,
            ortam.Store(ayarlar),
            new SkiaImageProcessor(),
            new MediaUrlResolver(ayarlar),
            new CarGallery.BuildingBlocks.Outbox.OutboxWriter<MediaDbContext>(
                context, new SabitSaat(new DateTimeOffset(2026, 10, 11, 9, 0, 0, TimeSpan.Zero))),
            ayarlar,
            new SabitSaat(new DateTimeOffset(2026, 10, 11, 9, 0, 0, TimeSpan.Zero)),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MediaProcessingService>.Instance);

        await servis.ProcessDueAsync(CancellationToken.None);
    }

    private async Task<T> SorgularAsync<T>(Func<MediaAssetQueries, Task<T>> is_)
    {
        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        return await is_(new MediaAssetQueries(context, new MediaUrlResolver(ayarlar)));
    }
}
