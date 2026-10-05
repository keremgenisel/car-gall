using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure;

/// <summary>
/// Modülün kendi DbContext'i, kendi şeması, kendi migration zinciri
/// (docs/architecture.md § 1). Migration yalnızca bu şemaya dokunur; CI bunu kontrol eder.
/// </summary>
internal sealed class VehicleCatalogDbContext(DbContextOptions<VehicleCatalogDbContext> options)
    : DbContext(options), IInboxDbContext
{
    internal const string Schema = "vehicle_catalog";

    /// <summary>
    /// 0010a — YAZMA modeli. Bu spec'e kadar `Vehicle` ve `Listing` hiç kaydedilmiyordu:
    /// `ShowcaseSeeder` agregatları bellekte kurup projekte ediyor ve atıyordu. Vitrin
    /// okuma modeli artık yazma modelinden türüyor.
    /// </summary>
    internal DbSet<Vehicle> Vehicles => Set<Vehicle>();

    internal DbSet<Listing> Listings => Set<Listing>();

    /// <summary>A6 — yalnız ekleme alır; silme ve güncelleme yolu yoktur.</summary>
    internal DbSet<PriceChange> PriceChanges => Set<PriceChange>();

    internal DbSet<ShowcaseListing> ShowcaseListings => Set<ShowcaseListing>();

    internal DbSet<VehicleFieldSetting> VehicleFieldSettings => Set<VehicleFieldSetting>();

    /// <summary>
    /// İşlenmiş olay kaydı ALICININ kendi şemasındadır; başka modülün inbox'ına yazılmaz
    /// (D4, Yasak Listesi 16).
    /// </summary>
    public DbSet<InboxEntry> InboxEntries => Set<InboxEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VehicleCatalogDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new InboxEntryConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);
}
