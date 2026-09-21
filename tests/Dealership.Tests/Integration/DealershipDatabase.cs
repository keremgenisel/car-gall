using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CarGallery.Modules.Dealership.Tests.Integration;

/// <summary>
/// Gerçek PostgreSQL. SQLite in-memory ile EF testi yapılmaz: jsonb, CHECK kısıtı ve
/// tekil indeks davranışı farklıdır ve yeşil test yanlış güven verir (docs/testing.md § 1).
/// </summary>
public sealed class DealershipDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using DealershipDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    internal DealershipDbContext CreateContext()
    {
        DbContextOptionsBuilder<DealershipDbContext> builder = new();
        builder.UseNpgsql(
            container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DealershipDbContext.Schema));

        return new DealershipDbContext(builder.Options);
    }
}

[CollectionDefinition(Name)]
public sealed class DealershipDatabaseCollection : ICollectionFixture<DealershipDatabase>
{
    public const string Name = "galeri-veritabani";
}
