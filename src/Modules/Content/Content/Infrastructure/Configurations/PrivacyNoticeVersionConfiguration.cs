using CarGallery.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Content.Infrastructure.Configurations;

/// <summary>
/// `0019` AC-41 — sürüm kaydının kolonları YALNIZ kimlik, dil, başlık, gövde, kayda geçtiği an
/// ve başlangıç işaretidir. Ziyaretçi, talep ya da personel kimliği taşıyan bir kolon eklemek
/// sürümü kişisel veri taşıyan bir kayda çevirir ve C5'i (silinmez) KVKK ile çatıştırır (C4).
/// </summary>
internal sealed class PrivacyNoticeVersionConfiguration : IEntityTypeConfiguration<PrivacyNoticeVersion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PrivacyNoticeVersion> builder)
    {
        builder.ToTable("privacy_notice_versions");
        builder.HasKey(surum => surum.Id);
        builder.Property(surum => surum.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(surum => surum.Locale).HasColumnName("locale").HasMaxLength(10);

        // Sayfa metniyle aynı kolon genişliği: kopya kaynağından dar olamaz.
        builder.Property(surum => surum.Title).HasColumnName("title").HasMaxLength(1000);
        builder.Property(surum => surum.Body).HasColumnName("body");
        builder.Property(surum => surum.RecordedAt).HasColumnName("recorded_at");
        builder.Property(surum => surum.IsInitial).HasColumnName("is_initial");
    }
}
