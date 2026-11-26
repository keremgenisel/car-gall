using CarGallery.Host.Seo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0004` **D2** · **P1** — sitenin kökü tek bir dağıtım değeridir ve D2'nin kuralına
/// uymayan değerle uygulama başlamaz.
/// </summary>
public sealed class SiteKokuYapilandirmasiTests
{
    [Theory(DisplayName = "0004/AC-22: Kök adres yapılandırması boş, `/galeri`, `galeri.com`, `ftp://x.test` ya da `https://x.test/yol` iken uygulama başlamaz ve hata anahtarın adını söyler.")]
    [InlineData("")]
    [InlineData("/galeri")]
    [InlineData("galeri.com")]
    [InlineData("ftp://x.test")]
    [InlineData("https://x.test/yol")]
    public void Baslamaz_KokAdresGecersizken(string deger)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
                IdentityTestSettings.Apply(builder);
                NotificationsTestSettings.Apply(builder);
                MediaTestSettings.Apply(builder);
                builder.UseSetting(SiteRoot.ConfigurationKey, deger);
            });

        InvalidOperationException hata = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("Notifications:PublicBaseUrl", hata.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://x.test/", "https://x.test")]
    [InlineData("https://x.test", "https://x.test")]
    [InlineData("http://localhost:5080/", "http://localhost:5080")]
    public void Normalize_SondakiEgikCizgi(string deger, string beklenen) =>
        Assert.Equal(beklenen, SiteRoot.Parse(deger).Value);
}
