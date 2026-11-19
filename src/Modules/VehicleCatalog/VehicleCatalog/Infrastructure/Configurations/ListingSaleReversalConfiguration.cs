using CarGallery.Modules.VehicleCatalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

/// <summary>
/// `0023` **D7** — satış geri alma kaydı, modülün kendi şemasında kendi tablosunda (`0013`'ün
/// denetim kaydı deseni). Tablo yalnız ekleme alır.
/// </summary>
internal sealed class ListingSaleReversalConfiguration : IEntityTypeConfiguration<ListingSaleReversal>
{
    public void Configure(EntityTypeBuilder<ListingSaleReversal> builder)
    {
        builder.ToTable("listing_sale_reversals");

        builder.HasKey(reversal => reversal.Id);

        // Anahtar istemcide üretiliyor; söylenmezse EF yeni kaydı "zaten var" okur ve
        // olmayan bir satıra UPDATE gönderir (`PriceChangeConfiguration`'ın ölçülmüş dersi).
        builder.Property(reversal => reversal.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new SaleReversalId(value))
            .ValueGeneratedNever();

        builder.Property(reversal => reversal.ListingId)
            .HasColumnName("listing_id")
            .HasConversion(CatalogConverters.ListingId);

        builder.Property(reversal => reversal.VehicleId)
            .HasColumnName("vehicle_id")
            .HasConversion(CatalogConverters.VehicleId);

        builder.Property(reversal => reversal.StaffId).HasColumnName("staff_id");

        builder.Property(reversal => reversal.StaffDisplayName)
            .HasColumnName("staff_display_name")
            .HasMaxLength(ListingSaleReversal.MaximumStaffNameLength);

        builder.Property(reversal => reversal.Reason)
            .HasColumnName("reason")
            .HasMaxLength(ListingSaleReversal.MaximumReasonLength);

        // Kod saklanır, çevrilmiş ad değil (Yasak Listesi 22).
        builder.Property(reversal => reversal.RestoredStatus)
            .HasColumnName("restored_status")
            .HasConversion<int>();

        builder.Property(reversal => reversal.RevertedAt).HasColumnName("reverted_at");

        // R7 — panel kayıtları ARAÇ ekranında okuyor; uçlar araç kimliğiyle çalışıyor.
        builder.HasIndex(reversal => reversal.VehicleId)
            .HasDatabaseName("ix_listing_sale_reversals_vehicle_id");
    }
}
