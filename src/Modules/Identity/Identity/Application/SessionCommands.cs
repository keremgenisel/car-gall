using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// R5/AC-18/AC-19 — çıkış YALNIZ o oturumu kapatır. Bir personel aynı anda birden çok
/// cihazda oturum açabilir ve çıkış ötekini etkilemez. Bu yüzden damga DÖNMEZ: damga
/// dönseydi tüm cihazlar düşerdi ve AC-19 kırılırdı.
/// </summary>
internal sealed class Logout(IdentityDbContext context, IClock clock)
{
    internal async Task<Result> HandleAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        Session? session = await context.Sessions
            .SingleOrDefaultAsync(item => item.Id == new SessionId(sessionId), cancellationToken);

        if (session is null)
        {
            return Result.Success();
        }

        session.Revoke(clock.UtcNow);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
