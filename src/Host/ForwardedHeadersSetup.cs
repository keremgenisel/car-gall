using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace CarGallery.Host;

/// <summary>
/// Ters proxy arkasında ziyaretçinin GERÇEK adresi. Bu, 0005'in hız sınırının doğru
/// bölünmesi için gerekli: proxy arkasında <c>RemoteIpAddress</c> proxy'nin adresidir ve
/// o zaman C17'nin "aynı IP'den 5 talep"i TÜM ziyaretçiler için toplam 5 talep olur —
/// altıncı ziyaretçi, dünyanın öbür ucundan gelse bile reddedilir.
///
/// **Varsayılan: KAPALI.** Yapılandırmada proxy listelenmemişse ara katman hiç
/// eklenmez ve <c>X-Forwarded-For</c> yok sayılır — bugünkü davranışın birebir aynısı.
/// Güvenilmeyen bir başlığa körü körüne güvenmek, sınırı istemcinin kapatabilmesi
/// demektir; bu yüzden güven AÇIKÇA listelenen adreslerle sınırlıdır.
///
/// Yapılandırma (`ForwardedHeaders:KnownProxies`, `ForwardedHeaders:KnownNetworks`):
/// <code>
/// "ForwardedHeaders": {
///   "KnownProxies": ["10.0.0.7"],
///   "KnownNetworks": ["10.0.0.0/8"]
/// }
/// </code>
/// </summary>
internal static class ForwardedHeadersSetup
{
    internal const string SectionName = "ForwardedHeaders";

    internal static ForwardedHeadersOptions? Build(IConfiguration configuration)
    {
        string[] proxies = configuration.GetSection($"{SectionName}:KnownProxies").Get<string[]>() ?? [];
        string[] networks = configuration.GetSection($"{SectionName}:KnownNetworks").Get<string[]>() ?? [];

        if (proxies.Length == 0 && networks.Length == 0)
        {
            return null;
        }

        ForwardedHeadersOptions options = new()
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor,
        };

        // Varsayılan listeler loopback içerir; açıkça yazılmayan hiçbir kaynağa
        // güvenilmesin diye temizleniyor.
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();

        foreach (string proxy in proxies)
        {
            if (!IPAddress.TryParse(proxy, out IPAddress? address))
            {
                throw new InvalidOperationException(
                    $"{SectionName}:KnownProxies içinde geçersiz IP adresi: '{proxy}'.");
            }

            options.KnownProxies.Add(address);
        }

        foreach (string network in networks)
        {
            string[] parts = network.Split('/');

            if (parts.Length != 2
                || !IPAddress.TryParse(parts[0], out IPAddress? prefix)
                || !int.TryParse(parts[1], out int length))
            {
                throw new InvalidOperationException(
                    $"{SectionName}:KnownNetworks içinde geçersiz CIDR: '{network}'.");
            }

            options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, length));
        }

        return options;
    }
}
