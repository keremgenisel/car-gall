using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

internal sealed class ShowcaseListingConfiguration : IEntityTypeConfiguration<ShowcaseListing>
{
    public void Configure(EntityTypeBuilder<ShowcaseListing> builder)
    {
        builder.ToTable("showcase_listings");

        builder.HasKey(listing => listing.ListingId);

        builder.Property(listing => listing.ListingId).HasColumnName("listing_id");
        builder.Property(listing => listing.VehicleId).HasColumnName("vehicle_id");
        builder.Property(listing => listing.IsReserved).HasColumnName("is_reserved");
        builder.Property(listing => listing.ShowcaseSortWeight).HasColumnName("showcase_sort_weight");
        builder.Property(listing => listing.PublishedAt).HasColumnName("published_at");
        builder.Property(listing => listing.BodyTypeSlug).HasColumnName("body_type_slug").HasMaxLength(40);
        builder.Property(listing => listing.VehicleModelId).HasColumnName("vehicle_model_id");
        builder.Property(listing => listing.ModelYear).HasColumnName("model_year");
        builder.Property(listing => listing.MileageKilometers).HasColumnName("mileage_kilometers");

        // decimal(_,2) Kuveyt'te sessiz veri kaybıdır: KWD üç ondalıklıdır (docs/conventions.md § 7).
        builder.Property(listing => listing.PriceAmount).HasColumnName("price_amount").HasColumnType("decimal(19,4)");
        builder.Property(listing => listing.PriceCurrencyCode).HasColumnName("price_currency_code").HasMaxLength(3);
        builder.Property(listing => listing.PriceMinorUnitExponent).HasColumnName("price_minor_unit_exponent");
        builder.Property(listing => listing.PriceOnRequest).HasColumnName("price_on_request");

        builder.Property(listing => listing.BranchId).HasColumnName("branch_id");
        builder.Property(listing => listing.BranchNames)
            .HasColumnName("branch_names")
            .HasColumnType("jsonb")
            .HasConversion(LocalizedNames.Converter, LocalizedNames.Comparer);
        builder.Property(listing => listing.CoverMediaUrl).HasColumnName("cover_media_url");

        // R7'nin tam sıralaması tek indeksle karşılanır; üçüncü kırıcı (ilan kimliği) indeksin
        // parçasıdır, yoksa eşit ağırlıklı ilanlar sayfalar arasında kayar (AC-5).
        builder.HasIndex(
            listing => new { listing.ShowcaseSortWeight, listing.PublishedAt, listing.ListingId })
            .HasDatabaseName("ix_showcase_listings_ordering")
            .IsDescending(true, true, false);

        builder.HasIndex(listing => listing.BodyTypeSlug)
            .HasDatabaseName("ix_showcase_listings_body_type");
    }
}
