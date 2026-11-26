using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Development;
using CarGallery.Modules.VehicleCatalog.Tests.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// Triyaj BULGU-3 — tohum medyasının kaydı ve kimlik biçimi. Sarmalayıcı yanlış kaydedilirse
/// (ya da hiç kaydedilmezse) tohum galerisi yeniden yansıtmada sessizce kaybolur; o yüzden
/// kaydın KENDİSİ ölçülür ("yazılmış ama kablolanmamış" dersi).
/// </summary>
public sealed class TohumMedyasiKaydiTests
{
    [Fact]
    public async Task Sarar_KayitliSorguyu_TohumKimlikleriniKendisiCevaplar()
    {
        FakeMediaAssetQueries gercek = new();
        Guid gercekVarlik = gercek.EkleTurevli(MediaProcessingStatus.Ready);
        Guid tohum = SeedMediaAssets.Id(7, 2, MediaProcessingStatus.Ready, 5, 3, 4);

        ServiceCollection services = new();
        services.AddScoped<IMediaAssetQueries>(_ => gercek);
        services.AddVehicleCatalogSeedMedia();

        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IMediaAssetQueries sorgu = scope.ServiceProvider.GetRequiredService<IMediaAssetQueries>();

        Assert.IsType<SeedAwareMediaAssetQueries>(sorgu);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IMediaAssetQueries));

        IReadOnlyList<MediaAssetDto> bulunan = await sorgu.FindManyAsync([tohum, gercekVarlik], CancellationToken.None);
        Assert.Equal([tohum, gercekVarlik], bulunan.Select(varlik => varlik.Id));
        Assert.Equal(1, gercek.TopluOkumaSayisi); // gerçek sorguya yalnız gerçek kimlik gitti

        IReadOnlySet<Guid> hazir = await sorgu.ReadyAmongAsync([tohum, gercekVarlik], CancellationToken.None);
        Assert.True(hazir.SetEquals([tohum, gercekVarlik]));
    }

    [Fact]
    public void TarifEder_TohumKimligini_GercekKimligiTanimaz()
    {
        MediaAssetDto dikey = SeedMediaAssets.Describe(SeedMediaAssets.Id(0, 1, MediaProcessingStatus.Ready, 1, 3, 4))!;
        Assert.Equal(MediaProcessingStatus.Ready, dikey.Status);
        Assert.Contains(dikey.Derivatives, turev => turev is { Variant: "display", Width: 600, Height: 800 });

        Assert.Equal(MediaProcessingStatus.Failed, SeedMediaAssets.Describe(SeedMediaAssets.Id(4, 1, MediaProcessingStatus.Failed, 0, 4, 3))!.Status);
        Assert.Empty(SeedMediaAssets.Describe(SeedMediaAssets.Id(4, 0, MediaProcessingStatus.Processing, 0, 4, 3))!.Derivatives);

        Assert.Null(SeedMediaAssets.Describe(Guid.CreateVersion7()));
        Assert.NotEqual(SeedMediaAssets.Id(0, 1, MediaProcessingStatus.Ready, 1, 3, 4), SeedMediaAssets.Id(1, 1, MediaProcessingStatus.Ready, 1, 3, 4));
    }

    [Fact(DisplayName = "0026/AC-59: Perf tohumunun logosu tohum medyasından çözülür: hazır, `logo` türünde, `logo-small` 320×80 ve `logo-large` 1200×300; fotoğraf kimlikleriyle çakışmaz.")]
    public void Cozulur_TohumLogosu()
    {
        MediaAssetDto logo = SeedMediaAssets.Describe(SeedMediaAssets.LogoId)!;

        Assert.Equal((MediaProcessingStatus.Ready, MediaAssetKind.Logo), (logo.Status, logo.Kind));
        Assert.Equal(
            [("logo-large", 1200, 300), ("logo-small", 320, 80)],
            logo.Derivatives.Select(turev => (turev.Variant, turev.Width, turev.Height)));
        Assert.All(logo.Derivatives, turev => Assert.StartsWith("/seed-media/frame-", turev.Url, StringComparison.Ordinal));
        Assert.Equal(MediaAssetKind.Photo, SeedMediaAssets.Describe(SeedMediaAssets.Id(7, 2, MediaProcessingStatus.Ready, 5, 3, 4))!.Kind);
    }
}
