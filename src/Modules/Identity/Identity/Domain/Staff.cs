using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain.Events;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Galeride çalışan kişi (docs/domain.md — **Personel** / `Staff`). `Identity` bu verinin
/// TEK yazma sahibidir; başka modüller personelin kim olduğunu bilmez, yalnız `StaffId`
/// taşır (C1).
///
/// Personelin <see cref="BranchId"/>'si bir **yetki sınırı değil**, bir bağlantı
/// bilgisidir: şube bazlı yetkilendirme V1'de yoktur ve bu rol modeli bir izin motoruna
/// dönüştürülmez (C2).
/// </summary>
internal sealed class Staff : AggregateRoot<StaffId>
{
    private Staff(
        StaffId id,
        string displayName,
        LoginId loginId,
        PasswordHash passwordHash,
        StaffRole role,
        Locale preferredLocale,
        Guid? branchId,
        bool mustChangePassword,
        DateTimeOffset createdAt)
        : base(id)
    {
        DisplayName = displayName;
        LoginId = loginId;
        NormalizedLoginId = loginId.Normalized;
        PasswordHash = passwordHash;
        Role = role;
        PreferredLocale = preferredLocale;
        BranchId = branchId;
        MustChangePassword = mustChangePassword;
        CreatedAt = createdAt;
        Status = StaffStatus.Active;
        SecurityStamp = SecurityStamp.New();
    }

    /// <summary>EF Core'un okuma yolu; iş kodundan çağrılmaz.</summary>
    private Staff()
        : base(default)
    {
        DisplayName = string.Empty;
        NormalizedLoginId = string.Empty;
    }

    internal string DisplayName { get; private set; }

    internal LoginId LoginId { get; private set; }

    /// <summary>
    /// Tekilliğin ve giriş eşleştirmesinin kurulduğu kolon. <see cref="LoginId"/>'nin
    /// normalize hâlinin KOPYASIDIR ve yalnız kurucuda yazılır; giriş kimliği kayıttan
    /// sonra değişmez.
    ///
    /// Ayrı bir kolon olmasının nedeni teknik: indeks ham kolon üzerinde `lower()` ile
    /// kurulamaz — Türkçe collation altında yanlış eşleşirdi (Yasak Listesi 27) — ve
    /// karmaşık tip üyesine indeks kurulamaz. Normalize değer bu yüzden yazma anında
    /// üretilip kendi kolonunda durur (docs/conventions.md § 6'nın "yazma anında üretilen
    /// normalize arama kolonu" kuralıyla aynı desen).
    /// </summary>
    internal string NormalizedLoginId { get; private set; }

    internal PasswordHash PasswordHash { get; private set; }

    internal StaffRole Role { get; private set; }

    internal StaffStatus Status { get; private set; }

    /// <summary>
    /// Personel locale'i ZİYARETÇİ locale'inden bağımsız ayrı bir eksendir
    /// (docs/architecture.md § 3, R16). Karıştırmak, Arapça vitrinden panele geçen Türk
    /// danışmanın paneli Arapça görmesine yol açar (Tuzak 5).
    /// </summary>
    internal Locale PreferredLocale { get; private set; }

    /// <summary>Opsiyonel (R15). Modüller arası referans SOFT'tur: foreign key yoktur.</summary>
    internal Guid? BranchId { get; private set; }

    /// <summary>R10 — ilk parola sistem dışından iletilir; personel ilk girişte değiştirir.</summary>
    internal bool MustChangePassword { get; private set; }

    internal SecurityStamp SecurityStamp { get; private set; }

    internal DateTimeOffset CreatedAt { get; private set; }

    internal bool IsActive => Status is StaffStatus.Active;

    internal static Result<Staff> Add(
        string? displayName,
        LoginId loginId,
        PasswordHash passwordHash,
        StaffRole role,
        Locale preferredLocale,
        Guid? branchId,
        DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result.Failure<Staff>(new Error(IdentityErrors.EmptyDisplayName));
        }

        return Result.Success(new Staff(
            StaffId.New(),
            displayName.Trim(),
            loginId,
            passwordHash,
            role,
            preferredLocale,
            branchId,
            mustChangePassword: true,
            createdAt));
    }

    /// <summary>
    /// D3/R12/AC-43 — son aktif Yöneticinin rolü düşürülemez. Sayı dışarıdan verilir:
    /// bir personel kaç kardeşi olduğunu bilemez, ama invariant'ın agregatın dışına
    /// kaçması da gerekmez (`Branch.Close` ile aynı desen).
    ///
    /// Sayıya YALNIZCA durumu aktif olanlar girer; geçici kilitlenme aktifliği etkilemez
    /// (AC-45) — kilit ayrı bir kayıttır, bu agregatın alanı değil.
    /// </summary>
    internal Result ChangeRole(StaffRole role, int otherActiveManagerCount)
    {
        if (IsLastActiveManagerLosingTheRole(role, otherActiveManagerCount))
        {
            return Result.Failure(new Error(IdentityErrors.LastActiveManager));
        }

        Role = role;
        return Result.Success();
    }

    /// <summary>D3/R12/AC-42 — son aktif Yönetici pasifleştirilemez.</summary>
    internal Result Deactivate(int otherActiveManagerCount)
    {
        if (Status is StaffStatus.Deactivated)
        {
            return Result.Failure(new Error(IdentityErrors.AlreadyDeactivated));
        }

        if (Role is StaffRole.Manager && otherActiveManagerCount == 0)
        {
            return Result.Failure(new Error(IdentityErrors.LastActiveManager));
        }

        Status = StaffStatus.Deactivated;

        // R14 — açık oturumlar DERHAL düşer. Damganın dönmesi, o personelin bütün
        // oturumlarını bir sonraki istekte geçersiz kılar; ayrıca oturum satırları da
        // iptal edilir (Application). İki katman, çünkü pasifleştirme bir güvenlik
        // işlemidir ve tek bir yolun unutulması onu sessizce etkisiz bırakır.
        SecurityStamp = SecurityStamp.New();

        Raise(new StaffWasDeactivated(Id));

        return Result.Success();
    }

    /// <summary>
    /// R9/AC-35 — Yönetici sıfırlar; personel ilk girişinde değiştirir. Damga döner:
    /// sıfırlanan parolayla eski oturumlar yaşamaya devam etmez.
    /// </summary>
    internal void ResetPassword(PasswordHash passwordHash)
    {
        PasswordHash = passwordHash;
        MustChangePassword = true;
        SecurityStamp = SecurityStamp.New();
    }

    /// <summary>
    /// R11/AC-34 — personel MEVCUT parolasını vererek değiştirir. Damga döner ve o
    /// personelin **tüm** yenileme hakları geçersizleşir (AC-22): parolası sızmış
    /// personel parolasını değiştirdiğinde saldırganın oturumu da düşmelidir.
    /// </summary>
    internal Result ChangeOwnPassword(string currentPassword, PasswordHash newPasswordHash)
    {
        if (!PasswordHash.Matches(currentPassword))
        {
            return Result.Failure(new Error(IdentityErrors.CurrentPasswordInvalid));
        }

        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        SecurityStamp = SecurityStamp.New();

        return Result.Success();
    }

    /// <summary>
    /// D22 — ikinci adımın sıfırlanması bir **güvenlik işlemidir**: damga döner ve
    /// personelin açık oturumları bir sonraki istekte düşer. Sıfırlamanın normal sebebi
    /// kaybolan telefondur; o telefonun açık oturumunu yaşatmak `SecurityStamp`'in var
    /// olma sebebinin tam tersidir. `ResetPassword` ile aynı iki katman: satırlar da
    /// iptal edilir (Application).
    /// </summary>
    internal void RegisterTwoFactorReset() => SecurityStamp = SecurityStamp.New();

    internal void ChangePreferredLocale(Locale locale) => PreferredLocale = locale;

    internal void AssignBranch(Guid? branchId) => BranchId = branchId;

    private bool IsLastActiveManagerLosingTheRole(StaffRole role, int otherActiveManagerCount) =>
        Role is StaffRole.Manager
        && role is not StaffRole.Manager
        && IsActive
        && otherActiveManagerCount == 0;
}
