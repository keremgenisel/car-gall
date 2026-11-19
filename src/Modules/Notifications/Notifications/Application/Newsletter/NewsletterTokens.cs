using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace CarGallery.Modules.Notifications.Application.Newsletter;

/// <summary>
/// `0018` **D25** — teyit anahtarı: CSPRNG, 256 bit. Veritabanında yalnız SHA-256 özeti
/// durur; düz anahtar yalnız iletide yaşar (AC-48, AC-49). Bağlantıda Base64Url taşınır.
/// </summary>
internal static class ConfirmationTokens
{
    internal const int TokenBytes = 32;

    internal static (string Token, byte[] Hash) Generate()
    {
        byte[] raw = RandomNumberGenerator.GetBytes(TokenBytes);

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
