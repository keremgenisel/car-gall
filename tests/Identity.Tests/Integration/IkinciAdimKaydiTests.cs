using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// R12/D12 — ikinci adımın kaydı. Bu depodaki **ilk kayıt tablosu**: üç seçenek
/// ölçülmüştü (tüketicisi olmayan bir entegrasyon olayı, yapılandırılmış log, kayıt
/// tablosu) ve kazanan üçüncüsü oldu.
///
/// Kaydın asıl işi D6'nın yazılı açığını azaltmak: ilk parola sistem dışından iletiliyor
/// ve onu ele geçiren biri personelden önce kendi uygulamasını kaydedebilir. Bunu
/// kapatmak `0006` D1'i değiştirmeyi gerektirir; azaltıcı tek şey **beklenmeyen bir
/// kurulumun görünür olması**.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class IkinciAdimKaydiTests(IdentityDatabase database)
{
    private const string YoneticiKimlik = "yonetici@ornek.com";
    private const string DanismanKimlik = "danisman@ornek.com";
    private const string Parola = "cok-guclu-parola";

    [Fact(DisplayName = "0013/AC-58: Kurulum, **kapatma**, sıfırlama ve kurtarma kodu kullanımı kayda yazılır")]
    public async Task Yazar_DortOlayi()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid yoneticiId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: YoneticiKimlik, parola: Parola, rol: "manager"))).Value;

        Guid danismanId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: DanismanKimlik, parola: Parola, rol: "sales_advisor"))).Value;

        // 1) Kurulum — iki personel için.
        (string sir, RecoveryCodeSet kodlar) = await harness.IkinciAdimKurVeKodlariAlAsync(yoneticiId);
        string danismanSir = await harness.IkinciAdimKurAsync(danismanId);

        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        // 2) Kurtarma kodu kullanımı.
        string araDurum = (await harness.GirisAsync(YoneticiKimlik, Parola)).Value.PendingToken!;
        Assert.True((await harness.IkinciAdimGecAsync(araDurum, kodlar.Codes[0])).IsSuccess);

        // 3) Kapatma — opsiyonel rolde.
        Assert.True((await harness.IkinciAdimAsync(komutlar => komutlar.DisableAsync(
            danismanId, Parola, harness.Kod(danismanSir), CancellationToken.None))).IsSuccess);

        // 4) Sıfırlama — Yönetici kendisini sıfırlıyor.
        Assert.True((await harness.IkinciAdimAsync(komutlar =>
            komutlar.ResetAsync(yoneticiId, yoneticiId, CancellationToken.None))).IsSuccess);

        await using IdentityDbContext context = database.CreateContext();
        List<TwoFactorAuditEntry> kayit = await context.TwoFactorAudit.AsNoTracking().ToListAsync();

        Assert.Equal(2, kayit.Count(satir => satir.Event is TwoFactorAuditEvent.Enrolled));
        Assert.Single(kayit, satir => satir.Event is TwoFactorAuditEvent.RecoveryCodeUsed);
        Assert.Single(kayit, satir => satir.Event is TwoFactorAuditEvent.Disabled);
        Assert.Single(kayit, satir => satir.Event is TwoFactorAuditEvent.Reset);

        // Kaydın KONUSU ve işlemi YAPAN ayrı alanlar: "beklenmeyen bir kurulum görünür
        // olsun" ancak kimin yaptığı yazılıysa anlam taşır.
        TwoFactorAuditEntry sifirlama = kayit.Single(satir => satir.Event is TwoFactorAuditEvent.Reset);

        Assert.Equal(yoneticiId, sifirlama.StaffId.Value);
        Assert.Equal(yoneticiId, sifirlama.ActorStaffId.Value);
        Assert.All(kayit, satir => Assert.Equal(harness.Saat.UtcNow, satir.OccurredAt, TimeSpan.FromMinutes(5)));
    }

    [Fact(DisplayName = "0013/AC-58: Kurulum, **kapatma**, sıfırlama ve kurtarma kodu kullanımı kayda yazılır")]
    public async Task Yazar_KumeYenilemesini()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid yoneticiId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: YoneticiKimlik, parola: Parola, rol: "manager"))).Value;

        string sir = await harness.IkinciAdimKurAsync(yoneticiId);

        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        Assert.True((await harness.IkinciAdimAsync(komutlar =>
            komutlar.RegenerateRecoveryCodesAsync(yoneticiId, harness.Kod(sir), CancellationToken.None)))
            .IsSuccess);

        await using IdentityDbContext context = database.CreateContext();

        Assert.Single(
            await context.TwoFactorAudit.AsNoTracking().ToListAsync(),
            satir => satir.Event is TwoFactorAuditEvent.RecoveryCodesRegenerated);
    }

    [Fact(DisplayName = "0013/AC-59: Başarısız denemeler kayda **girmez**")]
    public async Task Yazmaz_BasarisizDenemeleri()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid yoneticiId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: YoneticiKimlik, parola: Parola, rol: "manager"))).Value;

        await harness.IkinciAdimKurAsync(yoneticiId);

        harness.Saat.Advance(TimeSpan.FromSeconds(31));

        string araDurum = (await harness.GirisAsync(YoneticiKimlik, Parola)).Value.PendingToken!;

        for (int deneme = 0; deneme < 3; deneme++)
        {
            Assert.True((await harness.IkinciAdimGecAsync(araDurum, "000000")).IsFailure);
        }

        await using IdentityDbContext context = database.CreateContext();
        List<TwoFactorAuditEntry> kayit = await context.TwoFactorAudit.AsNoTracking().ToListAsync();

        // Yalnız kurulum var: başarısız denemeleri kilitlenme sayacı taşıyor ve her
        // denemeyi yazmak tabloyu kaba kuvvetin büyüteci yapardı.
        Assert.Single(kayit);
        Assert.Equal(TwoFactorAuditEvent.Enrolled, kayit[0].Event);
    }

    [Fact(DisplayName = "0013/AC-61: Kayıt satırı çevrilmiş metin değil **kod** saklar")]
    public async Task Saklar_KoduCevrilmisAdiDegil()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Guid yoneticiId = (await harness.PersonelEkleAsync(IdentityHarness.EkleKomutu(
            kimlik: YoneticiKimlik, parola: Parola, rol: "manager"))).Value;

        await harness.IkinciAdimKurAsync(yoneticiId);

        await using IdentityDbContext context = database.CreateContext();

        // Kolon HAM okunuyor: enum'un int karşılığı duruyor, adı ya da çevirisi değil
        // (C7, Yasak Listesi 22).
        List<int> olaylar = await context.Database
            .SqlQuery<int>($"SELECT event AS \"Value\" FROM identity.two_factor_audit")
            .ToListAsync();

        Assert.Equal([(int)TwoFactorAuditEvent.Enrolled], olaylar);
    }
}
