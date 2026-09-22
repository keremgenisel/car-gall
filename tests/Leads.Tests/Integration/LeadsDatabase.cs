using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CarGallery.Modules.Leads.Tests.Integration;

/// <summary>
/// Gerçek PostgreSQL. SQLite in-memory ile EF testi yapılmaz: `decimal` hassasiyeti,
/// tekil indeks ve tetikleyici davranışı farklıdır ve yeşil test yanlış güven verir
/// (docs/testing.md § 1). 0005'te bu üçü de kriter taşıyor (AC-22, AC-35, AC-28).
/// </summary>
public sealed class LeadsDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    internal string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using LeadsDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    internal LeadsDbContext CreateContext()
    {
        DbContextOptionsBuilder<LeadsDbContext> builder = new();
        builder.UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", LeadsDbContext.Schema));

        return new LeadsDbContext(builder.Options);
    }

    internal LeadsUnitOfWork CreateUnitOfWork(LeadsDbContext context, IClock clock) =>
        new(context, new OutboxWriter<LeadsDbContext>(context, clock), clock);

    /// <summary>
    /// Paylaşılan seed'e bağımlı test yazılmaz; her test kendi verisini kurar
    /// (docs/testing.md § 4).
    ///
    /// `TRUNCATE` kullanılıyor çünkü `DELETE` AC-28'in tetikleyicisine takılır — kuralın
    /// çalıştığının ilk işareti temizlik yolunun değişmek zorunda kalmasıdır.
    /// </summary>
    internal async Task ResetAsync()
    {
        await using LeadsDbContext context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {LeadsDbContext.Schema}.leads, {LeadsDbContext.Schema}.outbox_messages");
    }
}

[CollectionDefinition(Name)]
public sealed class LeadsDatabaseCollection : ICollectionFixture<LeadsDatabase>
{
    public const string Name = "talep-veritabani";
}
