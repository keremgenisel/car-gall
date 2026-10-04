using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// Gerçek PostgreSQL. SQLite in-memory ile EF testi yapılmaz (docs/testing.md § 1).
/// Burada somut karşılığı AC-22'dir: şablonlar SEED MIGRATION ile geliyor ve migration'ın
/// gerçekten koştuğunu ancak gerçek bir veritabanı gösterir.
/// </summary>
public sealed class NotificationsDatabase : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    internal string ConnectionString => container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        await using NotificationsDbContext context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    internal NotificationsDbContext CreateContext()
    {
        DbContextOptionsBuilder<NotificationsDbContext> builder = new();
        builder.UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.Schema));

        return new NotificationsDbContext(builder.Options);
    }

    /// <summary>Paylaşılan seed'e bağımlı test yazılmaz; her test kendi mesajlarını kurar.</summary>
    internal async Task ResetMessagesAsync()
    {
        await using NotificationsDbContext context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {NotificationsDbContext.Schema}.messages, {NotificationsDbContext.Schema}.inbox_entries");
    }
}

[CollectionDefinition(Name)]
public sealed class NotificationsDatabaseCollection : ICollectionFixture<NotificationsDatabase>
{
    public const string Name = "bildirim-veritabani";
}
