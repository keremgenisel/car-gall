using CarGallery.Modules.VehicleReference.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.VehicleReference.Infrastructure.Configurations;

internal sealed class ReferenceTermConfiguration : IEntityTypeConfiguration<ReferenceTerm>
{
    public void Configure(EntityTypeBuilder<ReferenceTerm> builder)
    {
        builder.ToTable("reference_terms");
        builder.HasKey(term => new { term.Kind, term.Slug });

        builder.Property(term => term.Kind).HasColumnName("kind").HasConversion<int>();
        builder.Property(term => term.Slug).HasColumnName("slug").HasMaxLength(40);
        builder.Property(term => term.CanonicalName).HasColumnName("canonical_name").HasMaxLength(120);

        builder.OwnsMany(term => term.Translations, translation =>
        {
            translation.ToTable("reference_term_translations");
            translation.WithOwner().HasForeignKey(item => new { item.Kind, item.Slug });
            translation.HasKey(item => new { item.Kind, item.Slug, item.Locale });
            translation.Property(item => item.Kind).HasColumnName("kind").HasConversion<int>();
            translation.Property(item => item.Slug).HasColumnName("slug").HasMaxLength(40);
            translation.Property(item => item.Locale).HasColumnName("locale").HasMaxLength(3);
            translation.Property(item => item.Name).HasColumnName("name").HasMaxLength(120);

            translation.HasData(Ceviriler());
        });

        builder.HasData(Terimler());
    }

    private static ReferenceTerm[] Terimler() =>
    [
        Terim(ReferenceTermKind.FuelType, "petrol", "Petrol"),
        Terim(ReferenceTermKind.FuelType, "diesel", "Diesel"),
        Terim(ReferenceTermKind.FuelType, "hybrid", "Hybrid"),
        Terim(ReferenceTermKind.FuelType, "electric", "Electric"),
        Terim(ReferenceTermKind.TransmissionType, "manual", "Manual"),
        Terim(ReferenceTermKind.TransmissionType, "automatic", "Automatic"),
        Terim(ReferenceTermKind.DrivetrainType, "front-wheel-drive", "Front-wheel drive"),
        Terim(ReferenceTermKind.DrivetrainType, "rear-wheel-drive", "Rear-wheel drive"),
        Terim(ReferenceTermKind.DrivetrainType, "all-wheel-drive", "All-wheel drive"),
        Terim(ReferenceTermKind.TrimLevel, "base", "Base"),
        Terim(ReferenceTermKind.TrimLevel, "comfort", "Comfort"),
        Terim(ReferenceTermKind.TrimLevel, "premium", "Premium"),
    ];

    private static ReferenceTermTranslation[] Ceviriler() =>
    [
        .. Ceviri(ReferenceTermKind.FuelType, "petrol", "Benzin", "بنزين", "Petrol"),
        .. Ceviri(ReferenceTermKind.FuelType, "diesel", "Dizel", "ديزل", "Diesel"),
        .. Ceviri(ReferenceTermKind.FuelType, "hybrid", "Hibrit", "هجين", "Hybrid"),
        .. Ceviri(ReferenceTermKind.FuelType, "electric", "Elektrik", "كهربائي", "Electric"),
        .. Ceviri(ReferenceTermKind.TransmissionType, "manual", "Manuel", "يدوي", "Manual"),
        .. Ceviri(ReferenceTermKind.TransmissionType, "automatic", "Otomatik", "أوتوماتيك", "Automatic"),
        .. Ceviri(ReferenceTermKind.DrivetrainType, "front-wheel-drive", "Önden çekiş", "دفع أمامي", "Front-wheel drive"),
        .. Ceviri(ReferenceTermKind.DrivetrainType, "rear-wheel-drive", "Arkadan itiş", "دفع خلفي", "Rear-wheel drive"),
        .. Ceviri(ReferenceTermKind.DrivetrainType, "all-wheel-drive", "Dört çeker", "دفع رباعي", "All-wheel drive"),
        .. Ceviri(ReferenceTermKind.TrimLevel, "base", "Standart", "أساسي", "Base"),
        .. Ceviri(ReferenceTermKind.TrimLevel, "comfort", "Comfort", "كومفورت", "Comfort"),
        .. Ceviri(ReferenceTermKind.TrimLevel, "premium", "Premium", "بريميوم", "Premium"),
    ];

    private static ReferenceTerm Terim(ReferenceTermKind kind, string slug, string canonicalName) =>
        new() { Kind = kind, Slug = slug, CanonicalName = canonicalName };

    private static ReferenceTermTranslation[] Ceviri(ReferenceTermKind kind, string slug, string tr, string ar, string en) =>
    [
        new() { Kind = kind, Slug = slug, Locale = "tr", Name = tr },
        new() { Kind = kind, Slug = slug, Locale = "ar", Name = ar },
        new() { Kind = kind, Slug = slug, Locale = "en", Name = en },
    ];
}
