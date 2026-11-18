using CarGallery.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Content.Infrastructure.Configurations;

/// <summary>`0017` R18 — SSS girdileri; dil başına soru, cevap ve durum; global sıra.</summary>
internal sealed class FaqEntryConfiguration : IEntityTypeConfiguration<FaqEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FaqEntry> builder)
    {
        builder.ToTable("faq_entries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(entry => entry.Position).HasColumnName("position");
        builder.Property(entry => entry.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(entry => entry.Position).HasDatabaseName("ix_faq_entries_position");

        builder.OwnsMany(entry => entry.Localizations, satir =>
        {
            satir.ToTable("faq_entry_localizations");
            satir.WithOwner().HasForeignKey("EntryId");
            satir.Property<Guid>("EntryId").HasColumnName("entry_id");
            satir.HasKey("EntryId", nameof(FaqLocalization.Locale));
            satir.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(10);
            satir.Property(item => item.Question).HasColumnName("question").HasMaxLength(1000);
            satir.Property(item => item.Answer).HasColumnName("answer");
            satir.Property(item => item.Status).HasColumnName("status").HasMaxLength(10)
                .HasConversion(ContentPageConfiguration.StatusConverter);
            satir.Ignore(item => item.IsComplete);
        });
    }
}
