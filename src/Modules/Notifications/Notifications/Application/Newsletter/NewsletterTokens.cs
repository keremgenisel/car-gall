using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` **D25** · **D28** — teyit anahtarı: teyit iletisinin GÖNDERİM KAYDI kimliğinden,
/// yapılandırmadaki imza sırrıyla HMAC-SHA256 ile türetilir (256 bit). Aynı kayıt her
/// denemede AYNI anahtarı verir: geçici hatayla yeniden denenen ileti aynı bağlantıyı taşır ve
/// ilk teslim edilmiş olanın bağlantısı ölmez. Yeni istek yeni kayıt, dolayısıyla yeni anahtar
/// demektir; abonelikte yalnız en sonuncunun SHA-256 özeti durur (**D7**, AC-34, AC-48, AC-49).
///
/// <para><b>Çıkış bağlantısıyla aynı sır, ayrı amaç önekiyle.</b> İki ayrı sır, kurulumda iki
/// ayrı zorunlu değişken ve iki ayrı yedek demekti; amaç öneki (<c>newsletter-confirm:</c> ·
/// <c>newsletter-unsubscribe:</c>) bir türün imzasının öbür türde geçmesini engeller. Bedel:
/// sır değişirse bekleyen teyit bağlantıları da ölür (çıkış bağlantılarıyla birlikte, D25).</para>
/// </summary>
internal sealed class ConfirmationTokens(NewsletterOptions options)
{
    internal const int TokenBytes = 32;

    private static readonly byte[] Purpose = Encoding.ASCII.GetBytes("newsletter-confirm:");

    /// <summary>Gönderim kaydının anahtarı ve onun SHA-256 özeti.</summary>
    internal (string Token, byte[] Hash) For(Guid messageId)
    {
        byte[] message = new byte[Purpose.Length + 16];
        Purpose.CopyTo(message, 0);
        messageId.TryWriteBytes(message.AsSpan(Purpose.Length));

        byte[] raw = HMACSHA256.HashData(options.UnsubscribeSigningKey, message);

        return (Base64Url.EncodeToString(raw), SHA256.HashData(raw));
    }

    /// <summary>Bozuk, kısa ya da uzun anahtar özet üretmez; çağıran onu "geçersiz bağlantı" okur.</summary>
    internal static byte[]? HashOf(string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 64)
        {
            return null;
        }

        byte[] buffer = new byte[Base64Url.GetMaxDecodedLength(token.Length)];

        if (Base64Url.DecodeFromChars(token, buffer, out _, out int written) != System.Buffers.OperationStatus.Done
            || written != TokenBytes)
        {
            return null;
        }

        return SHA256.HashData(buffer.AsSpan(0, written));
    }
}

/// <summary>
/// `0018` **D8** · **D25** — çıkış bağlantısı SAKLANMAZ: abonelik kimliği + kimliğin
/// HMAC-SHA256 imzası. Süresizdir, aboneye bağlıdır ve yeniden abonelik onu geçersiz kılmaz
/// (AC-42). Adresi içermez (AC-91). Sır değişirse eski bağlantılar doğrulanmaz (AC-47).
/// </summary>
internal sealed class UnsubscribeTokens(NewsletterOptions options)
{
    private const int IdBytes = 16;

    private const int SignatureBytes = 32;

    private static readonly byte[] Purpose = Encoding.ASCII.GetBytes("newsletter-unsubscribe:");

    internal string Create(Guid subscriptionId)
    {
        byte[] payload = new byte[IdBytes + SignatureBytes];
        subscriptionId.TryWriteBytes(payload.AsSpan(0, IdBytes));
        Sign(payload.AsSpan(0, IdBytes)).CopyTo(payload.AsSpan(IdBytes));

        return Base64Url.EncodeToString(payload);
    }

    internal bool TryVerify(string? token, out Guid subscriptionId)
    {
        subscriptionId = Guid.Empty;

        if (string.IsNullOrEmpty(token) || token.Length > 128)
        {
            return false;
        }

        byte[] buffer = new byte[Base64Url.GetMaxDecodedLength(token.Length)];

        if (Base64Url.DecodeFromChars(token, buffer, out _, out int written) != System.Buffers.OperationStatus.Done
            || written != IdBytes + SignatureBytes)
        {
            return false;
        }

        ReadOnlySpan<byte> id = buffer.AsSpan(0, IdBytes);

        // Sabit zamanlı karşılaştırma: imzanın kaçıncı baytta ayrıştığı süreden okunamaz.
        if (!CryptographicOperations.FixedTimeEquals(Sign(id), buffer.AsSpan(IdBytes, SignatureBytes)))
        {
            return false;
        }

        subscriptionId = new Guid(id);
        return true;
    }

    private byte[] Sign(ReadOnlySpan<byte> id)
    {
        byte[] message = new byte[Purpose.Length + id.Length];
        Purpose.CopyTo(message, 0);
        id.CopyTo(message.AsSpan(Purpose.Length));

        return HMACSHA256.HashData(options.UnsubscribeSigningKey, message);
    }
}
