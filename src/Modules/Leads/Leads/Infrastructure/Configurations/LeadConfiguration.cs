using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Leads.Infrastructure.Configurations;

/// <summary>
/// Kolon düzeni R13'ün doğrudan karşılığıdır: KİŞİSEL alanlar (`full_name`, `phone`,
/// `email`, `message`) hepsi null'lanabilir ve bir arada durur; İSTATİSTİKSEL alanlar
/// (tip, şube, locale, tarih, anlık görüntü) null'lanamaz. Anonimleştirme birincileri
/// boşaltır, ikincilere dokunmaz (**L6**, AC-29).
/// </summary>
internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");

        builder.HasKey(lead => lead.Id);
        builder.Property(lead => lead.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new LeadId(value));

        // Tip ve durum KOD olarak saklanır; çevrilmiş adı saklanmaz (Yasak Listesi 22).
        builder.Property(lead => lead.Type).HasColumnName("type").HasConversion<int>();
        builder.Property(lead => lead.Status).HasColumnName("status").HasConversion<int>();

        builder.Property(lead => lead.BranchId).HasColumnName("branch_id");

        builder.Property(lead => lead.SubmittedLocale)
            .HasColumnName("submitted_locale")
            .HasMaxLength(3)
            .HasConversion(locale => locale.Code, value => Locale.Create(value).Value);

        builder.Property(lead => lead.ConsentGiven).HasColumnName("consent_given");
        builder.Property(lead => lead.CapturedAt).HasColumnName("captured_at");

        builder.Property(lead => lead.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100);

        // R17/AC-35 — aynı anahtarla ikinci kayıt OLUŞAMAZ. Kural uygulamada değil
        // veritabanında dayatılır: iki eşzamanlı istek arasındaki yarışı bir SELECT
        // kazanamaz, tekil indeks kazanır.
        builder.HasIndex(lead => lead.IdempotencyKey)
            .HasDatabaseName("ux_leads_idempotency_key")
            .IsUnique();

        builder.ComplexProperty(lead => lead.Contact, contact =>
        {
            contact.Property(item => item.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(ContactDetails.MaximumNameLength);

            contact.Property(item => item.Phone).HasColumnName("phone").HasMaxLength(16);
            contact.Property(item => item.Email).HasColumnName("email").HasMaxLength(EmailAddress.MaximumLength);

            contact.Property(item => item.Message)
                .HasColumnName("message")
                .HasMaxLength(ContactDetails.MaximumMessageLength);
        });

        builder.ComplexProperty(lead => lead.Vehicle, vehicle =>
        {
            vehicle.Property(item => item.ListingId).HasColumnName("listing_id");

            vehicle.Property(item => item.CanonicalModelName)
                .HasColumnName("vehicle_canonical_model_name")
                .HasMaxLength(200);

            vehicle.Property(item => item.PriceOnRequest).HasColumnName("vehicle_price_on_request");

            // C6/AC-22 — decimal(19,4); üç ondalıklı para birimleri bozulmadan saklanır.
            vehicle.Property(item => item.PriceAmount)
                .HasColumnName("vehicle_price_amount")
                .HasColumnType("decimal(19,4)");

            vehicle.Property(item => item.PriceCurrencyCode)
                .HasColumnName("vehicle_price_currency_code")
                .HasMaxLength(3);

            vehicle.Property(item => item.PriceMinorUnitExponent)
                .HasColumnName("vehicle_price_minor_unit_exponent");
        });

        builder.HasIndex(lead => lead.BranchId).HasDatabaseName("ix_leads_branch_id");

        builder.Ignore(lead => lead.DomainEvents);
    }
}
