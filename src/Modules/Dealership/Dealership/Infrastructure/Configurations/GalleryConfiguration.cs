using CarGallery.Modules.Dealership.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Dealership.Infrastructure.Configurations;

internal sealed class GalleryConfiguration : IEntityTypeConfiguration<Gallery>
{
    public void Configure(EntityTypeBuilder<Gallery> builder)
    {
        builder.ToTable("galleries", table => table.HasCheckConstraint(
            "ck_galleries_singleton",
            $"id = '{Gallery.SingletonId}'"));

        builder.HasKey(gallery => gallery.Id);
        builder.Property(gallery => gallery.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(gallery => gallery.CommercialName).HasColumnName("commercial_name").HasMaxLength(200);
        builder.Property(gallery => gallery.LogoMediaAssetId).HasColumnName("logo_media_asset_id");
        builder.Property(gallery => gallery.TaxNumber).HasColumnName("tax_number").HasMaxLength(40);
        builder.Property(gallery => gallery.TradeRegistryNumber).HasColumnName("trade_registry_number").HasMaxLength(40);

        builder.OwnsMany(gallery => gallery.SocialLinks, link =>
        {
            link.ToTable("gallery_social_links");
            link.WithOwner().HasForeignKey("gallery_id");
            link.Property(item => item.Platform).HasColumnName("platform").HasMaxLength(40);
            link.Property(item => item.Url).HasColumnName("url").HasMaxLength(400);
            link.HasKey("gallery_id", "Platform");
        });
    }
}
