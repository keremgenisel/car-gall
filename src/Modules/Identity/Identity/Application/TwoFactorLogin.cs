using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

/// <param name="ClientKey">
/// Kilitlenme sayacını bölen OPAK anahtar (bugün kaynak IP) — `LoginCommand` ile
/// **aynı** anahtar. R8'in "tek sayaç" kuralı ancak iki fazın aynı anahtarı kullanmasıyla
/// işler.
/// </param>
internal sealed record TwoFactorVerifyCommand(string? PendingToken, string? Code, string ClientKey);

/// <summary>
/// Girişin ikinci adımı: zamana bağlı kod ya da kurtarma kodu (R1, R5).
///
/// **Reddin iki kodu var** (D20): `two_factor_invalid` yanlış, penceresi dolmuş ve
/// yeniden oynatılmış kodun ortak karşılığıdır; `two_factor_pending_expired` ara durumun
/// düştüğünü söyler ve ekranı parola fazına döndürür. Ayrım personelin **yapacağı şeye**
/// göredir: biri *"taze kod yaz"*, öteki *"parolanı yeniden gir"*.
///
/// Kilitlenme sayacı parola fazıyla **paylaşılır** (R8): ayrı bir sayaç, parolayı ele
/// geçirmiş saldırgana kodu sınırsız denemek için ikinci bir bütçe verirdi
/// (§ Bilinen tuzaklar 5). Kilitlenmiş hesabın reddi de ayırt edilemez kalır
/// (`0006` R3/AC-5'in suskunluğu, AC-27).
/// </summary>
internal sealed class TwoFactorLogin(
    IdentityDbContext context,
    ISecretProtector protector,
    IdentityOptions options,
    IClock clock)
{
    internal async Task<Result<LoginResult>> HandleAsync(
        TwoFactorVerifyCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.PendingToken))
        {
            return PendingExpired();
        }

        string tokenHash = PendingLogin.HashToken(command.PendingToken);

        PendingLogin? pending = await context.PendingLogins
            .SingleOrDefaultAsync(login => login.TokenHash == tokenHash, cancellationToken);

        // Bilinmeyen, tükenmiş ve süresi dolmuş ara durum AYNI sonucu verir: hiçbirinde
        // kullanılabilir bir ara durum yok ve ekranın yapacağı şey aynı — parola fazı.
        if (pending is null || !pending.IsUsableAt(clock.UtcNow))
        {
            return PendingExpired();
        }

        Staff? staff = await context.Staff
            .SingleOrDefaultAsync(item => item.Id == pending.StaffId, cancellationToken);

        if (staff is null || !staff.IsActive)
        {
            return PendingExpired();
        }

        LockoutState lockout = await LoadLockoutAsync(staff, command.ClientKey, cancellationToken);

        if (lockout.IsLockedAt(clock.UtcNow))
        {
            // AC-27 — kilit kendi kodunu taşımaz; kilitliyken doğru kod da reddedilir.
            return Invalid();
        }

        TwoFactorEnrollment? enrollment = await context.TwoFactorEnrollments
            .SingleOrDefaultAsync(item => item.Id == staff.Id, cancellationToken);

        if (enrollment is null || !enrollment.IsConfirmed)
        {
            return PendingExpired();
        }

        if (TryMatchCode(enrollment, command.Code, out long matchedStep))
        {
            enrollment.RegisterAcceptedStep(matchedStep);

            return await CompleteAsync(staff, pending, lockout, recoveryCodeUsed: false, cancellationToken);
        }

        RecoveryCode? recoveryCode = await MatchRecoveryCodeAsync(staff.Id, command.Code, cancellationToken);

        if (recoveryCode is not null)
        {
            // R5/AC-36 — kullanılan kod bir daha kabul edilmez.
            recoveryCode.MarkUsed(clock.UtcNow);

            return await CompleteAsync(staff, pending, lockout, recoveryCodeUsed: true, cancellationToken);
        }

        lockout.RegisterFailure(clock.UtcNow, options.FailedAttemptThreshold, options.LockoutDuration);
        pending.RegisterFailure();

        // D47/AC-35 — ara durumdaki sayı ikinci bir EŞİK değil; eşik tek ve paylaşılan
        // sayaçtır. Kilit düştüğü anda ara durum silinir ve personel parola fazına
        // döner: kilitli hesapta kod denemeye devam etmesinin hiçbir anlamı yok.
        if (lockout.IsLockedAt(clock.UtcNow))
        {
            context.PendingLogins.Remove(pending);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Invalid();
    }

    /// <summary>
    /// D14/AC-17 — *"Kullanıcı değiştir"* ara durumu siler. Çerezi yalnız silmek yetmez:
    /// satır yaşadığı sürece elinde kopya kalan biri doğrulamayı sürdürebilirdi.
    /// </summary>
    internal async Task<Result> DiscardAsync(string? pendingToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pendingToken))
        {
            return Result.Success();
        }

        string tokenHash = PendingLogin.HashToken(pendingToken);

        PendingLogin? pending = await context.PendingLogins
            .SingleOrDefaultAsync(login => login.TokenHash == tokenHash, cancellationToken);

        if (pending is null)
        {
            return Result.Success();
        }

        context.PendingLogins.Remove(pending);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result<LoginResult>> CompleteAsync(
        Staff staff,
        PendingLogin pending,
        LockoutState lockout,
        bool recoveryCodeUsed,
        CancellationToken cancellationToken)
    {
        lockout.Clear();

        // AC-15 — ara durum tek kullanımlıktır: başarıda tükenir.
        pending.Consume(clock.UtcNow);

        string token = Session.GenerateToken();
        Session session = Session.Issue(
            staff.Id,
            staff.SecurityStamp,
            token,
            clock.UtcNow,
            options.AccessLifetime,
            options.RefreshLifetime);

        context.Sessions.Add(session);

        if (recoveryCodeUsed)
        {
            // R12/D12 — kurtarma kodunun kullanılması kaydedilir; başarısız denemeler
            // kayda GİRMEZ (tabloyu kaba kuvvetin büyüteci yapardı).
            context.TwoFactorAudit.Add(TwoFactorAuditEntry.Record(
                staff.Id, staff.Id, TwoFactorAuditEvent.RecoveryCodeUsed, clock.UtcNow));
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResult(
            LoginStep.Completed,
            staff,
            token,
            session,
            PendingToken: null,
            PendingExpiresAt: null,
            TwoFactorEnrolled: true));
    }

    private bool TryMatchCode(TwoFactorEnrollment enrollment, string? code, out long matchedStep)
    {
        matchedStep = 0;

        Result<string> secret = protector.Unprotect(enrollment.SecretCipher);

        if (secret.IsFailure)
        {
            return false;
        }

        Result<TwoFactorSecret> parsed = TwoFactorSecret.FromBase32(secret.Value);

        return parsed.IsSuccess
            && TimeBasedCode.TryMatch(
                parsed.Value.ToBytes(),
                code,
                TimeBasedCode.StepAt(clock.UtcNow),
                enrollment.LastAcceptedStep,
                out matchedStep);
    }

    /// <summary>
    /// Girilen kod saklanan özetlerin HEPSİYLE karşılaştırılır — tuz hesaba özel olduğu
    /// için başka yolu yok. Maliyetin neden parola iterasyonundan ayrı olduğu
    /// `IPasswordHasher.HashRecoveryCode`'da yazılı (D2/D38).
    /// </summary>
    private async Task<RecoveryCode?> MatchRecoveryCodeAsync(
        StaffId staffId,
        string? code,
        CancellationToken cancellationToken)
    {
        string normalized = RecoveryCodeText.Normalize(code);

        if (normalized.Length == 0)
        {
            return null;
        }

        List<RecoveryCode> codes = await context.RecoveryCodes
            .Where(item => item.StaffId == staffId && item.UsedAt == null)
            .ToListAsync(cancellationToken);

        return codes.FirstOrDefault(item => item.CodeHash.Matches(normalized));
    }

    private async Task<LockoutState> LoadLockoutAsync(
        Staff staff,
        string clientKey,
        CancellationToken cancellationToken)
    {
        LockoutState? existing = await context.LoginAttempts.SingleOrDefaultAsync(
            state => state.NormalizedLoginId == staff.NormalizedLoginId && state.ClientKey == clientKey,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        LockoutState created = LockoutState.ForKey(staff.NormalizedLoginId, clientKey);
        context.LoginAttempts.Add(created);

        return created;
    }

    private static Result<LoginResult> Invalid() =>
        Result.Failure<LoginResult>(new Error(IdentityErrors.TwoFactorInvalid));

    private static Result<LoginResult> PendingExpired() =>
        Result.Failure<LoginResult>(new Error(IdentityErrors.TwoFactorPendingExpired));
}
