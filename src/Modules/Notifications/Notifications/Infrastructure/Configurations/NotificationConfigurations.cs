using CarGallery.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Notifications.Infrastructure.Configurations;

internal sealed class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).HasColumnName("id");

        builder.Property(message => message.LeadId).HasColumnName("lead_id");
        builder.Property(message => message.SubscriptionId).HasColumnName("subscription_id");
        builder.Property(message => message.PeriodId).HasColumnName("period_id");

        // `0018` D23 — kayıt ya talebe, ya abonelik + döneme, ya da yalnız aboneliğe bağlı.
        // Kural kod yoluna değil KISITA dayanır (AC-84).
        builder.ToTable(table => table.HasCheckConstraint(
            "ck_messages_single_target",
            "(lead_id IS NOT NULL AND subscription_id IS NULL AND period_id IS NULL) "
            + "OR (lead_id IS NULL AND subscription_id IS NOT NULL)"));
        builder.Property(message => message.TemplateKey).HasColumnName("template_key").HasMaxLength(100);
        builder.Property(message => message.Locale).HasColumnName("locale").HasMaxLength(3);

        // C7/AC-24 — durum ve kanal KOD olarak saklanır; çevrilmiş adı saklanmaz.
        builder.Property(message => message.Channel).HasColumnName("channel").HasConversion<int>();
        builder.Property(message => message.Status).HasColumnName("status").HasConversion<int>();

        builder.Property(message => message.AttemptCount).HasColumnName("attempt_count");
        builder.Property(message => message.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(message => message.FailureCode).HasColumnName("failure_code").HasMaxLength(100);
        builder.Property(message => message.QueuedAt).HasColumnName("queued_at");
        builder.Property(message => message.CompletedAt).HasColumnName("completed_at");

        // Kuyruk sorgusu: bekleyen ve sırası gelmiş satırlar.
        builder.HasIndex(message => new { message.Status, message.NextAttemptAt })
            .HasDatabaseName("ix_messages_status_next_attempt");

        // "Bu talebe ne gönderildi" sorusu kayıttan cevaplanır (R12).
        builder.HasIndex(message => message.LeadId).HasDatabaseName("ix_messages_lead_id");

        // `0018` D23 — bir aboneye bir dönem için en çok BİR bülten kaydı; ikinci işlenme —
        // ardışık ya da eşzamanlı — ikinci kayıt üretemez (AC-69, AC-70).
        builder.HasIndex(message => new { message.SubscriptionId, message.PeriodId })
            .HasDatabaseName("ux_messages_subscription_period")
            .IsUnique()
            .HasFilter("period_id IS NOT NULL");

        // `0018` D29 — adres sınırı aboneliğin teyit iletilerinden sayılır.
        builder.HasIndex(message => new { message.SubscriptionId, message.QueuedAt })
            .HasDatabaseName("ix_messages_subscription_queued_at");
    }
}

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("templates");

        // (anahtar × locale) bileşik anahtar: aynı anahtarın aynı dilde ikinci bir sürümü olamaz.
        builder.HasKey(template => new { template.TemplateKey, template.Locale });

        builder.Property(template => template.TemplateKey).HasColumnName("template_key").HasMaxLength(100);
        builder.Property(template => template.Locale).HasColumnName("locale").HasMaxLength(3);
        builder.Property(template => template.Subject).HasColumnName("subject").HasMaxLength(300);
        builder.Property(template => template.Body).HasColumnName("body");
    }
}
