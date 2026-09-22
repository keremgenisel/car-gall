using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarGallery.Modules.Identity.Infrastructure.Configurations;

internal sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    /// <summary>
    /// Tekillik NORMALİZE kolon üzerinde kurulur; ham kolon üzerinde `lower()` ile bir
    /// indeks kurulsaydı Türkçe collation altında yanlış eşleşirdi (Yasak Listesi 27).
    /// </summary>
    internal const string LoginIdIndexName = "ux_staff_normalized_login_id";

    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("staff");

        builder.HasKey(staff => staff.Id);
        builder.Property(staff => staff.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new StaffId(value));

        builder.Property(staff => staff.DisplayName).HasColumnName("display_name").HasMaxLength(200);

        // Okunurken normalize hâl değerden yeniden türetilir; ikisi ayrı düşemez.
        builder.Property(staff => staff.LoginId)
            .HasColumnName("login_id")
            .HasMaxLength(LoginId.MaximumLength)
            .HasConversion(loginId => loginId.Value, value => LoginId.Create(value).Value);

        builder.Property(staff => staff.NormalizedLoginId)
            .HasColumnName("normalized_login_id")
            .HasMaxLength(LoginId.MaximumLength);

        builder.HasIndex(staff => staff.NormalizedLoginId).HasDatabaseName(LoginIdIndexName).IsUnique();

        builder.ComplexProperty(staff => staff.PasswordHash, hash =>
            hash.Property(item => item.Value).HasColumnName("password_hash").HasMaxLength(200));

        // C7/AC-52 — rol ve durum KOD olarak saklanır; çevrilmiş adları saklanmaz.
        builder.Property(staff => staff.Role).HasColumnName("role").HasConversion<int>();
        builder.Property(staff => staff.Status).HasColumnName("status").HasConversion<int>();

        builder.Property(staff => staff.PreferredLocale)
            .HasColumnName("preferred_locale")
            .HasMaxLength(3)
            .HasConversion(locale => locale.Code, value => Locale.Create(value).Value);

        // Modüller arası referans SOFT'tur: foreign key kısıtı yoktur (Yasak Listesi 2).
        builder.Property(staff => staff.BranchId).HasColumnName("branch_id");

        builder.Property(staff => staff.MustChangePassword).HasColumnName("must_change_password");

        builder.Property(staff => staff.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasConversion(stamp => stamp.Value, value => new SecurityStamp(value));

        builder.Property(staff => staff.CreatedAt).HasColumnName("created_at");

        builder.Ignore(staff => staff.DomainEvents);
    }
}

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");

        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => new SessionId(value));

        builder.Property(session => session.StaffId)
            .HasColumnName("staff_id")
            .HasConversion(id => id.Value, value => new StaffId(value));

        builder.Property(session => session.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasConversion(stamp => stamp.Value, value => new SecurityStamp(value));

        // Tokenin KENDİSİ değil özeti; sızan bir yedek doğrudan oturum açmaya yetmesin.
        builder.Property(session => session.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
        builder.HasIndex(session => session.TokenHash).HasDatabaseName("ux_sessions_token_hash").IsUnique();

        builder.Property(session => session.IssuedAt).HasColumnName("issued_at");
        builder.Property(session => session.AccessExpiresAt).HasColumnName("access_expires_at");
        builder.Property(session => session.RefreshExpiresAt).HasColumnName("refresh_expires_at");
        builder.Property(session => session.RevokedAt).HasColumnName("revoked_at");

        // Pasifleştirme ve parola değişimi personelin TÜM oturumlarını arar.
        builder.HasIndex(session => session.StaffId).HasDatabaseName("ix_sessions_staff_id");
    }
}

internal sealed class LockoutStateConfiguration : IEntityTypeConfiguration<LockoutState>
{
    public void Configure(EntityTypeBuilder<LockoutState> builder)
    {
        builder.ToTable("login_attempts");

        // R6/AC-9 — anahtar hesap ve kaynak BİRLİKTE. Kayıtlı olmayan kimlikler için de
        // satır oluşur (R3, AC-8); bu yüzden `staff` tablosuna bağlı değildir.
        builder.HasKey(state => new { state.NormalizedLoginId, state.ClientKey });

        builder.Property(state => state.NormalizedLoginId)
            .HasColumnName("normalized_login_id")
            .HasMaxLength(LoginId.MaximumLength);

        builder.Property(state => state.ClientKey).HasColumnName("client_key").HasMaxLength(64);
        builder.Property(state => state.FailedAttempts).HasColumnName("failed_attempts");
        builder.Property(state => state.LockedUntil).HasColumnName("locked_until");
    }
}
