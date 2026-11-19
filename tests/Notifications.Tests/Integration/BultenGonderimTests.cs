using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Application.Newsletter;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `0018` R3 · R5 · R10 · R16 · R17 · R20 · **D11** · **D21** · **D22** · **D24** · **D25** —
/// teyit iletisinin ve bültenin GÖVDESİ ve dağıtımı. Kuyruk `0009`'undur: iletiler aynı
/// dağıtıcıdan, aynı tekrar deneme ve hata sınıflarıyla geçer.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed partial class BultenGonderimTests(NotificationsDatabase database)
{
    private const string Taban = "https://ornek.com";

    [Fact(DisplayName = "0018/AC-26: Teyit iletisi, abonenin dilindeki teyit sayfasını açan mutlak bir bağlantı ve galerinin görünen adını taşır; araç, fiyat ya da başka bir tanıtım içeriği taşımaz")]
    public async Task Tasir_MutlakBaglantiVeGaleriAdi_TanitimYok()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.YeniIlanAsync();
        await d.Abonelik.AboneOlAsync("ali@ornek.com", "en");
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        OutboundMessage ileti = tasiyici.Alici("ali@ornek.com");

        Assert.Matches(TeyitBaglantisi("en"), ileti.Body);
        Assert.Contains("Yildiz Motors", ileti.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Corolla", ileti.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("TRY", ileti.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("/cars", ileti.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("unsubscribe", ileti.Body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0018/AC-48: Teyit bağlantısındaki anahtar veritabanının hiçbir alanında düz olarak bulunmaz; aboneliğin kaydında yalnız anahtarın SHA-256 özeti durur")]
    public async Task Bulunmaz_DuzAnahtar_Veritabaninda()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.Abonelik.AboneOlAsync("ali@ornek.com");
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        string anahtar = AnahtarOf(tasiyici.Alici("ali@ornek.com").Body, "tr");
        byte[] ozet = SHA256.HashData(Base64Url.DecodeFromChars(anahtar));
        NewsletterSubscription abonelik = await d.Abonelik.AbonelikAsync("ali@ornek.com");

        Assert.Equal(ozet, abonelik.ConfirmationTokenHash);
        Assert.All(await d.Abonelik.TumSatirlarAsync(), satir => Assert.DoesNotContain(anahtar, satir, StringComparison.Ordinal));
        Assert.True((await d.Abonelik.TeyitEtAsync(anahtar)).IsSuccess);
    }

    [Fact(DisplayName = "0018/AC-49: Teyit anahtarı kodu çözüldüğünde 32 bayttır ve aynı adres için ardışık iki teyit iletisi farklı anahtar taşır")]
    public async Task OtuzIkiBayt_VeFarkli_ArdisikIkiAnahtar()
    {
        DonemDuzeni d = await HazirlaAsync();
        KayitTasiyici tasiyici = new();

        await d.Abonelik.AboneOlAsync("ali@ornek.com");
        await d.DagitAsync(tasiyici);
        await d.Abonelik.AboneOlAsync("ali@ornek.com");
        await d.DagitAsync(tasiyici);

        List<string> anahtarlar = [.. tasiyici.Gonderilenler.Select(ileti => AnahtarOf(ileti.Body, "tr"))];

        Assert.Equal(2, anahtarlar.Count);
        Assert.All(anahtarlar, anahtar => Assert.Equal(32, Base64Url.DecodeFromChars(anahtar).Length));
        Assert.NotEqual(anahtarlar[0], anahtarlar[1]);
    }

    /// <summary>
    /// **D28** (Takım Yöneticisi kararı) — anahtar gönderim kaydından türetilir: geçici hatayla
    /// yeniden denenen teyit iletisinin iki teslimi AYNI bağlantıyı taşır. Taşıyıcı ilk denemede
    /// iletiyi teslim edip geçici hata döndürüyor (bağlantı kopması sonrası sağlayıcı kabul
    /// etmişti); o ilk teslimin bağlantısı yeniden denemeden sonra da çalışmalı.
    /// </summary>
    [Fact(DisplayName = "0018/AC-48: Teyit bağlantısındaki anahtar veritabanının hiçbir alanında düz olarak bulunmaz; aboneliğin kaydında yalnız anahtarın SHA-256 özeti durur")]
    public async Task AyniBaglanti_YenidenDenenenTeyitIletisinde()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.Abonelik.AboneOlAsync("ali@ornek.com");

        List<OutboundMessage> teslimler = [];
        KayitTasiyici tasiyici = new()
        {
            Kural = ileti =>
            {
                teslimler.Add(ileti);
                return teslimler.Count == 1 ? SendResult.Transient("baglanti koptu") : SendResult.Success();
            },
        };

        await d.DagitAsync(tasiyici);
        NotificationMessage bekleyen = Assert.Single(await d.IletilerAsync());
        Assert.Equal(NotificationStatus.Queued, bekleyen.Status);

        d.Saat.Set(bekleyen.NextAttemptAt!.Value);
        await d.DagitAsync(tasiyici);

        Assert.Equal(2, teslimler.Count);
        string ilk = AnahtarOf(teslimler[0].Body, "tr");
        string ikinci = AnahtarOf(teslimler[1].Body, "tr");

        Assert.Equal(ilk, ikinci);
        Assert.Equal(NotificationStatus.Sent, Assert.Single(await d.IletilerAsync()).Status);
        Assert.Equal(SHA256.HashData(Base64Url.DecodeFromChars(ilk)), (await d.Abonelik.AbonelikAsync("ali@ornek.com")).ConfirmationTokenHash);
        Assert.All(await d.Abonelik.TumSatirlarAsync(), satir => Assert.DoesNotContain(ilk, satir, StringComparison.Ordinal));

        // İlk teslimin bağlantısı geçerli; ilk tıklamadan sonra ikisi birden kullanılmıştır.
        Assert.True((await d.Abonelik.TeyitEtAsync(ilk)).IsSuccess);
        Assert.True((await d.Abonelik.TeyitEtAsync(ikinci)).IsFailure);
    }

    /// <summary>
    /// **D7** kayıt düzeyinde: yeni istek yeni gönderim kaydı demektir; önceki kayıt henüz
    /// dağıtılmadıysa hiç gönderilmez — taşıyacağı bağlantı zaten geçersizdir.
    /// </summary>
    [Fact(DisplayName = "0018/AC-34: Teyit bekleyen adrese yeni teyit iletisi gittikten sonra önceki iletinin bağlantısı aboneliği etkinleştirmez")]
    public async Task Gonderilmez_YerineYenisiGelenTeyitKaydi()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.Abonelik.AboneOlAsync("ali@ornek.com");
        await d.Abonelik.AboneOlAsync("ali@ornek.com", "en");
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        OutboundMessage gonderilen = Assert.Single(tasiyici.Gonderilenler);
        List<NotificationMessage> kayitlar = await d.IletilerAsync();

        Assert.Matches(TeyitBaglantisi("en"), gonderilen.Body);
        Assert.Equal(NotificationErrors.SubscriptionInactive, kayitlar[0].FailureCode);
        Assert.Equal(NotificationStatus.Sent, kayitlar[1].Status);
        Assert.True((await d.Abonelik.TeyitEtAsync(AnahtarOf(gonderilen.Body, "en"))).IsSuccess);
    }

    [Fact(DisplayName = "0018/AC-34: Teyit bekleyen adrese yeni teyit iletisi gittikten sonra önceki iletinin bağlantısı aboneliği etkinleştirmez")]
    public async Task Etkinlestirmez_OncekiIletininBaglantisi_Dagitimla()
    {
        DonemDuzeni d = await HazirlaAsync();
        KayitTasiyici tasiyici = new();

        await d.Abonelik.AboneOlAsync("ali@ornek.com");
        await d.DagitAsync(tasiyici);
        await d.Abonelik.AboneOlAsync("ali@ornek.com");
        await d.DagitAsync(tasiyici);

        Assert.True((await d.Abonelik.TeyitEtAsync(AnahtarOf(tasiyici.Gonderilenler[0].Body, "tr"))).IsFailure);
        Assert.True((await d.Abonelik.TeyitEtAsync(AnahtarOf(tasiyici.Gonderilenler[1].Body, "tr"))).IsSuccess);
    }

    [Fact(DisplayName = "0018/AC-51: Teyit bağlantısı `{PublicBaseUrl}/{locale}/newsletter/confirm?token=…`, çıkış bağlantısı `{PublicBaseUrl}/{locale}/newsletter/unsubscribe?token=…` kalıbındadır ve ön yüz iki yolu da ilgili sayfayla açar")]
    public async Task Kalibinda_IkiBaglanti()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com", "ar");
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici tasiyici = new();

        await d.Abonelik.AboneOlAsync("veli@ornek.com", "ar");
        await d.DagitAsync(tasiyici);

        Assert.Matches(TeyitBaglantisi("ar"), tasiyici.Alici("veli@ornek.com").Body);
        Assert.Matches(CikisBaglantisi("ar"), tasiyici.Alici("ali@ornek.com").Body);
    }

    [Fact(DisplayName = "0018/AC-37: Her bülten iletisi aboneye özgü, mutlak bir çıkış bağlantısı taşır")]
    public async Task Tasir_AboneyeOzguCikisBaglantisi()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.EtkinAboneAsync("veli@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        foreach (string adres in new[] { "ali@ornek.com", "veli@ornek.com" })
        {
            string anahtar = CikisBaglantisi("tr").Match(tasiyici.Alici(adres).Body).Groups["anahtar"].Value;

            Assert.True(d.Abonelik.Cikis.TryVerify(Uri.UnescapeDataString(anahtar), out Guid id));
            Assert.Equal(await d.AbonelikIdOf(adres), id);
        }
    }

    [Fact(DisplayName = "0018/AC-46: Dönem işlendikten sonra geçici hatayla tekrar denemeye düşmüş bir bülten, abone çıktıktan sonraki denemede gönderilmez ve kalıcı başarısız işaretlenir (D22)")]
    public async Task Gonderilmez_CiktiktanSonrakiDeneme()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();

        await d.DagitAsync(new KayitTasiyici { Kural = _ => SendResult.Transient("x") });
        NotificationMessage bekleyen = Assert.Single(await d.BultenlerAsync());
        Assert.Equal(NotificationStatus.Queued, bekleyen.Status);

        await d.Abonelik.CikAsync(d.Abonelik.Cikis.Create(await d.AbonelikIdOf("ali@ornek.com")));
        d.Saat.Set(bekleyen.NextAttemptAt!.Value);
        KayitTasiyici ikinci = new();
        await d.DagitAsync(ikinci);

        NotificationMessage son = Assert.Single(await d.BultenlerAsync());

        Assert.Empty(ikinci.Gonderilenler);
        Assert.Equal(NotificationStatus.Failed, son.Status);
        Assert.Equal(NotificationErrors.SubscriptionInactive, son.FailureCode);
        Assert.Null(son.NextAttemptAt);
    }

    [Fact(DisplayName = "0018/AC-65: Dönem işlendikten sonra, iletiler dağıtılmadan önce dönemin bütün ilanları satılırsa ileti gönderilmez ve kalıcı başarısız işaretlenir (D21)")]
    public async Task Gonderilmez_ButunIlanlarSatilinca()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        Guid a = await d.YeniIlanAsync();
        Guid b = await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();

        d.Ilanlar.YayindanCikar(a);
        d.Ilanlar.YayindanCikar(b);
        KayitTasiyici tasiyici = new();
        await d.DagitAsync(tasiyici);

        NotificationMessage bulten = Assert.Single(await d.BultenlerAsync());

        Assert.Empty(tasiyici.Gonderilenler);
        Assert.Equal(NotificationStatus.Failed, bulten.Status);
        Assert.Equal(NotificationErrors.NewsletterEmpty, bulten.FailureCode);
        Assert.Equal(1, bulten.AttemptCount);
    }

    [Fact(DisplayName = "0018/AC-66: Dönemin iki ilanından biri dağıtımdan önce satılırsa ileti yalnız Yayında kalan ilanı taşır; dağıtımdan önce fiyatı değişen ilanın satırı yeni fiyatı taşır (D21)")]
    public async Task Tasir_YalnizKalaniVeYeniFiyati()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        Guid satilan = Guid.CreateVersion7();
        Guid kalan = Guid.CreateVersion7();
        d.Ilanlar.Yayinla(satilan, d.Saat.UtcNow, slug: "satilan-2019-aaa111");
        d.Ilanlar.Yayinla(kalan, d.Saat.UtcNow, fiyat: 1_000_000m, slug: "kalan-2019-bbb222");
        await d.DuyurAsync(satilan, d.Saat.UtcNow);
        await d.DuyurAsync(kalan, d.Saat.UtcNow);
        d.DonemeIlerle();
        await d.IsleAsync();

        d.Ilanlar.YayindanCikar(satilan);
        d.Ilanlar.FiyatDegistir(kalan, 950_000m);
        KayitTasiyici tasiyici = new();
        await d.DagitAsync(tasiyici);

        string govde = tasiyici.Alici("ali@ornek.com").Body;

        Assert.DoesNotContain("satilan-2019-aaa111", govde, StringComparison.Ordinal);
        Assert.Contains("kalan-2019-bbb222", govde, StringComparison.Ordinal);
        Assert.Contains("₺950.000,00", govde, StringComparison.Ordinal);
        Assert.DoesNotContain("₺1.000.000,00", govde, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0018/AC-74: Bültendeki her araç satırı model adını abonenin dilinde, model yılını, km'yi, fiyatı, şube adını ve ilanın abonenin dilindeki mutlak bağlantısını taşır")]
    public async Task Tasir_SatirinAltiAlani()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com", "ar");
        Guid ilan = Guid.CreateVersion7();
        d.Ilanlar.Yayinla(ilan, d.Saat.UtcNow, yil: 2021, km: 48_500, slug: "toyota-corolla-2021-ccc333");
        await d.DuyurAsync(ilan, d.Saat.UtcNow);
        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        string govde = tasiyici.Alici("ali@ornek.com").Body;

        Assert.Contains("تويوتا كورولا", govde, StringComparison.Ordinal);
        Assert.Contains("2021", govde, StringComparison.Ordinal);
        Assert.Contains("48,500 كم", govde, StringComparison.Ordinal);
        Assert.Contains("‏1,250,000.00 TRY", govde, StringComparison.Ordinal);
        Assert.Contains("فرع كاديكوي", govde, StringComparison.Ordinal);
        Assert.Contains($"{Taban}/ar/cars/toyota-corolla-2021-ccc333", govde, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0018/AC-75: Fiyatı \"sorunuz\" olan ilanın satırı tutar yerine şablon kataloğundaki \"Fiyat sorunuz\" karşılığını taşır")]
    public async Task Tasir_FiyatSorunuz()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.EtkinAboneAsync("john@ornek.com", "en");
        Guid ilan = Guid.CreateVersion7();
        d.Ilanlar.Yayinla(ilan, d.Saat.UtcNow, fiyat: null);
        await d.DuyurAsync(ilan, d.Saat.UtcNow);
        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        Assert.Contains("· Fiyat sorunuz", tasiyici.Alici("ali@ornek.com").Body, StringComparison.Ordinal);
        Assert.Contains("· Price on request", tasiyici.Alici("john@ornek.com").Body, StringComparison.Ordinal);
        Assert.DoesNotContain("0,00", tasiyici.Alici("ali@ornek.com").Body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0018/AC-76: KWD kurulumunda `12500.750` tutarlı ilanın fiyatı üç ondalık basamakla ve para birimiyle yazılır; son basamak (`0`) düşmez")]
    public async Task Yazilir_KwdUcOndalikla()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.EtkinAboneAsync("john@ornek.com", "en");
        await d.EtkinAboneAsync("ahmed@ornek.com", "ar");
        Guid ilan = Guid.CreateVersion7();
        d.Ilanlar.Yayinla(ilan, d.Saat.UtcNow, fiyat: 12500.750m, paraBirimi: "KWD", us: 3);
        await d.DuyurAsync(ilan, d.Saat.UtcNow);
        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        Assert.Contains("KWD 12.500,750", tasiyici.Alici("ali@ornek.com").Body, StringComparison.Ordinal);
        Assert.Contains("KWD 12,500.750", tasiyici.Alici("john@ornek.com").Body, StringComparison.Ordinal);
        Assert.Contains("‏12,500.750 د.ك.‏", tasiyici.Alici("ahmed@ornek.com").Body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0018/AC-78: 23 aday ilanlı bir dönemde bülten ilk yayını en yeni 20 ilanı sırayla taşır, ardından sayı içermeyen \"Diğer yeni araçlar için vitrine göz atın\" cümlesini ve vitrin listesinin bağlantısını taşır; 20 ya da daha az adayda bu cümle yoktur")]
    public async Task Tasir_EnYeniYirmiVeCumle()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        List<string> sluglar = [];

        for (int sira = 0; sira < 23; sira++)
        {
            Guid ilan = Guid.CreateVersion7();
            string slug = $"ilan-{sira:00}-2019-abc{sira:000}";
            d.Ilanlar.Yayinla(ilan, d.Saat.UtcNow + TimeSpan.FromMinutes(sira), slug: slug);
            await d.DuyurAsync(ilan, d.Saat.UtcNow + TimeSpan.FromMinutes(sira));
            sluglar.Add(slug);
        }

        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici tasiyici = new();
        await d.DagitAsync(tasiyici);

        string govde = tasiyici.Alici("ali@ornek.com").Body;
        List<string> gorulen = [.. IlanBaglantisi().Matches(govde).Select(eslesme => eslesme.Groups["slug"].Value)];

        // İlk yayını en yeni önce: 22, 21, … 3. En eski üçü (0, 1, 2) yok.
        Assert.Equal(Enumerable.Range(3, 20).Reverse().Select(sira => sluglar[sira]), gorulen);
        Assert.Contains($"Diğer yeni araçlar için vitrine göz atın: {Taban}/tr/cars", govde, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"Diğer yeni araçlar[^\n]*\d", govde.Split('\n').Single(satir => satir.StartsWith("Diğer", StringComparison.Ordinal)).Replace(Taban + "/tr/cars", string.Empty, StringComparison.Ordinal));
        Assert.True(govde.IndexOf("Diğer yeni araçlar", StringComparison.Ordinal) > govde.IndexOf(sluglar[3], StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0018/AC-78: 23 aday ilanlı bir dönemde bülten ilk yayını en yeni 20 ilanı sırayla taşır, ardından sayı içermeyen \"Diğer yeni araçlar için vitrine göz atın\" cümlesini ve vitrin listesinin bağlantısını taşır; 20 ya da daha az adayda bu cümle yoktur")]
    public async Task Yoktur_Cumle_YirmiAdayda()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");

        for (int sira = 0; sira < 20; sira++)
        {
            await d.YeniIlanAsync(d.Saat.UtcNow + TimeSpan.FromMinutes(sira));
        }

        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici tasiyici = new();
        await d.DagitAsync(tasiyici);

        string govde = tasiyici.Alici("ali@ornek.com").Body;

        Assert.Equal(20, IlanBaglantisi().Matches(govde).Count);
        Assert.DoesNotContain("Diğer yeni araçlar", govde, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0018/AC-79: Bülten galerinin abonenin dilindeki görünen adını taşır")]
    public async Task Tasir_GaleriAdi_AboneninDilinde()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.EtkinAboneAsync("ahmed@ornek.com", "ar");
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        Assert.Contains("Yıldız Otomotiv", tasiyici.Alici("ali@ornek.com").Body, StringComparison.Ordinal);
        Assert.Contains("يلدز للسيارات", tasiyici.Alici("ahmed@ornek.com").Body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0018/AC-80: Arapça abone olunmuş adrese teyit iletisi ve bülten Arapça şablonlarla gider")]
    public async Task Gider_ArapcaSablonlarla()
    {
        DonemDuzeni d = await HazirlaAsync();
        KayitTasiyici teyit = new();
        await d.Abonelik.AboneOlAsync("ahmed@ornek.com", "ar");
        await d.DagitAsync(teyit);
        await d.Abonelik.TeyitEtAsync(AnahtarOf(teyit.Alici("ahmed@ornek.com").Body, "ar"));
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();
        KayitTasiyici bulten = new();

        await d.DagitAsync(bulten);

        Assert.Equal("أكّد اشتراكك في النشرة", teyit.Alici("ahmed@ornek.com").Subject);
        Assert.Equal("سيارات وصلت حديثًا إلى المعرض", bulten.Alici("ahmed@ornek.com").Subject);
        Assert.Contains("لإلغاء الاشتراك بنقرة واحدة", bulten.Alici("ahmed@ornek.com").Body, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0018/AC-81: Abonenin dilinde şablon yoksa ileti varsayılan dilde gider ve düşüş loglanır")]
    public async Task Gider_VarsayilanDilde_SablonYoksa()
    {
        // `fr` kurulumda etkin ama şablon kataloğunda yok.
        DonemDuzeni d = await HazirlaAsync(Bulten.BultenSecenekleri(Bulten.BultenKurulumu(gecerliIstek: 500, adresSiniri: 50, diller: ["tr", "ar", "en", "fr"])));
        await d.Abonelik.AboneOlAsync("jean@ornek.com", "fr");
        KayitTasiyici tasiyici = new();
        YakalayanLog<TemplateResolver> log = new();

        await d.DagitAsync(tasiyici, log);

        OutboundMessage ileti = tasiyici.Alici("jean@ornek.com");

        Assert.Equal("Bülten aboneliğinizi onaylayın", ileti.Subject);
        Assert.Matches(TeyitBaglantisi("tr"), ileti.Body);
        Assert.Contains(log.Kayitlar, kayit => kayit.Seviye == LogLevel.Warning && kayit.Metin.Contains("newsletter.confirmation", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0018/AC-86: Bülten iletisinde geçici hata yapılandırılan sayıda, artan aralıkla yeniden denenir; kalıcı hata yeniden denenmez")]
    public async Task Denenir_GeciciHataArtanAralikla_KaliciDenenmez()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("gecici@ornek.com");
        await d.EtkinAboneAsync("kalici@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();

        KayitTasiyici tasiyici = new()
        {
            Kural = ileti => ileti.Recipient == "kalici@ornek.com" ? SendResult.Permanent("red") : SendResult.Transient("x"),
        };

        List<TimeSpan> araliklar = [];

        // Yapılandırılan deneme sayısı 3 (`Bulten.Secenekler`).
        for (int deneme = 0; deneme < 3; deneme++)
        {
            DateTimeOffset simdi = d.Saat.UtcNow;
            await d.DagitAsync(tasiyici);
            NotificationMessage gecici = (await d.BultenlerAsync("gecici@ornek.com")).Single();

            if (gecici.NextAttemptAt is DateTimeOffset sonraki)
            {
                araliklar.Add(sonraki - simdi);
                d.Saat.Set(sonraki);
            }
        }

        NotificationMessage geciciSon = (await d.BultenlerAsync("gecici@ornek.com")).Single();
        NotificationMessage kaliciSon = (await d.BultenlerAsync("kalici@ornek.com")).Single();

        Assert.Equal(2, araliklar.Count);
        Assert.True(araliklar[1] > araliklar[0], "aralık artmadı");
        Assert.Equal(NotificationStatus.Failed, geciciSon.Status);
        Assert.Equal(3, geciciSon.AttemptCount);
        Assert.Equal(NotificationStatus.Failed, kaliciSon.Status);
        Assert.Equal(1, kaliciSon.AttemptCount);
    }

    [Fact(DisplayName = "0018/AC-87: Bir abonenin iletisinin başarısız olması aynı dönemdeki diğer abonelerin iletilerini engellemez")]
    public async Task Engellemez_BirAboneninHatasi_Digerlerini()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("bozuk@ornek.com");
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.EtkinAboneAsync("veli@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();

        KayitTasiyici tasiyici = new()
        {
            Kural = ileti => ileti.Recipient == "bozuk@ornek.com"
                ? throw new InvalidOperationException("taşıyıcı sözleşmesini ihlal etti")
                : SendResult.Success(),
        };

        await d.DagitAsync(tasiyici);

        Assert.Equal(2, tasiyici.Gonderilenler.Count);
        Assert.Equal(NotificationStatus.Sent, (await d.BultenlerAsync("ali@ornek.com")).Single().Status);
        Assert.Equal(NotificationStatus.Sent, (await d.BultenlerAsync("veli@ornek.com")).Single().Status);
        Assert.Equal(1, (await d.BultenlerAsync("bozuk@ornek.com")).Single().AttemptCount);
    }

    [Fact(DisplayName = "0018/AC-89: Teyit ve bülten iletilerinin gönderim geçmişi kaydı alıcının e-posta adresini hiçbir alanda taşımaz")]
    public async Task Tasimaz_Adres_GonderimGecmisinde()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.EtkinAboneAsync("ali@ornek.com");
        await d.Abonelik.AboneOlAsync("veli@ornek.com");
        await d.YeniIlanAsync();
        d.DonemeIlerle();
        await d.IsleAsync();

        await d.DagitAsync(new KayitTasiyici());

        await using NotificationsDbContext context = database.CreateContext();
        List<string> satirlar = await context.Database
            .SqlQuery<string>($"""SELECT messages::text AS "Value" FROM notifications.messages""")
            .ToListAsync();

        // Üç kayıt: ali'nin teyit iletisi (ali dağıtımdan önce teyit etti — kalıcı başarısız,
        // gönderilmedi), ali'nin bülteni ve veli'nin teyit iletisi (ikisi gönderildi).
        Assert.Equal(3, satirlar.Count);
        Assert.Equal(2, (await d.IletilerAsync()).Count(ileti => ileti.Status == NotificationStatus.Sent));

        foreach (string adres in new[] { "ali@ornek.com", "veli@ornek.com" })
        {
            Assert.All(satirlar, satir => Assert.DoesNotContain(adres, satir, StringComparison.OrdinalIgnoreCase));
            Assert.All(satirlar, satir => Assert.DoesNotContain(adres.Split('@')[0], satir, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact(DisplayName = "0018/AC-91: Teyit ve çıkış bağlantıları adresi düz, URL kodlu ya da Base64 olarak içermez")]
    public async Task Icermez_Adresi_TeyitBaglantisi()
    {
        DonemDuzeni d = await HazirlaAsync();
        await d.Abonelik.AboneOlAsync("ali.veli@ornek.com");
        KayitTasiyici tasiyici = new();

        await d.DagitAsync(tasiyici);

        string baglanti = TeyitBaglantisi("tr").Match(tasiyici.Alici("ali.veli@ornek.com").Body).Value;

        Assert.NotEmpty(baglanti);

        foreach (string bicim in AdresBicimleri.Hepsi("ali.veli@ornek.com"))
        {
            Assert.DoesNotContain(bicim, baglanti, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact(DisplayName = "0018/AC-77: TRY ve KWD fiyatı, km ve model yılı için bültenin biçimi, aynı değerlerle vitrinin `formatMoney`, `formatMileage` ve `formatYear` çıktısıyla TR, AR ve EN'de bayt bayt aynıdır; Arapça'da rakam kümesi dâhil (D24)")]
    public void AyniBaytBayt_VitrininBicimiyle()
    {
        using JsonDocument fikstur = JsonDocument.Parse(File.ReadAllText(Fikstur.Value));
        int vaka = 0;

        foreach (JsonElement para in fikstur.RootElement.GetProperty("money").EnumerateArray())
        {
            decimal tutar = decimal.Parse(para.GetProperty("amount").GetString()!, System.Globalization.CultureInfo.InvariantCulture);
            string kod = para.GetProperty("currencyCode").GetString()!;
            int us = para.GetProperty("minorUnitExponent").GetInt32();

            foreach (string dil in Diller)
            {
                Assert.Equal(para.GetProperty(dil).GetString(), NewsletterFormat.Money(tutar, kod, us, dil));
                vaka++;
            }
        }

        foreach (JsonElement km in fikstur.RootElement.GetProperty("mileage").EnumerateArray())
        {
            foreach (string dil in Diller)
            {
                Assert.Equal(km.GetProperty(dil).GetString(), NewsletterFormat.Mileage(km.GetProperty("value").GetInt32(), dil));
                vaka++;
            }
        }

        foreach (JsonElement yil in fikstur.RootElement.GetProperty("year").EnumerateArray())
        {
            foreach (string dil in Diller)
            {
                Assert.Equal(yil.GetProperty(dil).GetString(), NewsletterFormat.Year(yil.GetProperty("value").GetInt32(), dil));
                vaka++;
            }
        }

        // Boş fikstürle koşan karşılaştırma yeşildir; kapı sessizce açılmasın.
        Assert.True(vaka >= 30, $"yalnız {vaka} vaka karşılaştırıldı");
        Assert.Contains("KWD", fikstur.RootElement.GetProperty("money").EnumerateArray().Select(item => item.GetProperty("currencyCode").GetString()));
    }

    private static readonly string[] Diller = ["tr", "ar", "en"];

    /// <summary>Kök YUKARI yürünerek bulunur, sabit sayıda `..` ile değil (docs/testing.md § 3).</summary>
    private static readonly Lazy<string> Fikstur = new(() =>
    {
        const string Goreceli = "frontend/src/shared/i18n/format.fixtures.json";

        for (DirectoryInfo? dizin = new(AppContext.BaseDirectory); dizin is not null; dizin = dizin.Parent)
        {
            string aday = Path.Combine(dizin.FullName, Goreceli);

            if (File.Exists(aday))
            {
                return aday;
            }
        }

        throw new FileNotFoundException($"`{Goreceli}` bulunamadı.");
    });

    private async Task<DonemDuzeni> HazirlaAsync(NewsletterOptions? secenekler = null)
    {
        await database.ResetNewsletterAsync();
        DonemDuzeni d = new(database, secenekler);
        await d.DevreyeGirAsync();
        d.Saat.Advance(TimeSpan.FromSeconds(1));

        return d;
    }

    private static string AnahtarOf(string govde, string dil) =>
        Uri.UnescapeDataString(TeyitBaglantisi(dil).Match(govde).Groups["anahtar"].Value);

    private static Regex TeyitBaglantisi(string dil) =>
        new($@"{Regex.Escape(Taban)}/{dil}/newsletter/confirm\?token=(?<anahtar>[A-Za-z0-9_%-]+)", RegexOptions.None, TimeSpan.FromSeconds(1));

    private static Regex CikisBaglantisi(string dil) =>
        new($@"{Regex.Escape(Taban)}/{dil}/newsletter/unsubscribe\?token=(?<anahtar>[A-Za-z0-9_%-]+)", RegexOptions.None, TimeSpan.FromSeconds(1));

    [GeneratedRegex(@"https://ornek\.com/tr/cars/(?<slug>[^\s]+)")]
    private static partial Regex IlanBaglantisi();
}
