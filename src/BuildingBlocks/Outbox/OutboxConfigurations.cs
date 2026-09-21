using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.BuildingBlocks.Outbox;

/// <summary>
/// Her modül bu yapılandırmayı KENDİ şemasında uygular; ortak bir outbox tablosu yoktur
/// (architecture.md § 2.C — "her şemada kendi").
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(message => message.Type).HasColumnName("type").HasMaxLength(300);
        builder.Property(message => message.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(message => message.OccurredAt).HasColumnName("occurred_at");
        builder.Property(message => message.ProcessedAt).HasColumnName("processed_at");
        builder.Property(message => message.AttemptCount).HasColumnName("attempt_count");
        builder.Property(message => message.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(message => message.FailedAt).HasColumnName("failed_at");
        builder.Property(message => message.LastError).HasColumnName("last_error").HasMaxLength(2000);

        // Dağıtıcı yalnız bekleyenleri okur; indeks o sorguyu sabit maliyette tutar.
        builder.HasIndex(message => new { message.ProcessedAt, message.FailedAt, message.NextAttemptAt })
            .HasDatabaseName("ix_outbox_messages_pending");
    }
}

public sealed class InboxEntryConfiguration : IEntityTypeConfiguration<InboxEntry>
{
    public void Configure(EntityTypeBuilder<InboxEntry> builder)
    {
        builder.ToTable("inbox_entries");

        // D4 — koruma (olay kimliği, alıcı) çiftine dayanır: aynı olayı birden çok alıcı
        // işler ve her biri kendi başına bir kez işlemelidir.
        builder.HasKey(entry => new { entry.EventId, entry.HandlerName });

        builder.Property(entry => entry.EventId).HasColumnName("event_id");
        builder.Property(entry => entry.HandlerName).HasColumnName("handler_name").HasMaxLength(200);
        builder.Property(entry => entry.ProcessedAt).HasColumnName("processed_at");
    }
}
