using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// Kurulumun üç kuralı bu dosyanın konusu: sır **bir kez** gösterilir, **doğrulanmamış**
/// kurulum etkinleşmez ve bekleyen kurulum **yaşamaz** (R3, D18). Üçü birlikte "bir kez
/// gösterilir" cümlesini doğru kılıyor — biri eksikse o cümle yalan olur.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class IkinciAdimKurulumTests(IdentityDatabase database)
{
    private const string Kimlik = "yonetici@ornek.com";
    private const string Parola = "cok-guclu-parola";

    [Fact(DisplayName = "0013/AC-1: Kurulum başlatıldığında sunucu bir doğrulama sırrı üretir ve yanıt `otpauth://` adresini **ve** Base32 sırrı **metin olarak** döner; yanıtta görsel bulunmaz")]
    public async Task Doner_AdresVeSirMetinOlarak()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        Result<TwoFactorEnrollmentStarted> sonuc = await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None));

        Assert.True(sonuc.IsSuccess);
        Assert.NotEmpty(sonuc.Value.Secret);
        Assert.StartsWith("otpauth://totp/", sonuc.Value.SecretUri, StringComparison.Ordinal);
        Assert.Contains(IdentityHarness.Kurulus.Replace(" ", "%20", StringComparison.Ordinal),
            sonuc.Value.SecretUri, StringComparison.Ordinal);

        // Yanıtta GÖRSEL yok: kare kodu uç çiziyor (C15/D17). Sunucuda üretilen bir
        // görsel sırrı önbellek ve log yüzeylerine taşırdı.
        Assert.DoesNotContain("data:image", sonuc.Value.SecretUri, StringComparison.Ordinal);
        Assert.DoesNotContain("base64,", sonuc.Value.SecretUri, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0013/AC-3: Doğrulanmamış kurulum etkinleşmez: kurulumu başlatmış ama kodla onaylamamış personel kurulumsuz sayılır ve girişinde ikinci adım istenmez")]
    public async Task SayilirKurulumsuz_OnaylanmamisKurulum()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None));

        Result<LoginResult> giris = await harness.GirisAsync(Kimlik, Parola);

        Assert.True(giris.IsSuccess);
        Assert.Equal(LoginStep.TwoFactorSetupRequired, giris.Value.Step);
        Assert.Null(giris.Value.PendingToken);

        Result<TwoFactorStatus> durum = await harness.IkinciAdimAsync(
            komutlar => komutlar.GetStatusAsync(personelId, CancellationToken.None));

        Assert.False(durum.Value.Enrolled);
    }

    [Fact(DisplayName = "0013/AC-4: Kurulum onayı geçerli bir kod ister; yanlış kodla gönderilen onay kurulumu etkinleştirmez")]
    public async Task Etkinlesmez_YanlisKodla()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None));

        Result<RecoveryCodeSet> onay = await harness.IkinciAdimAsync(
            komutlar => komutlar.ConfirmEnrollmentAsync(personelId, "000000", CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorInvalid, onay.Error.Code);

        Result<TwoFactorStatus> durum = await harness.IkinciAdimAsync(
            komutlar => komutlar.GetStatusAsync(personelId, CancellationToken.None));

        Assert.False(durum.Value.Enrolled);
    }

    [Fact(DisplayName = "0013/AC-5: Bekleyen kurulumun ömrü yapılandırmadan gelir; süresi dolmuş bekleyen kurulumun onayı reddedilir")]
    public async Task Reddedilir_SuresiDolmusBekleyenKurulum()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        TwoFactorEnrollmentStarted baslangic = (await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None))).Value;

        harness.Saat.Advance(IdentityHarness.BekleyenKurulumSuresi);

        Result<RecoveryCodeSet> onay = await harness.IkinciAdimAsync(komutlar =>
            komutlar.ConfirmEnrollmentAsync(personelId, harness.Kod(baslangic.Secret), CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorInvalid, onay.Error.Code);
    }

    [Fact(DisplayName = "0013/AC-6: Kurulum yeniden başlatıldığında yeni bir sır üretilir ve önceki sırrın ürettiği kod artık kabul edilmez")]
    public async Task UretirYeniSir_YenidenBaslatildiginda()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        TwoFactorEnrollmentStarted birinci = (await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None))).Value;

        TwoFactorEnrollmentStarted ikinci = (await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None))).Value;

        Assert.NotEqual(birinci.Secret, ikinci.Secret);

        Result<RecoveryCodeSet> eskiyle = await harness.IkinciAdimAsync(komutlar =>
            komutlar.ConfirmEnrollmentAsync(personelId, harness.Kod(birinci.Secret), CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorInvalid, eskiyle.Error.Code);

        Result<RecoveryCodeSet> yeniyle = await harness.IkinciAdimAsync(komutlar =>
            komutlar.ConfirmEnrollmentAsync(personelId, harness.Kod(ikinci.Secret), CancellationToken.None));

        Assert.True(yeniyle.IsSuccess);
    }

    [Fact(DisplayName = "0013/AC-7: Kurtarma kodları kurulum onayında **bir kez** döner; aynı kümeyi yeniden okutan bir yol yoktur")]
    public async Task Doner_KurtarmaKodlariBirKez()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        TwoFactorEnrollmentStarted baslangic = (await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None))).Value;

        Result<RecoveryCodeSet> onay = await harness.IkinciAdimAsync(komutlar =>
            komutlar.ConfirmEnrollmentAsync(personelId, harness.Kod(baslangic.Secret), CancellationToken.None));

        Assert.True(onay.IsSuccess);

        // Kümeyi yeniden okutan bir yol YOK: ikinci onay `409` ile düşüyor ve durum ucu
        // yalnız KALAN SAYIYI veriyor, kodları vermiyor.
        Result<RecoveryCodeSet> ikinciOnay = await harness.IkinciAdimAsync(komutlar =>
            komutlar.ConfirmEnrollmentAsync(personelId, harness.Kod(baslangic.Secret), CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorAlreadyEnrolled, ikinciOnay.Error.Code);

        // Saklanan şey özet: hiçbir kolonda kodun kendisi yok (C2b, AC-50).
        await using IdentityDbContext context = database.CreateContext();
        List<string> ozetler = await context.RecoveryCodes
            .Select(kod => kod.CodeHash.Value)
            .ToListAsync();

        Assert.All(ozetler, ozet => Assert.DoesNotContain(
            onay.Value.Codes[0], ozet, StringComparison.OrdinalIgnoreCase));
    }

    [Fact(DisplayName = "0013/AC-8: Kurtarma kodu sayısı yapılandırmadan gelir ve üretilen küme yapılandırılan sayıda kod taşır")]
    public async Task Gelir_KodSayisiYapilandirmadan()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        TwoFactorEnrollmentStarted baslangic = (await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None))).Value;

        Result<RecoveryCodeSet> onay = await harness.IkinciAdimAsync(komutlar =>
            komutlar.ConfirmEnrollmentAsync(personelId, harness.Kod(baslangic.Secret), CancellationToken.None));

        Assert.Equal(IdentityHarness.KurtarmaKoduSayisi, onay.Value.Codes.Count);
        Assert.Equal(IdentityHarness.KurtarmaKoduSayisi, onay.Value.Codes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact(DisplayName = "0013/AC-9: Kurulumu zaten olan personelin yeni kurulum isteği `409` ve `identity.two_factor_already_enrolled` ile reddedilir")]
    public async Task Reddedilir_ZatenKurulmussa()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        await harness.IkinciAdimKurAsync(personelId);

        Result<TwoFactorEnrollmentStarted> tekrar = await harness.IkinciAdimAsync(
            komutlar => komutlar.StartEnrollmentAsync(personelId, CancellationToken.None));

        Assert.Equal(IdentityErrors.TwoFactorAlreadyEnrolled, tekrar.Error.Code);
    }

    [Fact(DisplayName = "0013/AC-38: Personel kalan kurtarma kodu sayısını kendi ekranında görür")]
    public async Task Gorur_KalanKurtarmaKoduSayisini()
    {
        (IdentityHarness harness, Guid personelId) = await KurAsync();

        await harness.IkinciAdimKurAsync(personelId);

        Result<TwoFactorStatus> durum = await harness.IkinciAdimAsync(
            komutlar => komutlar.GetStatusAsync(personelId, CancellationToken.None));

        Assert.True(durum.Value.Enrolled);
        Assert.True(durum.Value.Required);
        Assert.Equal(IdentityHarness.KurtarmaKoduSayisi, durum.Value.RemainingRecoveryCodes);
    }

    private async Task<(IdentityHarness Harness, Guid PersonelId)> KurAsync()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid personelId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: Kimlik, parola: Parola, rol: "manager"))).Value;

        return (harness, personelId);
    }
}
