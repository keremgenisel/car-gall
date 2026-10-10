using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// C6 — medya yapılandırması KURULUŞTA doğrulanır; çalışma anında patlamaz.
///
/// <para><b>Neden uygulama hiç ayağa kalkmıyor:</b> filigran kaynağı yapılandırılmamışsa
/// her fotoğraf filigransız çıkar ve galeri sahibinin satın aldığı tek garanti sessizce
/// kaybolur — sessizliği fotoğraflar rakip sitede göründüğünde fark edilir ve o noktada
/// bütün türevlerin yeniden üretilmesi gerekir. Depolama kimliği eksikse her yükleme
/// çalışma anında patlar; taban adres eksikse türevin erişim yolu üretilemez.</para>
///
/// <para>`0006` kurulum parolası için (D2/AC-53), `0009` bildirim adresi için (AC-27) aynı
/// kararı verdi. Gerekçe üçünde de birebir aynı: üretilmiş bir varsayılan, eksikliği
/// GÖRÜNMEZ kılar.</para>
/// </summary>
public sealed class MedyaYapilandirmasiTests
{
    [Theory(DisplayName = "0011/AC-43: Depolama kimlik bilgileri eksikken uygulama ayağa kalkmaz")]
    [InlineData("Media:Storage:AccessKey")]
    [InlineData("Media:Storage:SecretKey")]
    public void Kalkmaz_DepolamaKimlikBilgileriYokken(string anahtar) =>
        Assert.Contains("Media", Baslatmayi(anahtar).Message, StringComparison.Ordinal);

    [Fact(DisplayName = "0011/AC-44: Erişim taban adresi eksikken uygulama ayağa kalkmaz")]
    public void Kalkmaz_ErisimTabanAdresiYokken() =>
        Assert.Contains("Media", Baslatmayi("Media:PublicBaseUrl").Message, StringComparison.Ordinal);

    [Fact(DisplayName = "0011/AC-45: Filigran kaynağı yapılandırılmamışken uygulama ayağa kalkmaz")]
    public void Kalkmaz_FiligranKaynagiYapilandirilmamisken() =>
        Assert.Contains("Media", Baslatmayi("Media:WatermarkObjectKey").Message, StringComparison.Ordinal);

    /// <summary>
    /// Sınırlar ve depolama adresi de YAPILANDIRMADAN gelir; biri boşaltıldığında kodda
    /// yerini alacak üretilmiş bir varsayılan YOKTUR. Koda gömülü bir sabit olsaydı bu
    /// test yeşil kalmazdı.
    /// </summary>
    [Theory]
    [InlineData("Media:Storage:ServiceUrl")]
    [InlineData("Media:Storage:Bucket")]
    [InlineData("Media:Storage:Region")]
    public void Gelir_YapilandirmadanDepolamaAdresi(string anahtar) =>
        Assert.Contains("Media", Baslatmayi(anahtar).Message, StringComparison.Ordinal);

    /// <summary>
    /// Sır repoda DURMAZ (`docs/conventions.md` § 11): depolama anahtarı ve parolası
    /// yapılandırma DOSYASINDA hiç bulunmaz, ortam değişkeninden gelir.
    /// </summary>
    [Fact]
    public void IcermezDepolamaSirrini_Yapilandirma()
    {
        string appsettings = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Host", "appsettings.json"));

        Assert.Contains("\"Media\"", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessKey", appsettings, StringComparison.Ordinal);
        Assert.DoesNotContain("SecretKey", appsettings, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tek bir anahtarı boşaltıp uygulamayı kurmayı dener. Veritabanına ve depoya hiç
    /// gidilmez: doğrulama DI kaydında, bağlantı açılmadan önce yapılıyor — iddia edilen
    /// de bu.
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
