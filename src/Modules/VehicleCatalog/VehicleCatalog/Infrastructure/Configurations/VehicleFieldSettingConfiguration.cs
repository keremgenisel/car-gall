using CarGallery.Modules.VehicleCatalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

internal sealed class VehicleFieldSettingConfiguration : IEntityTypeConfiguration<VehicleFieldSetting>
{
    public void Configure(EntityTypeBuilder<VehicleFieldSetting> builder)
    {
        builder.ToTable("vehicle_field_settings");

        builder.HasKey(setting => setting.FieldKey);

        builder.Property(setting => setting.FieldKey).HasColumnName("field_key").HasMaxLength(60);

        // Durum adı veritabanında çevrilmiş hâliyle saklanmaz; kod saklanır (Yasak Listesi 22).
        builder.Property(setting => setting.FormState).HasColumnName("form_state").HasConversion<int>();
        builder.Property(setting => setting.PublicVisibility).HasColumnName("public_visibility").HasConversion<int>();
        builder.Property(setting => setting.IsCore).HasColumnName("is_core");

        // Alan listesi kodda tanımlı, sabit ve versiyonludur; kullanıcı yeni alan tanımlayamaz
        // (F5, Yasak Listesi 40). Bu yüzden çekirdek alanlar migration ile gelir.
        builder.HasData(new VehicleFieldSetting
        {
            FieldKey = VehicleFieldKeys.BodyType,
            FormState = FieldFormState.Required,
            PublicVisibility = FieldPublicVisibility.Visible,
            IsCore = true,
        });
    }
}
