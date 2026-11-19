using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0023` — satışı geri almanın komut katmanı, GERÇEK PostgreSQL üzerinde: kilit, şube
/// okuması (**D12**), yayın koşullarının yeniden denetimi (**D13**), kayıt (**D7**), duyuru
/// (**D8**) ve vitrin yansıtması aynı işlemde.
///
/// <para>Geçiş kararı agregatta ve orada ayrıca sınanıyor (`Domain/SatisGeriAlmaTests`);
/// burada ölçülen şey kablo. Kayıt ve outbox TABLOSU sayılıyor, kod okunmuyor —
/// `0006`'nın ikinci kaçan hatasının dersi.</para>
/// </summary>
[Collection(ShowcaseDatabaseCollection.Name)]
public sealed class SatisGeriAlmaTests(ShowcaseDatabase database)
{
    private const string Gerekce = "Yanlış araca tıklandı, satış başka araçtaydı.";

    private static readonly DateTimeOffset Yayin = new(2026, 11, 1, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Satis = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset GeriAlma = new(2026, 11, 18, 14, 0, 0, TimeSpan.Zero);

    private static readonly Guid Yonetici = Guid.Parse("0192c000-0000-7000-8000-00000000a001");

    private static int sayac;

    [Fact(DisplayName = "0023/AC-1: Yayındayken satıldı işaretlenen ilanın satışı Yönetici tarafından geri alındığında ilan **Yayında** olur ve vitrinde yeniden görünür")]
    public async Task Doner_YayindaVeVitrinde_YayindanSatildiysa()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        Assert.Null(await VitrinSatiriAsync(arac.Id));

        Result sonuc = await GeriAlAsync(arac, "Published");

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Published, (await IlanAsync(arac.Id)).Status);

        ShowcaseListing? satir = await VitrinSatiriAsync(arac.Id);
        Assert.NotNull(satir);
        Assert.False(satir.IsReserved);
    }

    [Fact(DisplayName = "0023/AC-2: Rezervedeyken satıldı işaretlenen ilanın satışı geri alındığında ilan **Rezerve** olur ve vitrinde rezerve olarak görünür")]
    public async Task Doner_RezerveVeVitrindeRozetli_RezervedenSatildiysa()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: true);

        Result sonuc = await GeriAlAsync(arac, "Reserved");

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Reserved, (await IlanAsync(arac.Id)).Status);

        ShowcaseListing? satir = await VitrinSatiriAsync(arac.Id);
        Assert.NotNull(satir);
        Assert.True(satir.IsReserved);
    }

    /// <summary>
    /// **D3** — bu karttan önce satılmış ilanın kolonu migration'la NULL kaldı. Kolon burada
    /// SQL ile boşaltılıyor: agregat artık her satışta yazıyor ve "eski satış" ancak
    /// verinin kendisiyle kurulabilir.
    /// </summary>
    [Fact(DisplayName = "0023/AC-3: Bu kart gelmeden önce satıldı işaretlenmiş (önceki durumu kayıtlı olmayan) ilanın satışı geri alındığında ilan **Yayında** olur")]
    public async Task Doner_Yayinda_OncekiDurumBilinmiyorsa()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: true);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE vehicle_catalog.listings SET status_before_sale = NULL WHERE vehicle_id = {arac.Id}");
        }

        Assert.Equal("Published", (await OnizlemeAsync(arac)).TargetStatus);

        Result sonuc = await GeriAlAsync(arac, "Published");

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Published, (await IlanAsync(arac.Id)).Status);
    }

    [Fact(DisplayName = "0023/AC-4: Satıştan sonra şubesi kalıcı kapatılan aracın ilanı geri alındığında ilan **Taslak** olur, aracın \"şube ataması bekliyor\" bayrağı yazılır ve araç vitrinde görünmez (D12)")]
    public async Task Doner_TaslagaVeBayraklanir_SubesiKapandiysa()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);
        arac.Sube.Kapat();

        // A8 satılmış ilanı atlıyor: bayrak HİÇ yazılmamış. Yalnız bayrağa bakmak aracı
        // kapalı şubeyle vitrine çıkarırdı.
        Assert.False((await AracAsync(arac.Id)).AwaitingBranchAssignment);

        Result sonuc = await GeriAlAsync(arac, "Draft");

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Draft, (await IlanAsync(arac.Id)).Status);
        Assert.True((await AracAsync(arac.Id)).AwaitingBranchAssignment);
        Assert.Null(await VitrinSatiriAsync(arac.Id));
    }

    [Fact(DisplayName = "0023/AC-5: Satıştan sonra bütün fotoğrafları kaldırılan aracın ilanı geri alındığında ilan **Taslak** olur ve vitrinde görünmez (D13)")]
    public async Task Doner_Taslaga_FotograflariKaldirildiysa()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            Vehicle kayit = await context.Vehicles.SingleAsync(item => item.Id == new VehicleId(arac.Id));
            kayit.ReplaceMediaAssets([]);
            await context.SaveChangesAsync();
        }

        SaleReversalOverview onizleme = await OnizlemeAsync(arac);

        Assert.Equal("Draft", onizleme.TargetStatus);
        Assert.Contains(PublishErrors.NoMedia, onizleme.DraftReasons);

        Result sonuc = await GeriAlAsync(arac, "Draft");

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(ListingStatus.Draft, (await IlanAsync(arac.Id)).Status);
        Assert.False((await AracAsync(arac.Id)).AwaitingBranchAssignment);
        Assert.Null(await VitrinSatiriAsync(arac.Id));
    }

    [Theory(DisplayName = "0023/AC-6: Geri alınan ilan, onay penceresinde gösterilen duruma döner")]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Doner_PenceredekiDuruma(bool rezerveden, bool subeKapali)
    {
        Arac arac = await SatilmisAracAsync(rezerveden);

        if (subeKapali)
        {
            arac.Sube.Kapat();
        }

        string? pencere = (await OnizlemeAsync(arac)).TargetStatus;

        Result sonuc = await GeriAlAsync(arac, pencere);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(pencere, (await IlanAsync(arac.Id)).Status.ToString());
    }

    [Fact(DisplayName = "0023/AC-7: Onay penceresi açıldıktan sonra aracın şubesi kalıcı kapatılırsa, pencerenin gösterdiği \"Yayında\" hedefiyle gönderilen geri alma `409` ile reddedilir, ilan Satıldı kalır ve kayıt yazılmaz (D16)")]
    public async Task Reddedilir_PencereAcikkenSubeKapandiysa()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);
        string? pencere = (await OnizlemeAsync(arac)).TargetStatus;

        arac.Sube.Kapat();

        Result sonuc = await GeriAlAsync(arac, pencere);

        Assert.Equal("Published", pencere);
        Assert.True(sonuc.IsFailure);
        Assert.Equal(SaleReversalErrors.TargetChanged, sonuc.Error.Code);
        Assert.Equal("Draft", sonuc.Error.Parameters!["targetStatus"]);
        Assert.Equal(ListingStatus.Sold, (await IlanAsync(arac.Id)).Status);
        Assert.False((await AracAsync(arac.Id)).AwaitingBranchAssignment);
        Assert.Equal(0, await KayitSayisiAsync(arac.Id));
    }

    [Fact(DisplayName = "0023/AC-8: Geri alınan ilanın adresi değişmez; eski adres ilanı açar")]
    public async Task Acar_EskiAdres_GeriAlinanIlani()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);
        ListingSlug? once = (await IlanAsync(arac.Id)).Slug;

        await GeriAlAsync(arac, "Published");

        Assert.NotNull(once);
        Assert.Equal(once, (await IlanAsync(arac.Id)).Slug);

        await using VehicleCatalogDbContext context = database.CreateContext();
        ShowcaseListing? adrestekiIlan = await context.ShowcaseListings.AsNoTracking()
            .SingleOrDefaultAsync(row => row.Slug == once.Value.Value);

        Assert.NotNull(adrestekiIlan);
        Assert.Equal(arac.Id, adrestekiIlan.VehicleId);
    }

    [Fact(DisplayName = "0023/AC-9: Geri alınan ilanın ilk yayın tarihi değişmez ve vitrin sırasındaki yeri satıştan öncekiyle aynıdır")]
    public async Task Ayni_VitrinSirasi_SatistanOncekiyle()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false, satisOncesiVitrin: true);

        await GeriAlAsync(arac, "Published");

        ShowcaseListing? sonra = await VitrinSatiriAsync(arac.Id);

        Assert.NotNull(arac.SatisOncesiVitrin);
        Assert.NotNull(sonra);
        Assert.Equal(Yayin, (await IlanAsync(arac.Id)).PublishedAt);
        Assert.Equal(arac.SatisOncesiVitrin.PublishedAt, sonra.PublishedAt);
        Assert.Equal(arac.SatisOncesiVitrin.ShowcaseSortWeight, sonra.ShowcaseSortWeight);
        Assert.Equal(arac.SatisOncesiVitrin.ListingId, sonra.ListingId);
    }

    [Fact(DisplayName = "0023/AC-10: Geri alınan ilanın fotoğrafları ve fiyatı değişmez")]
    public async Task Degismez_FotograflarVeFiyat_GeriAlmada()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);
        Vehicle once = await AracAsync(arac.Id);

        await GeriAlAsync(arac, "Published");

        Vehicle sonra = await AracAsync(arac.Id);
        Assert.Equal(once.MediaAssetIds, sonra.MediaAssetIds);
        Assert.Equal(once.Price, sonra.Price);

        await using VehicleCatalogDbContext context = database.CreateContext();
        Assert.Equal(0, await context.PriceChanges.CountAsync(change => change.VehicleId == new VehicleId(arac.Id) && change.ChangedAt == GeriAlma));
    }

    [Fact(DisplayName = "0023/AC-11: Satıştan önce öne çıkarılmış ve satılmışken dokunulmamış ilan geri alındığında öne çıkarılmış olarak döner")]
    public async Task Doner_OneCikarilmis_Dokunulmadiysa()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false, oneCikarilmis: true);

        await GeriAlAsync(arac, "Published");

        Assert.True((await IlanAsync(arac.Id)).IsFeatured);
        Assert.Equal(Listing.FeaturedWeight, (await VitrinSatiriAsync(arac.Id))!.ShowcaseSortWeight);
    }

    [Fact(DisplayName = "0023/AC-12: Satılmışken öne çıkarması kaldırılan ilan geri alındığında öne çıkarılmamış olarak döner (D15)")]
    public async Task Doner_OneCikarilmamis_SatilmiskenKaldirildiysa()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false, oneCikarilmis: true);

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            Listing ilan = await context.Listings.SingleAsync(item => item.VehicleId == new VehicleId(arac.Id));
            ilan.Unfeature();
            await context.SaveChangesAsync();
        }

        await GeriAlAsync(arac, "Published");

        Assert.False((await IlanAsync(arac.Id)).IsFeatured);
        Assert.Equal(Listing.NormalWeight, (await VitrinSatiriAsync(arac.Id))!.ShowcaseSortWeight);
    }

    [Fact(DisplayName = "0023/AC-14: 10 karakterden kısa gerekçe sunucuda reddedilir ve ilan Satıldı kalır")]
    public async Task Reddedilir_KisaGerekce_IlanSatildiKalir()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        Result sonuc = await GeriAlAsync(arac, "Published", gerekce: "yanlış");

        Assert.Equal(ListingSaleReversal.ReasonTooShort, sonuc.Error.Code);
        Assert.Equal(ListingStatus.Sold, (await IlanAsync(arac.Id)).Status);
    }

    [Fact(DisplayName = "0023/AC-15: 500 karakterden uzun gerekçe reddedilir")]
    public async Task Reddedilir_UzunGerekce_IlanSatildiKalir()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        Result sonuc = await GeriAlAsync(arac, "Published", gerekce: new string('g', 501));

        Assert.Equal(ListingSaleReversal.ReasonTooLong, sonuc.Error.Code);
        Assert.Equal(ListingStatus.Sold, (await IlanAsync(arac.Id)).Status);
    }

    [Fact(DisplayName = "0023/AC-16: Yalnız boşluktan oluşan gerekçe boş sayılır ve reddedilir")]
    public async Task Reddedilir_BoslukGerekce_IlanSatildiKalir()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        Result sonuc = await GeriAlAsync(arac, "Published", gerekce: new string(' ', 40));

        Assert.Equal(ListingSaleReversal.ReasonRequired, sonuc.Error.Code);
        Assert.Equal(ListingStatus.Sold, (await IlanAsync(arac.Id)).Status);
    }

    [Fact(DisplayName = "0023/AC-18: Her başarılı geri alma bir kayıt yazar: ilan, geri alan personel, an, gerekçe ve dönülen durum")]
    public async Task Yazar_Kayit_BasariliGeriAlmada()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: true);

        await GeriAlAsync(arac, "Reserved", gerekce: $"  {Gerekce}  ");

        await using VehicleCatalogDbContext context = database.CreateContext();
        ListingSaleReversal kayit = await context.SaleReversals.AsNoTracking()
            .SingleAsync(reversal => reversal.VehicleId == new VehicleId(arac.Id));
        Listing ilan = await IlanAsync(arac.Id);

        Assert.Equal(ilan.Id, kayit.ListingId);
        Assert.Equal(Yonetici, kayit.StaffId);
        Assert.Equal("Ayşe Yönetici", kayit.StaffDisplayName);
        Assert.Equal(GeriAlma, kayit.RevertedAt);
        Assert.Equal(Gerekce, kayit.Reason);
        Assert.Equal(ListingStatus.Reserved, kayit.RestoredStatus);
    }

    [Fact(DisplayName = "0023/AC-19: Reddedilen bir geri alma kayıt yazmaz")]
    public async Task Yazmaz_Kayit_ReddedilenGeriAlmada()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        Result kisa = await GeriAlAsync(arac, "Published", gerekce: "x");
        Result yanlisHedef = await GeriAlAsync(arac, "Reserved");
        Result tanimsizHedef = await GeriAlAsync(arac, "Sold");
        await GeriAlAsync(arac, "Published");
        Result ikinci = await GeriAlAsync(arac, "Published");

        Assert.True(kisa.IsFailure);
        Assert.Equal(SaleReversalErrors.TargetChanged, yanlisHedef.Error.Code);
        Assert.Equal(SaleReversalErrors.ExpectedStatusInvalid, tanimsizHedef.Error.Code);
        Assert.Equal(Listing.InvalidTransition, ikinci.Error.Code);
        Assert.Equal(1, await KayitSayisiAsync(arac.Id));
    }

    [Fact(DisplayName = "0023/AC-20: Yönetici ilanın panel ekranında geri alma kayıtlarını en yenisi üstte, personel adı, an, gerekçe ve dönülen durumla görür")]
    public async Task Listeler_Kayitlar_EnYenisiUstte()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        await GeriAlAsync(arac, "Published", gerekce: "Birinci yanlış tıklama.");
        await SatAsync(arac.Id, rezerveEt: true);
        await GeriAlAsync(arac, "Reserved", gerekce: "İkinci yanlış tıklama.", an: GeriAlma.AddHours(2));

        SaleReversalOverview gorunum = await OnizlemeAsync(arac);

        Assert.Equal(2, gorunum.Records.Count);
        Assert.Equal("İkinci yanlış tıklama.", gorunum.Records[0].Reason);
        Assert.Equal("Reserved", gorunum.Records[0].RestoredStatus);
        Assert.Equal(GeriAlma.AddHours(2), gorunum.Records[0].RevertedAt);
        Assert.Equal("Ayşe Yönetici", gorunum.Records[0].StaffDisplayName);
        Assert.Equal("Birinci yanlış tıklama.", gorunum.Records[1].Reason);
        Assert.Equal("Published", gorunum.Records[1].RestoredStatus);
        Assert.Null(gorunum.TargetStatus);
    }

    [Fact(DisplayName = "0023/AC-22: Başarılı geri alma `VehicleSaleReverted` duyurusunu araç, ilan, adres ve dönülen durumla değişiklikle aynı işlemde outbox'a yazar (D17)")]
    public async Task Yazar_Duyuru_AyniIslemde()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: true);

        await using (VehicleCatalogDbContext temizle = database.CreateContext())
        {
            await temizle.OutboxMessages.ExecuteDeleteAsync();
        }

        Result red = await GeriAlAsync(arac, "Published");

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            Assert.True(red.IsFailure);
            Assert.Equal(0, await context.OutboxMessages.CountAsync());
        }

        await GeriAlAsync(arac, "Reserved");

        await using VehicleCatalogDbContext okuma = database.CreateContext();
        OutboxMessage mesaj = await okuma.OutboxMessages.AsNoTracking().SingleAsync();
        JsonElement govde = JsonDocument.Parse(mesaj.Payload).RootElement;
        Listing ilan = await IlanAsync(arac.Id);

        Assert.Equal(typeof(VehicleSaleReverted).FullName, mesaj.Type);
        Assert.Equal(arac.Id, govde.GetProperty("VehicleId").GetGuid());
        Assert.Equal(ilan.Id.Value, govde.GetProperty("ListingId").GetGuid());
        Assert.Equal(ilan.Slug!.Value.Value, govde.GetProperty("Slug").GetString());
        Assert.Equal("Reserved", govde.GetProperty("RestoredStatus").GetString());

        // C5 — gerekçe ve personel gövdede YOK.
        Assert.DoesNotContain(Gerekce, mesaj.Payload, StringComparison.Ordinal);
        Assert.False(govde.TryGetProperty("Reason", out _));
        Assert.False(govde.TryGetProperty("StaffId", out _));
    }

    [Fact(DisplayName = "0023/AC-23: Yayındayken satılıp geri alınan, sonra rezerve edilip yeniden satılan ilan ikinci geri almada **Rezerve**'ye döner; iki kayıt da durur (D18)")]
    public async Task Doner_Rezerveye_IkinciGeriAlmada()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        await GeriAlAsync(arac, "Published");
        await SatAsync(arac.Id, rezerveEt: true);

        Assert.Equal("Reserved", (await OnizlemeAsync(arac)).TargetStatus);

        Result ikinci = await GeriAlAsync(arac, "Reserved");

        Assert.True(ikinci.IsSuccess);
        Assert.Equal(ListingStatus.Reserved, (await IlanAsync(arac.Id)).Status);
        Assert.Equal(2, await KayitSayisiAsync(arac.Id));
    }

    [Fact(DisplayName = "0023/AC-24: Arşivdeki ilan için geri alma reddedilir")]
    public async Task Reddedilir_ArsivdekiIlan()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);
        await GecisAsync(arac.Id, ListingAction.Archive);

        Result sonuc = await GeriAlAsync(arac, "Published");

        Assert.Equal(Listing.InvalidTransition, sonuc.Error.Code);
        Assert.Equal(ListingStatus.Archived, (await IlanAsync(arac.Id)).Status);
        Assert.Equal(0, await KayitSayisiAsync(arac.Id));
    }

    [Theory(DisplayName = "0023/AC-25: Yayında, Rezerve ya da Taslak bir ilan için geri alma reddedilir")]
    [InlineData(nameof(ListingStatus.Published))]
    [InlineData(nameof(ListingStatus.Reserved))]
    [InlineData(nameof(ListingStatus.Draft))]
    public async Task Reddedilir_SatilmamisIlan(string durum)
    {
        Arac arac = await AracAsync(Enum.Parse<ListingStatus>(durum));

        Result sonuc = await GeriAlAsync(arac, durum);

        Assert.Equal(Listing.InvalidTransition, sonuc.Error.Code);
        Assert.Equal(durum, (await IlanAsync(arac.Id)).Status.ToString());
        Assert.Equal(0, await KayitSayisiAsync(arac.Id));
    }

    /// <summary>
    /// **R11** — `0012b` AC-31'in deseni: iki istek aynı satır kilidinde serileşir, ikincisi
    /// kilidin altında YENİ durumu okur ve kural gereği reddedilir.
    /// </summary>
    [Fact(DisplayName = "0023/AC-26: Aynı ilan için eşzamanlı iki geri alma isteğinden yalnız biri başarılı olur ve yalnız bir kayıt yazılır")]
    public async Task Basarili_Olur_YalnizBiri_IkiGeriAlmada()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        Result[] sonuclar = await Task.WhenAll(
            GeriAlAsync(arac, "Published"),
            GeriAlAsync(arac, "Published"));

        Assert.Single(sonuclar, sonuc => sonuc.IsSuccess);
        Result kaybeden = Assert.Single(sonuclar, sonuc => sonuc.IsFailure);
        Assert.Equal(Listing.InvalidTransition, kaybeden.Error.Code);
        Assert.NotEqual(ConcurrencyErrors.Lost, kaybeden.Error.Code);
        Assert.Equal(1, await KayitSayisiAsync(arac.Id));
    }

    /// <summary>
    /// **R11** — geri alma ile arşivleme aynı satır kilidinde serileşir.
    ///
    /// <para><b>Rezerve'ye dönen ilan seçildi ve bu bilinçli:</b> AC-27'nin "yalnız biri"
    /// iddiası yalnız bu hâlde sözlük anlamıyla doğru. `Yayında`ya ya da `Taslak`'a dönen
    /// ilanda geri alma önce kazanırsa arşivleme ARDINDAN yeni durumdan meşru bir A3
    /// geçişi olarak başarılı olur (`Yayında → Arşiv`, `Taslak → Arşiv`). O hâl aşağıdaki
    /// ayrı testte ölçülüyor; kriter **D20** ile serileşmeyi ölçecek biçimde düzeltildi.</para>
    /// </summary>
    [Fact(DisplayName = "0023/AC-27: Geri alma ile arşivleme eşzamanlı istendiğinde istekler aynı kilitte sırayla işlenir: hiçbir değişiklik kaybolmaz, geri alma kaydı yalnız geri alma başarılıysa yazılır ve ilanın son durumu iki isteğin bir sıralamasının sonucudur")]
    public async Task Basarili_Olur_YalnizBiri_GeriAlmaVeArsivde()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: true);

        Result[] sonuclar = await Task.WhenAll(
            GeriAlAsync(arac, "Reserved"),
            GecisAsync(arac.Id, ListingAction.Archive, basariBekle: false));

        Assert.Single(sonuclar, sonuc => sonuc.IsSuccess);
        Assert.Equal(Listing.InvalidTransition, Assert.Single(sonuclar, sonuc => sonuc.IsFailure).Error.Code);

        ListingStatus son = (await IlanAsync(arac.Id)).Status;
        int kayit = await KayitSayisiAsync(arac.Id);

        // Geri alma kazandıysa ilan Rezerve ve bir kayıt var; arşiv kazandıysa ilan Arşiv
        // ve kayıt YOK. "Arşivlenmiş ama kaydı yazılmış" bir hâl yarışın sızdığını gösterirdi.
        Assert.True(
            (son == ListingStatus.Reserved && kayit == 1) || (son == ListingStatus.Archived && kayit == 0),
            $"Beklenmeyen hâl: {son}, {kayit} kayıt.");
    }

    /// <summary>
    /// AC-27'nin sözlük anlamıyla TUTMAYAN hâli, olduğu gibi ölçülüyor: `Yayında`ya dönen
    /// ilanda iki istek serileşir ve sıra hangisi olursa olsun "satılmış ilan" üzerinde
    /// yalnız biri etkili olur — ama geri alma önce kazanırsa arşivleme `Yayında → Arşiv`
    /// olarak da başarılıdır. Kilit kayıp güncellemeyi önlüyor; "yalnız biri" iddiasını
    /// önlemiyor. Kriter bu davranışa göre düzeltildi (**D20**).
    /// </summary>
    [Fact(DisplayName = "0023/AC-27: Geri alma ile arşivleme eşzamanlı istendiğinde istekler aynı kilitte sırayla işlenir: hiçbir değişiklik kaybolmaz, geri alma kaydı yalnız geri alma başarılıysa yazılır ve ilanın son durumu iki isteğin bir sıralamasının sonucudur")]
    public async Task Serilesir_GeriAlmaVeArsiv_YayindayaDonenIlanda()
    {
        Arac arac = await SatilmisAracAsync(rezerveden: false);

        Result[] sonuclar = await Task.WhenAll(
            GeriAlAsync(arac, "Published"),
            GecisAsync(arac.Id, ListingAction.Archive, basariBekle: false));

        ListingStatus son = (await IlanAsync(arac.Id)).Status;
        int kayit = await KayitSayisiAsync(arac.Id);

        Assert.Equal(ListingStatus.Archived, son);
        Assert.True(sonuclar[1].IsSuccess);
        Assert.Equal(sonuclar[0].IsSuccess ? 1 : 0, kayit);
    }

    private sealed class Arac(Guid id, FakeMediaAssetQueries medya, DegisenSubeSorgusu sube)
    {
        internal Guid Id { get; } = id;

        internal FakeMediaAssetQueries Medya { get; } = medya;

        internal DegisenSubeSorgusu Sube { get; } = sube;

        internal ShowcaseListing? SatisOncesiVitrin { get; set; }
    }

    private async Task<Arac> AracAsync(ListingStatus durum)
    {
        FakeMediaAssetQueries medya = new();
        Vehicle kayit = new VehicleBuilder()
            .WithoutMedia()
            .WithVin($"SGERIA{Interlocked.Increment(ref sayac):D11}")
            .Build();
        kayit.AttachMediaAsset(medya.Ekle(MediaProcessingStatus.Ready));

        await using (VehicleCatalogDbContext context = database.CreateContext())
        {
            context.Vehicles.Add(kayit);
            context.Listings.Add(Listing.CreateDraft(kayit.Id, showcaseSortWeight: 0));
            await context.SaveChangesAsync();
        }

        Arac arac = new(kayit.Id.Value, medya, new DegisenSubeSorgusu());

        if (durum is not ListingStatus.Draft)
        {
            await GecisAsync(arac.Id, ListingAction.Publish, an: Yayin, medya: medya);
        }

        if (durum is ListingStatus.Reserved)
        {
            await GecisAsync(arac.Id, ListingAction.Reserve, medya: medya);
        }

        return arac;
    }

    private async Task<Arac> SatilmisAracAsync(
        bool rezerveden,
        bool oneCikarilmis = false,
        bool satisOncesiVitrin = false)
    {
        Arac arac = await AracAsync(rezerveden ? ListingStatus.Reserved : ListingStatus.Published);

        if (oneCikarilmis)
        {
            await using VehicleCatalogDbContext context = database.CreateContext();
            Listing ilan = await context.Listings.SingleAsync(item => item.VehicleId == new VehicleId(arac.Id));
            ilan.Feature();
            await context.SaveChangesAsync();
        }

        if (satisOncesiVitrin)
        {
            arac.SatisOncesiVitrin = await VitrinSatiriAsync(arac.Id);
        }

        await GecisAsync(arac.Id, ListingAction.MarkSold, an: Satis, medya: arac.Medya);

        return arac;
    }

    private async Task SatAsync(Guid aracId, bool rezerveEt)
    {
        if (rezerveEt)
        {
            Assert.True((await GecisAsync(aracId, ListingAction.Reserve)).IsSuccess);
        }

        Assert.True((await GecisAsync(aracId, ListingAction.MarkSold)).IsSuccess);
    }

    private async Task<Result> GecisAsync(
        Guid aracId,
        ListingAction eylem,
        bool basariBekle = true,
        DateTimeOffset? an = null,
        FakeMediaAssetQueries? medya = null)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(an ?? Satis);
        FakeMediaAssetQueries varliklar = medya ?? new FakeMediaAssetQueries();

        ListingTransitionCommands komut = new(
            context,
            KatalogYazicilari.Birim(context, saat),
            KatalogYazicilari.Yansitma(context, varliklar),
            varliklar,
            new FakeModelNameResolver(),
            new InvariantGuardOptions<VehicleCatalogDbContext>(TimeSpan.FromSeconds(20)),
            saat);

        Result sonuc = await komut.ExecuteAsync(aracId, eylem, CancellationToken.None);

        if (basariBekle)
        {
            Assert.True(sonuc.IsSuccess, $"{eylem} başarısız: {(sonuc.IsFailure ? sonuc.Error.Code : string.Empty)}");
        }

        return sonuc;
    }

    private async Task<Result> GeriAlAsync(
        Arac arac,
        string? beklenen,
        string gerekce = Gerekce,
        DateTimeOffset? an = null)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        FixedClock saat = new(an ?? GeriAlma);

        ListingSaleReversalCommand komut = new(
            context,
            KatalogYazicilari.Birim(context, saat),
            KatalogYazicilari.Yansitma(context, arac.Medya),
            Degerlendirici(context, arac),
            // Kilit süresi UZATILDI (`EszamanliGecisTests`'in gerekçesi): ölçülen şey KURAL
            // reddi, mekanizma kaybı değil.
            new InvariantGuardOptions<VehicleCatalogDbContext>(TimeSpan.FromSeconds(20)),
            saat);

        return await komut.ExecuteAsync(
            new RevertSaleRequest(arac.Id, gerekce, beklenen, Yonetici, "Ayşe Yönetici"),
            CancellationToken.None);
    }

    private async Task<SaleReversalOverview> OnizlemeAsync(Arac arac)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        SaleReversalOverview? sonuc = await new ListingSaleReversalQueries(context, Degerlendirici(context, arac))
            .FindAsync(arac.Id, CancellationToken.None);

        Assert.NotNull(sonuc);

        return sonuc;
    }

    private static SaleRevertAssessor Degerlendirici(VehicleCatalogDbContext context, Arac arac) =>
        new(
            context,
            arac.Sube,
            arac.Medya,
            new ShowcaseLocaleOptions(Locale.Create("tr").Value, [Locale.Create("tr").Value]));

    private async Task<Listing> IlanAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.Listings.AsNoTracking().SingleAsync(item => item.VehicleId == new VehicleId(aracId));
    }

    private async Task<Vehicle> AracAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.Vehicles.AsNoTracking().SingleAsync(item => item.Id == new VehicleId(aracId));
    }

    private async Task<ShowcaseListing?> VitrinSatiriAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.ShowcaseListings.AsNoTracking().SingleOrDefaultAsync(row => row.VehicleId == aracId);
    }

    private async Task<int> KayitSayisiAsync(Guid aracId)
    {
        await using VehicleCatalogDbContext context = database.CreateContext();

        return await context.SaleReversals.CountAsync(reversal => reversal.VehicleId == new VehicleId(aracId));
    }

    /// <summary>
    /// Şubenin durumunu test içinde değiştirebilen sahte (`Dealership`'in okuma yüzeyi).
    /// Kapatma yalnız DURUMU değiştirir — gerçek dünyadaki gibi, satılmış aracın bayrağına
    /// kimse dokunmaz (A8 satılmış ilanı atlıyor).
    /// </summary>
    internal sealed class DegisenSubeSorgusu : IBranchQueries
    {
        private string durum = "Active";

        internal void Kapat() => durum = "Closed";

        public Task<BranchSummaryDto?> FindAsync(Guid branchId, string locale, CancellationToken cancellationToken) =>
            Task.FromResult<BranchSummaryDto?>(new BranchSummaryDto(branchId, "Merkez Şube", durum));

        public Task<IReadOnlyList<BranchSummaryDto>> ListAsync(string locale, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BranchSummaryDto>>([]);

        public Task<IReadOnlyDictionary<string, string>?> FindNamesAsync(Guid branchId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<string, string>?>(
                new Dictionary<string, string>(StringComparer.Ordinal) { ["tr"] = "Merkez Şube" });
    }
}
