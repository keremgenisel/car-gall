using CarGallery.Modules.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CarGallery.Modules.Content.Infrastructure.Configurations;

/// <summary>
/// `0017` — sayfalar, dil başına metinleri ve eski slug'ları. Durum KOD olarak saklanır
/// (C3). Slug tekilliği dil başınadır ve taslakları da kapsar (R13, D30); boş slug
/// (girilmemiş) tekillik dışındadır.
/// </summary>
internal sealed class ContentPageConfiguration : IEntityTypeConfiguration<ContentPage>
{
    /// <summary>Durum ↔ kod dönüşümü (C3, AC-131).</summary>
    internal static readonly ValueConverter<PublicationStatus, string> StatusConverter = new(
        status => PublicationStatusCodes.ToCode(status),
        code => PublicationStatusCodes.FromCode(code));

    /// <summary>`0017` D6 — sistem sayfalarının sabit kimlikleri; kurulumda metinsiz taslak (R3).</summary>
    internal static readonly IReadOnlyDictionary<string, Guid> SystemPageIds = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        [SystemPageKeys.About] = Guid.Parse("0192c000-0000-7000-8000-0000c0a70001"),
        [SystemPageKeys.Privacy] = Guid.Parse("0192c000-0000-7000-8000-0000c0a70002"),
        [SystemPageKeys.Cookies] = Guid.Parse("0192c000-0000-7000-8000-0000c0a70003"),
        [SystemPageKeys.Faq] = Guid.Parse("0192c000-0000-7000-8000-0000c0a70004"),
    };

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ContentPage> builder)
    {
        builder.ToTable("pages");
        builder.HasKey(page => page.Id);
        builder.Property(page => page.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(page => page.SystemKey).HasColumnName("system_key").HasMaxLength(20);
        builder.HasIndex(page => page.SystemKey).IsUnique().HasDatabaseName("ux_pages_system_key");

        // C14 — iyimser eşzamanlılık: güncelleme `WHERE version = <okunan>` ile yazılır;
        // aynı sürümden iki paralel kayıttan biri sıfır satır günceller ve `409` olur (AC-32).
        builder.Property(page => page.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(page => page.CreatedAt).HasColumnName("created_at");
        builder.Ignore(page => page.IsSystem);

        builder.OwnsMany(page => page.Localizations, satir =>
        {
            satir.ToTable("page_localizations");
            satir.WithOwner().HasForeignKey("PageId");
            satir.Property<Guid>("PageId").HasColumnName("page_id");
            satir.HasKey("PageId", nameof(PageLocalization.Locale));
            satir.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(10);
            satir.Property(item => item.Title).HasColumnName("title").HasMaxLength(1000);
            satir.Property(item => item.Body).HasColumnName("body");
            satir.Property(item => item.Slug).HasColumnName("slug").HasMaxLength(80);
            satir.Property(item => item.Status).HasColumnName("status").HasMaxLength(10).HasConversion(StatusConverter);

            // `0019` P2 — işaretçi; sürüm tablosuna FK yok: sürüm silinemez (tetikleyici) ve
            // testlerin `TRUNCATE` temizliği bir FK ile kilitlenirdi.
            satir.Property(item => item.PublishedVersionId).HasColumnName("published_version_id");
            satir.Ignore(item => item.IsComplete);

            // R13/D30 — dil başına tekil; taslaklar dahil. Girilmemiş (boş) slug tekillik dışı.
            satir.HasIndex(item => new { item.Locale, item.Slug })
                .IsUnique()
                .HasFilter("slug <> ''")
                .HasDatabaseName("ux_page_localizations_locale_slug");
        });

        builder.OwnsMany(page => page.OldSlugs, eski =>
        {
            eski.ToTable("page_old_slugs");
            eski.WithOwner().HasForeignKey("PageId");
            eski.Property<Guid>("PageId").HasColumnName("page_id");
            eski.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(10);
            eski.Property(item => item.Slug).HasColumnName("slug").HasMaxLength(80);

            // R14 — bir dilde kaydedilmiş eski slug o dilde tek bir sayfaya aittir.
            eski.HasKey(nameof(PageOldSlug.Locale), nameof(PageOldSlug.Slug));
        });

        DateTimeOffset kurulum = new(2026, 11, 18, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(SystemPageIds.Select(pair => new
        {
            Id = pair.Value,
            SystemKey = (string?)pair.Key,
            Version = 1,
            CreatedAt = kurulum,
        }));
    }
}
