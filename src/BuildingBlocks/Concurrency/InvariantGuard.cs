using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CarGallery.BuildingBlocks.Concurrency;

/// <summary>Kilidin ne kadar bekleyeceği. Testler uzatır, üretim varsayılanı kullanır.</summary>
/// <param name="LockTimeout">
/// Aşılırsa sonuç <see cref="ConcurrencyErrors.Lost"/> olur. Sınırsız bekleme, kilitte
/// asılı kalan bir isteğin bağlantı havuzunu tüketmesi demekti.
/// </param>
public sealed record InvariantGuardOptions(TimeSpan LockTimeout)
{
    public static InvariantGuardOptions Default { get; } = new(TimeSpan.FromSeconds(3));
}

/// <summary>
/// Invariant korumasının TEK yeri (0007 R3). Modüller bunu çağırır, kendi kopyalarını
/// yazmaz — aynı kural üç yerde yazılsaydı üçü de ayrı ayrı yanlış olabilirdi.
///
/// <para>Burada yalnızca <b>tören</b> yaşar: transaction aç → kilit süresini sınırla →
/// satırları sabit sırada kilitle → say → <b>agregatı kilitten SONRA oku</b> → kararı
/// modüle ver → kaydet → commit. "Hangi satırlar" ve "sayı kaçken reddedilir" modülde
/// kalır (Yasak Listesi 11).</para>
///
/// <para><b>Kilit kümesi hedefi de kapsar.</b> Dışlanan satır yalnızca SAYIdan düşülür,
/// kilitten değil. Aksi hâlde iki istek iki AYRI satırı kilitler, hiç serileşmez ve
/// koruma yalnızca tek kullanıcılı bir dünyada çalışırdı.</para>
///
/// <para><b>`ORDER BY id`</b> zorunludur: eşzamanlı istekler satırları aynı sırada
/// kilitler, karşılıklı kilitlenme oluşmaz (0007/AC-19).</para>
///
/// <para><b>Agregat neden kilitten sonra okunur?</b> Önce okunsaydı, bekleyen isteğin
/// elindeki nesne kilit açıldığında bayat olurdu: EF'in izlediği kopya, ilk isteğin
/// commit'ini görmez. "Zaten pasifleştirilmiş" gibi korumalar sessizce atlanır ve kayıt
/// ikinci kez yazılırdı (0007/AC-3).</para>
///
/// <para><b>Paylaşılan durum bırakılmaz.</b> `lock_timeout` <c>set_config(..., true)</c>
/// ile TRANSACTION'A YERELDİR; transaction bitince kendiliğinden geri döner ve havuzdan
/// gelen bağlantıyı kalıcı olarak değiştirmez. Kalıcı ayarlansaydı, bu korumadan sonra
/// aynı bağlantıyı kullanan her sorgu sessizce üç saniyelik bir kilit sınırı devralırdı —
/// `RateLimiterOptions.OnRejected` kancasında yaşanan hatanın aynısı: global durumu son
/// yazan kazanır.</para>
/// </summary>
public static class InvariantGuard
{
    /// <summary>PostgreSQL: serileştirme hatası, karşılıklı kilitlenme kurbanı, kilit zaman aşımı.</summary>
    private static readonly string[] ConcurrencySqlStates = ["40001", "40P01", "55P03"];

    /// <summary>
    /// Kilit altında karar verir ve yazar.
    /// </summary>
    /// <param name="context">Modülün kendi <see cref="DbContext"/>'i.</param>
    /// <param name="rows">Kilitlenecek satırların tarifi.</param>
    /// <param name="excluded">Sayıdan düşülecek satır; kilitten düşülmez.</param>
    /// <param name="decideAndWrite">
    /// Kilit alındıktan sonra çalışır. Agregatı <b>burada</b> okur, domain metodunu çağırır
    /// ve modülün kendi birimiyle kaydeder. Başarısız dönerse hiçbir şey commit edilmez.
    /// </param>
    public static async Task<Result> ExecuteAsync(
        DbContext context,
        InvariantLock rows,
        Guid excluded,
        Func<int, CancellationToken, Task<Result>> decideAndWrite,
        InvariantGuardOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            await using IDbContextTransaction transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            await ApplyLockTimeoutAsync(context, options.LockTimeout, cancellationToken);

            int count = await LockAndCountAsync(context, rows, excluded, cancellationToken);

            Result result = await decideAndWrite(count, cancellationToken);

            if (result.IsFailure)
            {
                return result;
            }

            await transaction.CommitAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception exception) when (IsConcurrencyFailure(exception))
        {
            // İstisna akış kontrolü DEĞİL: sağlayıcının sinyali burada, sınırda, kararlı
            // bir koda çevriliyor (docs/conventions.md § 5). Yukarısı `Result` görür.
            return Result.Failure(new Error(ConcurrencyErrors.Lost));
        }
    }

    /// <summary>
    /// `SET LOCAL` yerine <c>set_config</c>: `SET` değeri parametre kabul etmez ve sayıyı
    /// dizeye gömmek gerekirdi. Üçüncü argüman <c>true</c> — yereldir.
    /// </summary>
    private static Task ApplyLockTimeoutAsync(
        DbContext context,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken) =>
        context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('lock_timeout', {0}, true)",
            [((int)lockTimeout.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)],
            cancellationToken);

    private static async Task<int> LockAndCountAsync(
        DbContext context,
        InvariantLock rows,
        Guid excluded,
        CancellationToken cancellationToken)
    {
        // `Value` takma adı EF'in skaler `SqlQuery` sözleşmesidir. Şema, tablo ve yüklem
        // modülün SABİTLERİNDEN gelir; değerler parametredir (bkz. InvariantLock).
        string sql = $"""
            SELECT id AS "Value"
            FROM {rows.Schema}.{rows.Table}
            WHERE {rows.Predicate}
            ORDER BY id
            FOR UPDATE
            """;

        List<Guid> locked = await context.Database
            .SqlQueryRaw<Guid>(sql, [.. rows.Parameters])
            .ToListAsync(cancellationToken);

        return locked.Count(id => id != excluded);
    }

    /// <summary>
    /// Sağlayıcı istisnası kimi zaman sarmalanır (`SaveChanges` → `DbUpdateException`),
    /// kimi zaman doğrudan gelir. Zincir sonuna kadar yürünür.
    ///
    /// `DbException.SqlState` sağlayıcıdan bağımsızdır; `BuildingBlocks` Npgsql'e referans
    /// vermez ve vermemelidir.
    /// </summary>
    private static bool IsConcurrencyFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException { SqlState: { } state }
                && ConcurrencySqlStates.Contains(state, StringComparer.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
