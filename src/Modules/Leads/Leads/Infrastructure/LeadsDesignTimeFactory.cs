using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarGallery.Modules.Leads.Infrastructure;

/// <summary>Yalnızca tasarım zamanı (migration üretimi) içindir.</summary>
internal sealed class LeadsDesignTimeFactory : IDesignTimeDbContextFactory<LeadsDbContext>
{
    public LeadsDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<LeadsDbContext> builder = new();
        builder.UseNpgsql(
            "Host=localhost;Database=cargallery_design_time;Username=postgres",
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", LeadsDbContext.Schema));

        return new LeadsDbContext(builder.Options);
    }
}
