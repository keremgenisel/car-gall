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

/// <summary>
/// D15 — girişin **iki dallı** sonucu: ya oturum açıldı ya bir adım daha var. Dal bir
/// hata DEĞİLDİR; `4xx` dönülmez, çünkü uç 2xx dışını `ApiError`'a çevirir ve ekran
/// **normal bir adımı hata olarak** basardı.
/// </summary>
internal enum LoginStep
{
    /// <summary>Oturum açıldı: ikinci adım ya geçildi ya hiç gerekmiyor.</summary>
    Completed = 1,

    /// <summary>Kurulumu olan hesap; ikinci adım bekleniyor ve oturum çerezi YAZILMAZ (D4).</summary>
    TwoFactorRequired = 2,

    /// <summary>
    /// Zorunlu roldeki kurulumsuz hesap: **kapılı** bir oturum verilir (D13). Parola bu
    /// hesapta bugün zaten tek faktör; kapılı oturum mevcut duruma göre hiçbir şeyi
    /// zayıflatmıyor ve R7'nin *"önce kurar"*ı ancak böyle yapılabilir.
    /// </summary>
    TwoFactorSetupRequired = 3,
}

/// <param name="Token">Oturum çerezine yazılacak opak değer; bekleyen dalda <c>null</c>.</param>
/// <param name="PendingToken">Ara durum çerezine yazılacak opak değer; yalnız bekleyen dalda.</param>
/// <param name="PendingExpiresAt">
/// Ara durum çerezinin son geçerlilik anı. Değer SATIRDAN geliyor, uçta yeniden
/// hesaplanmıyor: çerez ile satırın ayrı ayrı hesaplanması ikisini ayrı anlarda
/// bitirir ve saat de uçta değil `IClock`'ta yaşıyor (Yasak Listesi 30).
/// </param>
internal sealed record LoginResult(
    LoginStep Step,
    Staff Staff,
    string? Token,
    Session? Session,
    string? PendingToken,
    DateTimeOffset? PendingExpiresAt,
    bool TwoFactorEnrolled);

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

        // D4/AC-11 — kurulumu OLAN hesapta oturum çerezi yazılmaz: parola tek başına
        // geçerli bir oturum üretmez. Doğrulanmamış kurulum kurulumsuz sayılır (AC-3);
        // onaylanmamış bir satırla ikinci adım istemek, personeli geçemeyeceği bir
        // kapının arkasında bırakırdı.
        bool enrolled = await context.TwoFactorEnrollments.AnyAsync(
            enrollment => enrollment.Id == staff!.Id && enrollment.ConfirmedAt != null,
            cancellationToken);

        if (enrolled)
        {
            string pendingToken = PendingLogin.GenerateToken();

            PendingLogin pending = PendingLogin.Issue(
                staff!.Id, pendingToken, clock.UtcNow, options.PendingLoginLifetime);

            context.PendingLogins.Add(pending);

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(new LoginResult(
                LoginStep.TwoFactorRequired,
                staff,
                Token: null,
                Session: null,
                pendingToken,
                pending.ExpiresAt,
                TwoFactorEnrolled: true));
        }

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

        // D13/D30 — zorunlu rolde kurulumu olmayan hesap kapılı oturum alır; kapı
        // `TwoFactorSetupGate`'tedir ve izin verilen yollar orada AÇIKÇA sayılır.
        LoginStep step = TwoFactorPolicy.IsRequiredFor(staff.Role)
            ? LoginStep.TwoFactorSetupRequired
            : LoginStep.Completed;

        return Result.Success(new LoginResult(
            step,
            staff,
            token,
            session,
            PendingToken: null,
            PendingExpiresAt: null,
            TwoFactorEnrolled: false));
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
