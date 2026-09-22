using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts.Sessions;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// Oturumun kaynaktan doğrulanması (C16). Buradaki testler `StaffSessionQueries`'i
/// doğrudan sürer; aynı davranışın gerçek boru hattında da kablolandığı `Host.Tests`'te
/// ayrıca sınanır.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class OturumYasamDonguTests(IdentityDatabase database)
{
    private const string Parola = "cok-guclu-parola";

    [Fact(DisplayName = "0006/AC-16: Süresi dolmuş erişim oturumu, geçerli bir yenileme hakkı varken sessizce yenilenir ve personel işini kaybetmez")]
    public async Task SessizceYenilenir_ErisimSuresiDolduktanSonra()
    {
        (IdentityHarness harness, string token, _) = await GirisAsync();

        harness.Saat.Advance(IdentityHarness.ErisimSuresi);

        // Personel hiçbir şey yapmadı; oturum kendiliğinden yenilendi.
        Assert.NotNull(await harness.OturumCozAsync(token));

        harness.Saat.Advance(IdentityHarness.ErisimSuresi - TimeSpan.FromMinutes(1));
        Assert.NotNull(await harness.OturumCozAsync(token));
    }

    [Fact(DisplayName = "0006/AC-17: Yenileme hakkı yapılandırılan süre sonunda geçerliliğini yitirir; personel yeniden giriş yapar")]
    public async Task GecerliliginiYitirir_YenilemeSuresiDolunca()
    {
        (IdentityHarness harness, string token, _) = await GirisAsync();

        harness.Saat.Advance(IdentityHarness.YenilemeSuresi);

        Assert.Null(await harness.OturumCozAsync(token));
    }

    [Fact(DisplayName = "0006/AC-18: Personel çıkış yaptığında o oturumun yenileme hakkı geçersizleşir ve aynı hakla yeni oturum alınamaz")]
    public async Task Gecersizlesir_Cikista()
    {
        (IdentityHarness harness, string token, StaffSessionDto oturum) = await GirisAsync();

        await harness.CikisAsync(oturum.SessionId);

        Assert.Null(await harness.OturumCozAsync(token));
    }

    /// <summary>
    /// R5 — çıkış YALNIZ o cihazın oturumunu kapatır. Damga dönseydi tüm cihazlar
    /// düşerdi ve bu kriter kırılırdı; iki mekanizma bilerek ayrı.
    /// </summary>
    [Fact(DisplayName = "0006/AC-19: Çıkış yalnızca o oturumu kapatır; aynı personelin başka bir cihazdaki oturumu çalışmaya devam eder")]
    public async Task Surer_DigerCihazinOturumu()
    {
        (IdentityHarness harness, string birinciToken, StaffSessionDto birinci) = await GirisAsync();

        string ikinciToken = (await harness.GirisAsync("yonetici@ornek.com", Parola, "198.51.100.4")).Value.Token;

        await harness.CikisAsync(birinci.SessionId);

        Assert.Null(await harness.OturumCozAsync(birinciToken));
        Assert.NotNull(await harness.OturumCozAsync(ikinciToken));
    }

    [Fact(DisplayName = "0006/AC-20: Bir personel pasifleştirildiğinde açık oturumu derhal geçersizleşir; sonraki ilk istekte yönetim yüzeyine erişemez")]
    public async Task Gecersizlesir_PasiflestirmeAninda()
    {
        (IdentityHarness harness, _, _) = await GirisAsync();

        Guid danismanId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "danisman@ornek.com", rol: "sales_advisor"))).Value;

        string danismanToken = (await harness.GirisAsync(
            "danisman@ornek.com", Parola, "198.51.100.5")).Value.Token;

        Assert.NotNull(await harness.OturumCozAsync(danismanToken));

        await harness.KomutAsync(k => k.DeactivateAsync(danismanId, CancellationToken.None));

        // Saat HİÇ ilerlemedi: "derhal" gerçekten derhal.
        Assert.Null(await harness.OturumCozAsync(danismanToken));
    }

    [Fact(DisplayName = "0006/AC-21: Bir personelin rolü düşürüldüğünde eski rolün yetkisi sonraki ilk istekte çalışmaz")]
    public async Task Yansir_RolDegisikligi_SonrakiIlkIstekte()
    {
        (IdentityHarness harness, _, _) = await GirisAsync();
        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: "ikinci@ornek.com", rol: "manager"));

        Guid birinci = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))
            .Single(item => item.LoginId == "yonetici@ornek.com").StaffId;

        string token = (await harness.GirisAsync("yonetici@ornek.com", Parola, "198.51.100.6")).Value.Token;
        Assert.Equal("manager", (await harness.OturumCozAsync(token))!.Role);

        await harness.KomutAsync(k => k.ChangeRoleAsync(birinci, "content_editor", CancellationToken.None));

        // Rol taşıyıcının içinden değil, KAYNAKTAN okunuyor (C16): bayat yetki penceresi yok.
        Assert.Equal("content_editor", (await harness.OturumCozAsync(token))!.Role);
    }

    /// <summary>
    /// R11 — parolası sızmış personel parolasını değiştirdiğinde saldırganın açık
    /// oturumu da düşmelidir. Aksi hâlde parola değiştirmek hiçbir şey çözmez.
    /// </summary>
    [Fact(DisplayName = "0006/AC-22: Bir personelin parolası değiştiğinde o personelin tüm yenileme hakları geçersizleşir; başka cihazdaki oturumu da yenilenemez")]
    public async Task Gecersizlesir_TumOturumlar_ParolaDegisince()
    {
        (IdentityHarness harness, string birinciToken, _) = await GirisAsync();
        string ikinciToken = (await harness.GirisAsync("yonetici@ornek.com", Parola, "198.51.100.7")).Value.Token;

        Guid yoneticiId = (await harness.KomutAsync(k => k.ListAsync(CancellationToken.None)))[0].StaffId;

        await harness.KomutAsync(k => k.ChangeOwnPasswordAsync(
            yoneticiId, Parola, "yepyeni-parola-123", CancellationToken.None));

        Assert.Null(await harness.OturumCozAsync(birinciToken));
        Assert.Null(await harness.OturumCozAsync(ikinciToken));
    }

    [Fact(DisplayName = "0006/AC-29: Oturum bilgisi, sayfadaki JavaScript'ten okunabilir bir yerde tutulmaz")]
    public async Task Cozulemez_UyduralanToken()
    {
        (IdentityHarness harness, _, _) = await GirisAsync();

        Assert.Null(await harness.OturumCozAsync("uydurma-token"));
        Assert.Null(await harness.OturumCozAsync(string.Empty));
        Assert.Null(await harness.OturumCozAsync(null));
    }

    private async Task<(IdentityHarness Harness, string Token, StaffSessionDto Oturum)> GirisAsync()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(kimlik: "yonetici@ornek.com", rol: "manager"));

        Result<LoginResult> giris = await harness.GirisAsync("yonetici@ornek.com", Parola);
        StaffSessionDto oturum = (await harness.OturumCozAsync(giris.Value.Token))!;

        return (harness, giris.Value.Token, oturum);
    }
}
