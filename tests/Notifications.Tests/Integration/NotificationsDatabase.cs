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

    internal NotificationsDbContext CreateContext(params Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor[] interceptors)
    {
        DbContextOptionsBuilder<NotificationsDbContext> builder = new();
        builder.UseNpgsql(
            ConnectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.Schema));

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new NotificationsDbContext(builder.Options);
    }

    /// <summary>Paylaşılan seed'e bağımlı test yazılmaz; her test kendi mesajlarını kurar.</summary>
    internal async Task ResetMessagesAsync()
    {
        await using NotificationsDbContext context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            $"TRUNCATE TABLE {NotificationsDbContext.Schema}.messages, {NotificationsDbContext.Schema}.inbox_entries");
    }

    /// <summary>
    /// `0018` — bülten tablolarının da sıfırlanması. `TRUNCATE` izin olay kaydının "yalnız
    /// eklenir" tetikleyicisini (D34) çalıştırmaz: tetikleyici satır düzeyindedir ve yalnız
    /// <c>UPDATE</c>/<c>DELETE</c>'e bağlıdır.
    /// </summary>
    internal async Task ResetNewsletterAsync()
    {
        await using NotificationsDbContext context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE notifications.messages, notifications.inbox_entries, "
            + "notifications.newsletter_subscriptions, notifications.consent_events, "
            + "notifications.newsletter_candidates, notifications.newsletter_periods, "
            + "notifications.newsletter_period_listings");
    }

    /// <summary>
    /// `0018` AC-85 — aynı sunucuda BOŞ bir veritabanı: migration zincirinin bir ara noktasına
    /// kadar uygulanabilsin diye. Paylaşılan veritabanı en son migration'dadır.
    /// </summary>
    internal async Task<string> CreateEmptyDatabaseAsync(string name)
    {
        // Ad parametre olamaz (DDL); yalnız testin kendi sabitini kabul ediyoruz.
        if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z_0-9]+$"))
        {
            throw new ArgumentException("Veritabanı adı küçük harf, rakam ve alt çizgiden oluşur.", nameof(name));
        }

        await using NotificationsDbContext context = CreateContext();

#pragma warning disable EF1002 // Ad yukarıda kalıba karşı doğrulandı; DDL parametre almaz.
        await context.Database.ExecuteSqlRawAsync($"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
        await context.Database.ExecuteSqlRawAsync($"CREATE DATABASE {name}");
#pragma warning restore EF1002

        Npgsql.NpgsqlConnectionStringBuilder builder = new(ConnectionString) { Database = name };

        return builder.ConnectionString;
    }

    internal static NotificationsDbContext CreateContext(string connectionString)
    {
        DbContextOptionsBuilder<NotificationsDbContext> builder = new();
        builder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.Schema));

        return new NotificationsDbContext(builder.Options);
    }
}

[CollectionDefinition(Name)]
public sealed class NotificationsDatabaseCollection : ICollectionFixture<NotificationsDatabase>
{
    public const string Name = "bildirim-veritabani";
}
