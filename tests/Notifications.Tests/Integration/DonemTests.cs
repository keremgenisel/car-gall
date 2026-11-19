using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Contracts.Events;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `0018` R8 · R9 · R15 · **D4** · **D12** · **D13** · **D21** — "yeni araç" ve dönem.
/// İşleyici doğrudan çağrılır (**D13**); zamanlı servis yalnız onu tetikler.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class DonemTests(NotificationsDatabase database)
{
    [Fact(DisplayName = "0018/AC-55: Dönem işlenirken, henüz bir döneme atanmamış ve o an **Yayında** olan her aday ilan, her etkin abonenin bülteninde tam bir kez yer alır")]
    public async Task YerAlir_HerAdayHerEtkinAbonede_BirKez()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.EtkinAboneAsync("veli@ornek.com", "ar");
        Guid a = await d.YeniIlanAsync();
        Guid b = await d.YeniIlanAsync();
        d.DonemeIlerle();

        Guid donem = (await d.IsleAsync())!.Value;

        List<NotificationMessage> bultenler = await d.BultenlerAsync();

        Assert.Equal(2, bultenler.Count);
        Assert.All(bultenler, b => Assert.Equal(donem, b.PeriodId));
        Assert.Equal(2, bultenler.Select(b => b.SubscriptionId).Distinct().Count());
        Assert.Equal([a, b], (await d.DonemIlanlariAsync(donem)).Order());
    }

    [Fact(DisplayName = "0018/AC-29: Teyit bekleyen abone, dönem işlenirken Yayında aday varken bülten almaz")]
    public async Task Almaz_TeyitBekleyenAbone()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.Abonelik.AboneOlAsync("bekleyen@ornek.com");
        await d.EtkinAboneAsync("etkin@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();

        await d.IsleAsync();

        Assert.Empty(await d.BultenlerAsync("bekleyen@ornek.com"));
        Assert.Single(await d.BultenlerAsync("etkin@ornek.com"));
    }

    [Fact(DisplayName = "0018/AC-39: Çıkıştan sonraki ilk dönemde, Yayında aday varken o adrese bülten kuyruğa girmez")]
    public async Task GirmezKuyruga_CikistanSonraki()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.Abonelik.CikAsync(d.Abonelik.Cikis.Create(await d.AbonelikIdOf("ali@ornek.com")));
        await d.YeniIlanAsync();
        d.DonemeIlerle();

        await d.IsleAsync();

        Assert.Empty(await d.BultenlerAsync("ali@ornek.com"));
    }

    [Fact(DisplayName = "0018/AC-20: Çıkmış bir adres için istek yeni kayıt oluşturmaz, kaydı **teyit bekliyor**'a döndürür ve teyit iletisi kuyruğa koyar; önceki **teyit** ve **çıkış** izin olayları zamanlarıyla olay kaydında kalır; teyit edilmeden gelen dönemde o adrese bülten kuyruğa girmez")]
    public async Task GirmezKuyruga_YenidenAboneTeyitEtmeden()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.Abonelik.CikAsync(d.Abonelik.Cikis.Create(await d.AbonelikIdOf("ali@ornek.com")));
        await d.Abonelik.AboneOlAsync("ali@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();

        await d.IsleAsync();

        Assert.Empty(await d.BultenlerAsync("ali@ornek.com"));
    }

    [Fact(DisplayName = "0018/AC-56: Dönem kesiminden önce yayınlanan ama `VehiclePublished` olayı kesimden **sonra** işlenen ilan, bir sonraki dönemin bülteninde bir kez yer alır")]
    public async Task YerAlir_GecIslenenDuyuru_SonrakiDonemde()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        Guid erken = await d.YeniIlanAsync();
        DateTimeOffset kesim = d.SonrakiDonemAni();

        // Kesimden 10 dakika önce yayınlandı, duyurusu kesimden SONRA işlendi.
        Guid gec = Guid.CreateVersion7();
        d.Ilanlar.Yayinla(gec, kesim - TimeSpan.FromMinutes(10));
        d.Saat.Set(kesim + TimeSpan.FromMinutes(1));
        Guid birinci = (await d.IsleAsync())!.Value;
        await d.DuyurAsync(gec, kesim - TimeSpan.FromMinutes(10));

        d.DonemeIlerle();
        Guid ikinci = (await d.IsleAsync())!.Value;
        d.DonemeIlerle();
        Guid ucuncu = (await d.IsleAsync())!.Value;

        Assert.Equal([erken], await d.DonemIlanlariAsync(birinci));
        Assert.Equal([gec], await d.DonemIlanlariAsync(ikinci));
        Assert.Empty(await d.DonemIlanlariAsync(ucuncu));
    }

    [Fact(DisplayName = "0018/AC-57: Bir bültende yer almış ilan yayından kaldırılıp sonraki dönemde yeniden yayınlandığında sonraki bültende yer almaz")]
    public async Task YerAlmaz_YenidenYayin_SonrakiBultende()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        DateTimeOffset ilkYayin = d.Saat.UtcNow;
        Guid ilan = await d.YeniIlanAsync(ilkYayin);
        d.DonemeIlerle();
        Guid birinci = (await d.IsleAsync())!.Value;

        d.Ilanlar.YayindanCikar(ilan);
        d.Saat.Advance(TimeSpan.FromDays(1));
        d.Ilanlar.Yayinla(ilan, ilkYayin);
        await d.DuyurAsync(ilan, ilkYayin);
        Guid diger = await d.YeniIlanAsync();
        d.DonemeIlerle();
        Guid ikinci = (await d.IsleAsync())!.Value;

        Assert.Equal([ilan], await d.DonemIlanlariAsync(birinci));
        Assert.Equal([diger], await d.DonemIlanlariAsync(ikinci));
    }

    /// <summary>
    /// Bilinen tuzak 8 — `0012b` D31'in <c>OccurredAt &gt; PublishedAt</c> formülü burada
    /// ilanı DÜŞÜRÜRDÜ: duyurunun zamanı her okumada ilerleyen saatten ve ilk yayından SONRA.
    /// Sabit saatli test iki değeri eşit bulur ve kusuru göstermezdi.
    /// </summary>
    [Fact(DisplayName = "0018/AC-58: Her okumada ilerleyen bir sahte saatle (`OccurredAt ≠ PublishedAt`) bu dönemde ilk kez yayınlanan ilan bültende yer alır")]
    public async Task YerAlir_IlerleyenSaatle_IlkYayin()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");

        Guid ilan = Guid.CreateVersion7();
        DateTimeOffset ilkYayin = d.Saat.UtcNow;
        d.Ilanlar.Yayinla(ilan, ilkYayin);
        d.Saat.Advance(TimeSpan.FromMilliseconds(3));
        await d.DuyurAsync(ilan, ilkYayin, oluşma: d.Saat.UtcNow);

        d.DonemeIlerle();
        Guid donem = (await d.IsleAsync())!.Value;

        Assert.Equal([ilan], await d.DonemIlanlariAsync(donem));
    }

    [Fact(DisplayName = "0018/AC-59: Aynı dönemde ilk kez yayınlanan, sonra kaldırılıp yeniden yayınlanan ilan bültende bir kez yer alır")]
    public async Task YerAlirBirKez_AyniDonemdeYenidenYayin()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        DateTimeOffset ilkYayin = d.Saat.UtcNow;
        Guid ilan = await d.YeniIlanAsync(ilkYayin);

        d.Ilanlar.YayindanCikar(ilan);
        d.Saat.Advance(TimeSpan.FromHours(2));
        d.Ilanlar.Yayinla(ilan, ilkYayin);
        await d.DuyurAsync(ilan, ilkYayin);

        d.DonemeIlerle();
        Guid donem = (await d.IsleAsync())!.Value;

        Assert.Equal([ilan], await d.DonemIlanlariAsync(donem));
        Assert.Single(await d.AdaylarAsync());
        Assert.Single(await d.BultenlerAsync("ali@ornek.com"));
    }

    [Fact(DisplayName = "0018/AC-60: İlk yayını devreye giriş anından önce olan bir ilan, devreye girişten sonra yeniden yayınlansa da hiçbir bültende yer almaz")]
    public async Task YerAlmaz_DevreyeGiristenOnceYayinlanan()
    {
        DonemDuzeni d = new(database);
        await database.ResetNewsletterAsync();
        DateTimeOffset kartOncesi = d.Saat.UtcNow - TimeSpan.FromDays(30);
        await d.DevreyeGirAsync();
        await d.EtkinAboneAsync("ali@ornek.com");

        Guid eski = Guid.CreateVersion7();
        d.Ilanlar.Yayinla(eski, kartOncesi);
        await d.DuyurAsync(eski, kartOncesi);
        Guid yeni = await d.YeniIlanAsync();

        d.DonemeIlerle();
        Guid birinci = (await d.IsleAsync())!.Value;
        await d.DuyurAsync(eski, kartOncesi);
        d.DonemeIlerle();
        Guid ikinci = (await d.IsleAsync())!.Value;

        Assert.Equal([yeni], await d.DonemIlanlariAsync(birinci));
        Assert.Empty(await d.DonemIlanlariAsync(ikinci));
        Assert.DoesNotContain(await d.AdaylarAsync(), aday => aday.ListingId == eski);
    }

    [Theory(DisplayName = "0018/AC-61: Dönem işlenirken **satılmış**, **arşivlenmiş**, **yayından kaldırılmış** ya da **rezerve** olan aday o dönemin bülteninde yer almaz (dört ayrı test)")]
    [InlineData("Satıldı")]
    [InlineData("Arşiv")]
    [InlineData("Taslak (yayından kaldırıldı)")]
    [InlineData("Rezerve")]
    public async Task YerAlmaz_YayindaOlmayanAday(string durum)
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        Guid kalan = await d.YeniIlanAsync();
        Guid cikan = await d.YeniIlanAsync();

        // Dört durumun sözleşmedeki karşılığı aynıdır: ilan özeti okumasında YOKTUR
        // (`IListingSummaryQueries`, D30). Ayrımın Catalog yarısı `IlanOzetiSorgusuTests`'te.
        d.Ilanlar.YayindanCikar(cikan);
        d.DonemeIlerle();
        Guid donem = (await d.IsleAsync())!.Value;

        _ = durum;
        Assert.Equal([kalan], await d.DonemIlanlariAsync(donem));
        Assert.Equal(NewsletterCandidateStatus.Eliminated, (await d.AdaylarAsync()).Single(aday => aday.ListingId == cikan).Status);
    }

    [Fact(DisplayName = "0018/AC-62: Dönem işlenirken rezerve olduğu için elenen ilan, vitrine döndükten sonraki dönemlerin hiçbirinin bülteninde yer almaz (D12, D21)")]
    public async Task YerAlmaz_RezerveyleElenen_VitrineDonunce()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        DateTimeOffset ilkYayin = d.Saat.UtcNow;
        Guid ilan = await d.YeniIlanAsync(ilkYayin);
        d.Ilanlar.YayindanCikar(ilan);
        d.DonemeIlerle();
        await d.IsleAsync();

        d.Ilanlar.Yayinla(ilan, ilkYayin);
        List<Guid> sonrakiler = [];

        for (int hafta = 0; hafta < 2; hafta++)
        {
            await d.YeniIlanAsync();
            d.DonemeIlerle();
            sonrakiler.AddRange(await d.DonemIlanlariAsync((await d.IsleAsync())!.Value));
        }

        Assert.DoesNotContain(ilan, sonrakiler);
        Assert.Equal(2, sonrakiler.Count);
    }

    [Fact(DisplayName = "0018/AC-63: Dönem işlenirken **Satıldı** olduğu için elenen ilan, satışı geri alınıp (`0023`) Yayında'ya döndükten sonraki dönemlerin hiçbirinin bülteninde yer almaz (D21)")]
    public async Task YerAlmaz_SatildiylaElenen_GeriAlininca()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        DateTimeOffset ilkYayin = d.Saat.UtcNow;
        Guid ilan = await d.YeniIlanAsync(ilkYayin);
        d.Ilanlar.YayindanCikar(ilan);
        d.DonemeIlerle();
        await d.IsleAsync();

        // `0023` — satış geri alındı ve ilan Yayında'ya döndü; yayın duyurusu yeniden gelse
        // bile aday kaydı tekildir ve elenmiştir.
        d.Ilanlar.Yayinla(ilan, ilkYayin);
        await d.DuyurAsync(ilan, ilkYayin);
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        Guid sonraki = (await d.IsleAsync())!.Value;

        Assert.DoesNotContain(ilan, await d.DonemIlanlariAsync(sonraki));
    }

    [Fact(DisplayName = "0018/AC-64: Dönem işlenirken Yayında aday yokken hiçbir aboneye ileti kuyruğa girmez ve gönderim geçmişinde o dönem için kayıt oluşmaz")]
    public async Task GirmezKuyruga_YayindaAdayYokken()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        Guid ilan = await d.YeniIlanAsync();
        d.Ilanlar.YayindanCikar(ilan);
        d.DonemeIlerle();

        Guid donem = (await d.IsleAsync())!.Value;

        Assert.Empty(await d.BultenlerAsync());
        Assert.Contains(await d.DonemlerAsync(), period => period.Id == donem);
    }

    [Fact(DisplayName = "0018/AC-67: Dönem işleyicisi dönem anından önce çağrıldığında hiçbir dönem kaydı ve ileti üretmez")]
    public async Task Uretmez_DonemAnindanOnce()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.YeniIlanAsync();
        d.Saat.Set(d.SonrakiDonemAni() - TimeSpan.FromMinutes(1));

        Assert.Null(await d.IsleAsync());

        Assert.DoesNotContain(await d.DonemlerAsync(), period => period.Kind == NewsletterPeriodKind.Regular);
        Assert.Empty(await d.BultenlerAsync());
    }

    [Fact(DisplayName = "0018/AC-68: Kaçırılmış bir dönem sonradan işlendiğinde bülteni **işlenme** anında etkin olan aboneler alır: arada teyit eden alır, arada çıkan almaz")]
    public async Task Alir_IslenmeAnindaEtkinOlanlar()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("cikan@ornek.com");
        await d.YeniIlanAsync();
        d.Saat.Set(d.SonrakiDonemAni() + TimeSpan.FromHours(5));

        // Dönem anı geçti, işlenmedi (uygulama kapalıydı); arada biri abone olup teyit etti,
        // biri çıktı. Teyit bağlantısının süresi istekten başlar (D28).
        await d.Abonelik.AboneOlAsync("teyitEden@ornek.com");
        await d.Abonelik.TeyitEtAsync(await d.Abonelik.AnahtarVerAsync("teyitEden@ornek.com"));
        await d.Abonelik.CikAsync(d.Abonelik.Cikis.Create(await d.AbonelikIdOf("cikan@ornek.com")));

        await d.IsleAsync();

        Assert.Single(await d.BultenlerAsync("teyitEden@ornek.com"));
        Assert.Empty(await d.BultenlerAsync("cikan@ornek.com"));
    }

    [Fact(DisplayName = "0018/AC-69: Dönem işleyicisi aynı dönem için **ardışık** iki kez çağrıldığında her abone o dönem için en çok bir bülten kaydı alır")]
    public async Task AlirEnCokBir_ArdisikIkiCagri()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();

        Assert.NotNull(await d.IsleAsync());
        d.Saat.Advance(TimeSpan.FromMinutes(1));
        Assert.Null(await d.IsleAsync());

        Assert.Single(await d.BultenlerAsync("ali@ornek.com"));
        Assert.Single(await d.DonemlerAsync(), period => period.Kind == NewsletterPeriodKind.Regular);
    }

    [Fact(DisplayName = "0018/AC-70: Dönem işleyicisi aynı dönem için bir bariyerle **eşzamanlı** iki kez çağrıldığında her abone o dönem için en çok bir bülten kaydı alır")]
    public async Task AlirEnCokBir_EsZamanliIkiCagri()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.EtkinAboneAsync("veli@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();

        // Bariyer KAYIT anında: iki çağrı da "işlenmemiş" okumuş ve yazmaya gelmişken bırakılır.
        KayitBariyeri bariyer = new(2);

        Guid?[] sonuclar = await Task.WhenAll(
            Task.Run(() => d.IsleAsync(bariyer)),
            Task.Run(() => d.IsleAsync(bariyer)));

        Assert.Single(sonuclar, sonuc => sonuc is not null);
        Assert.Single(await d.BultenlerAsync("ali@ornek.com"));
        Assert.Single(await d.BultenlerAsync("veli@ornek.com"));
        Assert.Single(await d.DonemlerAsync(), period => period.Kind == NewsletterPeriodKind.Regular);
    }

    [Fact(DisplayName = "0018/AC-71: Uygulama iki dönem anı boyunca kapalı kaldıysa açılışta her etkin aboneye tek bir bülten gider ve iki dönemin adaylarını birlikte taşır")]
    public async Task GiderTekBulten_IkiKacirilmisDonem()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        Guid birinciHafta = await d.YeniIlanAsync();
        d.Saat.Set(d.SonrakiDonemAni() + TimeSpan.FromHours(1));
        Guid ikinciHafta = await d.YeniIlanAsync();
        d.Saat.Set(d.SonrakiDonemAni() + TimeSpan.FromHours(1));

        Guid donem = (await d.IsleAsync())!.Value;

        Assert.Single(await d.BultenlerAsync("ali@ornek.com"));
        Assert.Equal([birinciHafta, ikinciHafta], (await d.DonemIlanlariAsync(donem)).Order());
        Assert.Single(await d.DonemlerAsync(), period => period.Kind == NewsletterPeriodKind.Regular);
    }

    [Fact(DisplayName = "0018/AC-72: İlk açılış bir başlangıç dönemi kaydı yazar; ikinci açılış bu kaydı ve zamanını değiştirmez")]
    public async Task YazarBirKez_BaslangicKaydi()
    {
        await database.ResetNewsletterAsync();
        DonemDuzeni d = new(database);

        DateTimeOffset ilk = await d.DevreyeGirAsync();
        d.Saat.Advance(TimeSpan.FromDays(3));
        await d.IsleAsync();

        List<NewsletterPeriod> baslangic = [.. (await d.DonemlerAsync()).Where(period => period.Kind == NewsletterPeriodKind.Baseline)];

        Assert.Equal(ilk, Assert.Single(baslangic).CutoffAt);
    }

    [Fact(DisplayName = "0018/AC-73: Aynı `VehiclePublished` olayı iki kez teslim edildiğinde tek aday kaydı oluşur ve ilan bültende bir kez yer alır")]
    public async Task OlusurTekAday_AyniOlayIkiKez()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        Guid ilan = Guid.CreateVersion7();
        d.Ilanlar.Yayinla(ilan, d.Saat.UtcNow);

        var olay = await d.DuyurAsync(ilan, d.Saat.UtcNow);
        await d.TeslimEtAsync(olay);

        d.DonemeIlerle();
        Guid donem = (await d.IsleAsync())!.Value;

        Assert.Single(await d.AdaylarAsync());
        Assert.Equal([ilan], await d.DonemIlanlariAsync(donem));
        Assert.Single(await d.BultenlerAsync("ali@ornek.com"));
    }

    [Fact(DisplayName = "0018/AC-25: Talep formunda iletişim izni vererek e-postasıyla talep bırakan bir ziyaretçinin adresi, abone olmadıkça bülten almaz; `LeadCaptured` hiçbir abonelik kaydı oluşturmaz (D3)")]
    public async Task Olusturmaz_LeadCapturedAbonelik()
    {
        DonemDuzeni d = await HazirlaAsync();
        Guid talepId = Guid.CreateVersion7();

        await using (NotificationsDbContext context = database.CreateContext())
        {
            await new LeadCapturedNotificationHandler(
                context,
                d.Saat,
                new IzinliTalep(talepId),
                Bulten.Secenekler(),
                NullLogger<LeadCapturedNotificationHandler>.Instance).HandleAsync(
                    new LeadCaptured(Guid.CreateVersion7(), d.Saat.UtcNow, talepId, Guid.CreateVersion7(), Guid.CreateVersion7(), "InformationRequest", "tr"),
                    CancellationToken.None);
        }

        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();

        Assert.Equal(0, await d.Abonelik.AbonelikSayisiAsync());
        Assert.Empty(await d.BultenlerAsync());
    }

    [Fact(DisplayName = "0018/AC-82: Abonenin dili kurulumun etkin dilleri arasından çıkarıldıysa bülten varsayılan dilde gider")]
    public async Task Gider_VarsayilanDilde_DilEtkinDegilse()
    {
        await database.ResetNewsletterAsync();
        DonemDuzeni once = new(database);
        await once.DevreyeGirAsync();
        await once.EtkinAboneAsync("ali@ornek.com", "ar");

        // Kurulum artık Arapçayı etkin saymıyor.
        DonemDuzeni sonra = new(database, Bulten.BultenSecenekleri(Bulten.BultenKurulumu(diller: ["tr", "en"])));
        sonra.Saat.Set(once.Saat.UtcNow);
        await sonra.YeniIlanAsync();
        sonra.DonemeIlerle();
        await sonra.IsleAsync();

        Assert.Equal("tr", Assert.Single(await sonra.BultenlerAsync()).Locale);
    }

    private async Task<DonemDuzeni> HazirlaAsync()
    {
        await database.ResetNewsletterAsync();
        DonemDuzeni d = new(database);
        await d.DevreyeGirAsync();
        d.Saat.Advance(TimeSpan.FromSeconds(1));

        return d;
    }

    /// <summary>İletişim izni VERMİŞ, e-postası olan bir talep (D3: bülten izni değildir).</summary>
    private sealed class IzinliTalep(Guid talepId) : ILeadNotificationQueries
    {
        public Task<LeadNotificationDto?> FindAsync(Guid leadId, CancellationToken cancellationToken) =>
            Task.FromResult<LeadNotificationDto?>(new LeadNotificationDto(
                talepId, "izinli@ornek.com", "tr", "Toyota Corolla", Guid.CreateVersion7(), Guid.CreateVersion7()));
    }
}
