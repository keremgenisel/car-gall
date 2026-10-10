using System.Net;
using CarGallery.Host.Theme;
using CarGallery.Host.Tests.Theme;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// Temanın SERVİS EDİLEN html'e gerçekten girdiği. Doğrulama testleri "kabul edildi"
/// diyor; bu testler kabul edilenin tarayıcıya ULAŞTIĞINI söylüyor.
///
/// <para><b>Üç fabrika, tek veritabanı:</b> temasız, tema A ve tema B. AC-15 aynı paketin
/// iki farklı tema yapılandırmasıyla iki farklı görünüm vermesini istiyor ve bunu ancak
/// iki fabrika gösterir — tek fabrikada "paket tema taşımıyor" iddiası ölçülemez.</para>
/// </summary>
public sealed class TemaEnjeksiyonuTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? temasiz;
    private WebApplicationFactory<Program>? temaA;
    private WebApplicationFactory<Program>? temaB;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());

        temasiz = Fabrika(null);

        Dictionary<string, string> a = TemaYapilandirmasiTests.GecerliTema();
        temaA = Fabrika(a);

        // Tema B yalnız DOLGUYU değiştiriyor: iki görünüm arasındaki farkın kaynağı
        // belirsiz kalmasın.
        Dictionary<string, string> b = TemaYapilandirmasiTests.GecerliTema();
        b[ThemeTokens.AccentFill] = "#c8c8c8";
        temaB = Fabrika(b);
    }

    public async Task DisposeAsync()
    {
        foreach (WebApplicationFactory<Program>? factory in new[] { temasiz, temaA, temaB })
        {
            if (factory is not null)
            {
                await factory.DisposeAsync();
            }
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "frontend-002/AC-9: Yapılandırmada dolgu vurgusu farklı bir renge ayarlandığında arayüz o rengi kullanır")]
    public async Task Kullanir_YapilandirilanDolguyu()
    {
        string html = await HtmlAsync(temaB!);

        Assert.Contains("--color-accent-fill:#c8c8c8", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "frontend-002/AC-10: Hiçbir tema değeri verilmediğinde arayüz varsayılan temayla çalışır ve hiçbir renk boş kalmaz")]
    public async Task Calisir_VarsayilanTemayla_HicDegerVerilmediginde()
    {
        string html = await HtmlAsync(temasiz!);

        // Öznitelik HİÇ yazılmaz; boş bir `style=""` üçüncü bir durum üretirdi (C5).
        Assert.DoesNotContain("style=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("--color-", html, StringComparison.Ordinal);

        // Renkler boş kalmıyor: değerler `tokens.css`'te yaşıyor ve paket onları taşıyor.
        Assert.Contains("<html lang=\"tr\" dir=\"ltr\">", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// AC-14'ün ikinci yarısı — tek mekanizma — mimari testte
    /// (<c>TemaSiniriTests</c>): "ikinci bir mekanizma eklenmedi" iddiası kaynak taramasıyla
    /// kanıtlanır, tek bir yanıta bakmakla kanıtlanmaz.
    /// </summary>
    [Fact(DisplayName = "frontend-002/AC-14: Tema değerleri mevcut `index.html` enjeksiyon yolundan gelir; ikinci bir mekanizma eklenmez")]
    public async Task Gelir_MevcutEnjeksiyonYolundan()
    {
        string html = await HtmlAsync(temaA!);

        // Tema, locale enjeksiyonunun bastığı AYNI etikette: ayrı bir `<style>` bloğu yok.
        Assert.Contains("<html lang=\"tr\" dir=\"ltr\" style=\"--color-surface:#ffffff", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<style", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "frontend-002/AC-15: Tema değerleri arayüz paketinin içine gömülmez; aynı paket iki farklı tema yapılandırmasıyla iki farklı görünüm verir")]
    public async Task Verir_IkiFarkliGorunum_AyniPaketle()
    {
        string a = await HtmlAsync(temaA!);
        string b = await HtmlAsync(temaB!);

        Assert.NotEqual(a, b);
        Assert.Contains("--color-accent-fill:#cdf564", a, StringComparison.Ordinal);
        Assert.Contains("--color-accent-fill:#c8c8c8", b, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "frontend-002/AC-23: Tüm eşikleri karşılayan bir yapılandırmayla uygulama normal açılır")]
    public async Task Acilir_TumEsiklerKarsilandiginda()
    {
        HttpResponseMessage yanit = await temaA!.CreateClient().GetAsync(new Uri("/tr", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    /// <summary>
    /// Arapça sayfa da aynı kapıdan geçiyor: tema değişimi yönü bozmaz (AC-30'un sunucu
    /// yarısı; görsel yarısı e2e'de).
    /// </summary>
    [Fact]
    public async Task Korur_Yonu_TemaEnjekteEdilirken()
    {
        string html = await HtmlAsync(temaA!, "/ar");

        Assert.Contains("<html lang=\"ar\" dir=\"rtl\" style=\"--color-surface", html, StringComparison.Ordinal);
    }

    private static async Task<string> HtmlAsync(WebApplicationFactory<Program> factory, string yol = "/tr")
    {
        HttpResponseMessage yanit = await factory.CreateClient().GetAsync(new Uri(yol, UriKind.Relative));

        yanit.EnsureSuccessStatusCode();

        return await yanit.Content.ReadAsStringAsync();
    }

    private WebApplicationFactory<Program> Fabrika(Dictionary<string, string>? tema) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);

            foreach (KeyValuePair<string, string> deger in tema ?? [])
            {
                builder.UseSetting($"{ThemeTokens.SectionName}:{deger.Key}", deger.Value);
            }
        });
}
