using CarGallery.Modules.VehicleReference.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleReference.Infrastructure.Configurations;

internal sealed class BodyTypeConfiguration : IEntityTypeConfiguration<BodyType>
{
    public void Configure(EntityTypeBuilder<BodyType> builder)
    {
        builder.ToTable("body_types");
        builder.HasKey(type => type.Slug);
        builder.Property(type => type.Slug).HasColumnName("slug").HasMaxLength(40);
        builder.Property(type => type.CanonicalName).HasColumnName("canonical_name").HasMaxLength(120);

        // Çeviriler owned collection olarak agregatın parçasıdır, ayrı agregat değildir
        // (docs/conventions.md § 10).
        builder.OwnsMany(type => type.Translations, translation =>
        {
            translation.ToTable("body_type_translations");
            translation.WithOwner().HasForeignKey(item => item.BodyTypeSlug);
            translation.HasKey(item => new { item.BodyTypeSlug, item.Locale });
            translation.Property(item => item.BodyTypeSlug).HasColumnName("body_type_slug").HasMaxLength(40);
            translation.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(3);
            translation.Property(item => item.Name).HasColumnName("name").HasMaxLength(120);

            // Kanonik küme kodda tanımlıdır ve C13'ün slug biçimine uyar: küçük harfli ASCII.
            // Çeviri zorunludur — referans veri ziyaretçinin gördüğü metnin çoğunu üretir;
            // eksik çeviri sayfayı yarı yabancı dilde bırakır.
            translation.HasData(
                Translation("sedan", "tr", "Sedan"), Translation("sedan", "ar", "سيدان"), Translation("sedan", "en", "Sedan"),
                Translation("suv", "tr", "SUV"), Translation("suv", "ar", "دفع رباعي"), Translation("suv", "en", "SUV"),
                Translation("hatchback", "tr", "Hatchback"), Translation("hatchback", "ar", "هاتشباك"), Translation("hatchback", "en", "Hatchback"),
                Translation("station-wagon", "tr", "Station Wagon"), Translation("station-wagon", "ar", "ستيشن"), Translation("station-wagon", "en", "Station Wagon"),
                Translation("coupe", "tr", "Coupe"), Translation("coupe", "ar", "كوبيه"), Translation("coupe", "en", "Coupe"),
                Translation("pickup", "tr", "Pikap"), Translation("pickup", "ar", "بيك أب"), Translation("pickup", "en", "Pickup"));
        });

        builder.HasData(
            new BodyType { Slug = "sedan", CanonicalName = "Sedan" },
            new BodyType { Slug = "suv", CanonicalName = "SUV" },
            new BodyType { Slug = "hatchback", CanonicalName = "Hatchback" },
            new BodyType { Slug = "station-wagon", CanonicalName = "Station Wagon" },
            new BodyType { Slug = "coupe", CanonicalName = "Coupe" },
            new BodyType { Slug = "pickup", CanonicalName = "Pickup" });
    }

    private static BodyTypeTranslation Translation(string slug, string locale, string name) =>
        new() { BodyTypeSlug = slug, Locale = locale, Name = name };
}

internal sealed class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> builder)
    {
        builder.ToTable("vehicle_models");
        builder.HasKey(model => model.Id);
        builder.Property(model => model.Id).HasColumnName("id");
        builder.Property(model => model.CanonicalName).HasColumnName("canonical_name").HasMaxLength(160);

        // `0021` — marka bağı, tekillik anahtarı (D11: marka İÇİNDE tekil), pasiflik ve sürüm.
        builder.Property(model => model.BrandId).HasColumnName("brand_id");
        builder.Property(model => model.CanonicalKey).HasColumnName("canonical_key").HasMaxLength(160);
        builder.Property(model => model.IsActive).HasColumnName("is_active");
        builder.Property(model => model.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(model => new { model.BrandId, model.CanonicalKey })
            .IsUnique()
            .HasDatabaseName("ux_vehicle_models_brand_canonical_key");
        builder.HasOne<VehicleBrand>()
            .WithMany()
            .HasForeignKey(model => model.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(model => model.Translations, translation =>
        {
            translation.ToTable("vehicle_model_translations");
            translation.WithOwner().HasForeignKey(item => item.VehicleModelId);
            translation.HasKey(item => new { item.VehicleModelId, item.Locale });
            translation.Property(item => item.VehicleModelId).HasColumnName("vehicle_model_id");
            translation.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(3);
            translation.Property(item => item.Name).HasColumnName("name").HasMaxLength(160);
        });
    }
}

/// <summary>
/// `0021` — marka. Kanonik ad ve kısaltma galeri genelinde tekildir (C3, D19); ikisi ayrı
/// indekstir, çünkü çakışmaları ayrı hata kodlarıdır ("Alfa-Romeo" adı farklı ama kısaltması
/// "Alfa Romeo"nunkiyle aynı — AC-9). Yarışta ikinci ekleme indeksten düşer.
/// </summary>
internal sealed class VehicleBrandConfiguration : IEntityTypeConfiguration<VehicleBrand>
{
    internal const string CanonicalKeyIndex = "ux_vehicle_brands_canonical_key";

    internal const string SlugIndex = "ux_vehicle_brands_slug";

    public void Configure(EntityTypeBuilder<VehicleBrand> builder)
    {
        builder.ToTable("vehicle_brands");
        builder.HasKey(brand => brand.Id);
        builder.Property(brand => brand.Id).HasColumnName("id");
        builder.Property(brand => brand.CanonicalName).HasColumnName("canonical_name").HasMaxLength(ReferenceNames.BrandCanonicalMaxLength);
        builder.Property(brand => brand.CanonicalKey).HasColumnName("canonical_key").HasMaxLength(ReferenceNames.BrandCanonicalMaxLength);
        builder.Property(brand => brand.Slug).HasColumnName("slug").HasMaxLength(ReferenceNames.BrandCanonicalMaxLength);
        builder.Property(brand => brand.IsActive).HasColumnName("is_active");
        builder.Property(brand => brand.ShowOnHome).HasColumnName("show_on_home");
        builder.Property(brand => brand.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(brand => brand.CanonicalKey).IsUnique().HasDatabaseName(CanonicalKeyIndex);
        builder.HasIndex(brand => brand.Slug).IsUnique().HasDatabaseName(SlugIndex);

        builder.OwnsMany(brand => brand.Translations, translation =>
        {
            translation.ToTable("vehicle_brand_translations");
            translation.WithOwner().HasForeignKey(item => item.BrandId);
            translation.HasKey(item => new { item.BrandId, item.Locale });
            translation.Property(item => item.BrandId).HasColumnName("brand_id");
            translation.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(3);
            translation.Property(item => item.Name).HasColumnName("name").HasMaxLength(ReferenceNames.DisplayNameMaxLength);
        });
    }
}
