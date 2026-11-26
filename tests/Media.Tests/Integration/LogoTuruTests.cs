using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Media.Tests.Fixtures;
using CarGallery.Media.Tests.Infrastructure;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// `0026` D1 — logo türünün kalıcılığı ve işleme hattı, gerçek veritabanı ve gerçek depoyla.
/// Yükleme ucunun <c>kind</c> alanı (tek kapı, `422`) telden `Host.Tests/LogoYuklemeUcuTests`'te.
/// </summary>
[Collection(MedyaOrtamiKoleksiyonu.Name)]
public sealed class LogoTuruTests(MedyaOrtami ortam)
{
    private static readonly DateTimeOffset Simdi = new(2026, 11, 26, 9, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "0026/AC-9: Okuma yanıtı türü taşır: logo türünde `logo`, türsüz yüklemede `photo`.")]
    public async Task Tasir_Tur_OkumaYanitinda()
    {
        await ortam.SifirlaAsync();

        Guid logo = await YukleAsync(LogoIslemeTests.DuzRenkPng(800, 200), MediaAssetKind.Logo);
        Guid fotograf = await YukleAsync(GorselFixture.DikeyJpeg(), null);

        Assert.Equal(MediaAssetKind.Logo, (await SorguAsync(logo))!.Kind);
        Assert.Equal(MediaAssetKind.Photo, (await SorguAsync(fotograf))!.Kind);
    }

    [Fact(DisplayName = "0026/AC-1, AC-5: Logo türünün hazır varlığı tam olarak `logo-large` ve `logo-small` türevlerini kendi yollarında WebP olarak taşır; filigran kaynağı olmasa da işlenir.")]
    public async Task Isler_LogoTuru_FiligransizHat()
    {
        await ortam.SifirlaAsync();

        // Filigran YERLEŞTİRİLMEDİ: fotoğraf bu turda `watermark_unavailable` ile düşerdi.
        Guid id = await YukleAsync(LogoIslemeTests.SaydamKoseliPng(2000, 500), MediaAssetKind.Logo);

        await TurKosAsync();

        MediaAssetDto okunan = (await SorguAsync(id))!;
        Assert.Equal(MediaProcessingStatus.Ready, okunan.Status);
        Assert.Equal(["logo-large", "logo-small"], okunan.Derivatives.Select(turev => turev.Variant).Order(StringComparer.Ordinal));
        Assert.Equal((320, 80), okunan.Derivatives.Where(turev => turev.Variant == "logo-small").Select(turev => (turev.Width, turev.Height)).Single());

        foreach (MediaDerivativeDto turev in okunan.Derivatives)
        {
            byte[]? govde = await ortam.NesneOkuAsync($"public/assets/{id:D}/{turev.Variant}.webp");
            Assert.NotNull(govde);
            Assert.Equal(DetectedImageFormat.Webp, ImageFormatDetection.Detect(govde.AsSpan(0, 32)));
        }
    }

    [Fact(DisplayName = "0026/AC-71: Migration'dan önce yüklenmiş (tür kolonu yazılmadan eklenmiş) varlığın okuma yanıtında türü `photo`'dur.")]
    public async Task Doner_Photo_EskiVarlik()
    {
        await ortam.SifirlaAsync();
        Guid id = Guid.CreateVersion7();

        await using (MediaDbContext context = ortam.CreateContext())
        {
            // Kolon listesinde `kind` YOK: satır migration'ın varsayılanını alır.
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO media.assets (
                    id, status, content_type, byte_size, source_width, source_height,
                    original_object_key, alt_text, attempt_count, uploaded_at, updated_at)
                VALUES ({id}, 1, 'image/jpeg', 1024, 600, 400, {"private/originals/" + id.ToString("D") + ".jpg"}, {"{}"}::jsonb, 0, now(), now())
                """);
        }

        Assert.Equal(MediaAssetKind.Photo, (await SorguAsync(id))!.Kind);
    }

    private async Task<Guid> YukleAsync(byte[] govde, MediaAssetKind? kind)
    {
        MediaOptions ayarlar = ortam.Options();
        await using MediaDbContext context = ortam.CreateContext();

        UploadMediaAssetCommand komut = new(context, ortam.Store(ayarlar), new SkiaImageProcessor(), ayarlar, new SabitSaat(Simdi));
        Result<UploadMediaAssetResult> sonuc = kind is null
            ? await komut.ExecuteAsync(govde, CancellationToken.None)
            : await komut.ExecuteAsync(govde, CancellationToken.None, kind.Value);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : string.Empty);
        return sonuc.Value.Id;
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
            new OutboxWriter<MediaDbContext>(context, new SabitSaat(Simdi)),
            ayarlar,
            new SabitSaat(Simdi),
            NullLogger<MediaProcessingService>.Instance);

        await servis.ProcessDueAsync(CancellationToken.None);
    }

    private async Task<MediaAssetDto?> SorguAsync(Guid id)
    {
        MediaOptions ayarlar = ortam.Options();
        await using MediaDbContext context = ortam.CreateContext();

        return await new MediaAssetQueries(context, new MediaUrlResolver(ayarlar)).FindAsync(id, CancellationToken.None);
    }
}
