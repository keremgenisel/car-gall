using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.EntityFrameworkCore;
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

    internal VehicleReferenceDbContext CreateContext()
    {
        DbContextOptionsBuilder<VehicleReferenceDbContext> builder = new();
        builder.UseNpgsql(
            container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleReferenceDbContext.Schema));

        return new VehicleReferenceDbContext(builder.Options);
    }
}

[CollectionDefinition(Name)]
public sealed class ReferenceDatabaseCollection : ICollectionFixture<ReferenceDatabase>
{
    public const string Name = "referans-veritabani";
}
