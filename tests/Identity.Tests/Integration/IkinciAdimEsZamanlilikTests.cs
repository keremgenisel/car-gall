using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// `0013` — ikinci adımın üç "tek kullanımlık" şeyi EŞZAMANLILIK altında: ara durum
/// (AC-15), kabul edilmiş adım (AC-23/D11) ve kurtarma kodu (R4/AC-36).
///
/// <para><b>Bu dosya bir dış bulgunun bedeli ve ardışık testlerin neden yetmediğini
/// gösteriyor.</b> AC-15, AC-23 ve AC-36'nın mevcut testleri *önce doğrula, sonra ikinci
/// kez dene* biçiminde ardışıktı; ardışık bir test bu kusur sınıfını tanımı gereği
/// göremez. Kusur gerçekti: tek ara durum + tek kod iki geçerli oturum üretiyordu, tek
/// kurtarma kodu iki ara durumla iki oturum veriyordu. D11'in kuralı — toleransın açtığı
/// 90 saniyelik pencereyi kapatan şey — tam o pencerede, yani kodu gözlemleyen birinin
/// kullanacağı anda çalışmıyordu.</para>
///
/// <para>Her istek KENDİ `DbContext`'ini kullanıyor (`IkinciAdimGecAsync` her çağrıda
/// yeni bağlam açar): tek bağlamda koşan bir "eşzamanlılık" testi yalnızca EF'in change
/// tracker'ını ölçerdi, veritabanının davranışını değil. Turlar tekrarlı (`0007`/AC-21):
/// yarış tek turda her zaman gerçekleşmez.</para>
///
/// <para>İddia iki parçalı ve ikincisi şart: "biri reddedildi" demek yetmez, çünkü
/// reddedilen istek yine de bir oturum bırakmış olabilir. Oturum SAYISI tek tek
/// ölçülüyor.</para>
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class IkinciAdimEsZamanlilikTests(IdentityDatabase database)
{
    /// <summary>`0007`/AC-21 — spec'in verdiği tur sayısı.</summary>
    private const int Tur = 5;

    private const string Kimlik = "yonetici@ornek.com";
    private const string Parola = "cok-guclu-parola";

    /// <summary>
    /// Kaybeden isteğin kodu `two_factor_pending_expired`: elinden alınan şey ARA DURUM ve
    /// ekranın yapacağı şey parola fazına dönmek (D20).
    /// </summary>
    [Fact(DisplayName = "0013/AC-15, 0013/AC-23: Aynı ara durum ve aynı kodla yapılan iki EŞZAMANLI doğrulama tek oturum üretir; ikincisi reddedilir")]
    public async Task Uretir_TekOturum_AyniAraDurumEsZamanli()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, string sir, _) = await KurAsync();

            string araDurum = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;
            string kod = harness.Kod(sir);

            Result<LoginResult>[] sonuclar = await EsZamanliAsync(
                () => harness.IkinciAdimGecAsync(araDurum, kod),
                () => harness.IkinciAdimGecAsync(araDurum, kod));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            Assert.Equal(
                IdentityErrors.TwoFactorPendingExpired,
                sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);

            Assert.Equal(1, await OturumSayisiAsync());
        }
    }

    /// <summary>
    /// Ara durumlar AYRI, kod aynı: kaybeden isteğin ara durumu hâlâ canlı ve yapacağı şey
    /// taze bir kod yazmak, o yüzden kod `two_factor_invalid` (D20). Ara durumunun da
    /// tükenmemiş olması önemli — tüketilseydi personel sebepsiz yere parola fazına
    /// düşerdi.
    /// </summary>
    [Fact(DisplayName = "0013/AC-23: Aynı kod iki AYRI ara durumla eşzamanlı gönderildiğinde yalnız biri kabul edilir; yeniden oynatma o pencerede de kapalıdır")]
    public async Task KabulEder_YalnizBiri_AyniKodIkiAraDurumla()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, string sir, _) = await KurAsync();

            string birinci = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;
            string ikinci = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;
            string kod = harness.Kod(sir);

            Result<LoginResult>[] sonuclar = await EsZamanliAsync(
                () => harness.IkinciAdimGecAsync(birinci, kod),
                () => harness.IkinciAdimGecAsync(ikinci, kod));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            Assert.Equal(
                IdentityErrors.TwoFactorInvalid,
                sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);

            Assert.Equal(1, await OturumSayisiAsync());

            // Kaybedenin ara durumu TÜKENMEDİ: reddin sebebi kod, ara durum değil.
            Assert.Equal(1, await TukenmisAraDurumSayisiAsync());
        }
    }

    /// <summary>
    /// R4'ün bütün vaadi: *"yeniden kullanılabilir kod, yazılı bir yedek paroladır."* İki
    /// ara durum aynı kurtarma kodunu harcamaya çalışıyor; ara durumun tek kullanımlığı bu
    /// yolu kapatmaya YETMEZ, çünkü ara durumlar ayrı.
    /// </summary>
    [Fact(DisplayName = "0013/AC-36: Aynı kurtarma kodu iki eşzamanlı istekte bir kez harcanır; ikinci istek oturum almaz")]
    public async Task Harcanir_BirKez_AyniKurtarmaKodu()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, _, RecoveryCodeSet kodlar) = await KurAsync();

            string birinci = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;
            string ikinci = (await harness.GirisAsync(Kimlik, Parola)).Value.PendingToken!;

            Result<LoginResult>[] sonuclar = await EsZamanliAsync(
                () => harness.IkinciAdimGecAsync(birinci, kodlar.Codes[0]),
                () => harness.IkinciAdimGecAsync(ikinci, kodlar.Codes[0]));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            Assert.Equal(
                IdentityErrors.TwoFactorInvalid,
                sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);

            Assert.Equal(1, await OturumSayisiAsync());

            // Kalan sayı BİR düştü. İki istek kodu birlikte harcasaydı sayı yine bir
            // düşerdi ama oturum sayısı iki olurdu; iki iddia birlikte ölçülüyor.
            Assert.Equal(1, await KullanilmisKodSayisiAsync());
        }
    }

    private static async Task<Result<LoginResult>[]> EsZamanliAsync(
        Func<Task<Result<LoginResult>>> birinci,
        Func<Task<Result<LoginResult>>> ikinci)
    {
        Task<Result<LoginResult>> a = birinci();
        Task<Result<LoginResult>> b = ikinci();

        return await Task.WhenAll(a, b);
    }

    private async Task<(IdentityHarness Harness, string Sir, RecoveryCodeSet Kodlar)> KurAsync()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid personelId = (await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: Kimlik, parola: Parola, rol: "manager"))).Value;

        (string sir, RecoveryCodeSet kodlar) = await harness.IkinciAdimKurVeKodlariAlAsync(personelId);

        // D11/D45 — onay kodunun adımı tükendi; bir periyot ileri alınmazsa ölçülen şey
        // yarış değil, onayın kendi adımı olurdu.
        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        return (harness, sir, kodlar);
    }

    private async Task<int> OturumSayisiAsync()
    {
        await using IdentityDbContext context = database.CreateContext();

        return await context.Sessions.AsNoTracking().CountAsync();
    }

    private async Task<int> TukenmisAraDurumSayisiAsync()
    {
        await using IdentityDbContext context = database.CreateContext();

        return await context.PendingLogins.AsNoTracking().CountAsync(pending => pending.ConsumedAt != null);
    }

    private async Task<int> KullanilmisKodSayisiAsync()
    {
        await using IdentityDbContext context = database.CreateContext();

        return await context.RecoveryCodes.AsNoTracking().CountAsync(kod => kod.UsedAt != null);
    }
}
