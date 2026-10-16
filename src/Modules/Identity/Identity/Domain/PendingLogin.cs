using System.Security.Cryptography;
using System.Text;
using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Ara durum: parola doğrulandı, ikinci adım bekleniyor (R13/D4/D19).
///
/// **Bu bir oturum DEĞİLDİR** ve hiçbir yönetim isteği onunla çalışmaz (AC-16). Kurulumu
/// olan hesapta ikinci adım geçilmeden oturum çerezi hiç yazılmaz: `PasswordChangeGate`
/// deseniyle "yarım oturum" vermek ucuz olurdu ama o yolda parola **tek başına** geçerli
/// bir oturum üretir ve kapıdaki tek kusur özelliğin tamamını geçersiz kılar (D4,
/// § Bilinen tuzaklar 6).
///
/// Taşıma deseni `Session`'ın aynısıdır (D19): çerezte **opak rastgele dize**, satırda
/// **SHA-256 özeti**. Kendi kendini doğrulayan imzalı bir çerez, D3'ün sır için kurduğu
/// altyapının bir benzerini daha isterdi — hem de zaten sunucuda duran bir değer için.
/// </summary>
internal sealed class PendingLogin : Entity<PendingLoginId>
{
    private PendingLogin(
        PendingLoginId id,
        StaffId staffId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
        : base(id)
    {
        StaffId = staffId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>EF Core'un okuma yolu; iş kodundan çağrılmaz.</summary>
    private PendingLogin()
        : base(default) => TokenHash = string.Empty;

    internal StaffId StaffId { get; private set; }

    internal string TokenHash { get; private set; }

    internal DateTimeOffset CreatedAt { get; private set; }

    internal DateTimeOffset ExpiresAt { get; private set; }

    internal DateTimeOffset? ConsumedAt { get; private set; }

    /// <summary>
    /// D47 — bu sayı **ikinci bir eşik DEĞİLDİR**. Eşik tek ve paylaşılan
    /// `LockoutState` sayacıdır (R8); iki eşik tutmak § Bilinen tuzaklar 5'in tam olarak
    /// uyardığı şeydir. Buradaki sayı yalnız ara durumun ne zaman **silineceğini**
    /// belirler: hesap kilitlendiğinde personel parola fazına döner (AC-35).
    /// </summary>
    internal int Attempts { get; private set; }

    internal static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    internal static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    internal static PendingLogin Issue(StaffId staffId, string token, DateTimeOffset now, TimeSpan lifetime) =>
        new(PendingLoginId.New(), staffId, HashToken(token), now, now.Add(lifetime));

    internal bool IsUsableAt(DateTimeOffset now) => ConsumedAt is null && now < ExpiresAt;

    internal void RegisterFailure() => Attempts++;

    /// <summary>
    /// AC-15 — ara durum **tek kullanımlıktır**: başarılı doğrulamada tükenir. Aksi
    /// hâlde elinde çerez kalan biri aynı parolayla ikinci bir oturum açabilirdi.
    /// </summary>
    internal void Consume(DateTimeOffset now) => ConsumedAt ??= now;
}
