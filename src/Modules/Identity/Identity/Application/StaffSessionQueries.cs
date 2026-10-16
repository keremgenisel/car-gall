using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Contracts.Sessions;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// **C16'nın kalbi: bayat yetki penceresi yoktur.** Hesabın durumu, rolü ve parola
/// damgası HER İSTEKTE kaynaktan okunur. Taşıyıcının içinden hiçbir iddia gelmez —
/// gelseydi pasifleştirme ve rol düşürme yapılandırılan süre boyunca etkisiz kalır ve
/// R14'ün "derhal" sözü yalan olurdu (Tuzak 8).
///
/// Çözülemeyen her durum AYNI sonucu verir: <c>null</c>. Token yok, oturum iptal
/// edilmiş, süresi dolmuş, personel pasif, damga değişmiş — hiçbiri ayrı bir dal
/// üretmez, çünkü ayrım yapılmayan şey sızdırılamaz.
/// </summary>
internal sealed class StaffSessionQueries(
    IdentityDbContext context,
    IdentityOptions options,
    IClock clock) : IStaffSessionQueries
{
    public async Task<StaffSessionDto?> ResolveAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        string tokenHash = Session.HashToken(token);

        Session? session = await context.Sessions
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (session is null)
        {
            return null;
        }

        SessionValidity validity = session.Validate(clock.UtcNow);

        // AC-17/AC-18 — yenileme hakkı dolmuş veya çıkış yapılmış oturum yenilenmez.
        if (validity is SessionValidity.Expired or SessionValidity.Revoked)
        {
            return null;
        }

        // D35 — ikinci adım durumu personel satırıyla AYNI okumada çözülüyor. Ayrı bir
        // sorgu, aynı gerçeği tek istekte iki kez sormak olurdu: hem `/me` hem de her
        // yönetim isteğinin önündeki kapı bu bilgiye muhtaç (D13).
        var row = await context.Staff
            .Where(item => item.Id == session.StaffId)
            .Select(item => new
            {
                Staff = item,
                TwoFactorEnrolled = context.TwoFactorEnrollments
                    .Any(enrollment => enrollment.Id == item.Id && enrollment.ConfirmedAt != null),
            })
            .SingleOrDefaultAsync(cancellationToken);

        Staff? staff = row?.Staff;

        // AC-20 — pasifleştirilen personelin açık oturumu DERHAL geçersizdir.
        if (staff is null || !staff.IsActive)
        {
            return null;
        }

        // AC-22 — parola değiştiğinde o personelin TÜM oturumları düşer; başka cihazdaki
        // oturum da yenilenemez. Damga kuruluş anında dondurulmuştu.
        if (staff.SecurityStamp != session.SecurityStamp)
        {
            return null;
        }

        // R4/AC-16 — personel çalışmaya devam ederken oturum SESSİZCE yenilenir; uç
        // hiçbir şey yapmaz, ekran bir şey görmez.
        //
        // Her istekte bir yazma olması bilinçli: erişim penceresi ancak son etkinlikten
        // sayılırsa "hareketsizlikte dolar" anlamına gelir. Kurulum başına birkaç
        // personel var; *tetikleyici:* panel yüksek frekanslı isteklere çıkarsa bu
        // yazma eşiklenir.
        session.Touch(clock.UtcNow, options.AccessLifetime);
        await context.SaveChangesAsync(cancellationToken);

        return new StaffSessionDto(
            staff.Id.Value,
            session.Id.Value,
            staff.DisplayName,
            staff.LoginId.Value,
            StaffRoleCode.ToCode(staff.Role),
            staff.PreferredLocale.Code,
            staff.MustChangePassword,
            staff.BranchId,
            row!.TwoFactorEnrolled);
    }
}
