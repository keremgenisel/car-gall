using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Modules.VehicleReference.Tests;

public sealed class ReferenceDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using VehicleReferenceDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    internal VehicleReferenceDbContext CreateContext() => CreateContext(container.GetConnectionString());

    /// <summary>
    /// `0021` — testin kendi veritabanı, aynı kapta: marka adları galeri genelinde tekil ve
    /// kriterler ("Fiat", "Alfa Romeo") belirli adlar istiyor; paylaşılan veritabanında iki
    /// test aynı adı eklerse sıraya bağlı kırmızı üretirdi. <paramref name="migration"/>
    /// verilirse şema o migration'a kadar kurulur (güncelleme öncesi hâli ölçmek için).
    /// </summary>
    internal async Task<string> NewDatabaseAsync(string? migration = null)
    {
        string name = $"t_{Guid.NewGuid():N}";

        await using (NpgsqlConnection connection = new(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand command = new($"CREATE DATABASE {name}", connection);
            await command.ExecuteNonQueryAsync();
        }

        string connectionString = new NpgsqlConnectionStringBuilder(container.GetConnectionString()) { Database = name }.ConnectionString;

        await using VehicleReferenceDbContext context = CreateContext(connectionString);
        await context.GetService<IMigrator>().MigrateAsync(migration);

        return connectionString;
    }

    internal static VehicleReferenceDbContext CreateContext(string connectionString)
    {
        DbContextOptionsBuilder<VehicleReferenceDbContext> builder = new();
        builder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleReferenceDbContext.Schema));

        return new VehicleReferenceDbContext(builder.Options);
    }
}

[CollectionDefinition(Name)]
public sealed class ReferenceDatabaseCollection : ICollectionFixture<ReferenceDatabase>
{
    public const string Name = "referans-veritabani";
}
