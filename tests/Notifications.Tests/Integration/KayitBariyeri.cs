using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// Eşzamanlılık testinin bariyeri KAYIT ANINDADIR. `Task.WhenAll` iki isteği "aynı anda"
/// başlatır ama ilk istek kaydını bitirip ikincisi okumaya başlamadan önce tamamlanabilir —
/// o zaman test ardışık koşar ve yarışı hiç göstermez (Bilinen tuzak 9). Bu araç ilk
/// <see cref="Katilimci"/> sayıda <c>SaveChanges</c>'i, hepsi okumasını yapmış ve yazmaya
/// gelmişken birlikte bırakır; sonrakiler beklemeden geçer (çakışma sonrası yeniden deneme).
/// </summary>
internal sealed class KayitBariyeri(int katilimci) : SaveChangesInterceptor
{
    private readonly TaskCompletionSource hepsiGeldi = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int gelen;

    internal int Katilimci { get; } = katilimci;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        int sira = Interlocked.Increment(ref gelen);

        if (sira <= Katilimci)
        {
            if (sira == Katilimci)
            {
                hepsiGeldi.SetResult();
            }

            await hepsiGeldi.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        }

        return result;
    }
}
