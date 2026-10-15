using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// İkinci adımın kaldırıldığı üç yol: personelin **kapatması** (yalnız opsiyonel rolde),
/// kümeyi **yenilemesi** ve Yöneticinin **sıfırlaması**. Üçü farklı şey ister ve bu
/// bilinçli: yalnız açık oturumla yapılabilen bir kaldırma, çalınmış bir oturumu faktörün
/// kalıcı kaybına çevirirdi (D23).
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class IkinciAdimSifirlamaTests(IdentityDatabase database)
{
    private const string YoneticiKimlik = "yonetici@ornek.com";
    private const string DanismanKimlik = "danisman@ornek.com";
    private const string Parola = "cok-guclu-parola";

    [Fact(DisplayName = "0013/AC-39: Kurtarma kodu kümesinin yenilenmesi oturum **ve** taze bir kod ister; ikisinden biri eksikse yenileme reddedilir")]
    public async Task Ister_TazeKod_Yenilemede()
    {
        (IdentityHarness harness, Guid yoneticiId, string sir, RecoveryCodeSet eski) = await KurAsync();

        Result<RecoveryCodeSet> kodsuz = await harness.IkinciAdimAsync(
            komutlar => komutlar.RegenerateRecoveryCodesAsync(yoneticiId, null, CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorInvalid, kodsuz.Error.Code);

        Result<RecoveryCodeSet> yanlisKodla = await harness.IkinciAdimAsync(
            komutlar => komutlar.RegenerateRecoveryCodesAsync(yoneticiId, "000000", CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorInvalid, yanlisKodla.Error.Code);

        // Kurulumu OLMAYAN personel için yenileme de yok: kaldırılacak bir küme yok.
        Guid kurulumsuzId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: "kurulumsuz@ornek.com", rol: "sales_advisor"))).Value;

        Result<RecoveryCodeSet> kurulumsuz = await harness.IkinciAdimAsync(komutlar =>
            komutlar.RegenerateRecoveryCodesAsync(kurulumsuzId, harness.Kod(sir), CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorNotEnrolled, kurulumsuz.Error.Code);
    }

    [Fact(DisplayName = "0013/AC-40: Yenileme kalan kodların **hepsini** düşürür ve yeni kümeyi **bir kez** gösterir")]
    public async Task Duser_TumKalanlar_Yenilemede()
    {
        (IdentityHarness harness, Guid yoneticiId, string sir, RecoveryCodeSet eski) = await KurAsync();

        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        Result<RecoveryCodeSet> yeni = await harness.IkinciAdimAsync(komutlar =>
            komutlar.RegenerateRecoveryCodesAsync(yoneticiId, harness.Kod(sir), CancellationToken.None));

        Assert.True(yeni.IsSuccess);
        Assert.Equal(IdentityHarness.KurtarmaKoduSayisi, yeni.Value.Codes.Count);
        Assert.Empty(yeni.Value.Codes.Intersect(eski.Codes, StringComparer.Ordinal));

        // Eski küme GEÇERSİZ: girişte artık kabul edilmiyor.
        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        string araDurum = (await harness.GirisAsync(YoneticiKimlik, Parola)).Value.PendingToken!;

        Assert.Equal(
            IdentityErrors.TwoFactorInvalid,
            (await harness.IkinciAdimGecAsync(araDurum, eski.Codes[0])).Error.Code);

        Result<TwoFactorStatus> durum = await harness.IkinciAdimAsync(
            komutlar => komutlar.GetStatusAsync(yoneticiId, CancellationToken.None));

        Assert.Equal(IdentityHarness.KurtarmaKoduSayisi, durum.Value.RemainingRecoveryCodes);
    }

    [Fact(DisplayName = "0013/AC-41: Opsiyonel roldeki personel ikinci adımı **mevcut parolası ve geçerli bir kodla** kapatır; ikisinden biri eksik veya yanlışsa kapatma reddedilir")]
    public async Task Kapatir_ParolaVeKodla_OpsiyonelRolde()
    {
        (IdentityHarness harness, _, _, _) = await KurAsync();

        Guid danismanId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: DanismanKimlik, parola: Parola, rol: "sales_advisor"))).Value;

        string sir = await harness.IkinciAdimKurAsync(danismanId);

        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        Assert.Equal(
            IdentityErrors.CurrentPasswordInvalid,
            (await harness.IkinciAdimAsync(komutlar => komutlar.DisableAsync(
                danismanId, "yanlis-parola-123", harness.Kod(sir), CancellationToken.None))).Error.Code);

        Assert.Equal(
            IdentityErrors.TwoFactorInvalid,
            (await harness.IkinciAdimAsync(komutlar => komutlar.DisableAsync(
                danismanId, Parola, "000000", CancellationToken.None))).Error.Code);

        Assert.True((await harness.IkinciAdimAsync(komutlar => komutlar.DisableAsync(
            danismanId, Parola, harness.Kod(sir), CancellationToken.None))).IsSuccess);

        Result<TwoFactorStatus> durum = await harness.IkinciAdimAsync(
            komutlar => komutlar.GetStatusAsync(danismanId, CancellationToken.None));

        Assert.False(durum.Value.Enrolled);
        Assert.False(durum.Value.Required);
        Assert.Equal(0, durum.Value.RemainingRecoveryCodes);
    }

    [Fact(DisplayName = "0013/AC-42: Zorunlu rolde kapatma **yoktur**: istek reddedilir ve tek yol Yöneticinin sıfırlamasıdır")]
    public async Task Yok_Kapatma_ZorunluRolde()
    {
        (IdentityHarness harness, Guid yoneticiId, string sir, RecoveryCodeSet eski) = await KurAsync();

        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        Result kapatma = await harness.IkinciAdimAsync(komutlar =>
            komutlar.DisableAsync(yoneticiId, Parola, harness.Kod(sir), CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorDisableNotAllowed, kapatma.Error.Code);

        Result<TwoFactorStatus> durum = await harness.IkinciAdimAsync(
            komutlar => komutlar.GetStatusAsync(yoneticiId, CancellationToken.None));

        Assert.True(durum.Value.Enrolled);
    }

    [Fact(DisplayName = "0013/AC-43: Yönetici başka bir personelin ikinci adımını sıfırlar; sıfırlanan personel bir sonraki girişinde yeniden kurar")]
    public async Task Sifirlar_YoneticiBaskasini()
    {
        (IdentityHarness harness, Guid yoneticiId, _, _) = await KurAsync();

        Guid danismanId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: DanismanKimlik, parola: Parola, rol: "sales_advisor"))).Value;

        await harness.IkinciAdimKurAsync(danismanId);

        Assert.True((await harness.IkinciAdimAsync(komutlar =>
            komutlar.ResetAsync(yoneticiId, danismanId, CancellationToken.None))).IsSuccess);

        // Yeniden kurabiliyor: sıfırlanan hesapta kurulum satırı hiç yok.
        Result<TwoFactorEnrollmentStarted> yeniden = await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(danismanId, CancellationToken.None));

        Assert.True(yeniden.IsSuccess);
    }

    [Fact(DisplayName = "0013/AC-44: Sıfırlama bir **güvenlik işlemidir**: personelin açık oturumları düşer ve kalan kurtarma kodları geçersizleşir")]
    public async Task Duserir_Oturumlari_VeKodlari()
    {
        (IdentityHarness harness, Guid yoneticiId, string sir, RecoveryCodeSet eski) = await KurAsync();

        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        // Açık bir oturum kuruyoruz: sıfırlama onu düşürmeli.
        string araDurum = (await harness.GirisAsync(YoneticiKimlik, Parola)).Value.PendingToken!;
        string oturum = IdentityHarness.OturumTokeni(await harness.IkinciAdimGecAsync(araDurum, harness.Kod(sir)));

        Assert.NotNull(await harness.OturumCozAsync(oturum));

        Assert.True((await harness.IkinciAdimAsync(komutlar =>
            komutlar.ResetAsync(yoneticiId, yoneticiId, CancellationToken.None))).IsSuccess);

        // İki katman: damga döndü VE satır iptal edildi (`ResetPassword` deseni).
        Assert.Null(await harness.OturumCozAsync(oturum));

        await using IdentityDbContext context = database.CreateContext();

        Assert.Empty(await context.RecoveryCodes.ToListAsync());
        Assert.Empty(await context.PendingLogins.ToListAsync());
    }

    [Fact(DisplayName = "0013/AC-45: Yönetici **kendi** ikinci adımını da sıfırlayabilir")]
    public async Task Sifirlar_YoneticiKendini()
    {
        (IdentityHarness harness, Guid yoneticiId, _, _) = await KurAsync();

        Result sifirlama = await harness.IkinciAdimAsync(
            komutlar => komutlar.ResetAsync(yoneticiId, yoneticiId, CancellationToken.None));

        Assert.True(sifirlama.IsSuccess);

        // C10/D32 — son aktif Yönetici koruması burada ÇALIŞMAZ ve bu bilinçli: sıfırlama
        // faktörü KALDIRIR, eklemez; hiçbir hesabı kilitleyemez. D37 bedeli yazıyor.
        Result<TwoFactorStatus> durum = await harness.IkinciAdimAsync(
            komutlar => komutlar.GetStatusAsync(yoneticiId, CancellationToken.None));

        Assert.False(durum.Value.Enrolled);
    }

    [Fact(DisplayName = "0013/AC-43: Yönetici başka bir personelin ikinci adımını sıfırlar; sıfırlanan personel bir sonraki girişinde yeniden kurar")]
    public async Task Reddedilir_KurulumuOlmayaniSifirlama()
    {
        (IdentityHarness harness, Guid yoneticiId, _, _) = await KurAsync();

        Guid danismanId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: DanismanKimlik, rol: "sales_advisor"))).Value;

        Result sonuc = await harness.IkinciAdimAsync(
            komutlar => komutlar.ResetAsync(yoneticiId, danismanId, CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorNotEnrolled, sonuc.Error.Code);
    }

    private async Task<(IdentityHarness Harness, Guid YoneticiId, string Sir, RecoveryCodeSet Kodlar)>
        KurAsync()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid yoneticiId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: YoneticiKimlik, parola: Parola, rol: "manager"))).Value;

        (string sir, RecoveryCodeSet kodlar) = await harness.IkinciAdimKurVeKodlariAlAsync(yoneticiId);

        return (harness, yoneticiId, sir, kodlar);
    }
}
