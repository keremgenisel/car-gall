using System.Security.Cryptography;
using System.Text;
using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Doğrulama sırrı (docs/domain.md — **Doğrulama Sırrı** / `TwoFactorSecret`).
///
/// Bu tip, deponun tek **geri döndürülebilir** sırrıdır ve bu zorunluluktur (C2a/D1):
/// zamana bağlı kodu doğrulamanın tek yolu aynı sırla kodu yeniden üretmektir. Tek yönlü
/// özetlenmiş bir sırla doğrulama yapılamaz. Bedeli üç kuralla ödenir: sır
/// **loglanmaz**, yanıtta **yalnız kurulum anında bir kez** görünür ve veritabanında
/// **açık metin durmaz** (D3, şifrelemeyi `ISecretProtector` yapar).
///
/// Kodlama **Base32**'dir (RFC 4648) ve bu bir biçim tercihi değil, uyumluluk
/// sözleşmesidir: `otpauth://` adresini okuyan doğrulama uygulamalarının tamamı sırrı
/// Base32 bekler (D16/C12).
/// </summary>
internal readonly record struct TwoFactorSecret
{
    /// <summary>
    /// 160 bit. HMAC-SHA1'in blok boyutuna eşit uzunluk RFC 4226 § 4'ün önerisidir;
    /// daha kısası HMAC'ın anahtar uzayını, daha uzunu sırrı elle yazan personelin
    /// sabrını boşa harcar.
    /// </summary>
    private const int SecretBytes = 20;

    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private TwoFactorSecret(string base32) => Base32 = base32;

    /// <summary>Personele gösterilen ve adreste geçen biçim; saklanan biçim şifrelidir.</summary>
    internal string Base32 { get; }

    internal static TwoFactorSecret New() => new(Encode(RandomNumberGenerator.GetBytes(SecretBytes)));

    /// <summary>
    /// Şifreden çözülen ya da personelin elle yazdığı değeri okur. Boşluk ve küçük harf
    /// **tolere edilir**: elle giriş metni dörtlü gruplar hâlinde gösteriliyor ve
    /// personelin onu birebir kopyalaması beklenemez (D17).
    /// </summary>
    internal static Result<TwoFactorSecret> FromBase32(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<TwoFactorSecret>(new Error(IdentityErrors.CorruptTwoFactorSecret));
        }

        string normalized = value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .TrimEnd('=')
            .ToUpperInvariant();

        if (normalized.Length == 0 || normalized.Any(symbol => !Alphabet.Contains(symbol, StringComparison.Ordinal)))
        {
            return Result.Failure<TwoFactorSecret>(new Error(IdentityErrors.CorruptTwoFactorSecret));
        }

        return Result.Success(new TwoFactorSecret(normalized));
    }

    internal byte[] ToBytes() => Decode(Base32);

    /// <summary>
    /// `otpauth://` adresi — **parametre kümesi dondurulmuştur** (C12/D16). Kümeyi
    /// değiştirmek kurulmuş BÜTÜN kayıtları sessizce geçersizleştirir ve kusurun
    /// görüntüsü *"doğru kod reddediliyor"* olur.
    ///
    /// `issuer` dağıtım yapılandırmasından gelir (docs/conventions.md § 11); etiket
    /// `{issuer}:{loginId}` biçimindedir, çünkü doğrulama uygulamaları hesabı bu
    /// etiketle listeler ve iki kurulumun aynı personeli iki ayrı satır olarak
    /// göstermesi ancak böyle mümkün olur.
    /// </summary>
    internal string ToUri(string issuer, string loginId)
    {
        string label = Uri.EscapeDataString($"{issuer}:{loginId}");

        return $"otpauth://totp/{label}"
            + $"?secret={Base32}"
            + $"&issuer={Uri.EscapeDataString(issuer)}"
            + $"&algorithm={TimeBasedCode.Algorithm}"
            + $"&digits={TimeBasedCode.Digits}"
            + $"&period={TimeBasedCode.PeriodSeconds}";
    }

    /// <summary>
    /// Sır metni hiçbir yerde kazara basılmasın diye (C2a, AC-47): log, hata mesajı ve
    /// `ToString()` çağrısı sırra dair hiçbir şey sızdırmaz. `PasswordHash` ile aynı
    /// desen.
    /// </summary>
    public override string ToString() => "***";

    private static string Encode(byte[] bytes)
    {
        StringBuilder builder = new((bytes.Length * 8 / 5) + 1);
        int buffer = 0;
        int bits = 0;

        foreach (byte value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;

            while (bits >= 5)
            {
                bits -= 5;
                builder.Append(Alphabet[(buffer >> bits) & 31]);
            }
        }

        if (bits > 0)
        {
            builder.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        }

        return builder.ToString();
    }

    private static byte[] Decode(string base32)
    {
        List<byte> bytes = new(base32.Length * 5 / 8);
        int buffer = 0;
        int bits = 0;

        foreach (char symbol in base32)
        {
            buffer = (buffer << 5) | Alphabet.IndexOf(symbol, StringComparison.Ordinal);
            bits += 5;

            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)((buffer >> bits) & 255));
            }
        }

        return [.. bytes];
    }
}
