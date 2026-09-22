using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// Son Yönetici korumasının (D3) kilit kümesi. Kümeye YALNIZCA durumu aktif olanlar girer;
/// geçici kilitlenme burada hiç görünmez, çünkü kilit ayrı bir tabloda yaşar (0006/AC-45).
///
/// Kilidi bu dosya ALMAZ — tarifi verir, `InvariantGuard` uygular (0007 R3). Kilidin nasıl
/// alındığı, neden hedefin de kilitlendiği ve agregatın neden kilitten sonra okunduğu tek
/// bir yerde yazılı: `BuildingBlocks/Concurrency/InvariantGuard.cs`.
///
/// Yüklem `const`'tur ve değerler parametredir; dizeye gömülmez (bkz. `InvariantLock`).
/// </summary>
internal static class StaffQueries
{
    internal static readonly InvariantLock ActiveManagers = new(
        IdentityDbContext.Schema,
        "staff",
        "role = {0} AND status = {1}",
        [(int)StaffRole.Manager, (int)StaffStatus.Active]);
}
