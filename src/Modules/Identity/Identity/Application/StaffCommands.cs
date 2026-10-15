using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

internal sealed record AddStaffCommand(
    string? DisplayName,
    string? LoginId,
    string? InitialPassword,
    string? Role,
    string? PreferredLocale,
    Guid? BranchId);

/// <param name="TwoFactorEnrolled">
/// D25 — kapanmış sözleşmeye **eklemeli** alan (`0012c` D50 emsali). Üç durumlu değil
/// **tek boolean**: zorunluluğu ROL söylüyor ve satır rolü zaten taşıyor; ikinci bir alan
/// S1'in cevabını API sözleşmesine dondurur ve politika değiştiğinde sözleşme de değişmek
/// zorunda kalırdı. *"Zorunlu ama kurmamış"* rozetini ekran elindeki iki alandan kurar.
/// </param>
internal sealed record StaffListItem(
    Guid StaffId,
    string DisplayName,
    string LoginId,
    string Role,
    string Status,
    string PreferredLocale,
    Guid? BranchId,
    bool TwoFactorEnrolled);

/// <summary>
/// R9 — personel ekleme, rol değiştirme, pasifleştirme ve parola sıfırlama. Hepsi
/// Yönetici işlemidir; yetki kontrolü uçta politika ile yapılır (AC-25, AC-26, AC-28),
/// burada iş kuralı yaşar.
/// </summary>
internal sealed class StaffCommands(
    IdentityDbContext context,
    IdentityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IBranchValidator branches,
    IdentityLocaleOptions locales,
    IdentityOptions options,
    InvariantGuardOptions<IdentityDbContext> guardOptions,
    IClock clock)
{
    internal async Task<Result<Guid>> AddAsync(AddStaffCommand command, CancellationToken cancellationToken)
    {
        Result<LoginId> loginId = LoginId.Create(command.LoginId);

        if (loginId.IsFailure)
        {
            return Result.Failure<Guid>(Error.With(IdentityErrors.InvalidLoginId, "field", "loginId"));
        }

        if (!StaffRoleCode.TryParse(command.Role, out StaffRole role))
        {
            return Result.Failure<Guid>(Error.With(IdentityErrors.StaffNotFound, "field", "role"));
        }

        Result passwordCheck = EnsurePasswordLength(command.InitialPassword);

        if (passwordCheck.IsFailure)
        {
            return Result.Failure<Guid>(passwordCheck.Error);
        }

        if (!locales.TryResolve(command.PreferredLocale, out Locale preferredLocale))
        {
            return Result.Failure<Guid>(Error.With(IdentityErrors.LocaleNotEnabled, "field", "preferredLocale"));
        }

        // C4 — şube kimliği geçerli mi; tek atlama, salt okuma, aynı istek içinde.
        if (command.BranchId is not null
            && !await branches.ExistsAsync(command.BranchId.Value, cancellationToken))
        {
            return Result.Failure<Guid>(Error.With(IdentityErrors.BranchNotFound, "field", "branchId"));
        }

        bool taken = await context.Staff
            .AsNoTracking()
            .AnyAsync(staff => staff.NormalizedLoginId == loginId.Value.Normalized, cancellationToken);

        if (taken)
        {
            return Result.Failure<Guid>(Error.With(IdentityErrors.LoginIdTaken, "field", "loginId"));
        }

        Result<Staff> staff = Staff.Add(
            command.DisplayName,
            loginId.Value,
            passwordHasher.Hash(command.InitialPassword!),
            role,
            preferredLocale,
            command.BranchId,
            clock.UtcNow);

        if (staff.IsFailure)
        {
            return Result.Failure<Guid>(staff.Error);
        }

        context.Staff.Add(staff.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(staff.Value.Id.Value);
    }

    /// <summary>
    /// AC-43 — son aktif Yöneticinin rolü düşürülemez. Koruma ortak desenden gelir
    /// (0007 R3); gerekçeleri `InvariantGuard`'ta.
    /// </summary>
    internal Task<Result> ChangeRoleAsync(Guid staffId, string? role, CancellationToken cancellationToken)
    {
        if (!StaffRoleCode.TryParse(role, out StaffRole parsed))
        {
            return Task.FromResult(Result.Failure(Error.With(IdentityErrors.StaffNotFound, "field", "role")));
        }

        return InvariantGuard.ExecuteAsync(
            context,
            StaffQueries.ActiveManagers,
            staffId,
            async (otherActiveManagers, ct) =>
            {
                Staff? staff = await FindAsync(staffId, ct);

                if (staff is null)
                {
                    return Result.Failure(new Error(IdentityErrors.StaffNotFound));
                }

                Result result = staff.ChangeRole(parsed, otherActiveManagers);

                if (result.IsFailure)
                {
                    return result;
                }

                await unitOfWork.SaveChangesAsync(ct);

                return Result.Success();
            },
            guardOptions,
            cancellationToken);
    }

    /// <summary>
    /// AC-42 — son aktif Yönetici pasifleştirilemez. Koruma ortak desenden gelir
    /// (0007 R3); gerekçeleri `InvariantGuard`'ta.
    /// </summary>
    internal Task<Result> DeactivateAsync(Guid staffId, CancellationToken cancellationToken) =>
        InvariantGuard.ExecuteAsync(
            context,
            StaffQueries.ActiveManagers,
            staffId,
            async (otherActiveManagers, ct) =>
            {
                // Agregat KİLİTTEN SONRA okunuyor. Önce okunsaydı bekleyen isteğin
                // elindeki kopya bayat kalır, `AlreadyDeactivated` koruması hiç
                // tetiklenmez ve kayıt ikinci kez yazılırdı (0007/AC-3).
                Staff? staff = await FindAsync(staffId, ct);

                if (staff is null)
                {
                    return Result.Failure(new Error(IdentityErrors.StaffNotFound));
                }

                Result result = staff.Deactivate(otherActiveManagers);

                if (result.IsFailure)
                {
                    return result;
                }

                // R14 — açık oturumlar DERHAL geçersizleşir. Damga zaten döndü; satırları
                // da iptal ediyoruz. İki katman, çünkü pasifleştirme bir güvenlik
                // işlemidir ve tek bir yolun unutulması onu sessizce etkisiz bırakır.
                await RevokeSessionsAsync(staff.Id, ct);

                await unitOfWork.SaveChangesAsync(ct);

                return Result.Success();
            },
            guardOptions,
            cancellationToken);

    internal async Task<Result> ResetPasswordAsync(
        Guid staffId,
        string? newPassword,
        CancellationToken cancellationToken)
    {
        Result passwordCheck = EnsurePasswordLength(newPassword);

        if (passwordCheck.IsFailure)
        {
            return passwordCheck;
        }

        Staff? staff = await FindAsync(staffId, cancellationToken);

        if (staff is null)
        {
            return Result.Failure(new Error(IdentityErrors.StaffNotFound));
        }

        staff.ResetPassword(passwordHasher.Hash(newPassword!));
        await RevokeSessionsAsync(staff.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// R11/AC-22 — parola değiştiğinde o personelin TÜM yenileme hakları geçersizleşir.
    /// Damganın dönmesi bunu sağlar; oturum satırları da iptal edilir.
    /// </summary>
    internal async Task<Result> ChangeOwnPasswordAsync(
        Guid staffId,
        string? currentPassword,
        string? newPassword,
        CancellationToken cancellationToken)
    {
        Result passwordCheck = EnsurePasswordLength(newPassword);

        if (passwordCheck.IsFailure)
        {
            return passwordCheck;
        }

        Staff? staff = await FindAsync(staffId, cancellationToken);

        if (staff is null)
        {
            return Result.Failure(new Error(IdentityErrors.StaffNotFound));
        }

        Result result = staff.ChangeOwnPassword(currentPassword ?? string.Empty, passwordHasher.Hash(newPassword!));

        if (result.IsFailure)
        {
            return result;
        }

        await RevokeSessionsAsync(staff.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// AC-50 — parola ve parola özeti bu listede BULUNMAZ; alan olarak da yoktur.
    ///
    /// Projeksiyon SORGUYA yazılır, belleğe değil: bütün `Staff` entity'si çekilseydi
    /// `password_hash` hiç gerekmediği hâlde her liste isteğinde veritabanından süreç
    /// belleğine inerdi. Yanıt yine doğru olurdu ama parola malzemesi mümkün olandan
    /// geniş bir alanda dolaşırdı — C6 tam olarak bunu daraltmayı istiyor ve burada
    /// daraltmanın bedeli sıfır.
    ///
    /// Rol ve durum KODU bellekte üretiliyor: sözlük araması SQL'e çevrilemez. Çevrilen
    /// satırlar yalnızca yukarıda sayılan kolonlardır.
    /// </summary>
    internal async Task<IReadOnlyList<StaffListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Staff
            .AsNoTracking()
            .OrderBy(item => item.DisplayName)
            .Select(item => new
            {
                item.Id,
                item.DisplayName,
                item.LoginId,
                item.Role,
                item.Status,
                item.PreferredLocale,
                item.BranchId,

                // D25 — kurulum durumu da PROJEKSİYONDA: satır başına ikinci bir sorgu
                // açmak, listeyi personel sayısı kadar isteğe bölerdi.
                TwoFactorEnrolled = context.TwoFactorEnrollments
                    .Any(enrollment => enrollment.Id == item.Id && enrollment.ConfirmedAt != null),
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(row => new StaffListItem(
                row.Id.Value,
                row.DisplayName,
                row.LoginId.Value,
                StaffRoleCode.ToCode(row.Role),
                StaffStatusCode.ToCode(row.Status),
                row.PreferredLocale.Code,
                row.BranchId,
                row.TwoFactorEnrolled))
        ];
    }

    private Result EnsurePasswordLength(string? password) =>
        (password ?? string.Empty).Length < options.MinimumPasswordLength
            ? Result.Failure(Error.With(IdentityErrors.PasswordTooShort, "field", "password"))
            : Result.Success();

    private Task<Staff?> FindAsync(Guid staffId, CancellationToken cancellationToken) =>
        context.Staff.SingleOrDefaultAsync(staff => staff.Id == new StaffId(staffId), cancellationToken);

    private async Task RevokeSessionsAsync(StaffId staffId, CancellationToken cancellationToken)
    {
        List<Session> sessions = await context.Sessions
            .Where(session => session.StaffId == staffId && session.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (Session session in sessions)
        {
            session.Revoke(clock.UtcNow);
        }
    }
}
