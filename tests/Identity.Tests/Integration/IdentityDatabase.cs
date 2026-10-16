using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CarGallery.Modules.Identity.Tests.Integration;

/// <summary>
/// Gerçek PostgreSQL. SQLite in-memory ile EF testi yapılmaz: tekil indeks davranışı,
/// collation ve `jsonb` farklıdır ve yeşil test yanlış güven verir (docs/testing.md § 1).
/// 0006'da bunun somut karşılığı AC-12'dir — tekillik ancak gerçek indekste görünür.
/// </summary>
public sealed class IdentityDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    internal string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using IdentityDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    internal IdentityDbContext CreateContext()
    {
        DbContextOptionsBuilder<IdentityDbContext> builder = new();
        builder.UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema));

        return new IdentityDbContext(builder.Options);
    }

    internal static IdentityUnitOfWork CreateUnitOfWork(IdentityDbContext context, IClock clock) =>
        new(context, new OutboxWriter<IdentityDbContext>(context, clock), clock);

    /// <summary>Paylaşılan seed'e bağımlı test yazılmaz; her test kendi verisini kurar.</summary>
    internal async Task ResetAsync()
    {
        await using IdentityDbContext context = CreateContext();

        // Tablo listesi MODELDEN türetiliyor, elle sayılmıyor. `0013` dört tablo ekledi ve
        // elle yazılmış listede eksik kalanlar testleri sessizce birbirine bağladı: üç
        // test, kendinden önce koşan testin ara durum satırlarını gördü ve kırmızı
        // verdiği yer kusurun yeri değildi. Yeni bir tablo artık kendiliğinden kapsanıyor.
        string[] tablolar =
        [
            .. context.Model.GetEntityTypes()
                .Select(tip => tip.GetTableName())
                .Where(ad => ad is not null)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .Select(ad => $"{IdentityDbContext.Schema}.{ad}")
        ];

        // Tablo adları MODELDEN geliyor, dışarıdan değil: enterpolasyonun tek kaynağı
        // kendi şemamız. `ExecuteSqlAsync` parametreli çalışıyor ve tablo adı parametre
        // olarak geçirilemez, bu yüzden uyarı burada bastırılıyor.
#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {string.Join(", ", tablolar)}");
#pragma warning restore EF1002
    }
}

[CollectionDefinition(Name)]
public sealed class IdentityDatabaseCollection : ICollectionFixture<IdentityDatabase>
{
    public const string Name = "kimlik-veritabani";
}
