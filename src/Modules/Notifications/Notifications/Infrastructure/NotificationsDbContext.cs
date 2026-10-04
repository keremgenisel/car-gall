using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Notifications.Infrastructure;

/// <summary>
/// Modülün kendi şeması, kendi migration zinciri (C12).
///
/// **Outbox YOK** (C11): `Notifications` bir yaprak modüldür ve hiçbir modüle olay
/// yayınlamaz. Yalnız INBOX var — tükettiği olayın idempotentlik kaydı için (C3, AC-19).
/// </summary>
internal sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options), IInboxDbContext
{
    internal const string Schema = "notifications";

    internal DbSet<NotificationMessage> Messages => Set<NotificationMessage>();

    internal DbSet<NotificationTemplate> Templates => Set<NotificationTemplate>();

    public DbSet<InboxEntry> InboxEntries => Set<InboxEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new InboxEntryConfiguration());
    }
}
