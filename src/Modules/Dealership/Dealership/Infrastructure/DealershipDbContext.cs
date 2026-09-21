using CarGallery.Modules.Dealership.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Infrastructure;

/// <summary>
/// Modülün kendi şeması, kendi migration zinciri (C14, architecture.md § 1).
/// `TenantId` benzeri bir alan yoktur: kurulum = galeri (C11).
/// </summary>
internal sealed class DealershipDbContext(DbContextOptions<DealershipDbContext> options)
    : DbContext(options)
{
    internal const string Schema = "dealership";

    internal DbSet<Gallery> Galleries => Set<Gallery>();

    internal DbSet<Branch> Branches => Set<Branch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DealershipDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<decimal>().HavePrecision(18, 8);
}
