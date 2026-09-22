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

    internal static void Apply(IWebHostBuilder builder)
    {
        builder.UseSetting("Identity:SeedAdmin:LoginId", SeedAdminLoginId);
        builder.UseSetting("Identity:SeedAdmin:Password", SeedAdminPassword);
        builder.UseSetting("Identity:SeedAdmin:DisplayName", "Kurulum Yöneticisi");
        builder.UseSetting("Identity:PasswordIterations", "1000");
    }
}
