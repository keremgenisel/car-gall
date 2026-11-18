using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

/// <summary>
/// `0016b` D25 — galerinin okuma modeli tablosu. Vitrin satırının sahip olunan koleksiyonu
/// DEĞİL, ayrı bir varlıktır: medya olay işleyicisi yuvayı varlık kimliğiyle ve tek bir
/// `UPDATE` ile günceller, vitrin satırını yüklemeden.
/// </summary>
internal sealed class ShowcaseListingMediaConfiguration : IEntityTypeConfiguration<ShowcaseListingMedia>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShowcaseListingMedia> builder)
    {
        builder.ToTable("showcase_listing_media");
        builder.HasKey(item => new { item.ListingId, item.Position });

        builder.Property(item => item.ListingId).HasColumnName("listing_id");
        builder.Property(item => item.Position).HasColumnName("position");
        builder.Property(item => item.MediaAssetId).HasColumnName("media_asset_id");
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<short>();
        builder.Property(item => item.Variants)
            .HasColumnName("variants")
            .HasColumnType("jsonb")
            .HasConversion(CatalogConverters.MediaVariants, CatalogConverters.MediaVariantsComparer);

        // Olay işleyicileri yuvayı varlık kimliğiyle arar.
        builder.HasIndex(item => item.MediaAssetId).HasDatabaseName("ix_showcase_listing_media_asset");

        // Vitrin satırı silinince (ilan vitrinden çıkınca) yuvaları da gider.
        builder.HasOne<ShowcaseListing>()
            .WithMany()
            .HasForeignKey(item => item.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
