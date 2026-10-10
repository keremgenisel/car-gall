using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// C9 — bildirim yapılandırması KURULUŞTA doğrulanır; çalışma anında patlamaz.
///
/// <para><b>Neden uygulama hiç ayağa kalkmıyor:</b> bildirim adresi yapılandırılmamışsa
/// her talep sessizce kimseye gitmez. O sessizlik tam olarak bu spec'in varlık nedeni —
/// ve bir varsayılan üretmek onu görünmez kılardı. 0006 aynı kararı kurulum parolası için
/// verdi (D2/AC-53) ve gerekçe birebir aynı.</para>
///
/// <para>AC-27 ve AC-28, AC-26'nın saydığı anahtarların bir bölümünü tekrar eder; bu
/// spec'in kendi yapısıdır. Her kriterin kendi testi var, çünkü biri kalkarsa diğerinin
/// kapısı da sessizce açılmış olmamalı.</para>
/// </summary>
public sealed class BildirimYapilandirmasiTests
{
    [Theory(DisplayName = "0009/AC-26: Kurulum bildirim adresi, taban URL, yeniden deneme sayısı ve gönderim zaman aşımı dağıtım yapılandırmasından okunur; hiçbiri koda gömülü değildir")]
    [InlineData("Notifications:SetupNotificationAddress")]
    [InlineData("Notifications:PublicBaseUrl")]
    [InlineData("Notifications:MaximumAttempts")]
    [InlineData("Notifications:SendTimeoutSeconds")]
    public void Gelir_YapilandirmadanDortDeger(string anahtar)
    {
        // Dördü de YAPILANDIRMADAN geliyorsa, biri boşaltıldığında kodda yerini alacak
        // üretilmiş bir varsayılan YOKTUR ve uygulama başlamayı reddeder. Koda gömülü bir
        // sabit olsaydı bu test yeşil kalmazdı.
        Assert.Contains("Notifications", Baslatmayi(anahtar).Message, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "0009/AC-27: Kurulum bildirim adresi veya taban URL yapılandırmada yokken uygulama ayağa kalkmaz")]
    [InlineData("Notifications:SetupNotificationAddress")]
    [InlineData("Notifications:PublicBaseUrl")]
    public void Kalkmaz_BildirimAdresiVeyaTabanUrlYokken(string anahtar) =>
        Assert.Contains("Notifications", Baslatmayi(anahtar).Message, StringComparison.Ordinal);

    [Theory(DisplayName = "0009/AC-28: Sağlayıcı kimlik bilgileri yapılandırmada yokken uygulama ayağa kalkmaz")]
    [InlineData("Notifications:Smtp:User")]
    [InlineData("Notifications:Smtp:Password")]
    public void Kalkmaz_SaglayiciKimlikBilgileriYokken(string anahtar) =>
        Assert.Contains("Notifications", Baslatmayi(anahtar).Message, StringComparison.Ordinal);

    /// <summary>
    /// Sır repoda DURMAZ (docs/conventions.md § 11): SMTP kullanıcı adı ve parolası
    /// yapılandırma DOSYASINDA hiç bulunmaz, ortam değişkeninden gelir. Kriter değil,
    /// kuralın kapısı.
    /// </summary>
    [Fact]
    public void IcermezSaglayiciSirri_Yapilandirma()
    {
        string appsettings = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Host", "appsettings.json"));

        Assert.Contains("\"Notifications\"", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("\"User\"", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Password\"", appsettings, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tek bir anahtarı boşaltıp uygulamayı kurmayı dener. Veritabanına hiç gidilmez:
    /// doğrulama DI kaydında, bağlantı açılmadan önce yapılıyor — zaten iddia edilen de bu.
    /// </summary>
    private static InvalidOperationException Baslatmayi(string anahtar)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
                IdentityTestSettings.Apply(builder);
                NotificationsTestSettings.Apply(builder);
                MediaTestSettings.Apply(builder);
                builder.UseSetting(anahtar, string.Empty);
            });

        return Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }
}
