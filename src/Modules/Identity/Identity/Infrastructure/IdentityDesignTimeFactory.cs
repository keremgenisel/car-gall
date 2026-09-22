using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarGallery.Modules.Identity.Infrastructure;

/// <summary>Yalnızca tasarım zamanı (migration üretimi) içindir.</summary>
internal sealed class IdentityDesignTimeFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<IdentityDbContext> builder = new();
        builder.UseNpgsql(
            "Host=localhost;Database=cargallery_design_time;Username=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema));

        return new IdentityDbContext(builder.Options);
    }
}
