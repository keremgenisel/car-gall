namespace CarGallery.Modules.Identity.Tests;

/// <summary>
/// 0013 D3 — testlerin şifreleme anahtarı. Dize olarak GÖMÜLMÜYOR: depoda duran bir
/// anahtar dizesi, bir gün kopyalanıp üretimde kullanılabilecek bir dizedir (ADR `0011`
/// aynı dersi kurulum parolası için vermişti). Bu anahtar yalnız test veritabanındaki
/// satırları koruyor; üretim değeri dağıtım yapılandırmasından gelir.
/// </summary>
internal static class IkinciAdimTestAnahtari
{
    internal static readonly string Base64 =
        Convert.ToBase64String([.. Enumerable.Range(0, 32).Select(index => (byte)index)]);
}
