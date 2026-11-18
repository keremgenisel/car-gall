using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarGallery.Modules.Content.Infrastructure;

/// <summary>Migration üretimi için tasarım zamanı fabrikası; bağlantı açılmaz.</summary>
internal sealed class ContentDesignTimeFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    /// <inheritdoc />
    public ContentDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ContentDbContext> builder = new();
        builder.UseNpgsql(
            "Host=localhost;Database=cargallery_design_time;Username=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ContentDbContext.Schema));

        return new ContentDbContext(builder.Options);
    }
}
