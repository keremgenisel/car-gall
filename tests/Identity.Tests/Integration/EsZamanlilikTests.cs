using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
    /// "Reddedildi" demek yetmez: reddedilen istek geriye hiçbir iz bırakmamalı. Duyuru
    /// düşseydi `Leads` o personelin açık taleplerini yeniden atardı — personel hâlâ
    /// aktifken.
    /// </summary>
    [Fact(DisplayName = "0007/AC-18: Reddedilen istekte hiçbir yan etki kalmaz: ne satır değişir ne outbox'a duyuru düşer")]
    public async Task Birakmaz_YanEtki_ReddedilenIstekte()
    {
        (IdentityHarness harness, Guid birinci, Guid ikinci) = await IkiYoneticiKurAsync();

        Assert.True((await harness.KomutAsync(k => k.DeactivateAsync(birinci, CancellationToken.None))).IsSuccess);
        await DuyurulariTemizleAsync();

        Result sonuc = await harness.KomutAsync(k => k.DeactivateAsync(ikinci, CancellationToken.None));

        Assert.True(sonuc.IsFailure);
        Assert.Equal(IdentityErrors.LastActiveManager, sonuc.Error.Code);
        Assert.Equal(1, await AktifYoneticiSayisiAsync());
        Assert.Equal(0, await DuyuruSayisiAsync());
    }

    /// <summary>
    /// Kilit sabit sırada (`ORDER BY id`) alınmasaydı iki istek satırları ters sırada
    /// kilitler ve PostgreSQL birini deadlock kurbanı seçerdi; o durumda sonuç
    /// `concurrency.lost` olurdu, `last_active_manager` değil.
    /// </summary>
    [Fact(DisplayName = "0007/AC-19: Karara giren satırlar sabit bir sırada kilitlenir; eşzamanlı iki istek karşılıklı kilitlenmeye (deadlock) düşmez")]
    public async Task Dusmez_KarsilikliKilitlenmeye()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, Guid birinci, Guid ikinci) = await IkiYoneticiKurAsync();

            // Kasten ters yönde: iki istek aynı kümeyi zıt hedeflerle kilitliyor.
            Result[] sonuclar = await EsZamanliAsync(
                harness,
                komutlar => komutlar.DeactivateAsync(ikinci, CancellationToken.None),
                komutlar => komutlar.DeactivateAsync(birinci, CancellationToken.None));

            Assert.DoesNotContain(sonuclar, sonuc => sonuc.IsFailure && sonuc.Error.Code == ConcurrencyErrors.Lost);
        }
    }

    /// <summary>
    /// AP-03 — eşzamanlılık testi "bazen geçen" test demek değildir. Beş turun HEPSİ aynı
    /// sonucu vermeli; bir tur farklı çıkarsa koruma deterministik değildir ve o test
    /// güven vermez.
    /// </summary>
    [Fact(DisplayName = "0007/AC-21: Eşzamanlılık testleri gerçek PostgreSQL üzerinde koşar ve art arda beş turda da aynı sonucu verir")]
    public async Task Ayni_BesTurda()
    {
        List<string> turSonuclari = [];

        for (int tur = 0; tur < Tur; tur++)
        {
            (IdentityHarness harness, Guid birinci, Guid ikinci) = await IkiYoneticiKurAsync();

            Result[] sonuclar = await EsZamanliAsync(
                harness,
                komutlar => komutlar.DeactivateAsync(birinci, CancellationToken.None),
                komutlar => komutlar.DeactivateAsync(ikinci, CancellationToken.None));

            turSonuclari.Add(
                $"basarili={sonuclar.Count(sonuc => sonuc.IsSuccess)};" +
                $"kod={sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code};" +
                $"aktif={await AktifYoneticiSayisiAsync()}");
        }

        Assert.Equal(Tur, turSonuclari.Count);
        Assert.Single(turSonuclari.Distinct(StringComparer.Ordinal));
    }

    /// <summary>
    /// R6'nın iki red türü. Karamsar kilitte ikinci istek normalde KAYBETMEZ — bekler ve
    /// güncel sayıyla kural gereği reddedilir. Kaybın gerçek kaynağı kilit zaman aşımıdır:
    /// bu test kilidi dışarıdan tutup isteği o duvara çarptırıyor.
    ///
    /// İki kodun AYRI olması kozmetik değil: kural gereği red kalıcıdır ve o kullanıcıya
    /// "tekrar deneyin" denirse sonsuza kadar denemesi söylenmiş olur.
    /// </summary>
    [Fact(DisplayName = "0007/AC-12: Kural gereği red ile eşzamanlılık kaybı farklı hata kodlarıyla döner; iki durum hata kodundan ayırt edilebilir")]
    public async Task AyirtEdilir_KuralGeregiRedVeEsZamanlilikKaybi()
    {
        (IdentityHarness harness, Guid birinci, Guid ikinci) = await IkiYoneticiKurAsync();

        // Kural gereği red: tek aktif Yönetici kalınca ikincisi reddedilir.
        Assert.True((await harness.KomutAsync(k => k.DeactivateAsync(birinci, CancellationToken.None))).IsSuccess);

        Result kuralGeregi = await harness.KomutAsync(k => k.DeactivateAsync(ikinci, CancellationToken.None));

        Assert.True(kuralGeregi.IsFailure);
        Assert.Equal(IdentityErrors.LastActiveManager, kuralGeregi.Error.Code);

        // Eşzamanlılık kaybı: satırlar dışarıdan kilitliyken kısa süreli istek duvara çarpar.
        await using IdentityDbContext tutan = database.CreateContext();
        await using IDbContextTransaction kilit = await tutan.Database.BeginTransactionAsync();
        await KilitleAsync(tutan);

        Result kayip = await harness.KomutAsync(
            k => k.DeactivateAsync(ikinci, CancellationToken.None),
            new InvariantGuardOptions(TimeSpan.FromMilliseconds(200)));

        await kilit.RollbackAsync();

        Assert.True(kayip.IsFailure);
        Assert.Equal(ConcurrencyErrors.Lost, kayip.Error.Code);

        // Kriterin kendisi: ikisi ayırt edilebiliyor.
        Assert.NotEqual(kuralGeregi.Error.Code, kayip.Error.Code);
    }

    /// <summary>
    /// Çakışmanın GERÇEKTEN kurulduğunu gözler. Bu olmadan AC-1'in yeşili şüpheli kalırdı:
    /// iki istek hiç çakışmamış da olabilir ve test yine geçerdi.
    ///
    /// İddia yalnızca ALT sınırdır — "şu ana kadar bitmedi". Üst sınır iddia edilseydi
    /// ("şu kadar sürede bitti") test CI'ın yavaşlığında rastgele kırılırdı (AP-03).
    /// </summary>
    [Fact(DisplayName = "0007/AC-20: Test, çakışmanın gerçekten kurulduğunu gözler: ikinci isteğin birinci commit edene kadar beklediği doğrulanır")]
    public async Task Bekler_IkinciIstek_BirinciCommitEdene_Kadar()
    {
        (IdentityHarness harness, Guid birinci, _) = await IkiYoneticiKurAsync();

        await using IdentityDbContext tutan = database.CreateContext();
        await using IDbContextTransaction kilit = await tutan.Database.BeginTransactionAsync();
        await KilitleAsync(tutan);

        // Kilit süresi cömert: ölçülen şey zaman aşımı değil, BEKLEME.
        Task<Result> bekleyen = harness.KomutAsync(
            k => k.DeactivateAsync(birinci, CancellationToken.None),
            new InvariantGuardOptions(TimeSpan.FromSeconds(30)));

        await Task.Delay(TimeSpan.FromMilliseconds(500));

        Assert.False(bekleyen.IsCompleted);

        await kilit.CommitAsync();

        Assert.True((await bekleyen).IsSuccess);
    }

    /// <summary>
    /// Korumanın kilitlediği satırların aynısını dışarıdan kilitler.
    /// </summary>
    private static Task KilitleAsync(IdentityDbContext context) =>
        context.Database.ExecuteSqlRawAsync(
            $"SELECT id FROM {IdentityDbContext.Schema}.staff WHERE role = {{0}} AND status = {{1}} ORDER BY id FOR UPDATE",
            [(int)StaffRole.Manager, (int)StaffStatus.Active]);

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

    private async Task DuyurulariTemizleAsync()
    {
        await using IdentityDbContext context = database.CreateContext();

        await context.Database.ExecuteSqlRawAsync($"DELETE FROM {IdentityDbContext.Schema}.outbox_messages");
    }

    private async Task<int> DuyuruSayisiAsync()
    {
        await using IdentityDbContext context = database.CreateContext();

        return await context.OutboxMessages.AsNoTracking().CountAsync();
    }
}
