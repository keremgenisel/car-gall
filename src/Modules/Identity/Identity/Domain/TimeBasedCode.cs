using System.Globalization;
using System.Security.Cryptography;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Zamana bağlı kod (RFC 6238 / TOTP). **Parametre kümesi burada SABİTTİR ve
/// dondurulmuştur** (C12/D16): hane sayısı, pencere ve algoritma kurulmuş her kaydı
/// bağlar, değiştirmek bütün kurulumları geçersizleştirir. Bu yüzden değerler
/// yapılandırmadan okunan bir eşik DEĞİL, spec'te sayıyla yazılı bir sözleşmedir —
/// `0006` D4'ün eşiklerinden bilinçli olarak ayrılıyor.
///
/// Küme RFC 6238 varsayılanlarıdır, çünkü yaygın doğrulama uygulamalarının **tamamının**
/// okuduğu tek küme budur: SHA-256 ya da 8 hane bazı uygulamalarda sessizce
/// desteklenmiyor ve kusurun görüntüsü *"doğru kod reddediliyor"* oluyor.
/// </summary>
internal static class TimeBasedCode
{
    internal const string Algorithm = "SHA1";

    internal const int Digits = 6;

    internal const int PeriodSeconds = 30;

    /// <summary>
    /// D10 — saat kayması toleransı: önceki, içinde bulunulan ve sonraki adım kabul
    /// edilir (±30 sn). Tolerans olmadan sunucu ile cihaz saati arasındaki küçük fark
    /// doğru kodu reddeder ve personel sebebini anlamaz (C5, Tuzak 4). Kabul penceresinin
    /// 90 saniyeye çıkmasını <see cref="TryMatch"/>'in tek kullanım kuralı sınırlar (D11).
    /// </summary>
    internal const int SkewSteps = 1;

    private static readonly int[] Powers = [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000];

    internal static long StepAt(DateTimeOffset now) => now.ToUnixTimeSeconds() / PeriodSeconds;

    /// <summary>
    /// RFC 6238 § 4 + RFC 4226 § 5.3: adım sayacının sekiz baytlık big-endian hâlinin
    /// HMAC'ı alınır, son baytın alt dört biti ofseti verir ve o ofsetten okunan 31 bit
    /// hane sayısına indirgenir.
    /// </summary>
    internal static string Compute(byte[] secret, long step)
    {
        byte[] counter = BitConverter.GetBytes(step);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }

        byte[] mac = HMACSHA1.HashData(secret, counter);

        int offset = mac[^1] & 0x0F;
        int binary = ((mac[offset] & 0x7F) << 24)
            | (mac[offset + 1] << 16)
            | (mac[offset + 2] << 8)
            | mac[offset + 3];

        return (binary % Powers[Digits]).ToString(
            CultureInfo.InvariantCulture.NumberFormat).PadLeft(Digits, '0');
    }

    /// <summary>
    /// Girilen kodu tolerans penceresi içinde arar ve eşleşen adımı döndürür.
    ///
    /// <paramref name="lastAcceptedStep"/>'e **eşit veya daha küçük** adımlar hiç
    /// denenmez: kabul edilmiş bir kod ikinci kez kabul edilmez (D11). Bu, kurtarma
    /// kodunun tek kullanımlığının (R5) zamana bağlı koddaki karşılığıdır ve toleransın
    /// açtığı 90 saniyelik pencereyi kapatan şeydir.
    ///
    /// Karşılaştırma SABİT ZAMANLIDIR: sıradan bir dize karşılaştırması ilk farklı
    /// haneden döner ve doğru kodun ne kadarının tutduğunu zamanlamayla sızdırır
    /// (`PasswordHash.Matches` ile aynı gerekçe).
    /// </summary>
    internal static bool TryMatch(
        byte[] secret,
        string? code,
        long currentStep,
        long lastAcceptedStep,
        out long matchedStep)
    {
        matchedStep = 0;

        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        string girilen = code.Trim();

        if (girilen.Length != Digits)
        {
            return false;
        }

        bool eslesti = false;

        for (long step = currentStep - SkewSteps; step <= currentStep + SkewSteps; step++)
        {
            if (step <= lastAcceptedStep)
            {
                continue;
            }

            if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(Compute(secret, step)),
                System.Text.Encoding.ASCII.GetBytes(girilen)))
            {
                continue;
            }

            // Döngü KIRILMIYOR: erken çıkış, kaçıncı adımda eşleştiğini harcanan süreyle
            // sızdırır ve bir saldırgana cihaz saatinin yönünü söylerdi.
            eslesti = true;
            matchedStep = step;
        }

        return eslesti;
    }
}
