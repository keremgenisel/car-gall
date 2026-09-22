using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// 0007 — invariant koruması EŞZAMANLILIK altında.
///
/// Bu dosyadaki her test iki isteği GERÇEKTEN aynı anda başlatır ve her istek kendi
/// `DbContext`'ini kullanır: tek bağlamda koşan bir "eşzamanlılık" testi yalnızca EF'in
/// change tracker'ını ölçerdi, veritabanının kilidini değil.
///
/// Turlar tekrarlıdır (AC-21): yarış tek turda her zaman gerçekleşmez. Doğru kodda sonuç
/// her turda aynıdır; yanlış kodda bir turun kaçması kaçınılmaz değildir.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class EsZamanlilikTests(IdentityDatabase database)
{
    /// <summary>AC-21 — spec'in verdiği tur sayısı.</summary>
    private const int Tur = 5;

    private const string BirinciKimlik = "birinci@ornek.com";
    private const string IkinciKimlik = "ikinci@ornek.com";

    [Fact(DisplayName = "0007/AC-1: İki eşzamanlı istek son iki aktif Yöneticiden farklı ikisini pasifleştirmeye çalıştığında yalnızca biri başarılı olur; diğeri `identity.last_active_manager` ile reddedilir")]
    public async Task Gecer_YalnizcaBiri_FarkliIkiYoneticiPasiflestirilirken()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, Guid birinci, Guid ikinci) = await IkiYoneticiKurAsync();

            Result[] sonuclar = await EsZamanliAsync(
                harness,
                komutlar => komutlar.DeactivateAsync(birinci, CancellationToken.None),
                komutlar => komutlar.DeactivateAsync(ikinci, CancellationToken.None));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            Assert.Equal(IdentityErrors.LastActiveManager, sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);
        }
    }

    [Fact(DisplayName = "0007/AC-2: AC-1'in ardından galeride tam olarak bir aktif Yönetici kalır")]
    public async Task Kalir_TamOlarakBirAktifYonetici()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, Guid birinci, Guid ikinci) = await IkiYoneticiKurAsync();

            await EsZamanliAsync(
                harness,
                komutlar => komutlar.DeactivateAsync(birinci, CancellationToken.None),
                komutlar => komutlar.DeactivateAsync(ikinci, CancellationToken.None));

            Assert.Equal(1, await AktifYoneticiSayisiAsync());
        }
    }

    /// <summary>
    /// Yarışın en sessiz hâli: ikinci istek kilide girmeden ÖNCE yüklediği agregatla
    /// bekler. Kilit açıldığında EF'in izlediği nesne hâlâ bayattır (`Active`) ve
    /// `AlreadyDeactivated` koruması tetiklenmez — kayıt ikinci kez pasifleştirilir,
    /// güvenlik damgası ikinci kez döner, outbox'a ikinci duyuru düşer.
    ///
    /// Yan etkinin sayısı tek tek ölçülüyor; "başarısız oldu" demek yetmez, çünkü
    /// başarılı da olabilir ve yine de yanlış olur.
    /// </summary>
    [Fact(DisplayName = "0007/AC-3: İki eşzamanlı istek aynı Yöneticiyi pasifleştirmeye çalıştığında ikincisi hiçbir kaydı değiştirmez ve kararlı bir hata koduyla döner")]
    public async Task Degistirmez_HicbirKaydi_AyniYoneticiIkinciKez()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, Guid birinci, _) = await IkiYoneticiKurAsync();

            Result[] sonuclar = await EsZamanliAsync(
                harness,
                komutlar => komutlar.DeactivateAsync(birinci, CancellationToken.None),
                komutlar => komutlar.DeactivateAsync(birinci, CancellationToken.None));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));

            Result reddedilen = sonuclar.Single(sonuc => sonuc.IsFailure);
            Assert.Equal(IdentityErrors.AlreadyDeactivated, reddedilen.Error.Code);

            // Tek pasifleştirme = tek duyuru. İkinci istek kaydı yeniden yazsaydı burada
            // iki satır olurdu ve alıcı aynı olayı iki kez görürdü.
            Assert.Equal(1, await DuyuruSayisiAsync());
        }
    }

    [Fact(DisplayName = "0007/AC-4: İki eşzamanlı istek son iki aktif Yöneticiden farklı ikisinin rolünü düşürmeye çalıştığında yalnızca biri başarılı olur")]
    public async Task Gecer_YalnizcaBiri_FarkliIkiYoneticininRoluDusurulurken()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, Guid birinci, Guid ikinci) = await IkiYoneticiKurAsync();

            Result[] sonuclar = await EsZamanliAsync(
                harness,
                komutlar => komutlar.ChangeRoleAsync(birinci, "sales_advisor", CancellationToken.None),
                komutlar => komutlar.ChangeRoleAsync(ikinci, "sales_advisor", CancellationToken.None));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            Assert.Equal(IdentityErrors.LastActiveManager, sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);
            Assert.Equal(1, await AktifYoneticiSayisiAsync());
        }
    }

    /// <summary>
    /// İki komut aynı invariant'ı korur ama farklı yollardan yazar. Kilit kümesi ikisinde
    /// de AYNI olmasaydı — biri yalnız hedefi, diğeri yalnız "diğerleri"ni kilitleseydi —
    /// hiç serileşmez ve galeri yöneticisiz kalırdı.
    /// </summary>
    [Fact(DisplayName = "0007/AC-5: Pasifleştirme ve rol düşürme birbirine karşı da serileşir: biri pasifleştirirken diğeri rol düşürüyorsa ikisi birden geçemez")]
    public async Task Serilesir_PasiflestirmeVeRolDusurme()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, Guid birinci, Guid ikinci) = await IkiYoneticiKurAsync();

            Result[] sonuclar = await EsZamanliAsync(
                harness,
                komutlar => komutlar.DeactivateAsync(birinci, CancellationToken.None),
                komutlar => komutlar.ChangeRoleAsync(ikinci, "sales_advisor", CancellationToken.None));

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            Assert.Equal(IdentityErrors.LastActiveManager, sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);
            Assert.Equal(1, await AktifYoneticiSayisiAsync());
        }
    }

    /// <summary>
    /// İki komutu aynı anda başlatır. `KomutAsync` her çağrıda kendi bağlamını açtığı için
    /// bu, gerçek boru hattındaki iki ayrı isteğin karşılığıdır.
    /// </summary>
    private static async Task<Result[]> EsZamanliAsync(
        IdentityHarness harness,
        Func<StaffCommands, Task<Result>> birinci,
        Func<StaffCommands, Task<Result>> ikinci)
    {
        Task<Result> a = harness.KomutAsync(birinci);
        Task<Result> b = harness.KomutAsync(ikinci);

        return await Task.WhenAll(a, b);
    }

    private async Task<(IdentityHarness Harness, Guid Birinci, Guid Ikinci)> IkiYoneticiKurAsync()
    {
        await database.ResetAsync();
        IdentityHarness harness = new(database);

        Assert.True((await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: BirinciKimlik, rol: "manager"))).IsSuccess);

        Assert.True((await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: IkinciKimlik, rol: "manager"))).IsSuccess);

        IReadOnlyList<StaffListItem> liste =
            await harness.KomutAsync(komutlar => komutlar.ListAsync(CancellationToken.None));

        return (
            harness,
            liste.Single(item => item.LoginId == BirinciKimlik).StaffId,
            liste.Single(item => item.LoginId == IkinciKimlik).StaffId);
    }

    private async Task<int> AktifYoneticiSayisiAsync()
    {
        await using IdentityDbContext context = database.CreateContext();

        return await context.Staff
            .AsNoTracking()
            .CountAsync(staff => staff.Role == StaffRole.Manager && staff.Status == StaffStatus.Active);
    }

    private async Task<int> DuyuruSayisiAsync()
    {
        await using IdentityDbContext context = database.CreateContext();

        return await context.OutboxMessages.AsNoTracking().CountAsync();
    }
}
