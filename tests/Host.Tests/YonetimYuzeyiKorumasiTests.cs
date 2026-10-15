using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CarGallery.Host.Tests;

/// <summary>
/// C18/AC-23 — **yönetim rota kökü altındaki HER endpoint oturum gerektirir** ve bu,
/// tek tek uçları sayan bir testle değil, rotaları TARAYAN bir testle doğrulanır.
///
/// Ayrım önemlidir: uçları elle sayan bir test, bir sonraki panel spec'i yeni bir uç
/// eklediğinde onu görmez ve yeşil kalır. Intent'in başarı ölçüsü "korumasız kalmış tek
/// bir yönetim yüzeyi kalmaması"dır; sayılamayan bir ifade hiçbir testi kırmaz.
/// </summary>
public sealed partial class YonetimYuzeyiKorumasiTests(HostFactory factory) : IClassFixture<HostFactory>
{
    [Fact(DisplayName = "0006/AC-23: Yönetim rota kökü altındaki her endpoint, oturumsuz istekte `401` döner; bu, rotaları tarayan bir testle doğrulanır")]
    public async Task Doner401_YonetimKokuAltindakiHerUc()
    {
        HttpClient client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        IReadOnlyList<(string Method, string Path)> uclar = YonetimUclari();

        // Tarama BOŞ geçmemeli: hiç uç bulamayan bir tarama da "hepsi korumalı" der.
        Assert.NotEmpty(uclar);

        List<string> korumasiz = [];

        foreach ((string method, string path) in uclar)
        {
            HttpRequestMessage istek = new(new HttpMethod(method), path);

            if (method is "POST" or "PUT" or "PATCH")
            {
                istek.Content = JsonContent.Create(new { });
            }

            HttpResponseMessage yanit = await client.SendAsync(istek);

            if (yanit.StatusCode != HttpStatusCode.Unauthorized)
            {
                korumasiz.Add($"{method} {path} → {(int)yanit.StatusCode}");
            }
        }

        Assert.True(
            korumasiz.Count == 0,
            $"Yönetim kökü altındaki her uç oturum gerektirmeli (C18):{Environment.NewLine}{string.Join(Environment.NewLine, korumasiz)}");
    }

    /// <summary>
    /// Giriş ucu kökün DIŞINDADIR ve oturumsuz çalışmak zorundadır; taramanın onu
    /// yanlışlıkla kapsamadığını da doğruluyoruz.
    /// </summary>
    [Fact(DisplayName = "0006/AC-23: Yönetim rota kökü altındaki her endpoint, oturumsuz istekte `401` döner; bu, rotaları tarayan bir testle doğrulanır")]
    public void KapsamazGirisUcunu_Tarama()
    {
        Assert.DoesNotContain(YonetimUclari(), uc => uc.Path.StartsWith("/api/identity", StringComparison.Ordinal));
    }

    /// <summary>
    /// `0013` D42/AC-31 — **oturumsuz çalışan uçlar ADIYLA sayılır.** `0013` bu köke iki
    /// uç daha ekledi (ikinci adım doğrulaması ve ara durumun silinmesi) ve ikisi de C14
    /// gereği yönetim kökünün dışında. Beyaz listeyi büyütmek yerine listeyi KAPI yapmak,
    /// bir sonraki spec'in sessizce üçüncü bir uç açmasını engelliyor: listede olmayan her
    /// oturumsuz uç bu testi kırar.
    ///
    /// Liste kısalmaz da: bir uç kaldırıldığında da kırmızı verir ve o zaman kaldırmanın
    /// bilinçli olduğu burada yazılır.
    /// </summary>
    [Fact(DisplayName = "0013/AC-31: Kapılı oturumda izin verilenler dışındaki her yönetim isteği `403` ve `identity.two_factor_setup_required` ile reddedilir; oturumsuz çalışan uçların **adıyla sayıldığı** rota kapısı yeşil kalır")]
    public void Sayar_OturumsuzCalisanUclari()
    {
        string[] izinliler =
        [
            "POST /api/identity/sessions",
            "POST /api/identity/sessions/two-factor",
            "DELETE /api/identity/sessions/pending",
        ];

        EndpointDataSource kaynak = factory.Services.GetRequiredService<EndpointDataSource>();
        List<string> oturumsuzlar = [];

        foreach (Endpoint endpoint in kaynak.Endpoints)
        {
            if (endpoint is not RouteEndpoint route)
            {
                continue;
            }

            string path = $"/{route.RoutePattern.RawText?.TrimStart('/')}";

            if (!path.StartsWith("/api/identity", StringComparison.Ordinal))
            {
                continue;
            }

            // Politika taşıyan uç oturum istiyor; sayılacaklar politikasız olanlar.
            if (endpoint.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>() is not null)
            {
                continue;
            }

            foreach (string method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
            {
                oturumsuzlar.Add($"{method} {path}");
            }
        }

        Assert.Equal([.. izinliler.Order(StringComparer.Ordinal)], [.. oturumsuzlar.Order(StringComparer.Ordinal)]);
    }

    /// <summary>
    /// Rota parametreleri yer tutucuyla doldurulur; aksi hâlde `{staffId:guid}` gibi bir
    /// yol hiç eşleşmez ve test korumasız bir ucu 404 sanıp atlardı.
    /// </summary>
    private IReadOnlyList<(string Method, string Path)> YonetimUclari()
    {
        EndpointDataSource kaynak = factory.Services.GetRequiredService<EndpointDataSource>();
        List<(string, string)> uclar = [];

        foreach (Endpoint endpoint in kaynak.Endpoints)
        {
            if (endpoint is not RouteEndpoint route)
            {
                continue;
            }

            string path = $"/{route.RoutePattern.RawText?.TrimStart('/')}";

            if (!path.StartsWith(AdminSurface.RoutePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            string somut = RotaParametresi().Replace(path, Guid.Empty.ToString());

            foreach (string method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
            {
                uclar.Add((method, somut));
            }
        }

        return uclar;
    }

    [GeneratedRegex(@"\{[^}]+\}")]
    private static partial Regex RotaParametresi();
}
