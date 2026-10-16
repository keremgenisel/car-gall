using System.Security.Cryptography;
using System.Text;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// Kurtarma kodunun METİN biçimi. Kod **elle yazılacaktır** — personel onu bir yere
/// kaydedip aylar sonra okuyacak (D31: yalnız ekranda, bir kez) — bu yüzden biçim
/// okunabilirliğe göre seçildi: Base32 alfabesi (`0/O` ve `1/I` karışmaz), dörtlü
/// gruplar ve 80 bit entropi.
///
/// Girişte gruplama ve küçük harf **tolere edilir**: personelin tireleri birebir
/// yazmasını beklemek, kodun tek kullanımlığını bir yazım hatasına bağlamak olurdu.
/// </summary>
internal static class RecoveryCodeText
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>16 simge × 5 bit = 80 bit. Dört dörtlü grup hâlinde gösterilir.</summary>
    private const int Symbols = 16;

    private const int GroupSize = 4;

    internal static string Generate()
    {
        StringBuilder builder = new(Symbols + (Symbols / GroupSize) - 1);

        for (int index = 0; index < Symbols; index++)
        {
            if (index > 0 && index % GroupSize == 0)
            {
                builder.Append('-');
            }

            builder.Append(Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Özetleme ve karşılaştırma HER ZAMAN bu biçim üzerinden yapılır: saklanan özet
    /// normalize edilmiş metnin özetidir, personelin tuş vuruşlarının değil.
    /// </summary>
    internal static string Normalize(string? value) => (value ?? string.Empty)
        .Replace("-", string.Empty, StringComparison.Ordinal)
        .Replace(" ", string.Empty, StringComparison.Ordinal)
        .Trim()
        .ToUpperInvariant();

    /// <summary>
    /// Normalize edilmiş bir girdinin bu biçimde bir kurtarma kodu OLABİLECEĞİ.
    ///
    /// Soru "doğru mu" değil, "aramaya değer mi": doğrulama yolu bir kodu saklanan on
    /// özetin hepsiyle karşılaştırmak zorunda (tuz hesaba özel) ve altı haneli bir zamana
    /// bağlı kod bu biçime <b>asla</b> uymaz. Biçim bilgisi burada yaşıyor; elemeyi
    /// çağıranın içine yazmak onu ikinci bir yerde yeniden tanımlamak olurdu.
    /// </summary>
    internal static bool IsWellFormed(string normalized) =>
        normalized.Length == Symbols
        && normalized.All(symbol => Alphabet.Contains(symbol, StringComparison.Ordinal));
}
