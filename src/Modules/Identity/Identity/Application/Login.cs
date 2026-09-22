using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

/// <param name="ClientKey">
/// Kilitlenme sayacını bölen OPAK anahtar (bugün kaynak IP). Application onu yalnız bir
/// dize olarak görür; nereden geldiği uç katmanın bilgisidir.
/// </param>
internal sealed record LoginCommand(string? LoginId, string? Password, string ClientKey);

internal sealed record LoginResult(string Token, Staff Staff, Session Session);

/// <summary>
/// R3 — **başarısız her giriş denemesi ayırt edilemez bir karşılık üretir.** Bu sınıfta
/// reddin nedenine göre dallanan tek bir dönüş yoktur: kimliğin kayıtlı olmadığı,
/// parolanın yanlış olduğu, hesabın pasif olduğu ve hesabın KİLİTLİ olduğu ayrımı
/// çağırana hiç ulaşmaz (AC-5, AC-8).
///
/// Ayırt edilemezlik yalnız gövdede değil, HARCANAN SÜREDE de gözetilir: hesap yoksa
/// parola doğrulaması atlanmaz, eşdeğer iş boşa çalıştırılır. Aksi hâlde yanıtlar aynı
/// olsa bile hangi e-postaların kayıtlı olduğu zamanlamadan okunabilirdi (Tuzak 3).
/// </summary>
internal sealed class Login(
    IdentityDbContext context,
    IPasswordHasher passwordHasher,
    IdentityOptions options,
    IClock clock)
{
    internal async Task<Result<LoginResult>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        string password = command.Password ?? string.Empty;
        string counterKey = CounterKeyFor(command.LoginId);

        LockoutState lockout = await LoadLockoutAsync(counterKey, command.ClientKey, cancellationToken);

        // R6/AC-6 — kilitliyken doğru parola da reddedilir. Kayıtlı olmayan kimlik için
        // de aynı yoldan geçilir (R3, AC-8).
        if (lockout.IsLockedAt(clock.UtcNow))
        {
            passwordHasher.BurnEquivalentWork(password);
            return Failure();
        }

        Result<LoginId> parsed = LoginId.Create(command.LoginId);

        Staff? staff = parsed.IsSuccess
            ? await context.Staff.SingleOrDefaultAsync(
                item => item.NormalizedLoginId == parsed.Value.Normalized,
                cancellationToken)
            : null;

        bool authenticated = staff is not null
            && staff.IsActive
            && staff.PasswordHash.Matches(password);

        if (staff is null)
        {
            passwordHasher.BurnEquivalentWork(password);
        }
        else if (!staff.IsActive)
        {
            // AC-4 — pasif personelin parolası doğrulanmaz ama süre yine harcanır.
            passwordHasher.BurnEquivalentWork(password);
        }

        if (!authenticated)
        {
            lockout.RegisterFailure(clock.UtcNow, options.FailedAttemptThreshold, options.LockoutDuration);
            await context.SaveChangesAsync(cancellationToken);

            return Failure();
        }

        lockout.Clear();

        string token = Session.GenerateToken();
        Session session = Session.Issue(
            staff!.Id,
            staff.SecurityStamp,
            token,
            clock.UtcNow,
            options.AccessLifetime,
            options.RefreshLifetime);

        context.Sessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResult(token, staff, session));
    }

    /// <summary>
    /// Sayaç kayıtlı OLMAYAN kimlikler için de işler (R3). Kimlik ayrıştırılamasa bile
    /// bir anahtar üretilir; aksi hâlde "biçimi bozuk" girişler sayaca hiç dokunmaz ve
    /// sonsuz deneme kanalı açılırdı.
    /// </summary>
    private static string CounterKeyFor(string? loginId)
    {
        string raw = (loginId ?? string.Empty).Trim().ToLowerInvariant();

        return raw.Length > LoginId.MaximumLength ? raw[..LoginId.MaximumLength] : raw;
    }

    private async Task<LockoutState> LoadLockoutAsync(
        string counterKey,
        string clientKey,
        CancellationToken cancellationToken)
    {
        LockoutState? existing = await context.LoginAttempts.SingleOrDefaultAsync(
            state => state.NormalizedLoginId == counterKey && state.ClientKey == clientKey,
            cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        LockoutState created = LockoutState.ForKey(counterKey, clientKey);
        context.LoginAttempts.Add(created);

        return created;
    }

    private static Result<LoginResult> Failure() =>
        Result.Failure<LoginResult>(new Error(IdentityErrors.InvalidCredentials));
}
