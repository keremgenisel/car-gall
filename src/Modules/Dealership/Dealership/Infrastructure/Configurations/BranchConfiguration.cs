using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Dealership.Infrastructure.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches");

        builder.HasKey(branch => branch.Id);
        builder.Property(branch => branch.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new BranchId(value));

        builder.Property(branch => branch.Names)
            .HasColumnName("names")
            .HasColumnType("jsonb")
            .HasConversion(LocalizedTextStorage.Converter, LocalizedTextStorage.Comparer);

        builder.Property(branch => branch.Addresses)
            .HasColumnName("addresses")
            .HasColumnType("jsonb")
            .HasConversion(LocalizedTextStorage.Converter, LocalizedTextStorage.Comparer);

        builder.Property(branch => branch.Phone)
            .HasColumnName("phone")
            .HasMaxLength(16)
            .HasConversion(phone => phone.Value, value => PhoneNumber.Create(value).Value);

        builder.Property(branch => branch.WhatsApp)
            .HasColumnName("whatsapp")
            .HasMaxLength(16)
            .HasConversion(
                phone => phone == null ? null : phone.Value.Value,
                value => value == null ? null : PhoneNumber.Create(value).Value);

        builder.Property(branch => branch.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .HasConversion(
                email => email == null ? null : email.Value.Value,
                value => value == null ? null : EmailAddress.Create(value).Value);

        builder.Property(branch => branch.TimeZone)
            .HasColumnName("time_zone")
            .HasMaxLength(60)
            .HasConversion(zone => zone.Value, value => TimeZoneId.Create(value).Value);

        // C13 — durumun ÇEVRİLMİŞ adı saklanmaz; kod saklanır, uçta çevrilir.
        builder.Property(branch => branch.Status).HasColumnName("status").HasConversion<int>();

        builder.Property(branch => branch.Slug)
            .HasColumnName("slug")
            .HasMaxLength(80)
            .HasConversion(slug => slug.Value, value => BranchSlug.Create(value).Value);

        // R6/AC-11 — slug galeri genelinde tekildir.
        builder.HasIndex(branch => branch.Slug).HasDatabaseName("ux_branches_slug").IsUnique();

        // C8 — koordinat decimal kolonlarda; PostGIS yok.
        builder.ComplexProperty(branch => branch.Coordinate, coordinate =>
        {
            coordinate.IsRequired(false);
            coordinate.Property(item => item.Latitude).HasColumnName("latitude").HasPrecision(18, 8);
            coordinate.Property(item => item.Longitude).HasColumnName("longitude").HasPrecision(18, 8);
        });

        builder.PrimitiveCollection<List<Guid>>("mediaAssetIds").HasColumnName("media_asset_ids");

        builder.Ignore(branch => branch.MediaAssetIds);
        builder.Ignore(branch => branch.DomainEvents);
    }
}
