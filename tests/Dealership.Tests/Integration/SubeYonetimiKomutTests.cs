using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Tests.Integration;

/// <summary>
/// `0020` — galeri ve şube komutları gerçek PostgreSQL'de: outbox'a yazılan duyurular, sürüm
/// kolonu, kısaltma tekilliği ve B5'in kilidi.
///
/// <para><b>Kendi veritabanı</b> (`SubeKapatmaTests`'in gerekçesi): B5 galeri genelidir ve
/// testler şemadaki BÜTÜN şubeleri siler. Her test kendi başlangıcını kurar.</para>
///
/// <para>Her istek kendi <c>DbContext</c>'ini kullanır: tek bağlamda koşan bir eşzamanlılık
/// testi EF'in izleyicisini ölçerdi, veritabanını değil.</para>
/// </summary>
public sealed class SubeYonetimiKomutTests : IAsyncLifetime
{
    private static readonly Locale Tr = Locale.Create("tr").Value;
    private static readonly Locale Ar = Locale.Create("ar").Value;
    private static readonly Locale En = Locale.Create("en").Value;

    private readonly DealershipDatabase database = new();

    public Task InitializeAsync() => database.InitializeAsync();

    public Task DisposeAsync() => database.DisposeAsync();

    [Fact(DisplayName = "0020/AC-7: Migration'dan sonra var olan bir kurulumda varsayılan dildeki görünen ad ticari unvana eşittir ve diğer dillerin görünen adı boştur.")]
    public async Task EsittirUnvana_VarsayilanDildekiGorunenAd_MigrationSonrasi()
    {
        // Kurulum adımı (migration + doldurma) `DealershipDatabase`'in kendisinde koştu.
        await using DealershipDbContext context = database.CreateContext();
        Gallery galeri = await context.Galleries.AsNoTracking().SingleAsync();

        Assert.Equal(galeri.CommercialName, galeri.DisplayNames.Resolve(Tr, Tr));
        Assert.Equal(["tr"], galeri.DisplayNames.ToDictionary().Keys);
        Assert.Equal(1, galeri.Version);
    }

    [Fact(DisplayName = "0020/AC-7: Migration'dan sonra var olan bir kurulumda varsayılan dildeki görünen ad ticari unvana eşittir ve diğer dillerin görünen adı boştur.")]
    public async Task Doldurmaz_VarOlanAdi_KurulumAdimiTekrarKostugunda()
    {
        await using DealershipDbContext context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE dealership.galleries SET display_names = {0}::jsonb",
            ["""{"tr":"Panelden Yazılan"}"""]);

        // İdempotent: yöneticinin yazdığı ad ezilmez; varsayılan dil Arapça olan bir kurulum
        // yalnız EKSİK dili doldurur — kod `tr`'yi varsaymaz.
        await DealershipSchema.BackfillDisplayNameAsync(context, Tr, CancellationToken.None);
        await DealershipSchema.BackfillDisplayNameAsync(context, Ar, CancellationToken.None);

        Gallery galeri = await context.Galleries.AsNoTracking().SingleAsync();
        Assert.Equal("Panelden Yazılan", galeri.DisplayNames.Resolve(Tr, Tr));
        Assert.Equal(galeri.CommercialName, galeri.DisplayNames.Resolve(Ar, Ar));
    }

    [Fact(DisplayName = "0020/AC-2: Yönetici ticari unvanı değiştirip kaydettiğinde ekran yeniden açıldığında yeni unvan görünür.")]
    public async Task Gorunur_YeniUnvan_YenidenAcildiginda()
    {
        int surum = await GaleriSurumuAsync();

        Result<int> sonuc = await GaleriKaydetAsync(surum, "Yeni Unvan A.Ş.");

        Assert.True(sonuc.IsSuccess);
        AdminGalleryDto? okunan = await new DealershipAdminQueries(database.CreateContext()).GetGalleryAsync(CancellationToken.None);
        Assert.Equal("Yeni Unvan A.Ş.", okunan!.CommercialName);
        Assert.Equal(surum + 1, okunan.Version);
    }

    [Fact(DisplayName = "0020/AC-59: Galeri profilini, okunduktan sonra başka bir kaydetmeyle değişmiş bir sürüm üzerinden kaydetmek `409` ve bir hata koduyla reddedilir; kayıtlı değer değişmez.")]
    public async Task Reddedilir_EskiSurumdenGaleriKaydi()
    {
        int okunan = await GaleriSurumuAsync();
        Assert.True((await GaleriKaydetAsync(okunan, "Önce Yazan")).IsSuccess);

        Result<int> sonuc = await GaleriKaydetAsync(okunan, "Sonra Yazan");

        Assert.Equal(ConcurrencyErrors.Lost, sonuc.Error.Code);
        await using DealershipDbContext context = database.CreateContext();
        Assert.Equal("Önce Yazan", (await context.Galleries.AsNoTracking().SingleAsync()).CommercialName);
    }

    [Fact(DisplayName = "0020/AC-20: Listedeki şube adı yöneticinin panel dilindeki karşılığıdır; yoksa varsayılan dildekidir.")]
    public async Task Doner_PanelDilindekiAd_YoksaVarsayilan()
    {
        await TemizleAsync();
        await KomutAsync(komut => komut.OpenAsync(Form("iki-dilli"), CancellationToken.None));
        await KomutAsync(komut => komut.OpenAsync(
            Form("tek-dilli") with { Names = Adlar(("tr", "Yalnız Türkçe Şube")) },
            CancellationToken.None));

        await using DealershipDbContext context = database.CreateContext();
        IReadOnlyList<CarGallery.Modules.Dealership.Contracts.Events.BranchSummaryDto> liste =
            await new BranchQueries(context, Tr).ListAsync("ar", CancellationToken.None);

        Assert.Contains(liste, satir => satir.Name == "فرع كاديكوي");
        Assert.Contains(liste, satir => satir.Name == "Yalnız Türkçe Şube");
    }

    [Fact(DisplayName = "0020/AC-21: Yönetici zorunlu alanları doldurup kaydettiğinde yeni şube listede **aktif** olarak görünür.")]
    public async Task AktifGorunur_YeniSube()
    {
        await TemizleAsync();

        Result<(Guid Id, int Version)> sonuc = await KomutAsync(komut => komut.OpenAsync(Form("yeni-sube"), CancellationToken.None));

        Assert.True(sonuc.IsSuccess);
        await using DealershipDbContext context = database.CreateContext();
        Branch sube = await context.Branches.AsNoTracking().SingleAsync(item => item.Id == new BranchId(sonuc.Value.Id));
        Assert.Equal(BranchStatus.Active, sube.Status);
    }

    [Fact(DisplayName = "0020/AC-22: Yeni şube açıldığında şube açılış duyurusu şubenin kimliği ve dil başına adlarıyla outbox'a yazılır.")]
    public async Task YazilirAcilisDuyurusu_KimlikVeDilBasinaAdlarla()
    {
        await TemizleAsync();

        Result<(Guid Id, int Version)> sonuc = await KomutAsync(komut => komut.OpenAsync(Form("duyurulu-sube"), CancellationToken.None));

        OutboxMessage mesaj = Assert.Single(await DuyurularAsync());
        Assert.Equal(typeof(BranchOpened).FullName, mesaj.Type);
        BranchOpened duyuru = JsonSerializer.Deserialize<BranchOpened>(mesaj.Payload)!;
        Assert.Equal(sonuc.Value.Id, duyuru.BranchId);
        Assert.Contains(new BranchNameDto("tr", "Kadıköy Şubesi"), duyuru.Names);
        Assert.Contains(new BranchNameDto("ar", "فرع كاديكوي"), duyuru.Names);
    }

    [Fact(DisplayName = "0020/AC-32: Reddedilen bir kaydetmede hiçbir şube oluşmaz ve hiçbir duyuru yazılmaz.")]
    public async Task SubeVeDuyuruYok_ReddedilenKaydetmede()
    {
        await TemizleAsync();

        Result<(Guid Id, int Version)> sonuc = await KomutAsync(komut =>
            komut.OpenAsync(Form("reddedilen") with { Phone = "02165554433", Email = "kadikoy@" }, CancellationToken.None));

        Assert.True(sonuc.IsFailure);
        await using DealershipDbContext context = database.CreateContext();
        Assert.Equal(0, await context.Branches.CountAsync());
        Assert.Empty(await DuyurularAsync());
    }

    [Fact(DisplayName = "0020/AC-30: Başka bir şubenin — kapatılmış bir şube dâhil — kullandığı adres kısaltması `422` ve bir alan hata koduyla reddedilir; hata kısaltmanın zaten kullanıldığını söyler.")]
    public async Task Reddedilir_BaskaSubeninKisaltmasi_KapatilmisDahil()
    {
        await TemizleAsync();
        Guid acik = (await KomutAsync(komut => komut.OpenAsync(Form("acik-kisaltma"), CancellationToken.None))).Value.Id;
        Guid kapatilacak = (await KomutAsync(komut => komut.OpenAsync(Form("kapali-kisaltma"), CancellationToken.None))).Value.Id;
        Assert.True((await KomutAsync(komut => komut.CloseAsync(kapatilacak, null, CancellationToken.None))).IsSuccess);

        foreach (string kisaltma in new[] { "acik-kisaltma", "kapali-kisaltma" })
        {
            Result<(Guid Id, int Version)> sonuc = await KomutAsync(komut => komut.OpenAsync(Form(kisaltma), CancellationToken.None));

            Assert.Equal(FieldErrors.ValidationFailed, sonuc.Error.Code);
            Assert.Equal(BranchCommands.SlugTaken, sonuc.Error.Parameters!["slug"]);
        }

        // Düzenlemede de aynı: başka şubenin kısaltmasına geçilemez.
        Result<int> duzenleme = await KomutAsync(async komut =>
            await komut.UpdateAsync(acik, await SurumAsync(acik), Form("kapali-kisaltma"), CancellationToken.None));
        Assert.Equal(BranchCommands.SlugTaken, duzenleme.Error.Parameters!["slug"]);
    }

    [Fact(DisplayName = "0020/AC-34: Yönetici bir şubenin varsayılan dildeki adını değiştirip kaydettiğinde ad değişikliği duyurusu güncel adlarla outbox'a yazılır.")]
    public async Task YazilirAdDegisikligiDuyurusu_GuncelAdlarla()
    {
        Guid sube = await AcikSubeAsync("ad-degisen");

        Result<int> sonuc = await KomutAsync(async komut => await komut.UpdateAsync(
            sube,
            await SurumAsync(sube),
            Form("ad-degisen") with { Names = Adlar(("tr", "Moda Şubesi"), ("ar", "فرع كاديكوي")) },
            CancellationToken.None));

        Assert.True(sonuc.IsSuccess);
        OutboxMessage mesaj = Assert.Single(await DuyurularAsync());
        Assert.Equal(typeof(BranchDetailsChanged).FullName, mesaj.Type);
        BranchDetailsChanged duyuru = JsonSerializer.Deserialize<BranchDetailsChanged>(mesaj.Payload)!;
        Assert.Equal(sube, duyuru.BranchId);
        Assert.Contains(new BranchNameDto("tr", "Moda Şubesi"), duyuru.Names);
    }

    [Fact(DisplayName = "0020/AC-35: Yalnız Arapça adı değiştirilen kaydetme tam olarak **bir** ad değişikliği duyurusu yazar.")]
    public async Task TekDuyuruYazilir_YalnizArapcaAdDegistiginde()
    {
        Guid sube = await AcikSubeAsync("arapca-degisen");

        await KaydetAsync(sube, Form("arapca-degisen") with { Names = Adlar(("tr", "Kadıköy Şubesi"), ("ar", "فرع مودا")) });

        Assert.Single(await DuyurularAsync(), mesaj => mesaj.Type == typeof(BranchDetailsChanged).FullName);
    }

    [Fact(DisplayName = "0020/AC-36: Ad ve adres birlikte değiştirilen kaydetme tam olarak **bir** ad değişikliği duyurusu yazar.")]
    public async Task TekDuyuruYazilir_AdVeAdresBirlikteDegistiginde()
    {
        Guid sube = await AcikSubeAsync("ad-adres-degisen");

        await KaydetAsync(sube, Form("ad-adres-degisen") with
        {
            Names = Adlar(("tr", "Moda Şubesi")),
            Addresses = Adlar(("tr", "Moda Caddesi 9")),
        });

        Assert.Single(await DuyurularAsync());
    }

    [Fact(DisplayName = "0020/AC-37: Adı değişmeden yalnız adresi, telefonu, saat dilimi ya da kısaltması değiştirilen kaydetme **hiçbir** duyuru yazmaz.")]
    public async Task DuyuruYazilmaz_AdDegismedenKaydedildiginde()
    {
        Guid sube = await AcikSubeAsync("ad-sabit");

        await KaydetAsync(sube, Form("ad-sabit-yeni") with
        {
            Addresses = Adlar(("tr", "Başka Adres 3")),
            Phone = "+902169990000",
            TimeZone = "Asia/Kuwait",
        });

        Assert.Empty(await DuyurularAsync());
    }

    [Fact(DisplayName = "0020/AC-39: WhatsApp numarası, e-posta ve koordinat boşaltılıp kaydedildiğinde üçü de boş kaydedilir.")]
    public async Task BosKaydedilir_WhatsAppEpostaKoordinat()
    {
        await TemizleAsync();
        Guid sube = (await KomutAsync(komut => komut.OpenAsync(
            Form("dolu-opsiyonel") with { WhatsApp = "+905321112233", Email = "kadikoy@ornek.com", Latitude = 40.98m, Longitude = 29.06m },
            CancellationToken.None))).Value.Id;

        await KaydetAsync(sube, Form("dolu-opsiyonel") with { WhatsApp = string.Empty, Email = "  ", Latitude = null, Longitude = null });

        AdminBranchDto okunan = (await new DealershipAdminQueries(database.CreateContext()).FindBranchAsync(sube, CancellationToken.None))!;
        Assert.Null(okunan.WhatsApp);
        Assert.Null(okunan.Email);
        Assert.Null(okunan.Latitude);
        Assert.Null(okunan.Longitude);
    }

    [Fact(DisplayName = "0020/AC-40: Şubenin kendi kullandığı adres kısaltmasıyla kaydetmek tekillik hatası vermez.")]
    public async Task TekillikHatasiYok_KendiKisaltmasiyla()
    {
        Guid sube = await AcikSubeAsync("kendi-kisaltmasi");

        Result<int> sonuc = await KomutAsync(async komut =>
            await komut.UpdateAsync(sube, await SurumAsync(sube), Form("kendi-kisaltmasi"), CancellationToken.None));

        Assert.True(sonuc.IsSuccess);
    }

    [Theory(DisplayName = "0020/AC-41: AC-24–AC-31'deki doğrulamalar düzenlemede de aynı hatayla geçerlidir.")]
    [InlineData("name.tr", Branch.DefaultLocaleNameRequired)]
    [InlineData("address.tr", Branch.DefaultLocaleAddressRequired)]
    [InlineData("phone", PhoneNumber.InvalidFormat)]
    [InlineData("timeZone", TimeZoneId.InvalidZone)]
    [InlineData("slug", BranchSlug.InvalidFormat)]
    [InlineData("email", EmailAddress.InvalidFormat)]
    public async Task AyniHata_DuzenlemedeDe(string alan, string kod)
    {
        Guid sube = await AcikSubeAsync("duzenleme-dogrulama");
        BranchFormInput hatali = alan switch
        {
            "name.tr" => Form("duzenleme-dogrulama") with { Names = Adlar(("tr", " "), ("ar", "فرع")) },
            "address.tr" => Form("duzenleme-dogrulama") with { Addresses = Adlar(("ar", "عنوان")) },
            "phone" => Form("duzenleme-dogrulama") with { Phone = "02165554433" },
            "timeZone" => Form("duzenleme-dogrulama") with { TimeZone = "+03:00" },
            "slug" => Form("kadıköy-şubesi"),
            _ => Form("duzenleme-dogrulama") with { Email = "kadikoy@" },
        };

        Result<int> sonuc = await KomutAsync(async komut =>
            await komut.UpdateAsync(sube, await SurumAsync(sube), hatali, CancellationToken.None));

        Assert.Equal(FieldErrors.ValidationFailed, sonuc.Error.Code);
        Assert.Equal(kod, sonuc.Error.Parameters![alan]);
    }

    [Fact(DisplayName = "0020/AC-42: Kapatılmış bir şube düzenlenemez: ekran alanları salt okunur gösterir ve uç reddeder (R10, D21).")]
    public async Task Reddedilir_KapatilmisSubeninDuzenlenmesi()
    {
        Guid sube = await AcikSubeAsync("kapanacak");
        await AcikSubeAsync("kalan", temizle: false);
        Assert.True((await KomutAsync(komut => komut.CloseAsync(sube, null, CancellationToken.None))).IsSuccess);

        Result<int> sonuc = await KomutAsync(async komut =>
            await komut.UpdateAsync(sube, await SurumAsync(sube), Form("kapanacak") with { Names = Adlar(("tr", "Yeni")) }, CancellationToken.None));

        Assert.Equal(Branch.ClosedReadOnly, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0020/AC-48: Onaylanan kalıcı kapatmadan sonra şube **kapatılmış** olur ve şube kapanış duyurusu outbox'a yazılır.")]
    public async Task KapatilmisOlur_VeKapanisDuyurusuYazilir()
    {
        Guid sube = await AcikSubeAsync("kalici-kapanan");
        await AcikSubeAsync("ayakta-kalan", temizle: false);
        await DuyurulariSilAsync();

        Result<int> sonuc = await KomutAsync(async komut =>
            await komut.CloseAsync(sube, await SurumAsync(sube), CancellationToken.None));

        Assert.True(sonuc.IsSuccess);
        await using DealershipDbContext context = database.CreateContext();
        Assert.Equal(BranchStatus.Closed, (await context.Branches.AsNoTracking().SingleAsync(item => item.Id == new BranchId(sube))).Status);
        OutboxMessage mesaj = Assert.Single(await DuyurularAsync());
        Assert.Equal(typeof(BranchDeactivated).FullName, mesaj.Type);
        Assert.Equal(sube, JsonSerializer.Deserialize<BranchDeactivated>(mesaj.Payload)!.BranchId);
    }

    [Fact(DisplayName = "0020/AC-51: Galerinin tek aktif şubesi önce geçici kapatılıp ardından kalıcı kapatılmak istendiğinde kalıcı kapatma reddedilir ve şube geçici kapalı kalır (D13).")]
    public async Task Reddedilir_GeciciKapatilanTekAktifSubeninKalicisi()
    {
        Guid sube = await AcikSubeAsync("tek-aktif");
        Assert.True((await KomutAsync(async komut =>
            await komut.CloseTemporarilyAsync(sube, await SurumAsync(sube), CancellationToken.None))).IsSuccess);

        Result<int> sonuc = await KomutAsync(async komut => await komut.CloseAsync(sube, await SurumAsync(sube), CancellationToken.None));

        Assert.Equal(Branch.LastActiveBranch, sonuc.Error.Code);
        await using DealershipDbContext context = database.CreateContext();
        Assert.Equal(BranchStatus.TemporarilyClosed, (await context.Branches.AsNoTracking().SingleAsync()).Status);
    }

    [Fact(DisplayName = "0020/AC-52: Başka bir aktif şube varken geçici kapalı bir şube kalıcı kapatılabilir (D21).")]
    public async Task Kapatilir_GeciciKapali_BaskaAktifVarken()
    {
        Guid gecici = await AcikSubeAsync("gecici-kapanan");
        await AcikSubeAsync("aktif-olan", temizle: false);
        Assert.True((await KomutAsync(async komut =>
            await komut.CloseTemporarilyAsync(gecici, await SurumAsync(gecici), CancellationToken.None))).IsSuccess);

        Result<int> sonuc = await KomutAsync(async komut => await komut.CloseAsync(gecici, await SurumAsync(gecici), CancellationToken.None));

        Assert.True(sonuc.IsSuccess);
    }

    [Fact(DisplayName = "0020/AC-53: İki farklı aktif şubenin, başka aktif şube yokken eşzamanlı kalıcı kapatılması en fazla birinde başarılı olur.")]
    public async Task EnFazlaBiriBasarili_IkiAktifSubeEsZamanliKapatilirken()
    {
        for (int tur = 0; tur < 5; tur++)
        {
            Guid birinci = await AcikSubeAsync($"esz-bir-{tur}");
            Guid ikinci = await AcikSubeAsync($"esz-iki-{tur}", temizle: false);
            int s1 = await SurumAsync(birinci);
            int s2 = await SurumAsync(ikinci);

            Result<int>[] sonuclar = await Task.WhenAll(
                KomutAsync(komut => komut.CloseAsync(birinci, s1, CancellationToken.None)),
                KomutAsync(komut => komut.CloseAsync(ikinci, s2, CancellationToken.None)));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            await using DealershipDbContext context = database.CreateContext();
            Assert.Equal(1, await context.Branches.CountAsync(item => item.Status == BranchStatus.Active));
        }
    }

    [Fact(DisplayName = "0020/AC-60: Aynı kural şube düzenlemesi için de geçerlidir.")]
    public async Task Reddedilir_EskiSurumdenSubeKaydi()
    {
        Guid sube = await AcikSubeAsync("eski-surum");
        int okunan = await SurumAsync(sube);
        Assert.True((await KomutAsync(komut => komut.UpdateAsync(sube, okunan, Form("eski-surum") with { Phone = "+902161112233" }, CancellationToken.None))).IsSuccess);

        Result<int> sonuc = await KomutAsync(komut =>
            komut.UpdateAsync(sube, okunan, Form("eski-surum") with { Phone = "+902164445566" }, CancellationToken.None));

        Assert.Equal(ConcurrencyErrors.Lost, sonuc.Error.Code);
        AdminBranchDto okuma = (await new DealershipAdminQueries(database.CreateContext()).FindBranchAsync(sube, CancellationToken.None))!;
        Assert.Equal("+902161112233", okuma.Phone);
    }

    [Fact(DisplayName = "0020/AC-61: Aynı sürümden yapılan iki eşzamanlı şube kaydetmesinden yalnız biri kabul edilir; diğeri `409` alır.")]
    public async Task YalnizBiriKabul_AyniSurumdenIkiEsZamanliKaydetme()
    {
        for (int tur = 0; tur < 5; tur++)
        {
            Guid sube = await AcikSubeAsync($"yaris-{tur}");
            int okunan = await SurumAsync(sube);

            Result<int>[] sonuclar = await Task.WhenAll(
                KomutAsync(komut => komut.UpdateAsync(sube, okunan, Form($"yaris-{tur}") with { Phone = "+902161111111" }, CancellationToken.None)),
                KomutAsync(komut => komut.UpdateAsync(sube, okunan, Form($"yaris-{tur}") with { Phone = "+902162222222" }, CancellationToken.None)));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            Assert.Equal(ConcurrencyErrors.Lost, sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);
        }
    }

    [Fact(DisplayName = "0020/AC-62: Düzenleme formu açıkken şube geçici kapatılırsa formdaki kaydetme `409` ile reddedilir (D17).")]
    public async Task Reddedilir_FormAcikkenGeciciKapatilanSubeninKaydi()
    {
        Guid sube = await AcikSubeAsync("form-acik");
        int formunSurumu = await SurumAsync(sube);
        Assert.True((await KomutAsync(komut => komut.CloseTemporarilyAsync(sube, formunSurumu, CancellationToken.None))).IsSuccess);

        Result<int> sonuc = await KomutAsync(komut =>
            komut.UpdateAsync(sube, formunSurumu, Form("form-acik") with { Phone = "+902163334455" }, CancellationToken.None));

        Assert.Equal(ConcurrencyErrors.Lost, sonuc.Error.Code);
    }

    private async Task<Guid> AcikSubeAsync(string kisaltma, bool temizle = true)
    {
        if (temizle)
        {
            await TemizleAsync();
        }

        Result<(Guid Id, int Version)> sonuc = await KomutAsync(komut => komut.OpenAsync(Form(kisaltma), CancellationToken.None));
        Assert.True(sonuc.IsSuccess);

        // Açılış duyurusu bu testlerin ölçtüğü şey değil.
        await DuyurulariSilAsync();
        return sonuc.Value.Id;
    }

    private async Task KaydetAsync(Guid sube, BranchFormInput form)
    {
        Result<int> sonuc = await KomutAsync(async komut => await komut.UpdateAsync(sube, await SurumAsync(sube), form, CancellationToken.None));
        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
    }

    private async Task<T> KomutAsync<T>(Func<BranchCommands, Task<T>> islem)
    {
        await using DealershipDbContext context = database.CreateContext();
        TestSaati saat = new(DateTimeOffset.UtcNow);

        BranchCommands komutlar = new(
            context,
            new DealershipUnitOfWork(context, new OutboxWriter<DealershipDbContext>(context, saat), saat),
            InvariantGuardOptions<DealershipDbContext>.Default,
            Diller);

        return await islem(komutlar);
    }

    private async Task<Result<int>> GaleriKaydetAsync(int surum, string unvan)
    {
        await using DealershipDbContext context = database.CreateContext();

        return await new GalleryCommands(context, Diller).UpdateProfileAsync(
            surum,
            new GalleryProfileInput(unvan, null, null, new Dictionary<string, string?> { ["tr"] = "Oto" }),
            CancellationToken.None);
    }

    private async Task<int> GaleriSurumuAsync()
    {
        await using DealershipDbContext context = database.CreateContext();
        return (await context.Galleries.AsNoTracking().SingleAsync()).Version;
    }

    private async Task<int> SurumAsync(Guid sube)
    {
        await using DealershipDbContext context = database.CreateContext();
        return (await context.Branches.AsNoTracking().SingleAsync(item => item.Id == new BranchId(sube))).Version;
    }

    private async Task TemizleAsync()
    {
        await using DealershipDbContext context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("DELETE FROM dealership.branches");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM dealership.outbox_messages");
    }

    private async Task DuyurulariSilAsync()
    {
        await using DealershipDbContext context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("DELETE FROM dealership.outbox_messages");
    }

    private async Task<List<OutboxMessage>> DuyurularAsync()
    {
        await using DealershipDbContext context = database.CreateContext();
        return await context.OutboxMessages.AsNoTracking().ToListAsync();
    }

    private static readonly DealershipLocales Diller = new(Tr, [Tr, Ar, En]);

    private static BranchFormInput Form(string kisaltma) => new(
        Adlar(("tr", "Kadıköy Şubesi"), ("ar", "فرع كاديكوي")),
        Adlar(("tr", "Bağdat Caddesi 1")),
        "+902165554433",
        null,
        null,
        "Europe/Istanbul",
        kisaltma,
        null,
        null);

    private static Dictionary<string, string?> Adlar(params (string Dil, string? Deger)[] degerler) =>
        degerler.ToDictionary(item => item.Dil, item => item.Deger);
}
