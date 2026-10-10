using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Media.Infrastructure;

/// <summary>
/// Modülün kendi şeması, kendi migration zinciri (C9, Yasak Listesi 3).
///
/// <para><b>OUTBOX var, INBOX yok</b> — ve bu D6'nın doğrudan sonucu: `Media` konuşur,
/// dinlemez. Hiçbir modülün olayını tüketmiyor, bu yüzden idempotency kaydı tutacağı bir
/// alıcısı da yok. `Notifications` tam tersidir (inbox var, outbox yok) ve ikisinin birlikte
/// okunması sınırın yönünü gösteriyor.</para>
/// </summary>
internal sealed class MediaDbContext(DbContextOptions<MediaDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    internal const string Schema = "media";

    internal DbSet<MediaAsset> Assets => Set<MediaAsset>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
