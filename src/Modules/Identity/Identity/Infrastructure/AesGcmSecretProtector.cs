using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Infrastructure;

/// <summary>
/// AES-256-GCM ile şifreleme; anahtar dağıtım yapılandırmasından gelir (D3).
///
/// GCM seçildi çünkü **kimliklenmiş** şifrelemedir: yanlış anahtarla ya da kurcalanmış
/// bir satırla çözme sessizce bozuk bayt üretmek yerine **başarısız olur**. Sessiz
/// bozulma burada en pahalı hâldir — kusurun görüntüsü *"doğru kod reddediliyor"*
/// olurdu ve sebebi hiçbir yerde görünmezdi (D16'nın aynı dersi).
///
/// Biçim <b>sürümlüdür</b>: <c>aes-256-gcm$&lt;nonce&gt;$&lt;tag&gt;$&lt;payload&gt;</c>.
/// Nonce her yazmada yeniden üretilir — GCM'de aynı anahtarla nonce tekrarı anahtarı
/// açar.
/// </summary>
internal sealed class AesGcmSecretProtector : ISecretProtector
{
    internal const string Scheme = "aes-256-gcm";

    /// <summary>AES-256: anahtar 32 bayt. Yapılandırmada Base64 olarak durur.</summary>
    internal const int KeyBytes = 32;

    private const int NonceBytes = 12;
    private const int TagBytes = 16;

    private readonly byte[] key;

    internal AesGcmSecretProtector(string base64Key) => key = ReadKey(base64Key);

    /// <summary>
    /// Anahtarı KURULUŞTA doğrular: eksik ya da bozuk anahtarla uygulama hiç ayağa
    /// kalkmaz (AC-49). Çalışma anında patlamak, ilk kurulum denemesine kadar görünmez
    /// bir kusur demek olurdu (`docs/conventions.md` § 11).
    /// </summary>
    internal static byte[] ReadKey(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
        {
            throw new InvalidOperationException(
                "Identity:TwoFactor:EncryptionKey yapılandırılmamış; doğrulama sırrı "
                    + "şifrelenmeden saklanamaz (0013 D3).");
        }

        byte[] raw;

        try
        {
            raw = Convert.FromBase64String(base64Key);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Identity:TwoFactor:EncryptionKey Base64 değil; 32 baytlık bir anahtarın "
                    + "Base64 hâli bekleniyor (0013 D3).");
        }

        return raw.Length == KeyBytes
            ? raw
            : throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture,
                "Identity:TwoFactor:EncryptionKey {0} bayt olmalı, {1} bayt verildi (0013 D3).",
                KeyBytes,
                raw.Length));
    }

    public string Protect(string plaintext)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        byte[] plain = Encoding.UTF8.GetBytes(plaintext);
        byte[] payload = new byte[plain.Length];
        byte[] tag = new byte[TagBytes];

        using AesGcm aes = new(key, TagBytes);
        aes.Encrypt(nonce, plain, payload, tag);

        return string.Join(
            '$',
            Scheme,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(payload));
    }

    public Result<string> Unprotect(string cipher)
    {
        string[] parts = (cipher ?? string.Empty).Split('$');

        if (parts.Length != 4 || parts[0] != Scheme)
        {
            return Result.Failure<string>(new Error(IdentityErrors.CorruptTwoFactorSecret));
        }

        try
        {
            byte[] nonce = Convert.FromBase64String(parts[1]);
            byte[] tag = Convert.FromBase64String(parts[2]);
            byte[] payload = Convert.FromBase64String(parts[3]);
            byte[] plain = new byte[payload.Length];

            using AesGcm aes = new(key, TagBytes);
            aes.Decrypt(nonce, payload, tag, plain);

            return Result.Success(Encoding.UTF8.GetString(plain));
        }
        catch (Exception failure) when (failure is CryptographicException or FormatException or ArgumentException)
        {
            // Yanlış anahtar, kurcalanmış satır ve bozuk Base64 aynı sonucu verir: bu
            // noktada ayrım yapmak, çözemediğimizin NEDENİNİ çağırana söylemek olurdu.
            return Result.Failure<string>(new Error(IdentityErrors.CorruptTwoFactorSecret));
        }
    }
}
