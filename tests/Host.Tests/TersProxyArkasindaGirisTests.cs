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
/// İkinci test güven sınırının KORUNDUĞUNU sınar: proxy listelenmemişse istemcinin
/// gönderdiği `X-Forwarded-Proto` okunmaz.
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
        await using WebApplicationFactory<Program> factory = Fabrika(proxyListelendi: true);

        HttpResponseMessage yanit = await factory.CreateClient().SendAsync(GirisIstegi());

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    [Fact(DisplayName = "ADR 0012/K4: Proxy listelenmemişse istemcinin gönderdiği X-Forwarded-Proto okunmaz ve köken koruması isteği reddeder")]
    public async Task Reddedilir_ProxyListelenmemisken()
    {
        await using WebApplicationFactory<Program> factory = Fabrika(proxyListelendi: false);

        HttpResponseMessage yanit = await factory.CreateClient().SendAsync(GirisIstegi());

        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
    }

    private WebApplicationFactory<Program> Fabrika(bool proxyListelendi) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);

            if (proxyListelendi)
            {
                builder.UseSetting("ForwardedHeaders:KnownProxies:0", ProxyAdresi.ToString());
            }

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

    private static readonly IPAddress ProxyAdresi = IPAddress.Parse("10.0.0.7");

    private sealed class ProxyAdresiFiltresi : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((HttpContext context, Func<Task> devam) =>
            {
                context.Connection.RemoteIpAddress = ProxyAdresi;
                return devam();
            });

            next(app);
        };
    }
}
