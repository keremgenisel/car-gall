using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// AC-51 — **parola hiçbir log kaydında görünmez** (C6).
///
/// Bu, gözle bakılarak doğrulanacak bir şey değildir: EF Core parametreli sorguları
/// `Information` seviyesinde loglayabilir ve bir gün biri tanılama için `EnableSensitive
/// DataLogging` açarsa parola doğrudan log dosyasına düşer. Test bu yüzden loglama AÇIK
/// ve YAKALANIYORken koşuyor.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class ParolaSizintiTests(IdentityDatabase database)
{
    private const string Parola = "sizmamasi-gereken-parola";

    [Fact(DisplayName = "AC-51: Parola hiçbir log kaydında görünmez")]
    public async Task GorunmezParola_LogKayitlarinda()
    {
        await database.ResetAsync();

        YakalayanLogger logger = new();
        IdentityHarness harness = new(database);

        await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "yonetici@ornek.com", parola: Parola, rol: "manager"));

        // Giriş ve personel ekleme, parolanın geçtiği İKİ yoldur; ikisi de loglanan bir
        // bağlamda çalıştırılıyor.
        await using IdentityDbContext context = LogluContext(logger);
        Login login = new(context, new Pbkdf2PasswordHasher(harness.Secenekler), harness.Secenekler, harness.Saat);

        await login.HandleAsync(
            new LoginCommand("yonetici@ornek.com", Parola, IdentityHarness.VarsayilanIstemci),
            CancellationToken.None);

        await login.HandleAsync(
            new LoginCommand("yonetici@ornek.com", "yanlis-parola-123", IdentityHarness.VarsayilanIstemci),
            CancellationToken.None);

        Assert.NotEmpty(logger.Kayitlar);
        Assert.DoesNotContain(logger.Kayitlar, kayit => kayit.Contains(Parola, StringComparison.Ordinal));
        Assert.DoesNotContain(
            logger.Kayitlar,
            kayit => kayit.Contains("yanlis-parola-123", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "AC-51: Parola hiçbir log kaydında görünmez")]
    public async Task GorunmezOzet_LogKayitlarinda()
    {
        await database.ResetAsync();

        YakalayanLogger logger = new();
        IdentityHarness harness = new(database);

        await harness.PersonelEkleAsync(
            IdentityHarness.EkleKomutu(kimlik: "yonetici@ornek.com", parola: Parola, rol: "manager"));

        await using IdentityDbContext context = LogluContext(logger);
        await context.Staff.AsNoTracking().ToListAsync();

        Assert.NotEmpty(logger.Kayitlar);
        Assert.DoesNotContain(
            logger.Kayitlar,
            kayit => kayit.Contains("pbkdf2-sha256", StringComparison.OrdinalIgnoreCase));
    }

    private IdentityDbContext LogluContext(YakalayanLogger logger)
    {
        DbContextOptionsBuilder<IdentityDbContext> builder = new();

        builder
            .UseNpgsql(
                database.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
            .UseLoggerFactory(LoggerFactory.Create(logging =>
                logging.AddProvider(logger).SetMinimumLevel(LogLevel.Debug)))
            // Hassas veri loglaması KAPALI kalmalı; test bunun yeterliliğini sınıyor.
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning));

        return new IdentityDbContext(builder.Options);
    }
}

internal sealed class YakalayanLogger : ILoggerProvider, ILogger
{
    private readonly List<string> kayitlar = [];

    internal IReadOnlyList<string> Kayitlar
    {
        get
        {
            lock (kayitlar)
            {
                return [.. kayitlar];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => this;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        lock (kayitlar)
        {
            kayitlar.Add(formatter(state, exception));
        }
    }

    public void Dispose()
    {
    }
}
