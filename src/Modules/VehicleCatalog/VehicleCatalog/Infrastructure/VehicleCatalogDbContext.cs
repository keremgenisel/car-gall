using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure;

/// <summary>
/// Modülün kendi DbContext'i, kendi şeması, kendi migration zinciri
/// (docs/architecture.md § 1). Migration yalnızca bu şemaya dokunur; CI bunu kontrol eder.
/// </summary>
internal sealed class VehicleCatalogDbContext(DbContextOptions<VehicleCatalogDbContext> options)
    : DbContext(options)
{
    internal const string Schema = "vehicle_catalog";

    internal DbSet<ShowcaseListing> ShowcaseListings => Set<ShowcaseListing>();

    internal DbSet<VehicleFieldSetting> VehicleFieldSettings => Set<VehicleFieldSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VehicleCatalogDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);
}
