using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// R3'ün ayırt edilemezliği bu dosyanın ana konusudur. Testler tek tek "reddedildi mi"
/// sormaz; reddin **aynı** kodla geldiğini de sorar. Ayrım yapan bir dal olmadığı için
/// sızdırılacak bir şey de yoktur.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class GirisTests(IdentityDatabase database)
{
    private const string Kimlik = "ayse@ornek.com";
    private const string Parola = "cok-guclu-parola";

    [Fact(DisplayName = "0006/AC-1: Geçerli kimlik ve parolayla giriş yapan personel yönetim paneline erişir")]
    public async Task Acilir_GecerliKimlikVeParolayla()
    {
        IdentityHarness harness = await KurAsync();

        Result<LoginResult> sonuc = await harness.GirisAsync(Kimlik, Parola);

        Assert.True(sonuc.IsSuccess);

        string token = IdentityHarness.OturumTokeni(sonuc);

        Assert.NotEmpty(token);
        Assert.NotNull(await harness.OturumCozAsync(token));
    }

    [Fact(DisplayName = "0006/AC-2: Kayıtlı olmayan bir kimlikle giriş denendiğinde giriş reddedilir")]
    public async Task Reddedilir_KayitliOlmayanKimlik()
    {
        IdentityHarness harness = await KurAsync();

        Result<LoginResult> sonuc = await harness.GirisAsync("hic-yok@ornek.com", Parola);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-3: Kayıtlı bir kimlikle yanlış parola girildiğinde giriş reddedilir")]
    public async Task Reddedilir_YanlisParola()
    {
        IdentityHarness harness = await KurAsync();

        Result<LoginResult> sonuc = await harness.GirisAsync(Kimlik, "yanlis-parola-123");

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials, sonuc.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-4: Pasifleştirilmiş bir personel giriş yapamaz")]
    public async Task Reddedilir_PasiflestirilmisPersonel()
    {
        IdentityHarness harness = await KurAsync();

        Guid ikinciYonetici = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "ikinci@ornek.com", rol: "manager"))).Value;

        Assert.True(ikinciYonetici != Guid.Empty);

        Guid personelId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "danisman@ornek.com", rol: "sales_advisor"))).Value;

        await harness.KomutAsync(komutlar => komutlar.DeactivateAsync(personelId, CancellationToken.None));

        Result<LoginResult> sonuc = await harness.GirisAsync("danisman@ornek.com", Parola);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials, sonuc.Error.Code);
    }

    /// <summary>
    /// Tuzak 3 — "böyle bir kullanıcı yok" cevabı, hangi e-postaların sistemde kayıtlı
    /// olduğunu sızdırır. Üç ret de AYNI koddan geçer ve aralarında ayrım yapan bir dal
    /// yoktur.
    /// </summary>
    [Fact(DisplayName = "0006/AC-5: 0006/AC-2, 0006/AC-3 ve 0006/AC-4'te dönen cevap birbirinden ayırt edilemez: aynı hata kodu, aynı gövde")]
    public async Task AyirtEdilemez_UcRetSebebi()
    {
        IdentityHarness harness = await KurAsync();

        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: "ikinci@ornek.com", rol: "manager"));
        Guid pasif = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "pasif@ornek.com", rol: "sales_advisor"))).Value;
        await harness.KomutAsync(komutlar => komutlar.DeactivateAsync(pasif, CancellationToken.None));

        Error kayitliDegil = (await harness.GirisAsync("hic-yok@ornek.com", Parola)).Error;
        Error yanlisParola = (await harness.GirisAsync(Kimlik, "yanlis-parola-123")).Error;
        Error pasifHesap = (await harness.GirisAsync("pasif@ornek.com", Parola)).Error;

        Assert.Equal(kayitliDegil, yanlisParola);
        Assert.Equal(yanlisParola, pasifHesap);
        Assert.Null(kayitliDegil.Parameters);
    }

    [Fact(DisplayName = "0006/AC-6: Yapılandırılan eşik kadar arka arkaya başarısız giriş denemesinden sonra hesap yapılandırılan süre boyunca giriş denemesine kapanır; bu süre içinde doğru parola girilse de giriş reddedilir")]
    public async Task Kapanir_EsikKadarBasarisizDenemeden()
    {
        IdentityHarness harness = await KurAsync();

        for (int deneme = 0; deneme < IdentityHarness.Esik; deneme++)
        {
            Assert.True((await harness.GirisAsync(Kimlik, "yanlis-parola-123")).IsFailure);
        }

        // DOĞRU parola da reddedilir: kilit parolanın doğruluğuna bakmaz.
        Result<LoginResult> kilitli = await harness.GirisAsync(Kimlik, Parola);

        Assert.True(kilitli.IsFailure);
        Assert.Equal(IdentityErrors.InvalidCredentials, kilitli.Error.Code);
    }

    [Fact(DisplayName = "0006/AC-7: Kilitlenme süresi dolduktan sonra doğru parolayla giriş başarılı olur")]
    public async Task Acilir_KilitSuresiDolunca()
    {
        IdentityHarness harness = await KurAsync();

        for (int deneme = 0; deneme < IdentityHarness.Esik; deneme++)
        {
            await harness.GirisAsync(Kimlik, "yanlis-parola-123");
        }

        Assert.True((await harness.GirisAsync(Kimlik, Parola)).IsFailure);

        harness.Saat.Advance(IdentityHarness.KilitSuresi);

        Assert.True((await harness.GirisAsync(Kimlik, Parola)).IsSuccess);
    }

    /// <summary>
    /// Tuzak 7 — ayırt edilemezlik kuralı tek uçta tutulursa kilitlenme cevabı hangi
    /// e-postaların kayıtlı olduğunu TAM OLARAK sızdırır. Sayaç bu yüzden kayıtlı olmayan
    /// kimlikler için de işler.
    /// </summary>
    [Fact(DisplayName = "0006/AC-8: Kayıtlı olmayan bir kimlikle eşik sayıda başarısız denemeden sonra dönen cevap, kayıtlı bir kimlik için dönenle ayırt edilemez")]
    public async Task AyirtEdilemez_KayitliOlmayanKimliginKilitlenmesi()
    {
        IdentityHarness harness = await KurAsync();

        for (int deneme = 0; deneme < IdentityHarness.Esik; deneme++)
        {
            await harness.GirisAsync("hic-yok@ornek.com", "yanlis-parola-123", "198.51.100.1");
            await harness.GirisAsync(Kimlik, "yanlis-parola-123", "198.51.100.2");
        }

        Error kayitliOlmayan = (await harness.GirisAsync("hic-yok@ornek.com", Parola, "198.51.100.1")).Error;
        Error kayitli = (await harness.GirisAsync(Kimlik, Parola, "198.51.100.2")).Error;

        Assert.Equal(kayitliOlmayan, kayitli);
    }

    [Fact(DisplayName = "0006/AC-9: Kilitlenme sayacı hesap ve kaynak IP birlikte anahtarlanır: bir kaynaktan kilitlenen hesaba, kilitli olmayan başka bir kaynaktan doğru parolayla giriş yapılabilir")]
    public async Task Acilir_BaskaKaynaktan()
    {
        IdentityHarness harness = await KurAsync();

        for (int deneme = 0; deneme < IdentityHarness.Esik; deneme++)
        {
            await harness.GirisAsync(Kimlik, "yanlis-parola-123", "203.0.113.7");
        }

        Assert.True((await harness.GirisAsync(Kimlik, Parola, "203.0.113.7")).IsFailure);
        Assert.True((await harness.GirisAsync(Kimlik, Parola, "198.51.100.4")).IsSuccess);
    }

    [Fact(DisplayName = "0006/AC-11: `Ali@x.com` kimliğiyle eklenen personel `ali@x.com` yazarak giriş yapar")]
    public async Task Acilir_FarkliBuyukKucukHarfle()
    {
        IdentityHarness harness = await KurAsync(kimlik: "Ali@x.com");

        Assert.True((await harness.GirisAsync("ali@x.com", Parola)).IsSuccess);
        Assert.True((await harness.GirisAsync("ALI@X.COM", Parola, "198.51.100.9")).IsSuccess);
    }

    private async Task<IdentityHarness> KurAsync(string kimlik = Kimlik)
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Result<Guid> sonuc = await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: kimlik, parola: Parola, rol: "manager"));

        Assert.True(sonuc.IsSuccess);

        return harness;
    }
}
