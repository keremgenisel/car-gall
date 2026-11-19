using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Application.Newsletter;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// `0018` dönem testlerinin düzeni: `VehiclePublished` teslimi, dönem işleyicisinin doğrudan
/// çağrısı ve kayıtların okunması. Saat gerçek saatten başlar ve elle ilerler; dönem anı
/// kurulumun (Çarşamba 08:30 UTC) bir sonraki örneğidir.
/// </summary>
internal sealed class DonemDuzeni
{
    private readonly NotificationsDatabase database;

    internal DonemDuzeni(NotificationsDatabase database, NewsletterOptions? secenekler = null)
    {
        this.database = database;
        Secenekler = secenekler ?? Bulten.BultenSecenekleri(Bulten.BultenKurulumu(gecerliIstek: 500, adresSiniri: 50));

        DateTimeOffset baslangic = DateTimeOffset.UtcNow;
        Saat = new Bulten.IleriSaat(baslangic);
        Abonelik = new AbonelikDuzeni(database, Saat, Secenekler);
    }

    internal NewsletterOptions Secenekler { get; }

    internal Bulten.IleriSaat Saat { get; }

    internal AbonelikDuzeni Abonelik { get; }

    internal SahteIlanOzeti Ilanlar { get; } = new();

    /// <summary>Başlangıç dönemi kaydını ŞİMDİ yazar (kartın ilk açılışı).</summary>
    internal async Task<DateTimeOffset> DevreyeGirAsync()
    {
        Assert.Null(await IsleAsync());

        await using NotificationsDbContext context = database.CreateContext();

        return (await new NewsletterBaseline(context, Saat).FindAsync(CancellationToken.None))!.Value;
    }

    /// <summary>Bir sonraki dönem anı — devreye girişten sonraki ilk Çarşamba 08:30.</summary>
    internal DateTimeOffset SonrakiDonemAni()
    {
        DateTimeOffset aday = Saat.UtcNow;

        for (int gun = 0; gun < 8; gun++)
        {
            DateTimeOffset an = new(DateOnly.FromDateTime(aday.UtcDateTime.AddDays(gun)).ToDateTime(Secenekler.SendTimeUtc), TimeSpan.Zero);

            if (an.DayOfWeek == Secenekler.SendDayOfWeek && an > Saat.UtcNow)
            {
                return an;
            }
        }

        throw new InvalidOperationException("dönem anı bulunamadı");
    }

    /// <summary>Saati bir sonraki dönem anının <paramref name="sonra"/> sonrasına getirir.</summary>
    internal DateTimeOffset DonemeIlerle(TimeSpan? sonra = null)
    {
        DateTimeOffset an = SonrakiDonemAni();
        Saat.Set(an + (sonra ?? TimeSpan.FromMinutes(1)));

        return an;
    }

    internal async Task<VehiclePublished> DuyurAsync(Guid ilanId, DateTimeOffset ilkYayin, Guid? olayId = null, DateTimeOffset? oluşma = null)
    {
        VehiclePublished olay = new(
            olayId ?? Guid.CreateVersion7(),
            oluşma ?? Saat.UtcNow,
            Guid.CreateVersion7(),
            ilanId,
            $"ilan-{ilanId.ToString("N")[..6]}",
            ilkYayin);

        await TeslimEtAsync(olay);

        return olay;
    }

    internal async Task TeslimEtAsync(VehiclePublished olay)
    {
        await using NotificationsDbContext context = database.CreateContext();

        await new VehiclePublishedCandidateHandler(context, Saat, NullLogger<VehiclePublishedCandidateHandler>.Instance)
            .HandleAsync(olay, CancellationToken.None);
    }

    /// <summary>Yeni ilan: katalogda Yayında + duyurusu teslim edildi.</summary>
    internal async Task<Guid> YeniIlanAsync(DateTimeOffset? ilkYayin = null)
    {
        Guid ilanId = Guid.CreateVersion7();
        DateTimeOffset yayin = ilkYayin ?? Saat.UtcNow;

        Ilanlar.Yayinla(ilanId, yayin);
        await DuyurAsync(ilanId, yayin);

        return ilanId;
    }

    internal async Task<Guid?> IsleAsync(params IInterceptor[] araya)
    {
        await using NotificationsDbContext context = database.CreateContext(araya);

        return await new NewsletterPeriodProcessor(
            context,
            Ilanlar,
            Secenekler,
            Bulten.Secenekler(),
            Saat,
            NullLogger<NewsletterPeriodProcessor>.Instance).ProcessDueAsync(CancellationToken.None);
    }

    internal async Task<string> EtkinAboneAsync(string adres, string dil = "tr")
    {
        await Abonelik.AboneOlAsync(adres, dil);
        await Abonelik.TeyitEtAsync(await Abonelik.AnahtarVerAsync(adres));

        return adres;
    }

    internal async Task<List<NotificationMessage>> BultenlerAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Messages.AsNoTracking()
            .Where(message => message.PeriodId != null)
            .ToListAsync();
    }

    internal async Task<List<NotificationMessage>> BultenlerAsync(string adres)
    {
        Guid id = await AbonelikIdOf(adres);

        return [.. (await BultenlerAsync()).Where(message => message.SubscriptionId == id)];
    }

    internal async Task<Guid> AbonelikIdOf(string adres) => (await Abonelik.AbonelikAsync(adres)).Id;

    internal async Task<List<Guid>> DonemIlanlariAsync(Guid donemId)
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.PeriodListings.AsNoTracking()
            .Where(item => item.PeriodId == donemId)
            .Select(item => item.ListingId)
            .ToListAsync();
    }

    internal async Task<List<NewsletterPeriod>> DonemlerAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Periods.AsNoTracking().OrderBy(period => period.CutoffAt).ToListAsync();
    }

    internal async Task<List<NewsletterCandidate>> AdaylarAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Candidates.AsNoTracking().ToListAsync();
    }
}
