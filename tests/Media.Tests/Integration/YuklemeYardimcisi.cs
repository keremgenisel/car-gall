using CarGallery.BuildingBlocks;
using CarGallery.Media.Tests.Fixtures;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Infrastructure;

namespace CarGallery.Media.Tests.Integration;

/// <summary>
/// Tek bir varlık yükler. Birden çok test sınıfı aynı kurulumu istiyor ve her birinde
/// kopyalamak, kurulumun bir yerde değişip diğerinde kalmasına davet olurdu.
/// </summary>
internal static class YuklemeYardimcisi
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

    internal static async Task<Guid> YukleAsync(MedyaOrtami ortam, byte[]? govde = null)
    {
        ArgumentNullException.ThrowIfNull(ortam);

        MediaOptions ayarlar = ortam.Options();

        await using MediaDbContext context = ortam.CreateContext();

        UploadMediaAssetCommand komut = new(
            context,
            ortam.Store(ayarlar),
            new SkiaImageProcessor(),
            ayarlar,
            new SabitSaat(Simdi));

        Result<UploadMediaAssetResult> sonuc = await komut.ExecuteAsync(
            govde ?? GorselFixture.DikeyJpeg(), CancellationToken.None);

        if (sonuc.IsFailure)
        {
            throw new InvalidOperationException($"Test yüklemesi başarısız: {sonuc.Error.Code}");
        }

        return sonuc.Value.Id;
    }
}
