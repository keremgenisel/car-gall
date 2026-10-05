using CarGallery.Modules.VehicleCatalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

/// <summary>
/// A6 — fiyat geçmişi. **Silme ve güncelleme yolu yoktur**: ne domain'de kaldırma metodu,
/// ne uçta silme. Tablo yalnız ekleme alır (AC-24, gözlem noktası C19).
/// </summary>
internal sealed class PriceChangeConfiguration : IEntityTypeConfiguration<PriceChange>
{
    public void Configure(EntityTypeBuilder<PriceChange> builder)
    {
        builder.ToTable("price_changes");

        builder.HasKey(change => change.Id);
        // Anahtar İSTEMCİDE üretiliyor (`Guid.CreateVersion7`). Söylenmezse EF `Guid` bir
        // birincil anahtarı mağaza üretimli sayar ve istemcide dolu gelen anahtarı "zaten
        // var" diye okur: yeni kayıt `Added` yerine `Modified` olur ve olmayan bir satıra
        // UPDATE gider. Ölçüldü — `DbUpdateConcurrencyException`, "0 satır etkilendi".
        builder.Property(change => change.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(change => change.VehicleId)
            .HasColumnName("vehicle_id")
            .HasConversion(CatalogConverters.VehicleId);

        builder.Property(change => change.ChangedAt).HasColumnName("changed_at");

        builder.Property(change => change.Price)
            .HasColumnName("price")
            .HasMaxLength(64)
            .HasConversion(CatalogConverters.ListingPrice);

        builder.HasIndex(change => change.VehicleId).HasDatabaseName("ix_price_changes_vehicle_id");
    }
}
