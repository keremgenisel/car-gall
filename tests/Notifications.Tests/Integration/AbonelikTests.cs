using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `0018` R1 · R7 · R13 · R19 — abone olma isteğinin arkası. Cevabın KENDİSİ (durum kodu, bayt
/// bayt gövde — AC-16) ve kaba istek sınırı (AC-14) Host.Tests'te, gerçek boru hattında.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class AbonelikTests(NotificationsDatabase database)
{
    private readonly Bulten.IleriSaat saat = new(DateTimeOffset.UtcNow);

    [Theory(DisplayName = "0018/AC-7: `ali@` ve boş adres alan hatasıyla reddedilir; API `400` ve `notification.subscription_email_invalid` döner; kayıt oluşmaz ve ileti kuyruğa girmez")]
    [InlineData("ali@")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Reddeder_EksikAdresi(string? adres) => await ReddedilirAsync(adres);

    [Theory(DisplayName = "0018/AC-8: `Ali <ali@ornek.com>`, `\"x y\" <ali@ornek.com>`, `ali` ve `ali@ornek` AC-7 gibi reddedilir (D10)")]
    [InlineData("Ali <ali@ornek.com>")]
    [InlineData("\"x y\" <ali@ornek.com>")]
    [InlineData("ali")]
    [InlineData("ali@ornek")]
    public async Task Reddeder_GorunenAdliVeAlanAdsizBicimi(string adres) => await ReddedilirAsync(adres);

    [Fact(DisplayName = "0018/AC-9: 255 karakterlik adres AC-7 gibi reddedilir; 254 karakterlik geçerli adres kabul edilir")]
    public async Task Reddeder255_KabulEder254()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        string adres254 = UzunAdres(254);
        string adres255 = UzunAdres(255);

        Assert.Equal(254, adres254.Length);
        Assert.Equal(255, adres255.Length);

        Result red = await duzen.AboneOlAsync(adres255);
        Assert.Equal(NotificationErrors.SubscriptionEmailInvalid, red.Error.Code);
        Assert.Equal(0, await duzen.AbonelikSayisiAsync());

        Assert.True((await duzen.AboneOlAsync(adres254)).IsSuccess);
        Assert.Equal(1, await duzen.AbonelikSayisiAsync());
    }

    [Theory(DisplayName = "0018/AC-10: Kurulumda etkin olmayan ya da tanınmayan bir dille gelen istek `400` ve `notification.subscription_invalid_locale` alır; kayıt oluşmaz (D14)")]
    [InlineData("de")]
    [InlineData("xx")]
    [InlineData("TR")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Reddeder_EtkinOlmayanDili(string? dil)
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat, Bulten.BultenSecenekleri(Bulten.BultenKurulumu(diller: ["tr", "ar"])));

        Result sonuc = await duzen.AboneOlAsync("ali@ornek.com", dil);

        Assert.Equal(NotificationErrors.SubscriptionInvalidLocale, sonuc.Error.Code);
        Assert.Equal("locale", sonuc.Error.Parameters!["field"]);
        Assert.Equal(0, await duzen.AbonelikSayisiAsync());

        // `en` dil olarak GEÇERLİ ama bu kurulumda etkin değil.
        Assert.Equal(NotificationErrors.SubscriptionInvalidLocale, (await duzen.AboneOlAsync("ali@ornek.com", "en")).Error.Code);
    }

    [Fact(DisplayName = "0018/AC-12: Aynı IP'den yapılandırılan pencerede geçerli istek eşiği kadar istekten sonraki geçerli istek `429` ve `notification.subscription_rate_limited` alır; kayıt oluşmaz, ileti kuyruğa girmez ve form hız sınırı mesajı gösterir (D9)")]
    public async Task Reddeder_GecerliIstekEsigindenSonra()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        // Yapılandırılan eşik 4 (AC-98: spec'in önerisi 5 değil).
        for (int sira = 1; sira <= duzen.Secenekler.ValidRequestLimit; sira++)
        {
            Assert.True((await duzen.AboneOlAsync($"kisi{sira}@ornek.com")).IsSuccess);
        }

        Result sonuc = await duzen.AboneOlAsync("fazla@ornek.com");

        Assert.Equal(NotificationErrors.SubscriptionRateLimited, sonuc.Error.Code);
        Assert.Equal(duzen.Secenekler.ValidRequestLimit, await duzen.AbonelikSayisiAsync());
        Assert.Equal(duzen.Secenekler.ValidRequestLimit, (await duzen.TeyitIletileriAsync()).Count);

        // Başka bir kaynak etkilenmez.
        Assert.True((await duzen.AboneOlAsync("baska@ornek.com", ip: "203.0.113.9")).IsSuccess);
    }

    [Fact(DisplayName = "0018/AC-13: Biçimi geçersiz istekler geçerli istek bütçesini tüketmez: geçerli eşik kadar geçersiz istekten sonra gelen geçerli istek kabul edilir (D9)")]
    public async Task TuketmezButceyi_GecersizIstekler()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        for (int sira = 0; sira < duzen.Secenekler.ValidRequestLimit; sira++)
        {
            Assert.True((await duzen.AboneOlAsync("ali@")).IsFailure);
            Assert.True((await duzen.AboneOlAsync("ali@ornek.com", "de")).IsFailure);
        }

        Assert.True((await duzen.AboneOlAsync("ali@ornek.com")).IsSuccess);
    }

    [Fact(DisplayName = "0018/AC-15: İstek yapan IP hiçbir kalıcı kayıtta ve abonelik yolunun hiçbir log çağrısında yer almaz")]
    public async Task YerAlmaz_IpKaliciKayitta()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        await duzen.AboneOlAsync("ali@ornek.com");
        await duzen.AboneOlAsync("ali@ornek.com");
        await duzen.AboneOlAsync("veli@ornek.com");

        List<string> satirlar = await duzen.TumSatirlarAsync();

        Assert.NotEmpty(satirlar);
        Assert.All(satirlar, satir => Assert.DoesNotContain(AbonelikDuzeni.Ip, satir, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0018/AC-17: Yeni bir adres için istek tek bir **teyit bekliyor** kaydı, tek bir **istek** izin olayı oluşturur ve tek bir teyit iletisini isteğin dilinde kuyruğa koyar")]
    public async Task Olusturur_TekKayitTekOlayTekIleti()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        Assert.True((await duzen.AboneOlAsync("ali@ornek.com", "ar")).IsSuccess);

        NewsletterSubscription abonelik = await duzen.AbonelikAsync("ali@ornek.com");
        NotificationMessage ileti = Assert.Single(await duzen.TeyitIletileriAsync());
        ConsentEvent olay = Assert.Single(await duzen.OlaylarAsync(abonelik.Id));

        Assert.Equal(1, await duzen.AbonelikSayisiAsync());
        Assert.Equal(SubscriptionStatus.PendingConfirmation, abonelik.Status);
        Assert.Equal("ar", abonelik.Locale);
        Assert.Equal(ConsentEventKind.Requested, olay.Kind);
        Assert.Equal(saat.UtcNow, olay.OccurredAt);
        Assert.Equal(abonelik.Id, ileti.SubscriptionId);
        Assert.Null(ileti.LeadId);
        Assert.Null(ileti.PeriodId);
        Assert.Equal("ar", ileti.Locale);
        Assert.Equal(NotificationStatus.Queued, ileti.Status);
    }

    [Fact(DisplayName = "0018/AC-18: Etkin bir adres için istek yeni kayıt oluşturmaz, ileti kuyruğa koymaz; kaydın durumu, dili ve teyit zamanı değişmez")]
    public async Task Degistirmez_EtkinKaydi()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        await duzen.AboneOlAsync("ali@ornek.com", "tr");
        Assert.True((await duzen.TeyitEtAsync(await duzen.AnahtarVerAsync("ali@ornek.com"))).IsSuccess);
        NewsletterSubscription once = await duzen.AbonelikAsync("ali@ornek.com");

        saat.Advance(TimeSpan.FromHours(1));
        Assert.True((await duzen.AboneOlAsync("ali@ornek.com", "en")).IsSuccess);

        NewsletterSubscription sonra = await duzen.AbonelikAsync("ali@ornek.com");

        Assert.Equal(1, await duzen.AbonelikSayisiAsync());
        Assert.Single(await duzen.TeyitIletileriAsync());
        Assert.Equal(SubscriptionStatus.Active, sonra.Status);
        Assert.Equal("tr", sonra.Locale);
        Assert.Equal(once.ConfirmedAt, sonra.ConfirmedAt);
    }

    [Fact(DisplayName = "0018/AC-19: Teyit bekleyen bir adres için başka bir dilden gelen istek yeni kayıt oluşturmaz, kaydın dilini günceller ve yeni dilde bir teyit iletisi kuyruğa koyar")]
    public async Task Gunceller_DiliTeyitBekleyenKayitta()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        await duzen.AboneOlAsync("ali@ornek.com", "tr");
        await duzen.AboneOlAsync("ali@ornek.com", "ar");

        List<NotificationMessage> iletiler = await duzen.TeyitIletileriAsync();

        Assert.Equal(1, await duzen.AbonelikSayisiAsync());
        Assert.Equal("ar", (await duzen.AbonelikAsync("ali@ornek.com")).Locale);
        Assert.Equal(["tr", "ar"], iletiler.Select(item => item.Locale));
    }

    [Fact(DisplayName = "0018/AC-20: Çıkmış bir adres için istek yeni kayıt oluşturmaz, kaydı **teyit bekliyor**'a döndürür ve teyit iletisi kuyruğa koyar; önceki **teyit** ve **çıkış** izin olayları zamanlarıyla olay kaydında kalır; teyit edilmeden gelen dönemde o adrese bülten kuyruğa girmez")]
    public async Task Dondurur_CikmisKaydiTeyitBekliyora()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        await duzen.AboneOlAsync("ali@ornek.com");
        await duzen.TeyitEtAsync(await duzen.AnahtarVerAsync("ali@ornek.com"));
        NewsletterSubscription abonelik = await duzen.AbonelikAsync("ali@ornek.com");
        DateTimeOffset teyit = abonelik.ConfirmedAt!.Value;

        saat.Advance(TimeSpan.FromHours(1));
        await duzen.CikAsync(duzen.Cikis.Create(abonelik.Id));
        DateTimeOffset cikis = saat.UtcNow;

        saat.Advance(TimeSpan.FromDays(2));
        Assert.True((await duzen.AboneOlAsync("ali@ornek.com")).IsSuccess);

        NewsletterSubscription sonra = await duzen.AbonelikAsync("ali@ornek.com");
        List<ConsentEvent> olaylar = await duzen.OlaylarAsync(abonelik.Id);

        Assert.Equal(1, await duzen.AbonelikSayisiAsync());
        Assert.Equal(abonelik.Id, sonra.Id);
        Assert.Equal(SubscriptionStatus.PendingConfirmation, sonra.Status);
        Assert.Equal(2, (await duzen.TeyitIletileriAsync()).Count);
        Assert.Contains(olaylar, olay => olay.Kind == ConsentEventKind.Confirmed && olay.OccurredAt == teyit);
        Assert.Contains(olaylar, olay => olay.Kind == ConsentEventKind.Unsubscribed && olay.OccurredAt == cikis);
    }

    [Fact(DisplayName = "0018/AC-21: `Ali@Ornek.com ` (sonda boşlukla) ile `ali@ornek.com` aynı adres sayılır: ikinci istek yeni kayıt oluşturmaz (D10)")]
    public async Task AyniAdresSayar_HarfVeBosluk()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        Assert.True((await duzen.AboneOlAsync("Ali@Ornek.com ")).IsSuccess);
        Assert.True((await duzen.AboneOlAsync("ali@ornek.com")).IsSuccess);

        NewsletterSubscription abonelik = await duzen.AbonelikAsync("ali@ornek.com");

        Assert.Equal(1, await duzen.AbonelikSayisiAsync());
        Assert.Equal("Ali@Ornek.com", abonelik.Email);
        Assert.Equal("ali@ornek.com", abonelik.NormalizedEmail);
    }

    [Fact(DisplayName = "0018/AC-22: Kaydı olmayan bir adrese, harf büyüklüğü farklı iki istek bir bariyerle **aynı anda** başlatıldığında tek abonelik kaydı oluşur (D10)")]
    public async Task Olusur_TekKayit_EsZamanliIkiIstek()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        // Bariyer KAYIT anında: ikisi de okumasını yapmış ("kayıt yok") ve yazmaya gelmişken
        // birlikte bırakılır. Bariyersiz test ardışık koşar ve yarışı göstermez.
        KayitBariyeri bariyer = new(2);

        Result[] sonuclar = await Task.WhenAll(
            Task.Run(() => duzen.AboneOlAsync("Veli@Ornek.com", "tr", AbonelikDuzeni.Ip, bariyer)),
            Task.Run(() => duzen.AboneOlAsync("veli@ornek.com", "en", AbonelikDuzeni.Ip, bariyer)));

        Assert.All(sonuclar, sonuc => Assert.True(sonuc.IsSuccess));
        Assert.Equal(1, await duzen.AbonelikSayisiAsync());
    }

    [Fact(DisplayName = "0018/AC-23: Aynı adres için adres penceresindeki dördüncü istek ilk üçüyle aynı cevabı alır; teyit iletisi kuyruğa girmez ve kayıtta hiçbir şey değişmez — teyit anahtarı özeti, dil, durum ve izin olayları aynı kalır; üçüncü iletinin bağlantısı aboneliği hâlâ etkinleştirir (D9)")]
    public async Task Degistirmez_AdresSiniriAsilinca()
    {
        await database.ResetNewsletterAsync();

        // AC-98 — yapılandırılan sınır (2), spec'in önerisi (3) değil; bütçe kaynak sınırını
        // aşmasın diye geçerli istek eşiği yükseltildi.
        AbonelikDuzeni duzen = new(database, saat, Bulten.BultenSecenekleri(Bulten.BultenKurulumu(gecerliIstek: 50, adresSiniri: 2)));
        int sinir = duzen.Secenekler.AddressConfirmationLimit;

        for (int sira = 0; sira < sinir; sira++)
        {
            Assert.True((await duzen.AboneOlAsync("ali@ornek.com", "tr")).IsSuccess);
        }

        string sonAnahtar = await duzen.AnahtarVerAsync("ali@ornek.com");
        NewsletterSubscription once = await duzen.AbonelikAsync("ali@ornek.com");
        int olaySayisi = (await duzen.OlaylarAsync(once.Id)).Count;

        Result fazla = await duzen.AboneOlAsync("ali@ornek.com", "ar");

        NewsletterSubscription sonra = await duzen.AbonelikAsync("ali@ornek.com");

        Assert.True(fazla.IsSuccess);
        Assert.Equal(sinir, (await duzen.TeyitIletileriAsync()).Count);
        Assert.Equal(once.ConfirmationTokenHash, sonra.ConfirmationTokenHash);
        Assert.Equal("tr", sonra.Locale);
        Assert.Equal(SubscriptionStatus.PendingConfirmation, sonra.Status);
        Assert.Equal(olaySayisi, (await duzen.OlaylarAsync(once.Id)).Count);

        Assert.True((await duzen.TeyitEtAsync(sonAnahtar)).IsSuccess);

        // Pencere geçince sınır kalkar.
        saat.Advance(duzen.Secenekler.AddressWindow + TimeSpan.FromMinutes(1));
        await duzen.CikAsync(duzen.Cikis.Create(once.Id));
        Assert.True((await duzen.AboneOlAsync("ali@ornek.com")).IsSuccess);
        Assert.Equal(sinir + 1, (await duzen.TeyitIletileriAsync()).Count);
    }

    [Fact(DisplayName = "0018/AC-93: İzin olay kaydı e-posta adresini hiçbir alanda taşımaz; bir istek → teyit → çıkış → istek → teyit dizisinden sonra beş olayın beşi de zamanlarıyla durur")]
    public async Task Durur_BesOlayinBesi()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);
        List<(ConsentEventKind Tur, DateTimeOffset An)> beklenen = [];

        await duzen.AboneOlAsync("ali@ornek.com");
        beklenen.Add((ConsentEventKind.Requested, saat.UtcNow));
        saat.Advance(TimeSpan.FromMinutes(5));

        await duzen.TeyitEtAsync(await duzen.AnahtarVerAsync("ali@ornek.com"));
        beklenen.Add((ConsentEventKind.Confirmed, saat.UtcNow));
        Guid id = (await duzen.AbonelikAsync("ali@ornek.com")).Id;
        saat.Advance(TimeSpan.FromDays(3));

        await duzen.CikAsync(duzen.Cikis.Create(id));
        beklenen.Add((ConsentEventKind.Unsubscribed, saat.UtcNow));
        saat.Advance(TimeSpan.FromDays(3));

        await duzen.AboneOlAsync("ali@ornek.com");
        beklenen.Add((ConsentEventKind.Requested, saat.UtcNow));
        saat.Advance(TimeSpan.FromMinutes(5));

        await duzen.TeyitEtAsync(await duzen.AnahtarVerAsync("ali@ornek.com"));
        beklenen.Add((ConsentEventKind.Confirmed, saat.UtcNow));

        List<ConsentEvent> olaylar = await duzen.OlaylarAsync(id);

        Assert.Equal(beklenen, olaylar.Select(olay => (olay.Kind, olay.OccurredAt)));

        await using NotificationsDbContext context = database.CreateContext();
        List<string> olaySatirlari = await context.Database
            .SqlQuery<string>($"""SELECT t::text AS "Value" FROM notifications.consent_events t""")
            .ToListAsync();

        Assert.Equal(5, olaySatirlari.Count);
        Assert.All(olaySatirlari, satir => Assert.DoesNotContain("ornek.com", satir, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ReddedilirAsync(string? adres)
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat);

        Result sonuc = await duzen.AboneOlAsync(adres);

        Assert.Equal(NotificationErrors.SubscriptionEmailInvalid, sonuc.Error.Code);
        Assert.Equal("email", sonuc.Error.Parameters!["field"]);
        Assert.Equal(0, await duzen.AbonelikSayisiAsync());
        Assert.Empty(await duzen.TeyitIletileriAsync());
    }

    /// <summary>Yerel kısım 64, etiketler 63'ü aşmaz: geçerli bir adresin tam olarak <paramref name="uzunluk"/> karakteri.</summary>
    private static string UzunAdres(int uzunluk)
    {
        string yerel = new('a', 64);
        string alan = string.Empty;
        int kalan = uzunluk - yerel.Length - 1 - ".com".Length;

        while (kalan > 0)
        {
            int etiket = Math.Min(63, kalan);

            if (kalan - etiket == 1)
            {
                etiket--;
            }

            alan += new string('b', etiket);
            kalan -= etiket;

            if (kalan > 0)
            {
                alan += ".";
                kalan--;
            }
        }

        return $"{yerel}@{alan}.com";
    }
}
