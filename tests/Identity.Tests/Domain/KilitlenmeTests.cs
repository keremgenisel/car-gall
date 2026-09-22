using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Domain;

/// <summary>
/// Eşik ve süre YAPILANDIRMADAN gelir; testler yapılandırmayı sabitleyip doğrular,
/// koda gömülü bir sabiti değil (C20).
/// </summary>
public sealed class KilitlenmeTests
{
    private const int Esik = 5;
    private static readonly TimeSpan KilitSuresi = TimeSpan.FromMinutes(15);
    private static readonly DateTimeOffset An = StaffBuilder.VarsayilanAn;

    [Fact(DisplayName = "AC-6: Yapılandırılan eşik kadar arka arkaya başarısız giriş denemesinden sonra hesap yapılandırılan süre boyunca giriş denemesine kapanır; bu süre içinde doğru parola girilse de giriş reddedilir")]
    public void Kapanir_EsikKadarBasarisizDenemeden()
    {
        LockoutState sayac = Sayac();

        for (int deneme = 1; deneme < Esik; deneme++)
        {
            sayac.RegisterFailure(An, Esik, KilitSuresi);
            Assert.False(sayac.IsLockedAt(An));
        }

        sayac.RegisterFailure(An, Esik, KilitSuresi);

        Assert.True(sayac.IsLockedAt(An));
        Assert.True(sayac.IsLockedAt(An.Add(KilitSuresi) - TimeSpan.FromSeconds(1)));
    }

    [Fact(DisplayName = "AC-7: Kilitlenme süresi dolduktan sonra doğru parolayla giriş başarılı olur")]
    public void Acilir_KilitSuresiDolunca()
    {
        LockoutState sayac = Kilitli();

        Assert.False(sayac.IsLockedAt(An.Add(KilitSuresi)));
    }

    /// <summary>
    /// Kilitten dönen personel TEK yanlış denemede yeniden kilitlenmemelidir: eski
    /// kilitten arta kalan sayaç sıfırlanır.
    /// </summary>
    [Fact(DisplayName = "AC-7: Kilitlenme süresi dolduktan sonra doğru parolayla giriş başarılı olur")]
    public void SayacSifirlanir_KilitSuresiDolduktanSonrakiIlkHatada()
    {
        LockoutState sayac = Kilitli();

        DateTimeOffset sonra = An.Add(KilitSuresi);
        sayac.RegisterFailure(sonra, Esik, KilitSuresi);

        Assert.False(sayac.IsLockedAt(sonra));
        Assert.Equal(1, sayac.FailedAttempts);
    }

    /// <summary>
    /// R6 — yalnız hesaba bağlı bir kilit, e-postasını bilen birinin o kişiyi kasten
    /// dışarıda tutmasına izin verirdi; son Yönetici dâhil.
    /// </summary>
    [Fact(DisplayName = "AC-9: Kilitlenme sayacı hesap ve kaynak IP birlikte anahtarlanır: bir kaynaktan kilitlenen hesaba, kilitli olmayan başka bir kaynaktan doğru parolayla giriş yapılabilir")]
    public void BagimsizdirKaynakBasina_Sayac()
    {
        LoginId kimlik = LoginId.Create("ayse@ornek.com").Value;

        LockoutState birinciKaynak = LockoutState.For(kimlik, "203.0.113.7");
        LockoutState ikinciKaynak = LockoutState.For(kimlik, "198.51.100.4");

        for (int deneme = 0; deneme < Esik; deneme++)
        {
            birinciKaynak.RegisterFailure(An, Esik, KilitSuresi);
        }

        Assert.True(birinciKaynak.IsLockedAt(An));
        Assert.False(ikinciKaynak.IsLockedAt(An));
    }

    /// <summary>
    /// R3/AC-8 — sayaç kayıtlı OLMAYAN kimlikler için de işler. Bu tipin `Staff`'ın
    /// parçası olmamasının tek nedeni budur: yalnız var olan hesaplar için işletilseydi,
    /// kilitlenme cevabının gelip gelmemesi hangi e-postaların kayıtlı olduğunu tam
    /// olarak sızdırırdı (Tuzak 7).
    /// </summary>
    [Fact(DisplayName = "AC-8: Kayıtlı olmayan bir kimlikle eşik sayıda başarısız denemeden sonra dönen cevap, kayıtlı bir kimlik için dönenle ayırt edilemez")]
    public void Islenir_KayitliOlmayanKimlikIcinDe()
    {
        LockoutState sayac = LockoutState.For(
            LoginId.Create("hic-kayitli-olmayan@ornek.com").Value,
            "203.0.113.7");

        for (int deneme = 0; deneme < Esik; deneme++)
        {
            sayac.RegisterFailure(An, Esik, KilitSuresi);
        }

        Assert.True(sayac.IsLockedAt(An));
        Assert.Equal(IdentityErrors.InvalidCredentials, sayac.EnsureNotLocked(An).Error.Code);
    }

    [Fact(DisplayName = "AC-6: Yapılandırılan eşik kadar arka arkaya başarısız giriş denemesinden sonra hesap yapılandırılan süre boyunca giriş denemesine kapanır; bu süre içinde doğru parola girilse de giriş reddedilir")]
    public void Temizlenir_BasariliGiriste()
    {
        LockoutState sayac = Sayac();
        sayac.RegisterFailure(An, Esik, KilitSuresi);
        sayac.Clear();

        Assert.Equal(0, sayac.FailedAttempts);
        Assert.False(sayac.IsLockedAt(An));
    }

    private static LockoutState Sayac() =>
        LockoutState.For(LoginId.Create("ayse@ornek.com").Value, "203.0.113.7");

    private static LockoutState Kilitli()
    {
        LockoutState sayac = Sayac();

        for (int deneme = 0; deneme < Esik; deneme++)
        {
            sayac.RegisterFailure(An, Esik, KilitSuresi);
        }

        return sayac;
    }
}
