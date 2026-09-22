using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Domain;

/// <summary>
/// Süreler YAPILANDIRMADAN gelir; testler yapılandırmayı sabitleyip doğrular (C20).
/// </summary>
public sealed class OturumTests
{
    private static readonly TimeSpan ErisimSuresi = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan YenilemeSuresi = TimeSpan.FromDays(14);
    private static readonly DateTimeOffset An = StaffBuilder.VarsayilanAn;

    /// <summary>
    /// AC-15'in gözlem noktası burasıdır: erişim penceresi dolduğunda oturum ARTIK
    /// `Active` değildir. Bunun 401'e dönüşüp dönüşmeyeceği yenileme hakkının olup
    /// olmamasına bağlıdır (AC-16 / AC-17) ve o ayrım Host boru hattında sınanır.
    /// </summary>
    [Fact(DisplayName = "0006/AC-15: Erişim oturumu yapılandırılan süre sonunda geçerliliğini yitirir")]
    public void GecerliliginiYitirir_ErisimSuresiDolunca()
    {
        Session oturum = Ac();

        Assert.Equal(SessionValidity.Active, oturum.Validate(An.Add(ErisimSuresi) - TimeSpan.FromSeconds(1)));
        Assert.NotEqual(SessionValidity.Active, oturum.Validate(An.Add(ErisimSuresi)));
    }

    [Fact(DisplayName = "0006/AC-16: Süresi dolmuş erişim oturumu, geçerli bir yenileme hakkı varken sessizce yenilenir ve personel işini kaybetmez")]
    public void SessizceYenilenir_YenilemeHakkiVarken()
    {
        Session oturum = Ac();
        DateTimeOffset sonra = An.Add(ErisimSuresi);

        Assert.Equal(SessionValidity.NeedsRenewal, oturum.Validate(sonra));

        oturum.Touch(sonra, ErisimSuresi);

        Assert.Equal(SessionValidity.Active, oturum.Validate(sonra));
    }

    /// <summary>
    /// Yenileme yalnız ERİŞİM penceresini uzatır; mutlak yenileme sınırına dokunmaz.
    /// Aksi hâlde sürekli çalışan bir oturum hiç bitmezdi ve AC-17 anlamsız olurdu.
    /// </summary>
    [Fact(DisplayName = "0006/AC-17: Yenileme hakkı yapılandırılan süre sonunda geçerliliğini yitirir; personel yeniden giriş yapar")]
    public void UzatmazMutlakSinir_Yenileme()
    {
        Session oturum = Ac();
        DateTimeOffset mutlakSinir = oturum.RefreshExpiresAt;

        oturum.Touch(An.Add(ErisimSuresi), ErisimSuresi);

        Assert.Equal(mutlakSinir, oturum.RefreshExpiresAt);
    }

    [Fact(DisplayName = "0006/AC-17: Yenileme hakkı yapılandırılan süre sonunda geçerliliğini yitirir; personel yeniden giriş yapar")]
    public void GecerliliginiYitirir_YenilemeSuresiDolunca()
    {
        Session oturum = Ac();

        Assert.Equal(SessionValidity.Expired, oturum.Validate(An.Add(YenilemeSuresi)));
    }

    [Fact(DisplayName = "0006/AC-18: Personel çıkış yaptığında o oturumun yenileme hakkı geçersizleşir ve aynı hakla yeni oturum alınamaz")]
    public void Gecersizlesir_CikistanSonra()
    {
        Session oturum = Ac();
        oturum.Revoke(An);

        Assert.Equal(SessionValidity.Revoked, oturum.Validate(An));

        // Erişim penceresi hâlâ açık olsa bile iptal her şeyin önündedir.
        Assert.Equal(SessionValidity.Revoked, oturum.Validate(An.AddMinutes(1)));
    }

    /// <summary>
    /// Çerezde taşınan token satırda AÇIK durmaz: sızan bir veritabanı yedeği doğrudan
    /// oturum açmaya yetmesin.
    /// </summary>
    [Fact(DisplayName = "0006/AC-29: Oturum bilgisi, sayfadaki JavaScript'ten okunabilir bir yerde tutulmaz")]
    public void SaklanmazAcikToken_OturumSatirinda()
    {
        string token = Session.GenerateToken();
        Session oturum = Session.Issue(StaffId.New(), SecurityStamp.New(), token, An, ErisimSuresi, YenilemeSuresi);

        Assert.NotEqual(token, oturum.TokenHash);
        Assert.Equal(Session.HashToken(token), oturum.TokenHash);
    }

    [Fact(DisplayName = "0006/AC-29: Oturum bilgisi, sayfadaki JavaScript'ten okunabilir bir yerde tutulmaz")]
    public void BenzersizdirHerCagride_Token()
    {
        HashSet<string> uretilenler = [.. Enumerable.Range(0, 64).Select(_ => Session.GenerateToken())];

        Assert.Equal(64, uretilenler.Count);
    }

    private static Session Ac() => Session.Issue(
        StaffId.New(),
        SecurityStamp.New(),
        Session.GenerateToken(),
        An,
        ErisimSuresi,
        YenilemeSuresi);
}
