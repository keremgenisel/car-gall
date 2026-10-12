using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// **R14**/**D8** — eşzamanlı durum geçişi. `0007`'nin deseni: satır kilidi + tek
/// transaction, agregat **kilitten sonra** okunuyor.
///
/// <para><b>Kaybeden KURAL GEREĞİ reddediliyor</b> ve bu ayrım `0007`/AC-12 ile şart:
/// kural reddi KALICIDIR — kullanıcıya tekrar denemesi söylenirse sonsuza kadar denemesi
/// söylenmiş olur. `concurrency.lost` geçicidir ve yalnız kilit zaman aşımında döner.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class EszamanliGecisTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Simdi = new(2026, 10, 11, 18, 0, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0012b/AC-31: Aynı ilana eşzamanlı gelen iki durum geçişinden yalnız biri başarılı olur")]
    public async Task Basarili_Olur_YalnizBiri()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await YayindaAracAsync(varlik, medya);

        // İki istek AYNI ANDA satıldı işaretlemeye çalışıyor. Kilit onları serileştiriyor;
        // ikincisi kilidin altında YENİ durumu okuyor.
        Task<Result> birinci = UygulaAsync(aracId, ListingAction.MarkSold, medya);
        Task<Result> ikinci = UygulaAsync(aracId, ListingAction.MarkSold, medya);

        Result[] sonuclar = await Task.WhenAll(birinci, ikinci);

        Assert.Single(sonuclar, sonuc => sonuc.IsSuccess);
        Assert.Single(sonuclar, sonuc => sonuc.IsFailure);
    }

    [Fact(DisplayName = "0012b/AC-32: Kaybeden **kural gereği** reddedilir ve `listing.invalid_status_transition` alır; `concurrency.lost` yalnız kilit zaman aşımında döner")]
    public async Task Alir_KuralReddi_Kaybeden()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await YayindaAracAsync(varlik, medya);

        Result[] sonuclar = await Task.WhenAll(
            UygulaAsync(aracId, ListingAction.MarkSold, medya),
            UygulaAsync(aracId, ListingAction.MarkSold, medya));

        Result kaybeden = sonuclar.Single(sonuc => sonuc.IsFailure);

        Assert.Equal(Listing.InvalidTransition, kaybeden.Error.Code);
        Assert.NotEqual(ConcurrencyErrors.Lost, kaybeden.Error.Code);
    }

    private async Task<Guid> YayindaAracAsync(Guid varlik, FakeMediaAssetQueries medya)
    {
        Vehicle arac = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"ESZAMAN{Interlocked.Increment(ref sayac):D10}")
            .Build();

        arac.AttachMediaAsset(varlik);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            context.Vehicles.Add(arac);
            context.Listings.Add(Listing.CreateDraft(arac.Id, showcaseSortWeight: 0));
            await context.SaveChangesAsync();
        }

        Result yayin = await UygulaAsync(arac.Id.Value, ListingAction.Publish, medya);

        Assert.True(yayin.IsSuccess);

        return arac.Id.Value;
    }

    private async Task<Result> UygulaAsync(Guid aracId, ListingAction eylem, FakeMediaAssetQueries medya)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(Simdi);

        ListingTransitionCommands komut = new(
            context,
            KatalogYazicilari.Birim(context, saat),
            KatalogYazicilari.Yansitma(context, medya),
            medya,
            new FakeModelNameResolver(),
            // Kilit süresi UZATILDI: üretim varsayılanı üç saniye ve iki isteğin
            // serileşmesi test makinesinde ondan uzun sürebilir. Uzatmak testin ölçtüğü
            // şeyi değiştirmiyor — ölçülen şey KURAL reddi, mekanizma kaybı değil.
            new InvariantGuardOptions<VehicleCatalogDbContext>(TimeSpan.FromSeconds(20)),
            saat);

        return await komut.ExecuteAsync(aracId, eylem, CancellationToken.None);
    }
}
