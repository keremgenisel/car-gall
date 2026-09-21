using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Dealership.Tests.Integration;

[Collection(DealershipDatabaseCollection.Name)]
public sealed class OutboxTests(DealershipDatabase database)
{
    private static readonly Locale Tr = Locale.Create("tr").Value;
    private static readonly DateTimeOffset Simdi = new(2026, 4, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact(DisplayName = "AC-25: Şube değişikliği kaydedildiğinde duyurusu da kaydedilmiştir; değişiklik geri alındığında kaydedilmiş bir duyuru kalmaz")]
    public async Task BirlikteKalici_DegisiklikVeDuyuru()
    {
        await TemizleAsync();

        await using (DealershipDbContext context = database.CreateContext())
        {
            DealershipUnitOfWork unitOfWork = UnitOfWork(context);
            context.Branches.Add(Sube("gecici-sube-a"));
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }

        await using (DealershipDbContext okuma = database.CreateContext())
        {
            Assert.Equal(1, await okuma.OutboxMessages.CountAsync());
            Assert.Equal(1, await okuma.Branches.CountAsync(b => b.Slug == BranchSlug.Create("gecici-sube-a").Value));
        }

        // Geri alınan değişiklik: transaction rollback edilirse duyuru da kalmaz.
        await using (DealershipDbContext context = database.CreateContext())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            DealershipUnitOfWork unitOfWork = UnitOfWork(context);
            context.Branches.Add(Sube("gecici-sube-b"));
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            await transaction.RollbackAsync();
        }

        await using (DealershipDbContext okuma = database.CreateContext())
        {
            Assert.Equal(1, await okuma.OutboxMessages.CountAsync());
            Assert.Equal(0, await okuma.Branches.CountAsync(b => b.Slug == BranchSlug.Create("gecici-sube-b").Value));
        }
    }

    [Fact(DisplayName = "AC-26: Aynı duyuru alıcıya iki kez ulaştığında ikinci işleme ek bir etki üretmez; koruma duyurunun kimliğine dayanır")]
    public async Task EkEtkiYok_AyniDuyuruIkiKez()
    {
        await TemizleAsync();
        SayanAlici alici = new();

        await using DealershipDbContext context = database.CreateContext();
        await UretAsync(context, "sayac-subesi");

        OutboxDispatcher<DealershipDbContext> dispatcher = Dispatcher(context, alici);
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        // İkinci tur: mesaj zaten işlenmiş, alıcı bir daha çağrılmaz.
        await dispatcher.DispatchPendingAsync(CancellationToken.None);

        Assert.Equal(1, alici.CagrilmaSayisi);
    }

    [Fact(DisplayName = "AC-27: İki duyuru üretildikleri sıranın tersine işlendiğinde sonuç yine yayıncının son hâline yakınsar; hiçbir alıcı sıraya bağımlı değildir")]
    public async Task YakinsarSonHale_TersSirada()
    {
        await TemizleAsync();
        SonAdiTutanAlici alici = new();
        TestSaati saat = new(Simdi);

        await using DealershipDbContext context = database.CreateContext();

        Branch sube = Sube("sira-subesi");
        context.Branches.Add(sube);
        await UnitOfWork(context, saat).SaveChangesAsync(CancellationToken.None);

        // İki duyurunun OccurredAt'i FARKLI olmalı; aynı olsaydı sıralanacak bir şey kalmaz
        // ve test ters sıradan bağımsız olarak geçerdi.
        saat.Advance(TimeSpan.FromMinutes(5));
        sube.ChangeDisplayNames(Adlar("İkinci Ad"), Tr);
        await UnitOfWork(context, saat).SaveChangesAsync(CancellationToken.None);

        List<OutboxMessage> mesajlar = await context.OutboxMessages
            .OrderBy(message => message.OccurredAt)
            .ToListAsync();

        Assert.Equal(2, mesajlar.Count);
        Assert.True(mesajlar[0].OccurredAt < mesajlar[1].OccurredAt);

        // Sıra garantisi YOKTUR (architecture.md § 2.C): alıcıya TERS sırada veriliyor.
        await Teslim(alici, mesajlar[1]);
        await Teslim(alici, mesajlar[0]);

        // Yine de yayıncının son hâline yakınsıyor: alıcı sıraya değil, olayın zamanına bakıyor.
        Assert.Equal("İkinci Ad", alici.SonAd);
    }

    private static Task Teslim(IIntegrationEventDispatchTarget alici, OutboxMessage mesaj)
    {
        Type tip = typeof(BranchOpened).Assembly.GetType(mesaj.Type)!;
        IIntegrationEvent duyuru = (IIntegrationEvent)System.Text.Json.JsonSerializer.Deserialize(mesaj.Payload, tip)!;

        return alici.HandleAsync(duyuru, CancellationToken.None);
    }

    [Fact(DisplayName = "AC-28: Bir alıcı duyuruyu işlerken hata verdiğinde duyuru teslim edilmiş sayılmaz ve yeniden denenir; denemeler tükendiğinde duyuru başarısız olarak işaretlenir ve loglanır")]
    public async Task YenidenDenenir_SonraBasarisizIsaretlenir()
    {
        await TemizleAsync();
        HepHataVerenAlici alici = new();
        TestSaati saat = new(Simdi);

        await using DealershipDbContext context = database.CreateContext();
        await UretAsync(context, "hatali-sube");

        OutboxDispatcher<DealershipDbContext> dispatcher = Dispatcher(context, alici, saat, new OutboxDispatchOptions(20, 3));

        await dispatcher.DispatchPendingAsync(CancellationToken.None);
        OutboxMessage mesaj = await context.OutboxMessages.SingleAsync();
        Assert.Null(mesaj.ProcessedAt);
        Assert.Null(mesaj.FailedAt);
        Assert.NotNull(mesaj.NextAttemptAt);
        Assert.True(mesaj.NextAttemptAt > saat.UtcNow, "Yeniden deneme artan aralıkla ileriye atılmalı.");

        for (int tur = 0; tur < 3; tur++)
        {
            saat.Advance(TimeSpan.FromHours(1));
            await dispatcher.DispatchPendingAsync(CancellationToken.None);
        }

        mesaj = await context.OutboxMessages.SingleAsync();
        Assert.NotNull(mesaj.FailedAt);
        Assert.Null(mesaj.ProcessedAt);
        Assert.Contains("HepHataVerenAlici", mesaj.LastError);
    }

    [Fact(DisplayName = "AC-29: Bir alıcının başarısızlığı diğer alıcıların aynı duyuruyu işlemesini engellemez")]
    public async Task EngellemezDigerAlicilari()
    {
        await TemizleAsync();
        HepHataVerenAlici hatali = new();
        SayanAlici saglam = new();

        await using DealershipDbContext context = database.CreateContext();
        await UretAsync(context, "iki-alicili-sube");

        await Dispatcher(context, hatali, saglam).DispatchPendingAsync(CancellationToken.None);

        Assert.Equal(1, saglam.CagrilmaSayisi);
    }

    [Fact(DisplayName = "AC-30: Duyuru üretimi, alıcı modülün hazır olup olmamasından bağımsız çalışır; Dealership alıcıyı tanımaz")]
    public async Task Uretir_AliciHicYokken()
    {
        await TemizleAsync();

        await using DealershipDbContext context = database.CreateContext();
        await UretAsync(context, "alicisiz-sube");

        Assert.Equal(1, await context.OutboxMessages.CountAsync());

        // Kayıtlı hiçbir alıcı yokken dağıtım da hata vermez; mesaj işlenmiş sayılır.
        await Dispatcher(context).DispatchPendingAsync(CancellationToken.None);

        OutboxMessage mesaj = await context.OutboxMessages.SingleAsync();
        Assert.NotNull(mesaj.ProcessedAt);
    }

    private async Task<Branch> UretAsync(DealershipDbContext context, string slug)
    {
        Branch sube = Sube(slug);
        context.Branches.Add(sube);
        await UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
        return sube;
    }

    private async Task TemizleAsync()
    {
        await using DealershipDbContext context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("DELETE FROM dealership.outbox_messages");
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM dealership.branches WHERE slug NOT IN ('kadikoy-subesi', 'atasehir-subesi')");
    }

    private static DealershipUnitOfWork UnitOfWork(DealershipDbContext context, TestSaati? saat = null)
    {
        TestSaati kullanilan = saat ?? new TestSaati(Simdi);
        return new DealershipUnitOfWork(
            context,
            new OutboxWriter<DealershipDbContext>(context, kullanilan),
            kullanilan);
    }

    private static OutboxDispatcher<DealershipDbContext> Dispatcher(
        DealershipDbContext context,
        params IIntegrationEventDispatchTarget[] targets) =>
        Dispatcher(context, targets, new TestSaati(Simdi), new OutboxDispatchOptions());

    private static OutboxDispatcher<DealershipDbContext> Dispatcher(
        DealershipDbContext context,
        IIntegrationEventDispatchTarget target,
        TestSaati saat,
        OutboxDispatchOptions options) =>
        Dispatcher(context, [target], saat, options);

    private static OutboxDispatcher<DealershipDbContext> Dispatcher(
        DealershipDbContext context,
        IIntegrationEventDispatchTarget[] targets,
        TestSaati saat,
        OutboxDispatchOptions options)
    {
        ServiceCollection services = new();

        foreach (IIntegrationEventDispatchTarget target in targets)
        {
            services.AddSingleton(typeof(IIntegrationEventDispatchTarget), target);
        }

        return new OutboxDispatcher<DealershipDbContext>(
            context,
            services.BuildServiceProvider(),
            saat,
            options,
            NullLogger<OutboxDispatcher<DealershipDbContext>>.Instance);
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
}
