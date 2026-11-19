using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application.Newsletter;
using CarGallery.Modules.Notifications.Domain;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `0018` R5 · R6 · **D6** · **D8** · **D25** — çıkış. Oturum yok, onay yok: işleyiciye yalnız
/// bağlantıdaki anahtar gelir (AC-38). Bağlantının bültende basılması dağıtım testlerinde.
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class CikisTests(NotificationsDatabase database)
{
    private const string Adres = "ali@ornek.com";

    private readonly Bulten.IleriSaat saat = new(DateTimeOffset.UtcNow);

    [Fact(DisplayName = "0018/AC-38: Çıkış bağlantısı oturum açılmamış bir tarayıcıda açıldığında başka bir düğme ya da onay istemeden kaydı **çıkmış** yapar, çıkış zamanını UTC olarak yazar, bir **çıkış** izin olayı ekler ve sayfa \"abonelikten çıktınız\" mesajını gösterir")]
    public async Task CikmisYapar_TekAdimda()
    {
        (AbonelikDuzeni duzen, Guid id) = await EtkinAsync();
        saat.Advance(TimeSpan.FromDays(1));

        Result sonuc = await duzen.CikAsync(duzen.Cikis.Create(id));

        NewsletterSubscription abonelik = await duzen.AbonelikAsync(Adres);

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(SubscriptionStatus.Unsubscribed, abonelik.Status);
        Assert.Equal(saat.UtcNow, abonelik.UnsubscribedAt);
        Assert.Equal(TimeSpan.Zero, abonelik.UnsubscribedAt!.Value.Offset);
        ConsentEvent olay = (await duzen.OlaylarAsync(id))[^1];
        Assert.Equal(ConsentEventKind.Unsubscribed, olay.Kind);
        Assert.Equal(saat.UtcNow, olay.OccurredAt);
    }

    [Fact(DisplayName = "0018/AC-40: Çıkmış kayıt silinmez: çıkıştan sonra kayıt **çıkmış** durumunda ve çıkış zamanıyla durur")]
    public async Task Silinmez_CikmisKayit()
    {
        (AbonelikDuzeni duzen, Guid id) = await EtkinAsync();

        await duzen.CikAsync(duzen.Cikis.Create(id));
        saat.Advance(TimeSpan.FromDays(400));

        NewsletterSubscription abonelik = await duzen.AbonelikAsync(Adres);

        Assert.Equal(1, await duzen.AbonelikSayisiAsync());
        Assert.Equal(SubscriptionStatus.Unsubscribed, abonelik.Status);
        Assert.NotNull(abonelik.UnsubscribedAt);
    }

    [Fact(DisplayName = "0018/AC-41: Aynı çıkış bağlantısının ikinci kez açılması aynı mesajı gösterir; durum ve çıkış zamanı değişmez, ikinci çıkış olayı eklenmez")]
    public async Task Degistirmez_IkinciAcilis()
    {
        (AbonelikDuzeni duzen, Guid id) = await EtkinAsync();
        string baglanti = duzen.Cikis.Create(id);

        Result ilk = await duzen.CikAsync(baglanti);
        NewsletterSubscription once = await duzen.AbonelikAsync(Adres);

        saat.Advance(TimeSpan.FromHours(2));
        Result ikinci = await duzen.CikAsync(baglanti);

        NewsletterSubscription sonra = await duzen.AbonelikAsync(Adres);

        Assert.True(ilk.IsSuccess);
        Assert.True(ikinci.IsSuccess);
        Assert.Equal(once.UnsubscribedAt, sonra.UnsubscribedAt);
        Assert.Single(await duzen.OlaylarAsync(id), olay => olay.Kind == ConsentEventKind.Unsubscribed);
    }

    [Fact(DisplayName = "0018/AC-42: Çıkıp yeniden abone olan ve teyit eden bir adreste, **ilk** aboneliğin bültenindeki çıkış bağlantısı aboneliği yine çıkmış yapar")]
    public async Task CikmisYapar_IlkBaglanti_YenidenAbonelikSonrasi()
    {
        (AbonelikDuzeni duzen, Guid id) = await EtkinAsync();
        string ilkBaglanti = duzen.Cikis.Create(id);

        await duzen.CikAsync(ilkBaglanti);
        saat.Advance(TimeSpan.FromDays(1));
        await duzen.AboneOlAsync(Adres);
        await duzen.TeyitEtAsync(await duzen.AnahtarVerAsync(Adres));
        Assert.Equal(SubscriptionStatus.Active, (await duzen.AbonelikAsync(Adres)).Status);

        Assert.True((await duzen.CikAsync(ilkBaglanti)).IsSuccess);
        Assert.Equal(SubscriptionStatus.Unsubscribed, (await duzen.AbonelikAsync(Adres)).Status);
    }

    [Theory(DisplayName = "0018/AC-44: Geçersiz bir çıkış bağlantısı hiçbir kaydı değiştirmez ve sayfa \"bağlantı artık geçerli değil\" mesajını gösterir")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("bozuk!!")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public async Task Degistirmez_GecersizBaglanti(string? baglanti)
    {
        (AbonelikDuzeni duzen, Guid id) = await EtkinAsync();
        int olaySayisi = (await duzen.OlaylarAsync(id)).Count;

        Result sonuc = await duzen.CikAsync(baglanti);

        Assert.Equal(NotificationErrors.SubscriptionLinkInvalid, sonuc.Error.Code);
        Assert.Equal(SubscriptionStatus.Active, (await duzen.AbonelikAsync(Adres)).Status);
        Assert.Equal(olaySayisi, (await duzen.OlaylarAsync(id)).Count);
    }

    [Fact(DisplayName = "0018/AC-44: Geçersiz bir çıkış bağlantısı hiçbir kaydı değiştirmez ve sayfa \"bağlantı artık geçerli değil\" mesajını gösterir")]
    public async Task Degistirmez_KurcalanmisImza()
    {
        (AbonelikDuzeni duzen, Guid id) = await EtkinAsync();

        // Başka bir aboneliğin kimliğiyle imza taklidi: geçerli bir bağlantının imzası, başka
        // bir kimliğin yanına konuyor.
        string gecerli = duzen.Cikis.Create(id);
        string taklit = duzen.Cikis.Create(Guid.CreateVersion7())[..22] + gecerli[22..];

        Assert.Equal(NotificationErrors.SubscriptionLinkInvalid, (await duzen.CikAsync(taklit)).Error.Code);
        Assert.Equal(SubscriptionStatus.Active, (await duzen.AbonelikAsync(Adres)).Status);
    }

    [Fact(DisplayName = "0018/AC-47: Çıkış bağlantılarını imzalayan sır değiştirildiğinde, önceki sırla üretilmiş bir çıkış bağlantısı AC-44'ün cevabını verir (D25'in bedeli)")]
    public async Task Gecersiz_SirDegisinceEskiBaglanti()
    {
        (AbonelikDuzeni duzen, Guid id) = await EtkinAsync();
        string eski = duzen.Cikis.Create(id);

        string yeniSir = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        AbonelikDuzeni yeni = new(database, saat, Bulten.BultenSecenekleri(Bulten.BultenKurulumu(imzaSirri: yeniSir)));

        Assert.Equal(NotificationErrors.SubscriptionLinkInvalid, (await yeni.CikAsync(eski)).Error.Code);
        Assert.Equal(SubscriptionStatus.Active, (await yeni.AbonelikAsync(Adres)).Status);
        Assert.True((await yeni.CikAsync(yeni.Cikis.Create(id))).IsSuccess);
    }

    [Fact(DisplayName = "0018/AC-91: Teyit ve çıkış bağlantıları adresi düz, URL kodlu ya da Base64 olarak içermez")]
    public async Task Icermez_AdresiCikisBaglantisi()
    {
        (AbonelikDuzeni duzen, Guid id) = await EtkinAsync();

        string baglanti = duzen.Cikis.Create(id);

        foreach (string bicim in AdresBicimleri.Hepsi(Adres))
        {
            Assert.DoesNotContain(bicim, baglanti, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(duzen.Cikis.TryVerify(baglanti, out Guid cozulen));
        Assert.Equal(id, cozulen);
    }

    private async Task<(AbonelikDuzeni Duzen, Guid Id)> EtkinAsync()
    {
        await database.ResetNewsletterAsync();
        AbonelikDuzeni duzen = new(database, saat, Bulten.BultenSecenekleri(Bulten.BultenKurulumu(gecerliIstek: 50, adresSiniri: 50)));

        await duzen.AboneOlAsync(Adres);
        await duzen.TeyitEtAsync(await duzen.AnahtarVerAsync(Adres));

        return (duzen, (await duzen.AbonelikAsync(Adres)).Id);
    }
}

/// <summary>AC-91'in üç biçimi: düz, URL kodlu, Base64 (standart ve URL güvenli).</summary>
internal static class AdresBicimleri
{
    internal static IEnumerable<string> Hepsi(string adres)
    {
        string base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(adres));

        yield return adres;
        yield return Uri.EscapeDataString(adres);
        yield return System.Net.WebUtility.UrlEncode(adres);
        yield return base64.TrimEnd('=');
        yield return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        yield return adres.Split('@')[0];
    }
}
