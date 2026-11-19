using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0018` C14 — bülten yapılandırmasının herhangi bir değeri eksikse uygulama AYAĞA KALKMAZ.
/// Koda gömülü bir varsayılan olsaydı değer boşaltıldığında uygulama sessizce o varsayılanla
/// açılır ve bu testler yeşil kalmazdı (`0009` `BildirimYapilandirmasiTests` deseni).
/// </summary>
public sealed class BultenYapilandirmasiTests
{
    [Fact(DisplayName = "0018/AC-50: Çıkış bağlantısının imza sırrı yapılandırmada yokken uygulama ayağa kalkmaz")]
    public void Kalkmaz_ImzaSirriYokken() =>
        Assert.Contains("newsletter_signing_key_missing", Baslatmayi("Notifications:Newsletter:UnsubscribeSigningKey").Message, StringComparison.Ordinal);

    [Theory(DisplayName = "0018/AC-97: Dönem günü/saati, teyit bağlantısı süresi, üç hız sınırının (kaba istek, geçerli istek, adres başına teyit iletisi) eşik ve pencerelerinden ya da çıkış bağlantısı sırrından biri yapılandırmada yokken uygulama ayağa kalkmaz")]
    [InlineData("Notifications:Newsletter:SendDayOfWeek")]
    [InlineData("Notifications:Newsletter:SendTimeUtc")]
    [InlineData("Notifications:Newsletter:ConfirmationLinkHours")]
    [InlineData("Notifications:Newsletter:RequestLimit")]
    [InlineData("Notifications:Newsletter:RequestWindowMinutes")]
    [InlineData("Notifications:Newsletter:ValidRequestLimit")]
    [InlineData("Notifications:Newsletter:ValidRequestWindowMinutes")]
    [InlineData("Notifications:Newsletter:AddressConfirmationLimit")]
    [InlineData("Notifications:Newsletter:AddressWindowHours")]
    [InlineData("Notifications:Newsletter:UnsubscribeSigningKey")]
    public void Kalkmaz_BirDegerYokken(string anahtar) =>
        Assert.Contains("Notifications", Baslatmayi(anahtar).Message, StringComparison.Ordinal);

    /// <summary>D25 — imza sırrı yapılandırma DOSYASINDA durmaz; ortam değişkeninden gelir.</summary>
    [Fact]
    public void IcermezImzaSirri_Yapilandirma()
    {
        string appsettings = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Host", "appsettings.json"));

        Assert.Contains("\"Newsletter\"", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("UnsubscribeSigningKey", appsettings, StringComparison.Ordinal);
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
                builder.UseSetting(anahtar, string.Empty);
            });

        return Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
    }
}
