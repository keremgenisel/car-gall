using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// D3/AC-49 — **şifreleme anahtarı dağıtım yapılandırmasından gelir ve eksikken uygulama
/// ayağa kalkmaz.**
///
/// `0006` kurulum parolası için (D2/AC-53), `0009` bildirim adresi için (AC-27), `0011`
/// depolama kimliği için aynı kararı verdi. Gerekçe dördünde de birebir aynı: üretilmiş
/// bir varsayılan, eksikliği GÖRÜNMEZ kılar — ve burada görünmez eksiklik, doğrulama
/// sırlarının şifresiz saklanması olurdu.
///
/// Doğrulama DI kaydında, veritabanına gidilmeden önce yapılıyor; test de tam olarak
/// bunu iddia ediyor.
/// </summary>
public sealed class IkinciAdimYapilandirmasiTests
{
    [Fact(DisplayName = "0013/AC-49: Şifreleme anahtarı dağıtım yapılandırmasından gelir: anahtar yoksa uygulama **ayağa kalkmaz** ve anahtar yapılandırma dosyasında bulunmaz")]
    public void Kalkmaz_AnahtarYokken() =>
        Assert.Contains("TwoFactor:EncryptionKey", Baslatmayi(string.Empty).Message, StringComparison.Ordinal);

    [Theory(DisplayName = "0013/AC-49: Şifreleme anahtarı dağıtım yapılandırmasından gelir: anahtar yoksa uygulama **ayağa kalkmaz** ve anahtar yapılandırma dosyasında bulunmaz")]
    [InlineData("bu-base64-degil!!")]
    [InlineData("a2lzYS1hbmFodGFy")]
    public void Kalkmaz_BozukAnahtarla(string anahtar) =>
        Assert.Contains("TwoFactor:EncryptionKey", Baslatmayi(anahtar).Message, StringComparison.Ordinal);

    /// <summary>
    /// Sır repoda DURMAZ (`docs/conventions.md` § 11, ADR `0011`): anahtar yapılandırma
    /// DOSYASINDA hiç bulunmaz, koşuyu başlatandan gelir. Dosyada yalnız sır OLMAYAN
    /// ayarlar durur (kurulum adı, kod sayısı, süreler).
    /// </summary>
    [Fact(DisplayName = "0013/AC-49: Şifreleme anahtarı dağıtım yapılandırmasından gelir: anahtar yoksa uygulama **ayağa kalkmaz** ve anahtar yapılandırma dosyasında bulunmaz")]
    public void IcermezAnahtari_Yapilandirma()
    {
        string appsettings = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Host", "appsettings.json"));

        Assert.Contains("\"TwoFactor\"", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("EncryptionKey", appsettings, StringComparison.Ordinal);
    }

    private static InvalidOperationException Baslatmayi(string anahtar)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
                IdentityTestSettings.Apply(builder);
                NotificationsTestSettings.Apply(builder);
                MediaTestSettings.Apply(builder);

                // Yalnız ANAHTAR bozulur: başka bir eksiklik testi yanlış sebeple yeşil
                // bırakırdı (`KurulumTests` aynı tuzağı yazıyor).
                builder.UseSetting("Identity:TwoFactor:EncryptionKey", anahtar);
            });

        return Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }
}
