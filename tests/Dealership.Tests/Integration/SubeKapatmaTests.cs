using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Tests.Integration;

/// <summary>
/// 0007 — B5'in ("galeride her zaman en az bir aktif şube kalır") eşzamanlılık altındaki
/// karşılığı. Kapatma komutunun HTTP ucu yoktur (D2); burada komutun KENDİSİ sınanıyor.
///
/// Her istek kendi `DbContext`'ini kullanır: tek bağlamda koşan bir "eşzamanlılık" testi
/// yalnızca EF'in change tracker'ını ölçerdi, veritabanının kilidini değil.
/// </summary>
public sealed class SubeKapatmaTests : IAsyncLifetime
{
    /// <summary>
    /// **Kendi veritabanı.** B5 galeri geneli bir invariant'tır: "iki aktif şube kaldı"
    /// durumunu kurmak, şemadaki BÜTÜN aktif şubeleri saymak demektir. Paylaşılan fikstürde
    /// koşsaydı bu testler seed şubelerini kapatır ve aynı koleksiyondaki 0003 testlerini
    /// düşürürdü — ilk denemede tam olarak bu oldu (`0003/AC-15` ve `0003/AC-31` kırmızı).
    ///
    /// Paylaşılan duruma bağımlı test yazılmaz (docs/testing.md § 4); bu iş de zaten
    /// paylaşılan durumun kendisiyle ilgili.
    /// </summary>
    private readonly DealershipDatabase database = new();

    public Task InitializeAsync() => database.InitializeAsync();

    public Task DisposeAsync() => database.DisposeAsync();

    /// <summary>0007/AC-21 — spec'in verdiği tur sayısı.</summary>
    private const int Tur = 5;

    private static readonly Locale Tr = Locale.Create("tr").Value;
    private static readonly DateTimeOffset Simdi = new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);


    [Fact(DisplayName = "0007/AC-6: İki eşzamanlı istek son iki aktif şubeden farklı ikisini kapatmaya çalıştığında yalnızca biri başarılı olur; diğeri `branch.last_active_branch` ile reddedilir")]
    public async Task Gecer_YalnizcaBiri_FarkliIkiSubeKapatilirken()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (Guid birinci, Guid ikinci) = await IkiAktifSubeKurAsync();

            Result[] sonuclar = await EsZamanliKapatAsync(birinci, ikinci);

            Assert.Equal(1, sonuclar.Count(sonuc => sonuc.IsSuccess));
            Assert.Equal(Branch.LastActiveBranch, sonuclar.Single(sonuc => sonuc.IsFailure).Error.Code);
        }
    }

    [Fact(DisplayName = "0007/AC-7: AC-6'nın ardından galeride tam olarak bir aktif şube kalır")]
    public async Task Kalir_TamOlarakBirAktifSube()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (Guid birinci, Guid ikinci) = await IkiAktifSubeKurAsync();

            await EsZamanliKapatAsync(birinci, ikinci);

            Assert.Equal(1, await AktifSubeSayisiAsync());
        }
    }

    /// <summary>
    /// 0007/AC-19'un şube tarafı. Kilit sabit sırada (`ORDER BY id`) alınmasaydı iki istek
    /// satırları ters sırada kilitler ve PostgreSQL birini deadlock kurbanı seçerdi; o
    /// durumda sonuç `concurrency.lost` olurdu, `last_active_branch` değil.
    /// </summary>
    [Fact(DisplayName = "0007/AC-19: Karara giren satırlar sabit bir sırada kilitlenir; eşzamanlı iki istek karşılıklı kilitlenmeye (deadlock) düşmez")]
    public async Task Dusmez_KarsilikliKilitlenmeye()
    {
        for (int tur = 0; tur < Tur; tur++)
        {
            (Guid birinci, Guid ikinci) = await IkiAktifSubeKurAsync();

            // Kasten ters yönde: iki istek aynı kümeyi zıt hedeflerle kilitliyor.
            Result[] sonuclar = await EsZamanliKapatAsync(ikinci, birinci);

            Assert.DoesNotContain(sonuclar, sonuc => sonuc.IsFailure && sonuc.Error.Code == ConcurrencyErrors.Lost);
        }
    }

    /// <summary>
    /// 0007/AC-18'in şube tarafı. "Reddedildi" demek yetmez: reddedilen istek geriye
    /// hiçbir iz bırakmamalı — ne satır değişmeli ne de outbox'a duyuru düşmeli. Duyuru
    /// düşseydi `VehicleCatalog` o şubedeki araçları vitrinden indirirdi (A8) ve şube
    /// hâlâ açıkken vitrin boşalırdı.
    /// </summary>
    [Fact(DisplayName = "0007/AC-18: Reddedilen istekte hiçbir yan etki kalmaz: ne satır değişir ne outbox'a duyuru düşer")]
    public async Task Birakmaz_YanEtki_ReddedilenIstekte()
    {
        (Guid birinci, Guid ikinci) = await IkiAktifSubeKurAsync();

        Assert.True((await KapatAsync(birinci)).IsSuccess);
        await DuyurulariTemizleAsync();

        // Artık tek aktif şube kaldı; ikincisini kapatma girişimi kural gereği reddedilir.
        Result sonuc = await KapatAsync(ikinci);

        Assert.True(sonuc.IsFailure);
        Assert.Equal(Branch.LastActiveBranch, sonuc.Error.Code);
        Assert.Equal(1, await AktifSubeSayisiAsync());
        Assert.Equal(0, await DuyuruSayisiAsync());
    }

    private async Task<Result[]> EsZamanliKapatAsync(Guid birinci, Guid ikinci)
    {
        Task<Result> a = KapatAsync(birinci);
        Task<Result> b = KapatAsync(ikinci);

        return await Task.WhenAll(a, b);
    }

    private async Task<Result> KapatAsync(Guid branchId)
    {
        await using DealershipDbContext context = database.CreateContext();

        return await Komutlar(context).CloseAsync(branchId, CancellationToken.None);
    }

    private static BranchCommands Komutlar(DealershipDbContext context)
    {
        TestSaati saat = new(Simdi);

        return new BranchCommands(
            context,
            new DealershipUnitOfWork(context, new OutboxWriter<DealershipDbContext>(context, saat), saat),
            InvariantGuardOptions<DealershipDbContext>.Default);
    }

    /// <summary>
    /// Her tur kendi verisini kurar: seed şubeleri dâhil her şey silinir ve iki yeni aktif
    /// şube açılır. Testin ölçtüğü sayı şemadaki bütün aktif şubeler olduğu için başlangıç
    /// durumunun tam olarak bilinmesi gerekiyor.
    /// </summary>
    private async Task<(Guid Birinci, Guid Ikinci)> IkiAktifSubeKurAsync()
    {
        await using DealershipDbContext context = database.CreateContext();

        await context.Database.ExecuteSqlRawAsync("DELETE FROM dealership.outbox_messages");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM dealership.branches");

        Branch birinci = Sube("birinci-sube");
        Branch ikinci = Sube("ikinci-sube");

        context.Branches.AddRange(birinci, ikinci);
        await context.SaveChangesAsync();

        // Açılış duyuruları bu testlerin ölçtüğü şey değil; AC-18 yalnız kapatmanın
        // bıraktığı ize bakar.
        await context.Database.ExecuteSqlRawAsync("DELETE FROM dealership.outbox_messages");

        return (birinci.Id.Value, ikinci.Id.Value);
    }

    private static Branch Sube(string slug) => Branch.Open(
        Adlar("Test Şubesi"),
        Adlar("Test Adresi"),
        PhoneNumber.Create("+902160000001").Value,
        TimeZoneId.Create("Europe/Istanbul").Value,
        BranchSlug.Create(slug).Value,
        Tr).Value;

    private static LocalizedText Adlar(string tr) =>
        LocalizedText.From([new KeyValuePair<Locale, string>(Tr, tr)]);

    private async Task<int> AktifSubeSayisiAsync()
    {
        await using DealershipDbContext context = database.CreateContext();

        return await context.Branches.AsNoTracking().CountAsync(branch => branch.Status == BranchStatus.Active);
    }

    private async Task<int> DuyuruSayisiAsync()
    {
        await using DealershipDbContext context = database.CreateContext();

        return await context.OutboxMessages.AsNoTracking().CountAsync();
    }

    private async Task DuyurulariTemizleAsync()
    {
        await using DealershipDbContext context = database.CreateContext();

        await context.Database.ExecuteSqlRawAsync("DELETE FROM dealership.outbox_messages");
    }

    private sealed class TestSaati(DateTimeOffset an) : IClock
    {
        public DateTimeOffset UtcNow { get; } = an;
    }
}
