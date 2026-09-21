using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure;

/// <summary>
/// Yalnızca tasarım zamanı (migration üretimi) içindir. Bağlantı dizesi burada bir sır
/// değildir; gerçek bağlantı çalışma anında yapılandırmadan gelir (docs/conventions.md § 11).
/// </summary>
internal sealed class VehicleCatalogDesignTimeFactory : IDesignTimeDbContextFactory<VehicleCatalogDbContext>
{
    public VehicleCatalogDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<VehicleCatalogDbContext> builder = new();
        builder.UseNpgsql(
            "Host=localhost;Database=cargallery_design_time;Username=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleCatalogDbContext.Schema));

        return new VehicleCatalogDbContext(builder.Options);
    }
}
