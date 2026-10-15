using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// Kurulum başlatıldığında personele dönen çift. Sır **metin** olarak döner ve kare kodu
/// UÇ çizer (D17/C15): sunucuda üretilen bir görsel sırrı önbellek ve log yüzeylerine
/// taşırdı (§ Bilinen tuzaklar 2).
/// </summary>
internal sealed record TwoFactorEnrollmentStarted(string SecretUri, string Secret);

/// <summary>
/// Kurtarma kodu kümesi. **Bir kez** döner (D31/AC-7): kümeyi yeniden okutan bir yol
/// yoktur, çünkü saklanan şey özettir ve kodun kendisi hiçbir yerde durmaz (C2b).
/// </summary>
internal sealed record RecoveryCodeSet(IReadOnlyList<string> Codes);

/// <summary>
/// Personelin kendi ekranının beslendiği durum (R17/D26). `Required` ROLDEN türetilir
/// (D25): sözleşmeye ikinci bir alan eklemek, zorunluluk politikasını API'ye dondururdu.
/// </summary>
internal sealed record TwoFactorStatus(bool Enrolled, bool Required, int RemainingRecoveryCodes);

/// <summary>
/// İkinci adımın kurulumu, kapatılması, yenilenmesi ve sıfırlanması (R3–R6, R15, R16).
///
/// Yetki kontrolü UÇTA politika ile yapılır (`0006` C18 deseni); burada iş kuralı yaşar.
/// Her işlem **kaydedilir** (R12/D12) ve kayıt iş verisiyle aynı `SaveChanges` içinde
/// yazılır: işlem geri alındıysa kaydı da alınmıştır.
/// </summary>
internal sealed class TwoFactorCommands(
    IdentityDbContext context,
    IdentityUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ISecretProtector protector,
    IdentityOptions options,
    IClock clock)
{
    /// <summary>
    /// R3/AC-1 — sır üretilir, şifrelenerek saklanır ve personele **bir kez** metin
    /// olarak döner. Kurulum yeniden başlatıldığında YENİ sır üretilir ve eskisi düşer
    /// (D18/AC-6): aynı sır ikinci kez gösterilebildiği anda *"bir kez gösterilir"*
    /// kuralı yalandır.
    /// </summary>
    internal async Task<Result<TwoFactorEnrollmentStarted>> StartEnrollmentAsync(
        Guid staffId,
        CancellationToken cancellationToken)
    {
        Staff? staff = await FindStaffAsync(staffId, cancellationToken);

        if (staff is null)
        {
            return Result.Failure<TwoFactorEnrollmentStarted>(new Error(IdentityErrors.StaffNotFound));
        }

        TwoFactorEnrollment? enrollment = await FindEnrollmentAsync(staff.Id, cancellationToken);

        if (enrollment is { IsConfirmed: true })
        {
            return Result.Failure<TwoFactorEnrollmentStarted>(
                new Error(IdentityErrors.TwoFactorAlreadyEnrolled));
        }

        TwoFactorSecret secret = TwoFactorSecret.New();
        string cipher = protector.Protect(secret.Base32);

        if (enrollment is null)
        {
            context.TwoFactorEnrollments.Add(TwoFactorEnrollment.Start(
                staff.Id, cipher, clock.UtcNow, options.PendingEnrollmentLifetime));
        }
        else
        {
            enrollment.Restart(cipher, clock.UtcNow, options.PendingEnrollmentLifetime);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new TwoFactorEnrollmentStarted(
            secret.ToUri(options.TwoFactorIssuer, staff.LoginId.Value),
            secret.Base32));
    }

    /// <summary>
    /// R3/AC-4 — doğrulanmamış kurulum ancak bir kodla etkinleşir. Süresi dolmuş bekleyen
    /// kurulum reddedilir (D18/AC-5) ve personel kurulumu baştan başlatır.
    ///
    /// Yanlış koda **ayrı sayaç gerekmez** (D18): kendi bekleyen sırrını tahmin etmek
    /// saldırgana bir şey kazandırmaz — kontrol, süre ve değiştirmedir.
    /// </summary>
    internal async Task<Result<RecoveryCodeSet>> ConfirmEnrollmentAsync(
        Guid staffId,
        string? code,
        CancellationToken cancellationToken)
    {
        Staff? staff = await FindStaffAsync(staffId, cancellationToken);
        TwoFactorEnrollment? enrollment = staff is null
            ? null
            : await FindEnrollmentAsync(staff.Id, cancellationToken);

        if (staff is null || enrollment is null)
        {
            return Result.Failure<RecoveryCodeSet>(new Error(IdentityErrors.TwoFactorNotEnrolled));
        }

        if (enrollment.IsConfirmed)
        {
            return Result.Failure<RecoveryCodeSet>(new Error(IdentityErrors.TwoFactorAlreadyEnrolled));
        }

        if (enrollment.IsPendingExpiredAt(clock.UtcNow))
        {
            return Result.Failure<RecoveryCodeSet>(new Error(IdentityErrors.TwoFactorInvalid));
        }

        Result<long> matched = Match(enrollment, code);

        if (matched.IsFailure)
        {
            return Result.Failure<RecoveryCodeSet>(matched.Error);
        }

        Result confirmed = enrollment.Confirm(matched.Value, clock.UtcNow);

        if (confirmed.IsFailure)
        {
            return Result.Failure<RecoveryCodeSet>(confirmed.Error);
        }

        RecoveryCodeSet codes = IssueRecoveryCodes(staff.Id);

        Record(staff.Id, staff.Id, TwoFactorAuditEvent.Enrolled);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(codes);
    }

    /// <summary>
    /// R15/D23 — kapatma **mevcut parola ve geçerli bir kod** ister; zorunlu rolde
    /// kapatma yoktur ve tek yol R6'nın sıfırlamasıdır.
    ///
    /// *Gerekçe:* yalnız açık oturumla kapatılabilen bir anahtar, çalınmış bir oturumu
    /// faktörün KALICI olarak kaldırılmasına çevirir. `0006` R11'in *"mevcut parolasını
    /// vererek"* deseni zaten bu sınıf işlemler için kurulmuştu.
    /// </summary>
    internal async Task<Result> DisableAsync(
        Guid staffId,
        string? currentPassword,
        string? code,
        CancellationToken cancellationToken)
    {
        Staff? staff = await FindStaffAsync(staffId, cancellationToken);

        if (staff is null)
        {
            return Result.Failure(new Error(IdentityErrors.StaffNotFound));
        }

        if (TwoFactorPolicy.IsRequiredFor(staff.Role))
        {
            return Result.Failure(new Error(IdentityErrors.TwoFactorDisableNotAllowed));
        }

        TwoFactorEnrollment? enrollment = await FindEnrollmentAsync(staff.Id, cancellationToken);

        if (enrollment is null || !enrollment.IsConfirmed)
        {
            return Result.Failure(new Error(IdentityErrors.TwoFactorNotEnrolled));
        }

        if (!staff.PasswordHash.Matches(currentPassword ?? string.Empty))
        {
            return Result.Failure(new Error(IdentityErrors.CurrentPasswordInvalid));
        }

        Result<long> matched = Match(enrollment, code);

        if (matched.IsFailure)
        {
            return matched;
        }

        await ClearEnrollmentAsync(staff.Id, enrollment, cancellationToken);

        Record(staff.Id, staff.Id, TwoFactorAuditEvent.Disabled);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// R16/D24 — yenileme oturum **ve** taze bir kod ister, kalanların **hepsini**
    /// düşürür ve yeni kümeyi bir kez gösterir.
    ///
    /// *Gerekçe:* yenileme olmadan ilk kullanım tek yönlü bir geri sayım başlatır ve tek
    /// çıkışı Yöneticinin sıfırlamasıdır; D9'un varsayılan onu bunu yalnız geciktirir.
    /// </summary>
    internal async Task<Result<RecoveryCodeSet>> RegenerateRecoveryCodesAsync(
        Guid staffId,
        string? code,
        CancellationToken cancellationToken)
    {
        Staff? staff = await FindStaffAsync(staffId, cancellationToken);
        TwoFactorEnrollment? enrollment = staff is null
            ? null
            : await FindEnrollmentAsync(staff.Id, cancellationToken);

        if (staff is null || enrollment is null || !enrollment.IsConfirmed)
        {
            return Result.Failure<RecoveryCodeSet>(new Error(IdentityErrors.TwoFactorNotEnrolled));
        }

        Result<long> matched = Match(enrollment, code);

        if (matched.IsFailure)
        {
            return Result.Failure<RecoveryCodeSet>(matched.Error);
        }

        enrollment.RegisterAcceptedStep(matched.Value);

        await DropRecoveryCodesAsync(staff.Id, cancellationToken);
        RecoveryCodeSet codes = IssueRecoveryCodes(staff.Id);

        Record(staff.Id, staff.Id, TwoFactorAuditEvent.RecoveryCodesRegenerated);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(codes);
    }

    /// <summary>
    /// R6/D22 — sıfırlama bir **güvenlik işlemidir**: personelin açık oturumları düşer ve
    /// kalan kurtarma kodları geçersizleşir. `ResetPassword` ve `Deactivate` ile aynı iki
    /// katman (`StaffCommands`): damga döner **ve** oturum satırları iptal edilir, çünkü
    /// tek bir yolun unutulması işlemi sessizce etkisiz bırakır.
    ///
    /// *Gerekçe:* sıfırlamanın normal sebebi **kaybolan telefondur**; o telefonun açık
    /// oturumunu yaşatmak `SecurityStamp`'in var olma sebebinin tam tersidir.
    ///
    /// **Son aktif Yönetici koruması burada çalışmaz ve bu bilinçlidir** (C10/D32):
    /// sıfırlama faktörü KALDIRIR, eklemez — hiçbir hesabı kilitleyemez. D37 bunun
    /// bedelini yazıyor: telefonunu kaybeden TEK Yönetici kendini sıfırlayamaz.
    /// </summary>
    internal async Task<Result> ResetAsync(
        Guid actorStaffId,
        Guid staffId,
        CancellationToken cancellationToken)
    {
        Staff? staff = await FindStaffAsync(staffId, cancellationToken);

        if (staff is null)
        {
            return Result.Failure(new Error(IdentityErrors.StaffNotFound));
        }

        TwoFactorEnrollment? enrollment = await FindEnrollmentAsync(staff.Id, cancellationToken);

        if (enrollment is null)
        {
            return Result.Failure(new Error(IdentityErrors.TwoFactorNotEnrolled));
        }

        await ClearEnrollmentAsync(staff.Id, enrollment, cancellationToken);

        staff.RegisterTwoFactorReset();
        await RevokeSessionsAsync(staff.Id, cancellationToken);

        Record(staff.Id, new StaffId(actorStaffId), TwoFactorAuditEvent.Reset);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    internal async Task<Result<TwoFactorStatus>> GetStatusAsync(
        Guid staffId,
        CancellationToken cancellationToken)
    {
        Staff? staff = await FindStaffAsync(staffId, cancellationToken);

        if (staff is null)
        {
            return Result.Failure<TwoFactorStatus>(new Error(IdentityErrors.StaffNotFound));
        }

        TwoFactorEnrollment? enrollment = await FindEnrollmentAsync(staff.Id, cancellationToken);
        bool enrolled = enrollment is { IsConfirmed: true };

        int remaining = enrolled
            ? await context.RecoveryCodes
                .CountAsync(code => code.StaffId == staff.Id && code.UsedAt == null, cancellationToken)
            : 0;

        return Result.Success(new TwoFactorStatus(
            enrolled,
            TwoFactorPolicy.IsRequiredFor(staff.Role),
            remaining));
    }

    private Result<long> Match(TwoFactorEnrollment enrollment, string? code)
    {
        Result<string> secret = protector.Unprotect(enrollment.SecretCipher);

        if (secret.IsFailure)
        {
            return Result.Failure<long>(secret.Error);
        }

        Result<TwoFactorSecret> parsed = TwoFactorSecret.FromBase32(secret.Value);

        if (parsed.IsFailure)
        {
            return Result.Failure<long>(parsed.Error);
        }

        return TimeBasedCode.TryMatch(
            parsed.Value.ToBytes(),
            code,
            TimeBasedCode.StepAt(clock.UtcNow),
            enrollment.LastAcceptedStep,
            out long matchedStep)
            ? Result.Success(matchedStep)
            : Result.Failure<long>(new Error(IdentityErrors.TwoFactorInvalid));
    }

    private RecoveryCodeSet IssueRecoveryCodes(StaffId staffId)
    {
        List<string> codes = new(options.RecoveryCodeCount);

        for (int index = 0; index < options.RecoveryCodeCount; index++)
        {
            string code = RecoveryCodeText.Generate();
            codes.Add(code);

            context.RecoveryCodes.Add(RecoveryCode.Issue(
                staffId,
                passwordHasher.HashRecoveryCode(RecoveryCodeText.Normalize(code)),
                clock.UtcNow));
        }

        return new RecoveryCodeSet(codes);
    }

    private async Task ClearEnrollmentAsync(
        StaffId staffId,
        TwoFactorEnrollment enrollment,
        CancellationToken cancellationToken)
    {
        context.TwoFactorEnrollments.Remove(enrollment);
        await DropRecoveryCodesAsync(staffId, cancellationToken);
        await DropPendingLoginsAsync(staffId, cancellationToken);
    }

    private async Task DropRecoveryCodesAsync(StaffId staffId, CancellationToken cancellationToken)
    {
        List<RecoveryCode> codes = await context.RecoveryCodes
            .Where(code => code.StaffId == staffId)
            .ToListAsync(cancellationToken);

        context.RecoveryCodes.RemoveRange(codes);
    }

    private async Task DropPendingLoginsAsync(StaffId staffId, CancellationToken cancellationToken)
    {
        List<PendingLogin> pending = await context.PendingLogins
            .Where(login => login.StaffId == staffId)
            .ToListAsync(cancellationToken);

        context.PendingLogins.RemoveRange(pending);
    }

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

    private void Record(StaffId staffId, StaffId actorStaffId, TwoFactorAuditEvent auditEvent) =>
        context.TwoFactorAudit.Add(TwoFactorAuditEntry.Record(
            staffId, actorStaffId, auditEvent, clock.UtcNow));

    private Task<Staff?> FindStaffAsync(Guid staffId, CancellationToken cancellationToken) =>
        context.Staff.SingleOrDefaultAsync(staff => staff.Id == new StaffId(staffId), cancellationToken);

    private Task<TwoFactorEnrollment?> FindEnrollmentAsync(
        StaffId staffId,
        CancellationToken cancellationToken) =>
        context.TwoFactorEnrollments.SingleOrDefaultAsync(
            enrollment => enrollment.Id == staffId, cancellationToken);
}
