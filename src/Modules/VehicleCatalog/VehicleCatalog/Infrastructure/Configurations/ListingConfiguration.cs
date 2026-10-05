using CarGallery.Modules.VehicleCatalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

internal sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("listings");

        builder.HasKey(listing => listing.Id);
        builder.Property(listing => listing.Id).HasColumnName("id").HasConversion(CatalogConverters.ListingId);

        builder.Ignore(listing => listing.DomainEvents);

        builder.Property(listing => listing.VehicleId)
            .HasColumnName("vehicle_id")
            .HasConversion(CatalogConverters.VehicleId);

        // C11 — veritabanında çevrilmiş durum adı saklanmaz; KOD saklanır (Yasak Listesi 22).
        // Enum'un sayısal değeri kodun kendisidir.
        builder.Property(listing => listing.Status).HasColumnName("status").HasConversion<int>();

        builder.Property(listing => listing.ShowcaseSortWeight).HasColumnName("showcase_sort_weight");
        builder.Property(listing => listing.PublishedAt).HasColumnName("published_at");

        // Bir araca bir ilan: A3'ün durum makinesi tek ilan üzerinde yürüyor.
        builder.HasIndex(listing => listing.VehicleId).IsUnique().HasDatabaseName("ux_listings_vehicle_id");
    }
}
