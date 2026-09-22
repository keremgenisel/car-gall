using System.Security.Cryptography;
using System.Text;
using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>Oturumun bir istek anındaki hâli.</summary>
internal enum SessionValidity
{
    /// <summary>Erişim penceresi açık; istek çalışır.</summary>
    Active = 1,

    /// <summary>Erişim penceresi doldu ama yenileme hakkı duruyor — SESSİZCE yenilenir (AC-16).</summary>
    NeedsRenewal = 2,

    /// <summary>Yenileme hakkı da doldu; personel yeniden giriş yapar (AC-17).</summary>
    Expired = 3,

    /// <summary>Çıkış yapıldı veya pasifleştirme/parola değişimi düşürdü (AC-18, AC-20, AC-22).</summary>
    Revoked = 4,
}

/// <summary>
/// Sunucu tarafı oturum. Çerezde taşınan şey **opak, rastgele bir dizedir** ve kendi
/// başına hiçbir iddia taşımaz: hesabın durumu ve rolü her istekte KAYNAKTAN okunur
/// (C16). Kendi kendini doğrulayan bir taşıyıcı (JWT) seçilseydi, pasifleştirme ve rol
/// düşürme yapılandırılan süre boyunca etkisiz kalır ve R14'ün "derhal" sözü yalan
/// olurdu (Tuzak 8).
///
/// Satırda tokenin KENDİSİ değil, SHA-256 özeti durur: sızan bir veritabanı yedeği
/// doğrudan oturum açmaya yetmesin. Burada yavaş bir özet gerekmez — token yüksek
/// entropili rastgele bir değerdir, parola gibi tahmin edilebilir değildir (C6'nın
/// gerekçesi parolaya özeldir).
/// </summary>
internal sealed class Session : Entity<SessionId>
{
    private Session(
        SessionId id,
        StaffId staffId,
        SecurityStamp securityStamp,
        string tokenHash,
        DateTimeOffset issuedAt,
        DateTimeOffset accessExpiresAt,
        DateTimeOffset refreshExpiresAt)
        : base(id)
    {
        StaffId = staffId;
        SecurityStamp = securityStamp;
        TokenHash = tokenHash;
        IssuedAt = issuedAt;
        AccessExpiresAt = accessExpiresAt;
        RefreshExpiresAt = refreshExpiresAt;
    }

    /// <summary>EF Core'un okuma yolu; iş kodundan çağrılmaz.</summary>
    private Session()
        : base(default) => TokenHash = string.Empty;

    internal StaffId StaffId { get; private set; }

    /// <summary>
    /// Kuruluş anındaki damga. Personelin damgası değişince (parola değişimi) bu oturum
    /// artık eşleşmez ve düşer — çıkış yapmadan, o cihaza dokunmadan (AC-22).
    /// </summary>
    internal SecurityStamp SecurityStamp { get; private set; }

    internal string TokenHash { get; private set; }

    internal DateTimeOffset IssuedAt { get; private set; }

    /// <summary>Son etkinlik + yapılandırılan erişim süresi. Her istekte kayar (R4).</summary>
    internal DateTimeOffset AccessExpiresAt { get; private set; }

    /// <summary>MUTLAK sınır; etkinlik bunu uzatmaz (AC-17).</summary>
    internal DateTimeOffset RefreshExpiresAt { get; private set; }

    internal DateTimeOffset? RevokedAt { get; private set; }

    internal static Session Issue(
        StaffId staffId,
        SecurityStamp securityStamp,
        string token,
        DateTimeOffset now,
        TimeSpan accessLifetime,
        TimeSpan refreshLifetime) =>
        new(
            SessionId.New(),
            staffId,
            securityStamp,
            HashToken(token),
            now,
            now.Add(accessLifetime),
            now.Add(refreshLifetime));

    /// <summary>Çerezde taşınacak opak değer. 256 bit entropi; tahmin edilemez.</summary>
    internal static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    internal static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    internal SessionValidity Validate(DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return SessionValidity.Revoked;
        }

        if (now >= RefreshExpiresAt)
        {
            return SessionValidity.Expired;
        }

        return now >= AccessExpiresAt ? SessionValidity.NeedsRenewal : SessionValidity.Active;
    }

    /// <summary>
    /// R4 — personel çalışmaya devam ederken oturum sessizce yenilenir. Yenileme
    /// yalnız ERİŞİM penceresini uzatır; mutlak yenileme sınırına dokunmaz.
    /// </summary>
    internal void Touch(DateTimeOffset now, TimeSpan accessLifetime) =>
        AccessExpiresAt = now.Add(accessLifetime);

    /// <summary>R5/AC-18 — yalnız BU oturum; aynı personelin başka cihazı etkilenmez.</summary>
    internal void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
