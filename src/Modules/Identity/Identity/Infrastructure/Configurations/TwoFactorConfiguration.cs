using CarGallery.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Identity.Infrastructure.Configurations;

/// <summary>
/// Kurulum personel başına TEK satırdır: birincil anahtar `staff_id`'nin kendisidir.
/// Ayrı bir kimlik kolonu, aynı personel için ikinci bir kurulum satırının doğmasına
/// teknik olarak izin verirdi ve *"bekleyen kurulum yeniden başlatılır"* (D18) kuralı
/// o anda iki sır arasında seçim yapmak zorunda kalırdı.
/// </summary>
internal sealed class TwoFactorEnrollmentConfiguration : IEntityTypeConfiguration<TwoFactorEnrollment>
{
    public void Configure(EntityTypeBuilder<TwoFactorEnrollment> builder)
    {
        builder.ToTable("two_factor_enrollments");

        builder.HasKey(enrollment => enrollment.Id);
        builder.Property(enrollment => enrollment.Id)
            .HasColumnName("staff_id")
            .HasConversion(id => id.Value, value => new StaffId(value));

        // C2a/D3 — kolonda ŞİFRELİ metin durur; açık sır hiçbir kolonda bulunmaz (AC-48).
        // Biçim sürümlü olduğu için uzunluk şifreli gövdeden fazla pay taşıyor.
        builder.Property(enrollment => enrollment.SecretCipher)
            .HasColumnName("secret_cipher")
            .HasMaxLength(256);

        builder.Property(enrollment => enrollment.CreatedAt).HasColumnName("created_at");
        builder.Property(enrollment => enrollment.PendingExpiresAt).HasColumnName("pending_expires_at");
        builder.Property(enrollment => enrollment.ConfirmedAt).HasColumnName("confirmed_at");
        builder.Property(enrollment => enrollment.LastAcceptedStep).HasColumnName("last_accepted_step");
    }
}

internal sealed class RecoveryCodeConfiguration : IEntityTypeConfiguration<RecoveryCode>
{
    public void Configure(EntityTypeBuilder<RecoveryCode> builder)
    {
        builder.ToTable("recovery_codes");

        builder.HasKey(code => code.Id);
        builder.Property(code => code.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new RecoveryCodeId(value));

        builder.Property(code => code.StaffId)
            .HasColumnName("staff_id")
            .HasConversion(id => id.Value, value => new StaffId(value));

        // C2b — kodun KENDİSİ değil özeti. Özet kendi algoritmasını ve maliyetini içinde
        // taşıdığı için iterasyon sayısı değişse de eski satırlar doğrulanmaya devam eder.
        builder.ComplexProperty(code => code.CodeHash, hash =>
            hash.Property(item => item.Value).HasColumnName("code_hash").HasMaxLength(200));

        builder.Property(code => code.CreatedAt).HasColumnName("created_at");
        builder.Property(code => code.UsedAt).HasColumnName("used_at");

        // Giriş ve kalan sayı, personelin kodlarını birlikte arar.
        builder.HasIndex(code => code.StaffId).HasDatabaseName("ix_recovery_codes_staff_id");
    }
}

internal sealed class PendingLoginConfiguration : IEntityTypeConfiguration<PendingLogin>
{
    public void Configure(EntityTypeBuilder<PendingLogin> builder)
    {
        builder.ToTable("pending_logins");

        builder.HasKey(pending => pending.Id);
        builder.Property(pending => pending.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new PendingLoginId(value));

        builder.Property(pending => pending.StaffId)
            .HasColumnName("staff_id")
            .HasConversion(id => id.Value, value => new StaffId(value));

        // `Session` ile aynı desen (D19): satırda tokenin SHA-256 özeti durur, kendisi
        // yalnız çerezte yaşar. Sızan bir yedek doğrudan ikinci adımı geçmeye yetmesin.
        builder.Property(pending => pending.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
        builder.HasIndex(pending => pending.TokenHash)
            .HasDatabaseName("ux_pending_logins_token_hash")
            .IsUnique();

        builder.Property(pending => pending.CreatedAt).HasColumnName("created_at");
        builder.Property(pending => pending.ExpiresAt).HasColumnName("expires_at");
        builder.Property(pending => pending.ConsumedAt).HasColumnName("consumed_at");
        builder.Property(pending => pending.Attempts).HasColumnName("attempts");
    }
}

internal sealed class TwoFactorAuditEntryConfiguration : IEntityTypeConfiguration<TwoFactorAuditEntry>
{
    public void Configure(EntityTypeBuilder<TwoFactorAuditEntry> builder)
    {
        builder.ToTable("two_factor_audit");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new TwoFactorAuditEntryId(value));

        builder.Property(entry => entry.StaffId)
            .HasColumnName("staff_id")
            .HasConversion(id => id.Value, value => new StaffId(value));

        builder.Property(entry => entry.ActorStaffId)
            .HasColumnName("actor_staff_id")
            .HasConversion(id => id.Value, value => new StaffId(value));

        // C7/AC-61 — olay KOD olarak saklanır; çevrilmiş adı saklanmaz.
        builder.Property(entry => entry.Event).HasColumnName("event").HasConversion<int>();

        builder.Property(entry => entry.OccurredAt).HasColumnName("occurred_at");

        // Kaydın okunma yolu: "bu personele ne oldu".
        builder.HasIndex(entry => entry.StaffId).HasDatabaseName("ix_two_factor_audit_staff_id");
    }
}
