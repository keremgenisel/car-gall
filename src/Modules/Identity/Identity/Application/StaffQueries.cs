using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// Son Yönetici korumasının (D3) sayısı. Sayıya YALNIZCA durumu aktif olanlar girer;
/// geçici kilitlenme burada hiç görünmez, çünkü kilit ayrı bir tabloda yaşar (AC-45).
///
/// **Sayım kilitleyerek okur.** R12 "galeride her zaman en az bir aktif Yönetici bulunur"
/// diyor; bir invariant dar bir pencerede bile delinebiliyorsa invariant değildir. Kilitsiz
/// oku-karar-ver-yaz, iki eşzamanlı pasifleştirme isteğinin İKİSİNİN de "benden başka bir
/// Yönetici var" görmesine ve ikisinin de geçmesine izin veriyordu — sonuç sıfır aktif
/// Yönetici, yani sistemin kalıcı olarak kapanması (Tuzak 1). Kurulum hesabı idempotent
/// olduğu için (var olan hesaba dokunmaz) o durumdan kurtarmaz.
/// </summary>
internal static class StaffQueries
{
    /// <summary>
    /// Aktif Yönetici satırlarının HEPSİNİ — hedef dâhil — `FOR UPDATE` ile kilitler ve
    /// sonra sayar. Kilit çağıranın transaction'ı boyunca sürer; ikinci istek burada
    /// bekler, birinci commit edince YENİDEN okur ve kararını güncel sayıyla verir.
    ///
    /// `ORDER BY id`: eşzamanlı iki istek satırları aynı sırada kilitler, karşılıklı
    /// kilitlenme (deadlock) oluşmaz.
    ///
    /// Hedef satır da kilit kümesindedir: dışlanan yalnız SAYIdan düşülür, kilitten değil.
    /// Aksi hâlde iki istek iki ayrı satır kilitler ve hiç serileşmezlerdi.
    /// </summary>
    internal static async Task<int> LockAndCountOtherActiveManagersAsync(
        IdentityDbContext context,
        StaffId excluded,
        CancellationToken cancellationToken)
    {
        List<Guid> activeManagers = await context.Database
            .SqlQueryRaw<Guid>(LockActiveManagersSql, (int)StaffRole.Manager, (int)StaffStatus.Active)
            .ToListAsync(cancellationToken);

        return activeManagers.Count(id => id != excluded.Value);
    }

    /// <summary>
    /// `Value` takma adı EF'in skaler `SqlQuery` sözleşmesidir. Şema adı parametre
    /// olamaz; sabitten gelir, kullanıcı girdisinden değil.
    /// </summary>
    private const string LockActiveManagersSql = $$"""
        SELECT id AS "Value"
        FROM {{IdentityDbContext.Schema}}.staff
        WHERE role = {0} AND status = {1}
        ORDER BY id
        FOR UPDATE
        """;
}
