using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// D2/AC-53 — **başlangıç Yönetici parolası dağıtım yapılandırmasında yokken uygulama
/// başlamaz; üretilmiş bir varsayılan parolayla açılmaz.**
///
/// Bedeli bilinçli ödendi: yapılandırmada bir başlangıç parolası bulunur ve
/// değiştirilmediği sürece bilinen bir parola olarak kalır. Bu yüzden ilk girişte
/// değiştirme zorunluluğu kararın ayrılmaz parçasıdır (R10). Sessizce üretilmiş bir
/// varsayılan ise bilinen parolalı bir Yönetici hesabı demek olurdu — kimsenin haberi
/// olmadan.
/// </summary>
public sealed class KurulumTests
{
    [Fact(DisplayName = "0006/AC-53: Başlangıç Yönetici parolası dağıtım yapılandırmasında yokken uygulama başlamaz; üretilmiş bir varsayılan parolayla açılmaz")]
    public void Baslamaz_BaslangicParolasiYokken()
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));

                // Bildirim ayarları VERİLİR: verilmezse uygulama yine başlamaz ama bu kez
                // 0009/AC-28 yüzünden ve bu test yanlış sebeple yeşil kalırdı.
                NotificationsTestSettings.Apply(builder);
                MediaTestSettings.Apply(builder);
                builder.UseSetting("Identity:SeedAdmin:Password", string.Empty);
            });

        InvalidOperationException hata = Assert.Throws<InvalidOperationException>(
            () => factory.CreateClient());

        Assert.Contains("SeedAdmin", hata.Message, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "ADR 0011: Depoda yayınlanmış bir geliştirme parolasıyla uygulama başlamaz")]
    [InlineData("gelistirme-parolasi")]
    [InlineData("ci-kurulum-parolasi")]
    [InlineData("perf-kurulum-parolasi")]
    public void Baslamaz_YayinlanmisGelistirmeParolasiyla(string yayinlanmisParola)
    {
        // Depo herkese açık (ADR 0011). Bu üç dize bir süre `scripts/dev-up.sh` ve iş
        // akışlarında SABİT olarak durdu ve git geçmişinde kalıcı. Birinin bir sunucuda
        // o betiği çalıştırması, parolası KAMUYA AÇIK bir Yönetici hesabı kurması demek
        // olurdu; bu yüzden uygulama o parolalarla hiç başlamıyor.
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));

                NotificationsTestSettings.Apply(builder);
                MediaTestSettings.Apply(builder);
                builder.UseSetting("Identity:SeedAdmin:Password", yayinlanmisParola);
            });

        InvalidOperationException hata = Assert.Throws<InvalidOperationException>(
            () => factory.CreateClient());

        // Boş parolanın hatasıyla KARIŞMAMALI: ikisi ayrı sebep, ayrı mesaj.
        Assert.Contains("yayınlanmış", hata.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0006/AC-53: Başlangıç Yönetici parolası dağıtım yapılandırmasında yokken uygulama başlamaz; üretilmiş bir varsayılan parolayla açılmaz")]
    public void IcermezVarsayilanParola_Yapilandirma()
    {
        string appsettings = File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Host", "appsettings.json"));

        // Sır repoda durmaz (docs/conventions.md § 11): parola yapılandırma DOSYASINDA
        // hiç bulunmaz, ortam değişkeninden gelir.
        Assert.DoesNotContain("\"Password\"", appsettings, StringComparison.Ordinal);
        Assert.Contains("SeedAdmin", appsettings, StringComparison.Ordinal);
    }
}
