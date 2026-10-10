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

        // C22 — ilan metni `jsonb`, locale başına satır olarak değil (0010a D13).
        // `color_names` ile aynı desen; ek tablo açmadan aynı yolu tekrar ediyor.
        //
        // Karşılaştırıcı olmadan EF `LocalizedText`'i REFERANSLA karşılaştırır ve içerik
        // değişikliğini kaçırır: düzenleme sessizce kaybolur.
        builder.Property(listing => listing.Title)
            .HasColumnName("title")
            .HasColumnType("jsonb")
            .HasConversion(CatalogConverters.LocalizedText, CatalogConverters.LocalizedTextComparer);

        builder.Property(listing => listing.Description)
            .HasColumnName("description")
            .HasColumnType("jsonb")
            .HasConversion(CatalogConverters.LocalizedText, CatalogConverters.LocalizedTextComparer);

        // Bir araca bir ilan: A3'ün durum makinesi tek ilan üzerinde yürüyor.
        builder.HasIndex(listing => listing.VehicleId).IsUnique().HasDatabaseName("ux_listings_vehicle_id");
    }
}
