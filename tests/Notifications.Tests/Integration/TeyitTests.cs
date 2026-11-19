using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Domain;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `0018` R2 · R14 · **D6** · **D7** — teyit. Sayfanın açılması teyit değildir (AC-27'nin uç
/// yarısı `frontend`'de): bu işleyici yalnız düğmeyle çağrılır ve burada onun sonuçları ölçülür.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class TeyitTests(NotificationsDatabase database)
{
    private const string Adres = "ali@ornek.com";

    private readonly Bulten.IleriSaat saat = new(DateTimeOffset.UtcNow);

    [Fact(DisplayName = "0018/AC-27: Teyit bağlantısının açılması (sayfanın yüklenmesi) aboneliği etkinleştirmez: kayıt **teyit bekliyor** kalır ve sayfa \"Aboneliği onayla\" düğmesini gösterir")]
    public async Task KalirTeyitBekliyor_AnahtarVerildiktenSonra()
    {
        AbonelikDuzeni duzen = await HazirlaAsync();

        await duzen.AnahtarVerAsync(Adres);

        Assert.Equal(SubscriptionStatus.PendingConfirmation, (await duzen.AbonelikAsync(Adres)).Status);
    }

    [Fact(DisplayName = "0018/AC-28: Düğmeye basıldığında kayıt **etkin** olur, teyit zamanı UTC olarak yazılır, bir **teyit** izin olayı eklenir ve sayfa başarı mesajını gösterir")]
    public async Task Etkinlestirir_DugmeyeBasilinca()
    {
        AbonelikDuzeni duzen = await HazirlaAsync();
        string anahtar = await duzen.AnahtarVerAsync(Adres);
        saat.Advance(TimeSpan.FromMinutes(3));

        Result sonuc = await duzen.TeyitEtAsync(anahtar);

        NewsletterSubscription abonelik = await duzen.AbonelikAsync(Adres);
        List<ConsentEvent> olaylar = await duzen.OlaylarAsync(abonelik.Id);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, abonelik.Status);
        Assert.Equal(saat.UtcNow, abonelik.ConfirmedAt);
        Assert.Equal(TimeSpan.Zero, abonelik.ConfirmedAt!.Value.Offset);
        Assert.Equal(ConsentEventKind.Confirmed, olaylar[^1].Kind);
        Assert.Equal(saat.UtcNow, olaylar[^1].OccurredAt);
    }

    [Fact(DisplayName = "0018/AC-30: Yapılandırılan süre dolduktan 1 dakika sonra kullanılan bağlantı aboneliği etkinleştirmez ve sayfa \"bağlantı artık geçerli değil\" mesajıyla yeniden abone olma yolunu gösterir")]
    public async Task Etkinlestirmez_SureDolduktanBirDakikaSonra()
    {
        AbonelikDuzeni duzen = await HazirlaAsync();
        string anahtar = await duzen.AnahtarVerAsync(Adres);

        // AC-98 — yapılandırılan süre (48 saat), spec'in önerisi (72) değil.
        saat.Advance(duzen.Secenekler.ConfirmationLinkLifetime + TimeSpan.FromMinutes(1));

        Result sonuc = await duzen.TeyitEtAsync(anahtar);

        Assert.Equal(NotificationErrors.SubscriptionLinkInvalid, sonuc.Error.Code);
        Assert.Equal(SubscriptionStatus.PendingConfirmation, (await duzen.AbonelikAsync(Adres)).Status);
    }

    [Fact(DisplayName = "0018/AC-31: Süre dolmadan 1 dakika önce kullanılan bağlantı aboneliği etkinleştirir")]
    public async Task Etkinlestirir_SureDolmadanBirDakikaOnce()
    {
        AbonelikDuzeni duzen = await HazirlaAsync();
        string anahtar = await duzen.AnahtarVerAsync(Adres);

        saat.Advance(duzen.Secenekler.ConfirmationLinkLifetime - TimeSpan.FromMinutes(1));

        Assert.True((await duzen.TeyitEtAsync(anahtar)).IsSuccess);
        Assert.Equal(SubscriptionStatus.Active, (await duzen.AbonelikAsync(Adres)).Status);
    }

    [Fact(DisplayName = "0018/AC-32: Bir kez kullanılmış bağlantı ikinci kez kullanıldığında kaydın durumu ve teyit zamanı değişmez, ikinci teyit olayı eklenmez ve sayfa AC-30'un mesajını gösterir")]
    public async Task Degistirmez_IkinciKullanim()
    {
        AbonelikDuzeni duzen = await HazirlaAsync();
        string anahtar = await duzen.AnahtarVerAsync(Adres);

        await duzen.TeyitEtAsync(anahtar);
        NewsletterSubscription once = await duzen.AbonelikAsync(Adres);

        saat.Advance(TimeSpan.FromMinutes(10));
        Result ikinci = await duzen.TeyitEtAsync(anahtar);

        NewsletterSubscription sonra = await duzen.AbonelikAsync(Adres);

        Assert.Equal(NotificationErrors.SubscriptionLinkInvalid, ikinci.Error.Code);
        Assert.Equal(SubscriptionStatus.Active, sonra.Status);
        Assert.Equal(once.ConfirmedAt, sonra.ConfirmedAt);
        Assert.Single(await duzen.OlaylarAsync(once.Id), olay => olay.Kind == ConsentEventKind.Confirmed);
    }

    [Fact(DisplayName = "0018/AC-33: Aynı bağlantıyla **aynı anda** gelen iki teyit isteğinden (istekler bir bariyerle eşzamanlı başlatılır) yalnız biri başarılı olur; teyit zamanı ve teyit olayı bir kez yazılır")]
    public async Task BasariliOlurYalnizBiri_EsZamanliIkiTeyit()
    {
        AbonelikDuzeni duzen = await HazirlaAsync();
        string anahtar = await duzen.AnahtarVerAsync(Adres);

        // Bariyer KAYIT anında: ikisi de kaydı "teyit bekliyor" okumuş ve yazmaya gelmişken
        // birlikte bırakılır (Bilinen tuzak 9).
        KayitBariyeri bariyer = new(2);

        Result[] sonuclar = await Task.WhenAll(
            Task.Run(() => duzen.TeyitEtAsync(anahtar, bariyer)),
            Task.Run(() => duzen.TeyitEtAsync(anahtar, bariyer)));

        NewsletterSubscription abonelik = await duzen.AbonelikAsync(Adres);

        Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
        Assert.Equal(NotificationErrors.SubscriptionLinkInvalid, sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);
        Assert.Single(await duzen.OlaylarAsync(abonelik.Id), olay => olay.Kind == ConsentEventKind.Confirmed);
        Assert.Equal(SubscriptionStatus.Active, abonelik.Status);
    }

    [Fact(DisplayName = "0018/AC-34: Teyit bekleyen adrese yeni teyit iletisi gittikten sonra önceki iletinin bağlantısı aboneliği etkinleştirmez")]
    public async Task Etkinlestirmez_OncekiIletininBaglantisi()
    {
        AbonelikDuzeni duzen = await HazirlaAsync();
        string ilk = await duzen.AnahtarVerAsync(Adres);

        await duzen.AboneOlAsync(Adres);
        string ikinci = await duzen.AnahtarVerAsync(Adres);

        Assert.Equal(NotificationErrors.SubscriptionLinkInvalid, (await duzen.TeyitEtAsync(ilk)).Error.Code);
        Assert.Equal(SubscriptionStatus.PendingConfirmation, (await duzen.AbonelikAsync(Adres)).Status);
        Assert.True((await duzen.TeyitEtAsync(ikinci)).IsSuccess);
    }

    [Theory(DisplayName = "0018/AC-35: Hiç var olmamış ya da bozuk bir teyit bağlantısının cevabı, süresi dolmuş ve kullanılmış bağlantınınkiyle aynı durum kodu ve aynı gövdedir")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("bozuk!!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("QUFB")]
    public async Task AyniSonuc_HicOlmayanVeBozukAnahtar(string? anahtar)
    {
        AbonelikDuzeni duzen = await HazirlaAsync();

        Result sonuc = await duzen.TeyitEtAsync(anahtar);

        Assert.Equal(NotificationErrors.SubscriptionLinkInvalid, sonuc.Error.Code);
        Assert.Null(sonuc.Error.Parameters);
        Assert.Equal(SubscriptionStatus.PendingConfirmation, (await duzen.AbonelikAsync(Adres)).Status);
    }

    private async Task<AbonelikDuzeni> HazirlaAsync()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat, Bulten.BultenSecenekleri(Bulten.BultenKurulumu(gecerliIstek: 50, adresSiniri: 50)));

        Assert.True((await duzen.AboneOlAsync(Adres)).IsSuccess);

        return duzen;
    }
}
