using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// ADR 0012 / K4 — TLS'i ters proxy'nin sonlandırdığı kurulumda personel GİREBİLİR.
///
/// Oturum çerezi `Secure`, yani canlıda HTTPS zorunlu; standart kurulumda HTTPS proxy'de
/// biter ve uygulama isteği `http` olarak görür. Yönetim yüzeyinin köken koruması
/// beklenen kökeni `Request.Scheme`'den kurduğu için tarayıcının `https://` Origin'i
/// eşleşmiyor ve GİRİŞ DÂHİL her yazma isteği 403 alıyordu. Hiçbir test bunu görmüyordu,
/// çünkü test sunucusu da tarayıcı da aynı şemayı konuşuyor. Dağıtım imajı ilk kez
/// proxy arkasına konunca ölçüldü.
///
/// İkinci test güven sınırının KORUNDUĞUNU sınar ve bunu ara katman AÇIKKEN yapar: bir
/// proxy listelidir ama istek başka bir adresten gelir. Ara katmanın hiç eklenmediği hâli
/// (liste boş) sınamak yetmezdi — o yalnız "kapalı" anahtarını ölçer ve `KnownProxies`'in
/// varsayılan loopback girdisini temizleyen satır silinse bile yeşil kalırdı (QA, PR #101).
/// </summary>
public sealed class TersProxyArkasindaGirisTests : IAsyncLifetime
{
    private const string Kok = "https://galeri.example";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());
    }

    public async Task DisposeAsync() => await container.DisposeAsync();

    [Fact(DisplayName = "ADR 0012/K4: Listelenmiş proxy https iletirse tarayıcının https kökeninden gelen giriş isteği köken korumasına takılmaz")]
    public async Task Girilir_ListelenmisProxyHttpsIletince()
    {
        await using WebApplicationFactory<Program> factory = Fabrika(listelenenProxy: IstegiGonderen);

        HttpResponseMessage yanit = await factory.CreateClient().SendAsync(GirisIstegi());

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    [Fact(DisplayName = "ADR 0012/K4: Listede olmayan bir adresten gelen X-Forwarded-Proto okunmaz ve köken koruması isteği reddeder")]
    public async Task Reddedilir_IstekListedeOlmayanAdrestenGelince()
    {
        await using WebApplicationFactory<Program> factory = Fabrika(listelenenProxy: BaskaProxy);

        HttpResponseMessage yanit = await factory.CreateClient().SendAsync(GirisIstegi());

        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
    }

    private WebApplicationFactory<Program> Fabrika(IPAddress listelenenProxy) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);

            builder.UseSetting("ForwardedHeaders:KnownProxies:0", listelenenProxy.ToString());

            // Test sunucusunda bağlantının uzak adresi YOK; proxy'nin adresi boru hattının
            // en başında verilir ki ileri başlık ara katmanı onu listedekiyle karşılaştırabilsin.
            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter>(new ProxyAdresiFiltresi()));
        });

    private static HttpRequestMessage GirisIstegi()
    {
        HttpRequestMessage istek = new(HttpMethod.Post, "/api/identity/sessions")
        {
            Content = JsonContent.Create(new
            {
                loginId = IdentityTestSettings.SeedAdminLoginId,
                password = IdentityTestSettings.SeedAdminPassword,
            }),
        };

        // Tarayıcının gördüğü: https kökü. Proxy'nin ilettiği: aynı Host + şema başlığı.
        istek.Headers.Host = new Uri(Kok).Host;
        istek.Headers.Add("Origin", Kok);
        istek.Headers.Add("X-Forwarded-Proto", "https");

        return istek;
    }

    /// <summary>İsteğin geldiği bağlantı adresi — her iki testte de aynı.</summary>
    private static readonly IPAddress IstegiGonderen = IPAddress.Parse("10.0.0.7");

    private static readonly IPAddress BaskaProxy = IPAddress.Parse("10.0.0.8");

    private sealed class ProxyAdresiFiltresi : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((HttpContext context, Func<Task> devam) =>
            {
                context.Connection.RemoteIpAddress = IstegiGonderen;
                return devam();
            });

            next(app);
        };
    }
}
