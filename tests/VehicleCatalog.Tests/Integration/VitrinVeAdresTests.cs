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

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012b` — vitrin yansıtması (**D12**) ve ilanın adresi (**D13**), gerçek PostgreSQL
/// üzerinde.
///
/// <para>İkisi aynı dosyada çünkü aynı olgunun iki yüzü: adres yazma modelinde yaşıyor ve
/// yansıtma onu **kopyalıyor**. Bu spec'e kadar okuma modeline yazan tek üretim yolu
/// geliştirme seed'iydi — yayına alma düğmesi hiçbir ziyaretçi sayfasını
/// değiştirmezdi.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class VitrinVeAdresTests(ShowcaseDatabase database)
{
    private static readonly DateTimeOffset IlkYayin = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset IkinciYayin = new(2026, 11, 20, 10, 0, 0, TimeSpan.Zero);

    private static int sayac;

    [Fact(DisplayName = "0012b/AC-41: Yayına alma vitrin okuma modeline satır yazar ve ilan ziyaretçiye görünür olur")]
    public async Task Yazar_VitrinSatiri_YayinaAlmada()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya, IlkYayin);

        ShowcaseListing? satir = await SatirAsync(aracId);

        Assert.NotNull(satir);
        Assert.False(satir.IsReserved);
        Assert.Equal(IlkYayin, satir.PublishedAt);

        // D40 — şube adı locale → ad HARİTASI. Tek dille yazılsaydı araç diğer iki dilde
        // şubesiz görünürdü ve bir sonraki `BranchDetailsChanged`'e kadar öyle kalırdı.
        Assert.Equal(["ar", "en", "tr"], satir.BranchNames.Keys.Order());
    }

    [Fact(DisplayName = "0012b/AC-42: Görünmez bir duruma geçen ilanın vitrin satırı silinir")]
    public async Task Siler_VitrinSatirini_GorunmezDuruma_Gecince()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya, IlkYayin);
        Assert.NotNull(await SatirAsync(aracId));

        await UygulaAsync(aracId, ListingAction.MarkSold, medya, IlkYayin);

        // Okuma modeli YALNIZ görünür ilanları taşıyor: `Satıldı` satır olarak hiç
        // bulunmaz ve AC-3'ün süzgeci sorgunun `where`'inde değil, modelin kendisinde.
        Assert.Null(await SatirAsync(aracId));
    }

    [Fact(DisplayName = "0012b/AC-42: Görünmez bir duruma geçen ilanın vitrin satırı silinir")]
    public async Task Kalir_VitrinSatiri_RezerveEdilince()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya, IlkYayin);
        await UygulaAsync(aracId, ListingAction.Reserve, medya, IlkYayin);

        ShowcaseListing? satir = await SatirAsync(aracId);

        // R6 (domain) — rezerve ilan vitrinde ROZETİYLE kalır; satır düşmez.
        Assert.NotNull(satir);
        Assert.True(satir.IsReserved);
    }

    [Fact(DisplayName = "0012b/AC-43: Araç düzenlemesi vitrin satırını **aynı işlemde** günceller")]
    public async Task Gunceller_VitrinSatirini_AracDuzenlemesi()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya, IlkYayin);

        ShowcaseListing once = (await SatirAsync(aracId))!;

        Assert.Equal(125_000, once.MileageKilometers);
        Assert.Equal("sedan", once.BodyTypeSlug);

        // Düzenleme komutunun tamamını kurmak yerine agregatı değiştirip YANSITMAYI
        // çağırıyoruz: ölçülen şey yansıtmanın güncelleme yolu — satır zaten varken
        // `Add` çağıran bir uygulama burada çakışırdı.
        //
        // **Aracın YAZILABİLİR her alanı değiştiriliyor** ve bu bir dış bulgunun bedeli:
        // ilk hâlinde test yalnız kilometreyi değiştiriyordu ve o, kopyalanan kolonlardan
        // biriydi — beş kolonun atlandığı kapı sessiz kaldı. Tek elemanlı bir küme bir
        // kümeyi ölçmez.
        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            Vehicle arac = await context.Vehicles.SingleAsync(item => item.Id == new VehicleId(aracId));

            arac.ChangeCoreDetails(
                VehicleSpecification.Create(
                    BodyTypeSlug.Create("suv").Value,
                    "gasoline",
                    "manual",
                    "all-wheel-drive",
                    "premium").Value,
                LocalizedText.From([new KeyValuePair<Locale, string>(Locale.Create("tr").Value, "Siyah")]),
                2022,
                61_250,
                IkinciYayin);

            arac.ChangePrice(ListingPrice.OnRequest(), IkinciYayin);

            await KatalogYazicilari.Yansitma(context, medya).ProjectAsync(arac, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        ShowcaseListing sonra = (await SatirAsync(aracId))!;

        Assert.Equal(61_250, sonra.MileageKilometers);
        Assert.Equal(2022, sonra.ModelYear);
        Assert.Equal("Siyah", sonra.ColorNames["tr"]);
        Assert.True(sonra.PriceOnRequest);
        Assert.Null(sonra.PriceAmount);

        // Ziyaretçinin gövde tipi FİLTRESİ bu kolonu okuyor: atlanırsa "sedan" diye
        // kaydedilip "suv" olarak düzeltilen araç sedan filtresinde kalırdı.
        Assert.Equal("suv", sonra.BodyTypeSlug);
        Assert.Equal("gasoline", sonra.FuelTypeSlug);
        Assert.Equal("manual", sonra.TransmissionTypeSlug);
        Assert.Equal("all-wheel-drive", sonra.DrivetrainTypeSlug);
        Assert.Equal("premium", sonra.TrimLevelSlug);

        // D13 — adres DEĞİŞMEZ ve kopyalanmıyor.
        Assert.Equal(once.Slug, sonra.Slug);
    }

    /// <summary>
    /// Kolon kümesinin kendisi bir kapı: `ShowcaseListing`'e yazılabilir bir alan
    /// eklendiğinde yansıtma onu kendiliğinden kopyalamalı. Elle sayılan bir liste
    /// altıncı kolonda aynı hatayı tekrarlardı — dış bulgu tam olarak buydu.
    /// </summary>
    [Fact]
    public void Kopyalar_YansitmaGuncellemesi_HerYazilabilirKolonu()
    {
        IReadOnlyList<string> kopyalanan =
        [
            .. ShowcaseProjectionWriter.KopyalanacakOzellikler.Select(ozellik => ozellik.Name).Order()
        ];

        // Dışarıda kalan ÜÇ ad: satırın anahtarı, aracın kimliği ve ilanın adresi (D13).
        // Dördüncü bir ad eklenirse o kolon sessizce kopyalanmaz olur.
        Assert.Equal(
            [nameof(ShowcaseListing.ListingId), nameof(ShowcaseListing.Slug), nameof(ShowcaseListing.VehicleId)],
            ShowcaseProjectionWriter.Degismezler.Order());

        // Ve atlanan beşi kopyalanıyor.
        Assert.Contains(nameof(ShowcaseListing.BodyTypeSlug), kopyalanan);
        Assert.Contains(nameof(ShowcaseListing.FuelTypeSlug), kopyalanan);
        Assert.Contains(nameof(ShowcaseListing.TransmissionTypeSlug), kopyalanan);
        Assert.Contains(nameof(ShowcaseListing.DrivetrainTypeSlug), kopyalanan);
        Assert.Contains(nameof(ShowcaseListing.TrimLevelSlug), kopyalanan);
        Assert.DoesNotContain(nameof(ShowcaseListing.Slug), kopyalanan);
    }

    [Fact(DisplayName = "0012b/AC-44: Aracın medya sırası değiştiğinde vitrin satırının kapağı güncellenir")]
    public async Task Gunceller_Kapagi_MedyaSirasiDegisince()
    {
        FakeMediaAssetQueries medya = new();
        Guid birinci = medya.Ekle(MediaProcessingStatus.Ready);
        Guid ikinci = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: birinci, ikinciMedya: ikinci);

        await UygulaAsync(aracId, ListingAction.Publish, medya, IlkYayin);

        Assert.Equal(birinci, (await SatirAsync(aracId))!.CoverAssetId);

        // D41 — kapak sıralı listenin İLK elemanı; sıra değişince vitrindeki kapak
        // değişiyor. R15'in saydığı yazma yolları arasında yoktu.
        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            UpdateVehicleMediaCommand komut = new(
                context,
                medya,
                KatalogYazicilari.Yansitma(context, medya),
                new FixedClock(IkinciYayin));

            Result sonuc = await komut.ExecuteAsync(aracId, [ikinci, birinci], CancellationToken.None);

            Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : string.Empty);
        }

        Assert.Equal(ikinci, (await SatirAsync(aracId))!.CoverAssetId);
    }

    [Fact(DisplayName = "0012b/AC-46: Adres **ilk yayına alma** anında üretilir ve yazma modelinde saklanır")]
    public async Task Uretilir_Adres_IlkYayinda()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        Assert.Null(await AdresAsync(aracId));

        await UygulaAsync(aracId, ListingAction.Publish, medya, IlkYayin);

        string? adres = await AdresAsync(aracId);

        Assert.NotNull(adres);
        Assert.StartsWith("toyota-corolla-2019-", adres, StringComparison.Ordinal);

        // Yansıtma onu KOPYALIYOR; iki yerde iki farklı adres olamaz.
        Assert.Equal(adres, (await SatirAsync(aracId))!.Slug);
    }

    [Fact(DisplayName = "0012b/AC-47: Yayından kaldırılıp yeniden yayınlanan ilan **aynı adrese** döner")]
    public async Task Doner_AyniAdrese_YenidenYayinda()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya, IlkYayin);
        string? ilkAdres = await AdresAsync(aracId);

        await UygulaAsync(aracId, ListingAction.Unpublish, medya, IlkYayin);

        // Satır düştü ama ADRES yazma modelinde DURUYOR — değişmezlik silinen bir satırda
        // yaşayamaz; D13'ün bütün gerekçesi buydu.
        Assert.Null(await SatirAsync(aracId));
        Assert.Equal(ilkAdres, await AdresAsync(aracId));

        await UygulaAsync(aracId, ListingAction.Publish, medya, IkinciYayin);

        Assert.Equal(ilkAdres, await AdresAsync(aracId));
        Assert.Equal(ilkAdres, (await SatirAsync(aracId))!.Slug);
    }

    [Fact(DisplayName = "0012b/AC-48: Yeniden yayında ilk yayın zamanı **donar**; ilan vitrinde yerini değiştirmez")]
    public async Task Donar_IlkYayinZamani_YenidenYayinda()
    {
        FakeMediaAssetQueries medya = new();
        Guid varlik = medya.Ekle(MediaProcessingStatus.Ready);
        Guid aracId = await AracEkleAsync(medya: varlik);

        await UygulaAsync(aracId, ListingAction.Publish, medya, IlkYayin);
        await UygulaAsync(aracId, ListingAction.Unpublish, medya, IlkYayin);
        await UygulaAsync(aracId, ListingAction.Publish, medya, IkinciYayin);

        ShowcaseListing satir = (await SatirAsync(aracId))!;

        // D31 — aksi hâlde yeniden yayınlanan araç vitrin sıralamasının başına ZIPLAR ve
        // "yeni gelen araç" yanlış sinyalini üretir; sıralama `published_at`'e dayanıyor.
        Assert.Equal(IlkYayin, satir.PublishedAt);
    }

    [Fact(DisplayName = "0012b/AC-49: Hiç yayınlanmamış bir ilanın adresi yoktur")]
    public async Task Yoktur_Adres_HicYayinlanmamisIlanda()
    {
        Guid aracId = await AracEkleAsync();

        Assert.Null(await AdresAsync(aracId));
        Assert.Null(await SatirAsync(aracId));
    }

    private async Task<Guid> AracEkleAsync(Guid? medya = null, Guid? ikinciMedya = null)
    {
        Vehicle arac = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"VITRIN{Interlocked.Increment(ref sayac):D11}")
            .Build();

        if (medya is not null)
        {
            arac.AttachMediaAsset(medya.Value);
        }

        if (ikinciMedya is not null)
        {
            arac.AttachMediaAsset(ikinciMedya.Value);
        }

        await using VehicleCatalogDbContext context = database.CreateContext();
        context.Vehicles.Add(arac);
        context.Listings.Add(Listing.CreateDraft(arac.Id, showcaseSortWeight: 0));
        await context.SaveChangesAsync();

        return arac.Id.Value;
    }

    private async Task UygulaAsync(
        Guid aracId,
        ListingAction eylem,
        FakeMediaAssetQueries medya,
        DateTimeOffset an)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(an);

        ListingTransitionCommands komut = new(
            context,
            KatalogYazicilari.Birim(context, saat),
            KatalogYazicilari.Yansitma(context, medya),
            medya,
            new FakeModelNameResolver(),
            InvariantGuardOptions.Default,
            saat);

        Result sonuc = await komut.ExecuteAsync(aracId, eylem, CancellationToken.None);

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : string.Empty);
    }

    private async Task<ShowcaseListing?> SatirAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.ShowcaseListings.AsNoTracking()
            .FirstOrDefaultAsync(row => row.VehicleId == aracId);
    }

    private async Task<string?> AdresAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        Listing listing = await context.Listings.AsNoTracking()
            .SingleAsync(item => item.VehicleId == new VehicleId(aracId));

        return listing.Slug?.Value;
    }
}
