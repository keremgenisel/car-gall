using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012b` — durum geçişlerinin komut katmanı, GERÇEK PostgreSQL üzerinde.
///
/// <para>Neden entegrasyon: geçiş kararı agregatta ve orada ayrıca sınanıyor
/// (`GecisTablosuTests`); burada ölçülen şey **kablo** — kilit, araç ve ilanın birlikte
/// yazılması, arşiv bayrağının aynı işleme girmesi ve şase indeksinin gerçekten serbest
/// kalması. Hiçbiri bellek içi bir testte görünmez.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class GecisKomutlariTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset Kayit = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Gecis = new(2026, 10, 11, 14, 30, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0012b/AC-1: Oturum açmış personel, koşulları sağlanan bir taslak ilanı yayına alır ve ilan `Yayında` durumuna geçer")]
    public async Task Gecer_Yayinda_KosullarSaglandiginda()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        Result sonuc = await UygulaAsync(aracId, ListingAction.Publish, medya);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Published, await DurumAsync(aracId));
    }

    [Fact(DisplayName = "0012b/AC-2: Hazır medya varlığı olmayan araç yayına alınamaz; yanıt `vehicle.cannot_publish.no_media` taşır")]
    public async Task Alinamaz_Yayina_HazirMedyaYoksa()
    {
        FakeMediaAssetQueries medya = new();
        Guid isleniyor = medya.Ekle(MediaProcessingStatus.Processing);
        Guid aracId = await AracEkleAsync(medya: isleniyor);

        Result sonuc = await UygulaAsync(aracId, ListingAction.Publish, medya);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(PublishErrors.CannotPublish, sonuc.Error.Code);
        Assert.Equal(PublishErrors.NoMedia, sonuc.Error.Parameters!["reasons"]);
        Assert.Equal(ListingStatus.Draft, await DurumAsync(aracId));
    }

    [Fact(DisplayName = "0012b/AC-6: Lokasyon ataması bekleyen araç yayına alınamaz; yanıt `vehicle.cannot_publish.branch_unassigned` taşır")]
    public async Task Alinamaz_Yayina_LokasyonAtamasiBekliyorsa()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik, lokasyonBekliyor: true);

        Result sonuc = await UygulaAsync(aracId, ListingAction.Publish, medya);

        Assert.True(sonuc.IsFailure);
        Assert.Contains(PublishErrors.BranchUnassigned, sonuc.Error.Parameters!["reasons"], StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0012b/AC-30: Şube atandığında bayrak temizlenir ve araç yeniden yayına alınabilir")]
    public async Task Alinabilir_Yayina_SubeAtandiktanSonra()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik, lokasyonBekliyor: true);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            Vehicle arac = await context.Vehicles.SingleAsync(item => item.Id == new VehicleId(aracId));
            arac.AssignBranch(Guid.CreateVersion7());
            await context.SaveChangesAsync();
        }

        Result sonuc = await UygulaAsync(aracId, ListingAction.Publish, medya);

        Assert.True(sonuc.IsSuccess);
    }

    [Fact(DisplayName = "0012b/AC-7: Adresi üretilemeyen ilan yayına alınamaz; yanıt `vehicle.cannot_publish.slug_unavailable` taşır")]
    public async Task Alinamaz_Yayina_AdresUretilemiyorsa()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        // Kanonik ad Latin olmak ZORUNDA (`docs/architecture.md` § 3); Arapça bir ad
        // yalnız yıl ve sonekten oluşan anlamsız bir adres üretirdi ve `ListingSlug`
        // bunu `invalid_source` ile reddediyor.
        Result sonuc = await UygulaAsync(aracId, ListingAction.Publish, medya, kanonikAd: "تويوتا");

        Assert.True(sonuc.IsFailure);
        Assert.Contains(PublishErrors.SlugUnavailable, sonuc.Error.Parameters!["reasons"], StringComparison.Ordinal);
        Assert.Equal(ListingStatus.Draft, await DurumAsync(aracId));
    }

    [Fact(DisplayName = "0012b/AC-20: Durum geçişi aracın son güncellenme zamanını ilerletir")]
    public async Task Ilerletir_SonGuncellenme_Gecis()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        // D20 — listenin tek satırı İKİ agregattan besleniyor; yalnız `Listing`'e
        // yazsaydık personel bir aracı yayına alır ve araç listede yerinden kıpırdamazdı.
        await UygulaAsync(aracId, ListingAction.Publish, medya);

        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle arac = await context.Vehicles.AsNoTracking()
            .SingleAsync(item => item.Id == new VehicleId(aracId));

        Assert.Equal(Gecis, arac.UpdatedAt);
    }

    [Fact(DisplayName = "0012b/AC-23: Arşivleme aracın arşiv bayrağını **aynı işlemde** yazar")]
    public async Task Yazar_ArsivBayragini_AyniIslemde()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya);

        Result sonuc = await UygulaAsync(aracId, ListingAction.Archive, medya);

        Assert.True(sonuc.IsSuccess);

        await using VehicleCatalogDbContext context = database.CreateContext();
        Vehicle arac = await context.Vehicles.AsNoTracking()
            .SingleAsync(item => item.Id == new VehicleId(aracId));

        // ADR 0006 bunu `0012`'nin borcu olarak yazmıştı: bayrak ayrı bir işlemde
        // yazılırsa bayatlar ve tekillik indeksi yanlış kararı SESSİZCE verir.
        Assert.True(arac.IsArchived);
        Assert.Equal(ListingStatus.Archived, await DurumAsync(aracId));
    }

    [Fact(DisplayName = "0012b/AC-24: Arşivlenen aracın şasesiyle yeni bir araç kaydedilebilir")]
    public async Task Kaydedilebilir_AyniSase_ArsivdenSonra()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        string sase = $"ARSIV{Interlocked.Increment(ref sayac):D12}";
        Guid aracId = await AracEkleAsync(medya: varlik, vin: sase);

        await UygulaAsync(aracId, ListingAction.Publish, medya);
        await UygulaAsync(aracId, ListingAction.Archive, medya);

        // A5'in muafiyeti: kısmi tekil indeks `NOT is_archived` filtreli ve bu ancak
        // GERÇEK PostgreSQL üzerinde görünür.
        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(new VehicleBuilder().WithoutMedia().WithVin(sase).Build());

        await context.SaveChangesAsync();
    }

    [Fact(DisplayName = "0012b/AC-25: Arşivlenmemiş bir aracın şasesi yeniden kaydedilemez")]
    public async Task Kaydedilemez_AyniSase_ArsivlenmemisAracta()
    {
        string sase = $"TEKIL{Interlocked.Increment(ref sayac):D12}";
        await AracEkleAsync(vin: sase);

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(new VehicleBuilder().WithoutMedia().WithVin(sase).Build());

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact(DisplayName = "0012b/AC-19: A3'te yazılı olmayan her geçiş `listing.invalid_status_transition` ile reddedilir")]
    public async Task Reddedilir_GecersizGecis_KomutKatmanindan()
    {
        FakeMediaAssetQueries medya = new();
        Guid aracId = await AracEkleAsync();

        // Taslak bir ilan rezerve EDİLEMEZ; kod A2'nin reddinden FARKLI (D26: `409` vs
        // `422`) ve panelin tepkisi de farklı — satırı tazelemek mi, eksikleri göstermek mi.
        Result sonuc = await UygulaAsync(aracId, ListingAction.Reserve, medya);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Listing.InvalidTransition, sonuc.Error.Code);
    }

    [Fact]
    public async Task Reddedilir_VarOlmayanArac()
    {
        FakeMediaAssetQueries medya = new();

        Result sonuc = await UygulaAsync(Guid.CreateVersion7(), ListingAction.Publish, medya);

        Assert.True(sonuc.IsFailure);
        Assert.Equal("vehicle.not_found", sonuc.Error.Code);
    }

    private async Task<Guid> AracEkleAsync(
        Guid? medya = null,
        bool lokasyonBekliyor = false,
        string? vin = null)
    {
        Vehicle arac = new VehicleBuilder()
            .WithoutMedia()
            .WithVin(vin ?? $"GECIS{Interlocked.Increment(ref sayac):D12}")
            .CreatedAt(Kayit)
            .Build();

        if (medya is not null)
        {
            arac.AttachMediaAsset(medya.Value);
        }

        if (lokasyonBekliyor)
        {
            arac.MarkAwaitingBranchAssignment();
        }

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(arac);
        context.Listings.Add(Listing.CreateDraft(arac.Id, showcaseSortWeight: 0));
        await context.SaveChangesAsync();

        return arac.Id.Value;
    }

    private async Task<Result> UygulaAsync(
        Guid aracId,
        ListingAction eylem,
        FakeMediaAssetQueries medya,
        string kanonikAd = "Toyota Corolla")
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(Gecis);

        ListingTransitionCommands komut = new(
            context,
            KatalogYazicilari.Birim(context, saat),
            KatalogYazicilari.Yansitma(context, medya),
            medya,
            new FakeModelNameResolver(kanonikAd),
            InvariantGuardOptions.Default,
            saat);

        return await komut.ExecuteAsync(aracId, eylem, CancellationToken.None);
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
