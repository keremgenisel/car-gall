using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Infrastructure;

/// <summary>
/// Modülün kendi DbContext'i, kendi şeması, kendi migration zinciri
/// (docs/architecture.md § 1). Migration yalnızca `identity` şemasına dokunur (C14).
/// `TenantId` benzeri bir alan yoktur: kurulum = galeri (C10).
/// </summary>
internal sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    internal const string Schema = "identity";

    internal DbSet<Staff> Staff => Set<Staff>();

    internal DbSet<Session> Sessions => Set<Session>();

    internal DbSet<LockoutState> LoginAttempts => Set<LockoutState>();

    internal DbSet<TwoFactorEnrollment> TwoFactorEnrollments => Set<TwoFactorEnrollment>();

    internal DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();

    /// <summary>D19 — ara durum; oturum DEĞİL, kendi kısa ömürlü tablosu.</summary>
    internal DbSet<PendingLogin> PendingLogins => Set<PendingLogin>();

    /// <summary>R12/D12 — ikinci adımın kaydı; bu depodaki ilk kayıt tablosu.</summary>
    internal DbSet<TwoFactorAuditEntry> TwoFactorAudit => Set<TwoFactorAuditEntry>();

    /// <summary>Outbox YAYINCININ kendi şemasındadır (architecture.md § 2.C).</summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
