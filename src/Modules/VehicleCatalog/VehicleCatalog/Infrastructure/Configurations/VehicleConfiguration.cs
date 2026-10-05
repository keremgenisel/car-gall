using CarGallery.Modules.VehicleCatalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles");

        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).HasColumnName("id").HasConversion(CatalogConverters.VehicleId);

        builder.Ignore(vehicle => vehicle.DomainEvents);
        builder.Ignore(vehicle => vehicle.CoverAssetId);
        builder.Ignore(vehicle => vehicle.BodyType);

        builder.Property(vehicle => vehicle.BranchId).HasColumnName("branch_id");
        builder.Property(vehicle => vehicle.VehicleModelId).HasColumnName("vehicle_model_id");
        builder.Property(vehicle => vehicle.ModelYear).HasColumnName("model_year");
        builder.Property(vehicle => vehicle.MileageKilometers).HasColumnName("mileage_kilometers");
        builder.Property(vehicle => vehicle.AwaitingBranchAssignment).HasColumnName("awaiting_branch_assignment");
        builder.Property(vehicle => vehicle.IsArchived).HasColumnName("is_archived");

        // A5/AC-16, AC-17 — şase galeri genelinde TEKİL, `Arşiv` muaf.
        //
        // **Neden indeks, neden `InvariantGuard` değil:** satır kilidi VAR OLAN satırları
        // kilitler. Yeni bir şase için henüz satır yoktur; iki eşzamanlı kayıt da "0 tane
        // var" görür ve ikisi de geçer. `0007`'nin deseni mevcut satırları sayan
        // invariant'lar için doğru, yeni bir değerin tekilliği için değil — ölçüldü ve
        // D11 bu yüzden düzeltildi.
        builder.HasIndex(vehicle => vehicle.Vin)
            .IsUnique()
            .HasFilter("NOT is_archived")
            .HasDatabaseName("ux_vehicles_vin");

        builder.Property(vehicle => vehicle.Vin)
            .HasColumnName("vin")
            .HasConversion(CatalogConverters.Vin)
            .HasMaxLength(Vin.MaximumLength);

        builder.Property(vehicle => vehicle.Color)
            .HasColumnName("color_names")
            .HasColumnType("jsonb")
            .HasConversion(CatalogConverters.LocalizedText, CatalogConverters.LocalizedTextComparer);

        builder.ComplexProperty(vehicle => vehicle.Specification, specification =>
        {
            specification.Property(item => item.BodyType)
                .HasColumnName("body_type_slug")
                .HasConversion(CatalogConverters.BodyTypeSlug)
                .HasMaxLength(40);

            specification.Property(item => item.FuelTypeSlug).HasColumnName("fuel_type_slug").HasMaxLength(40);
            specification.Property(item => item.TransmissionTypeSlug).HasColumnName("transmission_type_slug").HasMaxLength(40);
            specification.Property(item => item.DrivetrainTypeSlug).HasColumnName("drivetrain_type_slug").HasMaxLength(40);
            specification.Property(item => item.TrimLevelSlug).HasColumnName("trim_level_slug").HasMaxLength(40);
        });

        builder.Property(vehicle => vehicle.Price)
            .HasColumnName("price")
            .HasMaxLength(64)
            .HasConversion(CatalogConverters.ListingPrice);

        builder.Property<List<Guid>>("mediaAssetIds")
            .HasColumnName("media_asset_ids")
            .HasColumnType("jsonb")
            .HasConversion(CatalogConverters.GuidList, CatalogConverters.GuidListComparer);

        // Koleksiyonlar salt okunur özelliklerle dışarı veriliyor (`=> optionalFieldValues`);
        // EF onlara ALAN üzerinden yazmak zorunda. Erişim modu `OwnsMany`/`HasMany`'den
        // SONRA ayarlanır — öncesinde navigation henüz tanımlı değildir ve
        // `FindNavigation` null döner.
        builder.OwnsMany(vehicle => vehicle.OptionalFieldValues, value =>
        {
            value.ToTable("vehicle_optional_field_values");
            value.WithOwner().HasForeignKey("vehicle_id");
            value.Property(item => item.FieldKey).HasColumnName("field_key").HasMaxLength(80);
            value.Property(item => item.Value)
                .HasColumnName("value")
                .HasColumnType("jsonb")
                .HasConversion(CatalogConverters.LocalizedText, CatalogConverters.LocalizedTextComparer);
            value.HasKey("vehicle_id", nameof(VehicleOptionalFieldValue.FieldKey));
        });

        builder.Navigation(vehicle => vehicle.OptionalFieldValues)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Fiyat geçmişi KENDİ tablosunda ama araca aittir: `Vehicle.ChangePrice` yazıyor ve
        // dışarıdan bir servis yazamıyor — invariant agregatın invariantı (A6).
        builder.HasMany(vehicle => vehicle.PriceChanges)
            .WithOne()
            .HasForeignKey(change => change.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(vehicle => vehicle.PriceChanges)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
