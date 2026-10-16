using Microsoft.AspNetCore.Hosting;

namespace CarGallery.Host.Tests;

/// <summary>
/// AC-53 gereği uygulama başlangıç Yönetici parolası olmadan BAŞLAMAZ. Bu, 0006'nın
/// tüm ortamların başlangıç sözleşmesini değiştirdiği anlamına gelir: her test fabrikası
/// da bu ayarı vermek zorundadır.
///
/// Parola özetleme maliyeti testlerde düşük: her giriş için 210.000 iterasyon beklemek
/// test süresini gereksiz uzatır. Üretim değeri `appsettings.json`'dadır ve testin
/// doğruladığı şey maliyetin kendisi değil, davranıştır.
/// </summary>
internal static class IdentityTestSettings
{
    internal const string SeedAdminLoginId = "kurulum@ornek.com";
    internal const string SeedAdminPassword = "kurulum-parolasi-1";

    /// <summary>
    /// 0013 D3 — şifreleme anahtarı testlerde de YAPILANDIRMADAN gelir; burada üretilmesi
    /// onu bir sır yapmıyor: bu anahtar yalnız test veritabanındaki satırları koruyor ve
    /// üretim değeri dağıtım yapılandırmasındadır (`docs/conventions.md` § 11).
    /// Dize olarak gömmek yerine üretiliyor — depoda duran bir anahtar dizesi, bir gün
    /// kopyalanıp üretimde kullanılabilecek bir dizedir (ADR `0011`'in aynı dersi).
    /// </summary>
    internal static readonly string TwoFactorEncryptionKey =
        Convert.ToBase64String([.. Enumerable.Range(0, 32).Select(index => (byte)index)]);

    internal static void Apply(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:SeedAdmin:LoginId", SeedAdminLoginId);
        builder.UseSetting("Identity:SeedAdmin:Password", SeedAdminPassword);
        builder.UseSetting("Identity:SeedAdmin:DisplayName", "Kurulum Yöneticisi");
        builder.UseSetting("Identity:PasswordIterations", "1000");

        // 0013 D3/AC-49 — anahtar olmadan uygulama ayağa KALKMAZ, yani her test fabrikası
        // onu vermek zorunda. Kurulum parolasıyla (AC-53) aynı sözleşme.
        builder.UseSetting("Identity:TwoFactor:EncryptionKey", TwoFactorEncryptionKey);

        // Kurtarma kodu özetinin maliyeti de testlerde düşük: on kodu üretim değeriyle
        // özetlemek her kurulumda saniyeler eklerdi (D2'nin ölçümü).
        builder.UseSetting("Identity:TwoFactor:RecoveryCodeIterations", "1000");
    }
}
