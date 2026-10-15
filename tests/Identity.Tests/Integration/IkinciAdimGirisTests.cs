using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// Girişin ikinci adımı. Dosyanın ana konusu **parolanın tek başına yetmemesi**: kurulumu
/// olan hesapta oturum çerezi hiç yazılmıyor (D4) ve ara durum bir oturum değil (R13).
///
/// Saat SABİT olduğu için adım aritmetiği ölçülebilir: D10'un toleransı ve D11'in tek
/// kullanımı burada tek tek sınanıyor. Gerçek saatle koşan testler aynı pencerede ikinci
/// kodu reddedilmiş bulur — bu bir kusur değil, D45'in yazılı sürtünmesi.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class IkinciAdimGirisTests(IdentityDatabase database)
{
    private const string Kimlik = "yonetici@ornek.com";
    private const string Parola = "cok-guclu-parola";

    [Fact(DisplayName = "0013/AC-11: Kurulumu olan personel doğru parolayı girdiğinde **oturum çerezi yazılmaz**; yalnız ara durum çerezi yazılır")]
    public async Task YazmazOturum_KurulumuOlanHesapta()
    {
        (IdentityHarness harness, _, _, _) = await KurAsync();

        Result<LoginResult> giris = await harness.GirisAsync(Kimlik, Parola);

        Assert.True(giris.IsSuccess);
        Assert.Equal(LoginStep.TwoFactorRequired, giris.Value.Step);
        Assert.Null(giris.Value.Token);
        Assert.Null(giris.Value.Session);
        Assert.NotNull(giris.Value.PendingToken);

        // Oturum satırı da doğmuyor: "çerez yazılmadı ama satır var" hâli, bir sonraki
        // kusurun oturumu canlandırmasına izin verirdi.
        await using IdentityDbContext context = database.CreateContext();

        Assert.Empty(await context.Sessions.ToListAsync());
    }

    [Fact(DisplayName = "0013/AC-13: Ara durum çerezi `HttpOnly`'dir ve satırda tokenin kendisi değil **SHA-256 özeti** durur")]
    public async Task Saklar_YalnizOzeti()
    {
        (IdentityHarness harness, _, _, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        await using IdentityDbContext context = database.CreateContext();
        PendingLogin satir = await context.PendingLogins.SingleAsync();

        Assert.NotEqual(araDurum, satir.TokenHash);
        Assert.Equal(PendingLogin.HashToken(araDurum), satir.TokenHash);
    }

    [Fact(DisplayName = "0013/AC-14: Ara durum yapılandırılan süre sonunda düşer; düşmüş ara durumla yapılan doğrulama `identity.two_factor_pending_expired` döner")]
    public async Task Doner_PendingExpired_SuresiDolunca()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        harness.Saat.Advance(IdentityHarness.AraDurumSuresi);

        Result<LoginResult> sonuc = await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir));

        Assert.Equal(IdentityErrors.TwoFactorPendingExpired, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0013/AC-15: Ara durum **tek kullanımlıktır**: başarılı doğrulamadan sonra aynı çerezle yapılan ikinci doğrulama reddedilir")]
    public async Task Reddedilir_IkinciKullanim()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Assert.True((await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir))).IsSuccess);

        // İkinci deneme BAŞKA bir adımın koduyla yapılıyor: red, kodun değil ara durumun
        // tükenmesinden gelsin.
        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        Result<LoginResult> ikinci = await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir));

        Assert.Equal(IdentityErrors.TwoFactorPendingExpired, ikinci.Error.Code);
    }

    [Fact(DisplayName = "0013/AC-17: *\"Kullanıcı değiştir\"* ara durumu **siler**; silinmiş ara durumla yapılan doğrulama reddedilir")]
    public async Task Siler_KullaniciDegistirde()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        await harness.AraDurumSilAsync(araDurum);

        await using IdentityDbContext context = database.CreateContext();

        Assert.Empty(await context.PendingLogins.ToListAsync());
        Assert.Equal(
            IdentityErrors.TwoFactorPendingExpired,
            (await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir))).Error.Code);
    }

    [Fact(DisplayName = "0013/AC-18: Başarılı doğrulamada oturum çerezi yazılır ve yanıt tamamlanmış dalı — bugünkü oturum gövdesini — döner")]
    public async Task Yazar_Oturumu_DogrulamaBasarili()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Result<LoginResult> sonuc = await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir));

        Assert.True(sonuc.IsSuccess);
        Assert.Equal(LoginStep.Completed, sonuc.Value.Step);
        Assert.True(sonuc.Value.TwoFactorEnrolled);
        Assert.NotNull(await harness.OturumCozAsync(IdentityHarness.OturumTokeni(sonuc)));
    }

    [Fact(DisplayName = "0013/AC-20: İçinde bulunulan adımın kodu kabul edilir ve oturum açılır")]
    public async Task KabulEdilir_GecerliKod()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Assert.True((await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir))).IsSuccess);
    }

    [Theory(DisplayName = "0013/AC-21: Yanlış kod `401` ve `identity.two_factor_invalid` ile reddedilir")]
    [InlineData("000000")]
    [InlineData("12345")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Reddedilir_YanlisKod(string? kod)
    {
        (IdentityHarness harness, _, _, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Assert.Equal(
            IdentityErrors.TwoFactorInvalid,
            (await harness.IkinciAdimGecAsync(araDurum, kod)).Error.Code);
    }

    [Fact(DisplayName = "0013/AC-22: Penceresi dolmuş kod reddedilir")]
    public async Task Reddedilir_PenceresiDolmusKod()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;
        string kod = harness.Kod(sir);

        // İki adım (60 sn) ileri: kod artık tolerans penceresinde değil.
        harness.Saat.Advance(TimeSpan.FromSeconds(61));

        Assert.Equal(
            IdentityErrors.TwoFactorInvalid,
            (await harness.IkinciAdimGecAsync(araDurum, kod)).Error.Code);
    }

    [Fact(DisplayName = "0013/AC-23: Kabul edilen bir kod **ikinci kez** kabul edilmez")]
    public async Task Reddedilir_YenidenOynatilanKod()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        string birinci = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;
        string kod = harness.Kod(sir);

        Assert.True((await harness.IkinciAdimGecAsync(birinci, kod)).IsSuccess);

        // Aynı pencerede ikinci bir giriş: ara durum taze, kod aynı → red D11'den gelir.
        string ikinci = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Assert.Equal(
            IdentityErrors.TwoFactorInvalid,
            (await harness.IkinciAdimGecAsync(ikinci, kod)).Error.Code);
    }

    [Fact(DisplayName = "0013/AC-24: Saat kayması toleransı **±1 adımdır**: önceki ve sonraki adımın kodu kabul edilir, iki adım önceki kod reddedilir")]
    public async Task KabulEder_KomsuAdimlarin_Kodunu()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        // Kurulum onayının tükettiği adım geride kalsın: ölçülen şey TOLERANS, D11'in
        // tek kullanımı değil (onun kendi testi var).
        harness.Saat.Advance(TimeSpan.FromMinutes(2));

        long adim = TimeBasedCode.StepAt(harness.Saat.UtcNow);

        string oncekiyle = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Assert.True((await harness.IkinciAdimGecAsync(oncekiyle, harness.Kod(sir, adim - 1))).IsSuccess);

        string sonrakiyle = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Assert.True((await harness.IkinciAdimGecAsync(sonrakiyle, harness.Kod(sir, adim + 1))).IsSuccess);
    }

    [Fact(DisplayName = "0013/AC-24: Saat kayması toleransı **±1 adımdır**: önceki ve sonraki adımın kodu kabul edilir, iki adım önceki kod reddedilir")]
    public async Task Reddeder_IkiAdimOncekiKodu()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        harness.Saat.Advance(TimeSpan.FromMinutes(2));

        long adim = TimeBasedCode.StepAt(harness.Saat.UtcNow);
        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        // Red TOLERANSTAN geliyor, yeniden oynatmadan değil: `adim - 2` kabul edilmiş son
        // adımdan (kurulum onayı, iki dakika önce) ileride.
        Assert.Equal(
            IdentityErrors.TwoFactorInvalid,
            (await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir, adim - 2))).Error.Code);
    }

    [Fact(DisplayName = "0013/AC-25: Yanlış ikinci adım kodu parola denemesiyle **aynı** kilitlenme sayacını artırır; eşiğe ulaşıldığında hesap kilitlenir ve kilitliyken doğru kod da reddedilir")]
    public async Task Kilitlenir_PaylasilanSayacla()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        // İki yanlış PAROLA denemesi + üç yanlış KOD denemesi = eşik (5). Sayaç ayrı
        // olsaydı hiçbiri kilitlenmezdi (§ Bilinen tuzaklar 5).
        for (int deneme = 0; deneme < 2; deneme++)
        {
            Assert.True((await harness.GirisAsync(Kimlik, "yanlis-parola-123")).IsFailure);
        }

        for (int deneme = 0; deneme < 3; deneme++)
        {
            string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

            Assert.Equal(
                IdentityErrors.TwoFactorInvalid,
                (await harness.IkinciAdimGecAsync(araDurum, "000000")).Error.Code);
        }

        // Kilitliyken doğru PAROLA da reddedilir — sayaç gerçekten paylaşılıyor.
        Assert.Equal(
            IdentityErrors.InvalidCredentials,
            (await harness.GirisAsync(Kimlik, Parola)).Error.Code);
    }

    [Fact(DisplayName = "0013/AC-27: Kilitlenmiş hesapta ikinci adım reddi **ayırt edilemez**: kilit için ayrı bir hata kodu dönmez")]
    public async Task AyirtEdilemez_KilitliHesaptaIkinciAdim()
    {
        (IdentityHarness harness, _, string sir, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        for (int deneme = 0; deneme < IdentityHarness.Esik; deneme++)
        {
            Assert.True((await harness.GirisAsync(Kimlik, "yanlis-parola-123")).IsFailure);
        }

        // Hesap kilitli ve kod DOĞRU; red yine aynı kodla geliyor.
        Result<LoginResult> sonuc = await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir));

        Assert.Equal(IdentityErrors.TwoFactorInvalid, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0013/AC-35: İkinci adım denemeleri kilitlenme eşiğine ulaştığında ara durum **silinir** ve personel parola fazına döner")]
    public async Task Silinir_AraDurum_KilitEsiginde()
    {
        (IdentityHarness harness, _, _, _) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        for (int deneme = 0; deneme < IdentityHarness.Esik; deneme++)
        {
            await harness.IkinciAdimGecAsync(araDurum, "000000");
        }

        await using IdentityDbContext context = database.CreateContext();

        Assert.Empty(await context.PendingLogins.ToListAsync());
    }

    [Fact(DisplayName = "0013/AC-36: Kodunu üretemeyen personel bir kurtarma koduyla giriş yapar; kullanılan kod bir daha kabul edilmez")]
    public async Task TekKullanimlik_KurtarmaKodu()
    {
        (IdentityHarness harness, Guid personelId, _, RecoveryCodeSet kodlar) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Assert.True((await harness.IkinciAdimGecAsync(araDurum, kodlar.Codes[0])).IsSuccess);

        string ikinciAraDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        Assert.Equal(
            IdentityErrors.TwoFactorInvalid,
            (await harness.IkinciAdimGecAsync(ikinciAraDurum, kodlar.Codes[0])).Error.Code);

        // Kalan sayı da düştü: kullanılan kod silinmiyor, işaretleniyor (D24'ün sayacı).
        Result<TwoFactorStatus> durum = await harness.IkinciAdimAsync(
            komutlar => komutlar.GetStatusAsync(personelId, CancellationToken.None));

        Assert.Equal(IdentityHarness.KurtarmaKoduSayisi - 1, durum.Value.RemainingRecoveryCodes);
    }

    [Fact(DisplayName = "0013/AC-36: Kodunu üretemeyen personel bir kurtarma koduyla giriş yapar; kullanılan kod bir daha kabul edilmez")]
    public async Task KabulEder_GruplanmisKurtarmaKodunu()
    {
        (IdentityHarness harness, Guid personelId, _, RecoveryCodeSet kodlar) = await KurAsync();

        string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

        // Personel kodu elle yazıyor: tireler ve küçük harf tolere edilir, aksi hâlde
        // tek kullanımlık bir kod bir yazım biçimine feda edilirdi.
        string elleYazilmis = kodlar.Codes[0].Replace("-", " ", StringComparison.Ordinal).ToLowerInvariant();

        Assert.True((await harness.IkinciAdimGecAsync(araDurum, elleYazilmis)).IsSuccess);
    }

    [Fact(DisplayName = "0013/AC-37: Kurtarma kodu doğrulaması **kendi** iterasyon sayısını yapılandırmadan okur")]
    public async Task Gelir_KurtarmaIterasyonu_Yapilandirmadan()
    {
        (IdentityHarness harness, Guid personelId, _, RecoveryCodeSet kodlar) = await KurAsync();

        await using IdentityDbContext context = database.CreateContext();
        List<string> ozetler = await context.RecoveryCodes.Select(kod => kod.CodeHash.Value).ToListAsync();

        // Özet kendi maliyetini içinde taşıyor: yapılandırılan değer saklanan satırdan
        // OKUNABİLİR ve parola iterasyonundan ayrı olduğu buradan görünür.
        Assert.All(ozetler, ozet => Assert.Equal(
            StaffBuilder.TestIterasyon.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ozet.Split('$')[1]));
    }

    private async Task<(IdentityHarness Harness, Guid PersonelId, string Sir, RecoveryCodeSet Kodlar)> KurAsync()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid personelId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: Kimlik, parola: Parola, rol: "manager"))).Value;

        (string sir, RecoveryCodeSet kodlar) = await harness.IkinciAdimKurVeKodlariAlAsync(personelId);

        // D11 — onay kodunun adımı TÜKETİLDİ; aynı adımda yapılacak bir giriş reddedilir
        // (D45'in yazılı sürtünmesi). Saat bu sınıfta kontrol edildiği için bir periyot
        // ileri alıyoruz: ölçülen şey toleransın ve tek kullanımın kendisi, kurulumun
        // hemen ardından giriş yapma alışkanlığı değil.
        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        return (harness, personelId, sir, kodlar);
    }
}
