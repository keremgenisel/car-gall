using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Leads.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Infrastructure;

/// <summary>
/// Modülün kendi DbContext'i, kendi şeması, kendi migration zinciri
/// (docs/architecture.md § 1). Migration yalnızca `leads` şemasına dokunur (C14).
/// </summary>
internal sealed class LeadsDbContext(DbContextOptions<LeadsDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    internal const string Schema = "leads";

    internal DbSet<Lead> Leads => Set<Lead>();

    /// <summary>Outbox YAYINCININ kendi şemasındadır (architecture.md § 2.C).</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeadsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }

    /// <summary>
    /// `decimal(19,4)` — `decimal(_,2)` YASAKTIR: KWD/BHD/JOD/OMR üç ondalıklıdır ve
    /// iki ondalıkla saklamak Kuveyt'te sessiz veri kaybıdır (docs/conventions.md § 7).
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);
}
