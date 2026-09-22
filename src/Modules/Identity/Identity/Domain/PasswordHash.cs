using System.Globalization;
using System.Security.Cryptography;
using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Saklanan parola özeti. Parola HİÇBİR YERDE geri döndürülebilir biçimde saklanmaz,
/// loglanmaz ve API yanıtında bulunmaz (C6, AC-50, AC-51).
///
/// Biçim <b>sürümlüdür</b>: <c>pbkdf2-sha256$&lt;iterasyon&gt;$&lt;tuz&gt;$&lt;özet&gt;</c>.
/// Algoritma adının özetin İÇİNDE durması, ileride daha güçlü bir algoritmaya (Argon2id)
/// geçerken saklanmış özetleri bozmadan geçiş yapmayı mümkün kılar: eski satırlar eski
/// algoritmayla doğrulanmaya devam eder, yenileri yeniyle yazılır.
///
/// "Geri döndürülemez" tek başına HIZLI bir özeti dışlamaz; sızan bir veritabanında fark
/// tam olarak budur (C6). Bu yüzden algoritma parolaya özel ve yavaştır, tuz hesaba
/// özeldir ve iterasyon sayısı dağıtım yapılandırmasından gelir.
/// </summary>
internal readonly record struct PasswordHash
{
    internal const string Pbkdf2Sha256 = "pbkdf2-sha256";

    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    private PasswordHash(string value) => Value = value;

    internal string Value { get; init; }

    /// <summary>
    /// Tuz her çağrıda YENİDEN üretilir: aynı parolayı kullanan iki personelin özetleri
    /// birbirinden farklıdır (AC-37). Ortak tuz, bir gökkuşağı tablosunun iki hesabı
    /// birden açması demektir.
    /// </summary>
    internal static PasswordHash Create(string password, int iterations)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
        byte[] hash = Derive(password, salt, iterations);

        return new PasswordHash(string.Format(
            CultureInfo.InvariantCulture,
            "{0}${1}${2}${3}",
            Pbkdf2Sha256,
            iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash)));
    }

    internal static Result<PasswordHash> FromStorage(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Split('$').Length != 4
            ? Result.Failure<PasswordHash>(new Error("identity.corrupt_password_hash"))
            : Result.Success(new PasswordHash(value));

    /// <summary>
    /// Karşılaştırma SABİT ZAMANLIDIR. Sıradan bir dizi karşılaştırması ilk farklı baytta
    /// döner ve ölçülebilir bir zaman farkı bırakır; R3'ün "ayırt edilemez karşılık"
    /// kuralı yanıt gövdesiyle sınırlı değildir.
    /// </summary>
    internal bool Matches(string password)
    {
        string[] parts = Value.Split('$');

        if (parts.Length != 4 || parts[0] != Pbkdf2Sha256)
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int iterations))
        {
            return false;
        }

        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] expected = Convert.FromBase64String(parts[3]);

        return CryptographicOperations.FixedTimeEquals(Derive(password, salt, iterations), expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashBytes);

    /// <summary>
    /// Özet METNİ hiçbir yerde kazara basılmasın diye: log, hata mesajı veya
    /// `ToString()` çağrısı parolaya dair hiçbir şey sızdırmaz (AC-51).
    /// </summary>
    public override string ToString() => "***";
}
