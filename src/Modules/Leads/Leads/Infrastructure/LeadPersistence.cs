using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.Leads.Infrastructure;

/// <summary>
/// Kalıcılık katmanının hatalarını YAPISAL olarak tanır.
///
/// Eskiden bu karar istisna METNİNDE indeks adını aramakla veriliyordu. İki yönden
/// kırılgandı: indeks yeniden adlandırıldığında sessizce eşleşmeyi bırakır (o anda
/// eşzamanlı ikinci istek 500 alır ve AC-35'in koruması tam da işe yarayacağı yerde
/// kaybolur), başka bir hatanın metni aynı dizeyi taşırsa da yanlış dallanır.
///
/// Npgsql bunu zaten yapısal veriyor: `SqlState` 23505 tekil kısıt ihlalidir ve
/// `ConstraintName` hangi kısıt olduğunu söyler.
/// </summary>
internal static class LeadPersistence
{
    /// <summary>
    /// İndeks adı TEK YERDE yaşar; yapılandırma da buradan okur, tespit de. İki yerde
    /// dize olarak tekrarlanırsa biri değişip öteki kalır.
    /// </summary>
    internal const string IdempotencyKeyIndexName = "ux_leads_idempotency_key";

    private const string UniqueViolation = "23505";

    internal static bool IsIdempotencyKeyConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: UniqueViolation,
            ConstraintName: IdempotencyKeyIndexName,
        };
}
