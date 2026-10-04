using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarGallery.Modules.Notifications.Infrastructure;

/// <summary>Yalnızca tasarım zamanı (migration üretimi) içindir.</summary>
internal sealed class NotificationsDesignTimeFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<NotificationsDbContext> builder = new();
        builder.UseNpgsql(
            "Host=localhost;Database=cargallery_design_time;Username=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsDbContext.Schema));

        return new NotificationsDbContext(builder.Options);
    }
}
