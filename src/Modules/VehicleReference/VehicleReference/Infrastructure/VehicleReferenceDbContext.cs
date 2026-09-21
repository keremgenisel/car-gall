using CarGallery.Modules.VehicleReference.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleReference.Infrastructure;

internal sealed class VehicleReferenceDbContext(DbContextOptions<VehicleReferenceDbContext> options)
    : DbContext(options)
{
    internal const string Schema = "vehicle_reference";

    internal DbSet<BodyType> BodyTypes => Set<BodyType>();

    internal DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VehicleReferenceDbContext).Assembly);
    }
}
