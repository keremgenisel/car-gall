using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarGallery.Modules.VehicleReference.Infrastructure;

internal sealed class VehicleReferenceDesignTimeFactory : IDesignTimeDbContextFactory<VehicleReferenceDbContext>
{
    public VehicleReferenceDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<VehicleReferenceDbContext> builder = new();
        builder.UseNpgsql(
            "Host=localhost;Database=cargallery_design_time;Username=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleReferenceDbContext.Schema));

        return new VehicleReferenceDbContext(builder.Options);
    }
}
