using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// Şube kapatma (0007 D2). **HTTP ucu YOKTUR** ve açılmayacaktır: `0003` § D1 gereği bu
/// modülün korumasız yazma ucu olamaz ve kimlik doğrulama gelene kadar da olmayacak.
/// Komut modül içinden ve testten çağrılabilir.
///
/// Neden bugün çağıranı olmayan bir komut yazılıyor: B5'in koruması Application katmanında
/// yaşar ve komut olmadan 0007/AC-6 ile AC-7 yalnızca testin kendi kurduğu düzeneği
/// doğrulardı, korumayı değil. Şube yönetimi spec'i geldiğinde koruma hazır olur.
/// </summary>
internal sealed class BranchCommands(
    DealershipDbContext context,
    DealershipUnitOfWork unitOfWork,
    InvariantGuardOptions guardOptions)
{
    /// <summary>
    /// B5 — son aktif şube kapatılamaz. Okuma, karar ve yazma tek transaction içinde ve
    /// kilit altında; gerekçesi `InvariantGuard`'ta.
    /// </summary>
    internal Task<Result> CloseAsync(Guid branchId, CancellationToken cancellationToken) =>
        InvariantGuard.ExecuteAsync(
            context,
            ActiveBranches,
            branchId,
            async (otherActiveBranchCount, ct) =>
            {
                // Agregat KİLİTTEN SONRA okunuyor: önce okunsaydı, bekleyen istek kilit
                // açıldığında bayat bir kopyayla karar verirdi ve `Closed` kontrolü
                // sessizce atlanırdı.
                Branch? branch = await context.Branches
                    .SingleOrDefaultAsync(item => item.Id == new BranchId(branchId), ct);

                if (branch is null)
                {
                    return Result.Failure(new Error(BranchNotFound));
                }

                Result result = branch.Close(otherActiveBranchCount);

                if (result.IsFailure)
                {
                    return result;
                }

                await unitOfWork.SaveChangesAsync(ct);

                return Result.Success();
            },
            guardOptions,
            cancellationToken);

    internal const string BranchNotFound = "branch.not_found";

    /// <summary>
    /// B5'in "aktif" tanımı: yalnızca <see cref="BranchStatus.Active"/>. `TemporarilyClosed`
    /// sayılmaz — şubenin KENDİ durumu geçici de olsa kapalıdır. (`Identity`'deki "geçici
    /// kilitli Yönetici aktif sayılır" kuralı farklı bir şeydir: orada kilit ayrı bir
    /// tabloda yaşayan, hesabın durumuna dokunmayan geçici bir engeldir.)
    ///
    /// Yüklem `const`'tur ve değer parametredir; dizeye gömülmez (bkz. `InvariantLock`).
    /// </summary>
    private static readonly InvariantLock ActiveBranches = new(
        DealershipDbContext.Schema,
        "branches",
        "status = {0}",
        [(int)BranchStatus.Active]);
}
