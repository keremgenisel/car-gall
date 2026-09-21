using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// Gerçek PostgreSQL. SQLite in-memory ile EF testi yapılmaz: şema davranışı, collation,
/// jsonb ve decimal hassasiyeti farklıdır ve yeşil test yanlış güven verir
/// (docs/testing.md § 1).
/// </summary>
public sealed class ShowcaseDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    internal string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using VehicleCatalogDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    internal VehicleCatalogDbContext CreateContext()
    {
        DbContextOptionsBuilder<VehicleCatalogDbContext> builder = new();
        builder.UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleCatalogDbContext.Schema));

        return new VehicleCatalogDbContext(builder.Options);
    }

    /// <summary>
    /// Paylaşılan seed'e bağımlı test yazılmaz; her test kendi verisini kurar
    /// (docs/testing.md § 4). Alan ayarları da migration'ın seed ettiği hâle döndürülür:
    /// bir test onları değiştirip patlarsa koleksiyondaki sonraki testler kirli durumu
    /// devralırdı.
    /// </summary>
    internal async Task ResetAsync()
    {
        await using VehicleCatalogDbContext context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {VehicleCatalogDbContext.Schema}.showcase_listings");

        await context.Database.ExecuteSqlRawAsync(
            $"""
            UPDATE {VehicleCatalogDbContext.Schema}.vehicle_field_settings
            SET form_state = 2, public_visibility = 1
            WHERE field_key = 'body_type'
            """);
    }
}

[CollectionDefinition(Name)]
public sealed class ShowcaseDatabaseCollection : ICollectionFixture<ShowcaseDatabase>
{
    public const string Name = "vitrin-veritabani";
}
