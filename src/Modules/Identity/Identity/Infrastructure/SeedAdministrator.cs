using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Infrastructure;

/// <summary>
/// D2 — **ilk Yönetici hesabı kurulumla gelir.** Aksi hâlde hiç kimse giriş yapamaz ve
/// sistem kalıcı olarak kapalı kalır (Tuzak 1).
///
/// Adımın migration'ın İÇİNDE değil hemen ARDINDAN çalışmasının nedeni teknik: EF
/// migration'ı tasarım zamanında üretilir ve uygulanırken çalışma anı yapılandırmasına
/// erişemez. Parolanın özeti migration dosyasına yazılsaydı, repoda sabit bir parola
/// özeti dururdu. D2'nin amacı — sistem kapalı kalmasın, sessiz varsayılan parola
/// olmasın — birebir korunuyor; değişen yalnız hangi adımda çalıştığı.
///
/// Hesabın parolası dağıtım yapılandırmasından okunur ve **ilk girişte değiştirilmesi
/// zorunludur**: yapılandırmada duran bir parola, değiştirilmediği sürece bilinen bir
/// paroladır. Bu zorunluluk kararın ayrılmaz parçasıdır.
///
/// İşlem idempotenttir: hesap varsa hiç dokunulmaz — parolası da sıfırlanmaz.
/// </summary>
internal static class SeedAdministrator
{
    internal static async Task EnsureAsync(
        IdentityDbContext context,
        IPasswordHasher passwordHasher,
        IdentityLocaleOptions locales,
        IClock clock,
        string loginId,
        string password,
        string displayName,
        CancellationToken cancellationToken)
    {
        Result<LoginId> parsed = LoginId.Create(loginId);

        if (parsed.IsFailure)
        {
            throw new InvalidOperationException(
                $"Identity:SeedAdmin:LoginId geçerli bir e-posta değil: '{loginId}'.");
        }

        bool exists = await context.Staff
            .AsNoTracking()
            .AnyAsync(staff => staff.NormalizedLoginId == parsed.Value.Normalized, cancellationToken);

        if (exists)
        {
            return;
        }

        Result<Staff> staff = Staff.Add(
            displayName,
            parsed.Value,
            passwordHasher.Hash(password),
            StaffRole.Manager,
            locales.Default,
            branchId: null,
            clock.UtcNow);

        if (staff.IsFailure)
        {
            throw new InvalidOperationException(
                $"Kurulum Yönetici hesabı oluşturulamadı: {staff.Error.Code}.");
        }

        context.Staff.Add(staff.Value);
        await context.SaveChangesAsync(cancellationToken);
    }
}
