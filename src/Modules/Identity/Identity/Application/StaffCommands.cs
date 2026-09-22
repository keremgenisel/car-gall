using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarGallery.Modules.Identity.Application;

internal sealed record AddStaffCommand(
    string? DisplayName,
    string? LoginId,
    string? InitialPassword,
    string? Role,
    string? PreferredLocale,
    Guid? BranchId);

internal sealed record StaffListItem(
    Guid StaffId,
    string DisplayName,
    string LoginId,
    string Role,
    string Status,
    string PreferredLocale,
    Guid? BranchId);

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
    /// AC-43 — son aktif Yöneticinin rolü düşürülemez. Okuma, karar ve yazma TEK
    /// transaction içinde ve kilit altında yapılır; gerekçesi `StaffQueries`'te.
    /// </summary>
    internal async Task<Result> ChangeRoleAsync(Guid staffId, string? role, CancellationToken cancellationToken)
    {
        if (!StaffRoleCode.TryParse(role, out StaffRole parsed))
        {
            return Result.Failure(Error.With(IdentityErrors.StaffNotFound, "field", "role"));
        }

        await using IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        Staff? staff = await FindAsync(staffId, cancellationToken);

        if (staff is null)
        {
            return Result.Failure(new Error(IdentityErrors.StaffNotFound));
        }

        int otherActiveManagers =
            await StaffQueries.LockAndCountOtherActiveManagersAsync(context, staff.Id, cancellationToken);

        Result result = staff.ChangeRole(parsed, otherActiveManagers);

        if (result.IsFailure)
        {
            return result;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// AC-42 — son aktif Yönetici pasifleştirilemez. Okuma, karar ve yazma TEK
    /// transaction içinde ve kilit altında yapılır; gerekçesi `StaffQueries`'te.
    /// </summary>
    internal async Task<Result> DeactivateAsync(Guid staffId, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        Staff? staff = await FindAsync(staffId, cancellationToken);

        if (staff is null)
        {
            return Result.Failure(new Error(IdentityErrors.StaffNotFound));
        }

        int otherActiveManagers =
            await StaffQueries.LockAndCountOtherActiveManagersAsync(context, staff.Id, cancellationToken);

        Result result = staff.Deactivate(otherActiveManagers);

        if (result.IsFailure)
        {
            return result;
        }

        // R14 — açık oturumlar DERHAL geçersizleşir. Damga zaten döndü; satırları da
        // iptal ediyoruz. İki katman, çünkü pasifleştirme bir güvenlik işlemidir ve tek
        // bir yolun unutulması onu sessizce etkisiz bırakır.
        await RevokeSessionsAsync(staff.Id, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Result.Success();
    }

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
                row.BranchId))
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
