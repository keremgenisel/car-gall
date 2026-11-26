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

        // `0020` C4 — kurulum adımı migration'dan sonra görünen adı doldurur; test veritabanı
        // aynı sırayı izler, yoksa testler kurulumun üretmediği bir durumu ölçerdi.
        await DealershipSchema.BackfillDisplayNameAsync(context, CarGallery.BuildingBlocks.Locale.Create("tr").Value, CancellationToken.None);
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    internal DealershipDbContext CreateContext(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors)
    {
        DbContextOptionsBuilder<DealershipDbContext> builder = new();
        builder.UseNpgsql(
            container.GetConnectionString(),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DealershipDbContext.Schema));

        // `0026` AC-76 — sorgu sayan testler bir yakalayıcı verir; diğer çağıranlar boş geçer.
        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new DealershipDbContext(builder.Options);
    }
}

[CollectionDefinition(Name)]
public sealed class DealershipDatabaseCollection : ICollectionFixture<DealershipDatabase>
{
    public const string Name = "galeri-veritabani";
}
