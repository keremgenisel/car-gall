using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// R6'nın İKİNCİ yarısı: giriş ucu ayrıca KAYNAK IP bazlı hız sınırına tabidir.
///
/// Hesap kilidinden farklıdır ve ikisi karıştırılmamalı: kilit hesap başınadır, doğru
/// parolayı da reddeder ve ayırt edilemez bir karşılık verir; hız sınırı kaynak başınadır
/// ve isteği hiç işlemeden keser.
///
/// Sınır BORU HATTINDA yaşıyor; modül yalnız politikayı tanımlıyor. `UseRateLimiter()`
/// silinirse sessizce kaybolur — bu test onu gerçek boru hattında sınıyor.
/// </summary>
public sealed class GirisHizSiniriTests : IAsyncLifetime
{
    private const int Esik = 3;

    private readonly PostgreSqlContainer container =
        new PostgreSqlBuilder("postgres:17-alpine").WithDatabase("cargallery").Build();

    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        await container.StartAsync();

        // Kurulum adımı uygulamadan ÖNCE koşar: uygulama artık şema UYGULAMAZ, yalnız
        // güncel mi diye bakar ve eksikse başlamayı reddeder (docs/decisions/0002).
        await TestSchema.ApplyAsync(container.GetConnectionString());

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);

            // Eşik YAPILANDIRMADAN gelir; test onu sabitleyip doğruluyor, koda gömülü bir
            // sabiti değil (C20).
            builder.UseSetting("Identity:LoginRequestLimit", Esik.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("Identity:LoginRequestWindowMinutes", "10");
        });

        _ = factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0006/AC-10: Giriş ucuna aynı kaynak IP'den yapılandırılan hız sınırının üzerinde istek geldiğinde sonraki istekler reddedilir")]
    public async Task Reddedilir_EsikUstuIstek()
    {
        HttpClient client = factory!.CreateClient();

        for (int istek = 0; istek < Esik; istek++)
        {
            HttpResponseMessage kabul = await GonderAsync(client);

            Assert.NotEqual(HttpStatusCode.TooManyRequests, kabul.StatusCode);
        }

        HttpResponseMessage reddedilen = await GonderAsync(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, reddedilen.StatusCode);

        JsonElement govde = JsonDocument.Parse(await reddedilen.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("identity.rate_limited", govde.GetProperty("errorCode").GetString());
    }

    [Fact(DisplayName = "0006/AC-59: Giriş ve personel yönetimi ekranlarının hiçbir hata durumunda HTTP durum kodu, hata kodu, `traceId`, endpoint yolu veya istisna metni görünmez")]
    public async Task IcermezTeknikAyrinti_HizSinirYaniti()
    {
        HttpClient client = factory!.CreateClient();

        for (int istek = 0; istek <= Esik; istek++)
        {
            await GonderAsync(client);
        }

        string govde = await (await GonderAsync(client)).Content.ReadAsStringAsync();

        foreach (string isaret in new[] { "Exception", "   at ", "Npgsql", "/api/identity", "RateLimit", "title" })
        {
            Assert.DoesNotContain(isaret, govde, StringComparison.OrdinalIgnoreCase);
        }

        // Kaynak KİŞİSEL VERİDİR: yanıtta da bulunmaz (docs/conventions.md § 12).
        Assert.DoesNotContain("127.0.0.1", govde, StringComparison.Ordinal);
        Assert.DoesNotContain("::1", govde, StringComparison.Ordinal);
    }

    private static Task<HttpResponseMessage> GonderAsync(HttpClient client) =>
        client.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = "hic-yok@ornek.com",
            password = "yanlis-parola-123",
        });
}
