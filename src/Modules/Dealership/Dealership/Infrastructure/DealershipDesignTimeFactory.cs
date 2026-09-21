using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarGallery.Modules.Dealership.Infrastructure;

/// <summary>Yalnızca tasarım zamanı (migration üretimi) içindir.</summary>
internal sealed class DealershipDesignTimeFactory : IDesignTimeDbContextFactory<DealershipDbContext>
{
    public DealershipDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<DealershipDbContext> builder = new();
        builder.UseNpgsql(
            "Host=localhost;Database=cargallery_design_time;Username=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DealershipDbContext.Schema));

        return new DealershipDbContext(builder.Options);
    }
}
