using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// Son Yönetici korumasının (D3) sayısı. Sayıya YALNIZCA durumu aktif olanlar girer;
/// geçici kilitlenme burada hiç görünmez, çünkü kilit ayrı bir tabloda yaşar (AC-45).
/// </summary>
internal static class StaffQueries
{
    internal static Task<int> CountOtherActiveManagersAsync(
        IdentityDbContext context,
        StaffId excluded,
        CancellationToken cancellationToken) =>
        context.Staff
            .AsNoTracking()
            .CountAsync(
                staff => staff.Id != excluded
                    && staff.Role == StaffRole.Manager
                    && staff.Status == StaffStatus.Active,
                cancellationToken);
}
