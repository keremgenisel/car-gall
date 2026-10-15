using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0013` D34 — **giriş yapan dokuz zincir** ikinci adımı buradan kuruyor ve geçiyor.
///
/// <para>D28 yalnız e2e'nin on dört dosyasını saymıştı; ölçüm `Host.Tests`'te dokuz
/// dosyanın da kurulum **Yöneticisi** ile giriş yaptığını gösterdi. D30 Yönetici'yi
/// zorunlu kıldığı ve D13 kurulumsuz hesabı kapıladığı için dokuzu da `403` alıyordu.
/// Dokuz yerde ayrı ayrı yazmak aynı mantığın dokuz kopyası olurdu.</para>
///
/// <para><b>Kod burada SIFIRDAN hesaplanıyor</b> ve bu bilinçli: `Host.Tests`
/// `Identity`'nin internal tiplerini görmüyor, yani bu dosya üretim kodunun
/// **bağımsız bir ikinci uygulamasıdır**. Kümenin (HMAC-SHA1 · 6 hane · 30 sn · Base32)
/// iki ayrı uygulamada aynı sonucu vermesi, D16'nın donmuş sözleşmesini üretim kodunun
/// kendi aritmetiğine sormadan doğruluyor.</para>
/// </summary>
internal static class IkinciAdim
{
    private const int PeriyotSaniye = 30;
    private const int Hane = 6;
    private const string Alfabe = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Kapılı oturumda kurulumu tamamlar ve sırrı döndürür. Kapının izin verdiği üç
    /// yoldan biri tam olarak bu (D13).
    /// </summary>
    internal static async Task<string> KurAsync(HttpClient client, DateTimeOffset? sunucuAni = null)
    {
        HttpResponseMessage baslat = await client.PostAsJsonAsync("/api/admin/me/two-factor", new { });

        Assert.Equal(HttpStatusCode.OK, baslat.StatusCode);

        string sir = (await baslat.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("secret").GetString()!;

        HttpResponseMessage onay = await client.PostAsJsonAsync(
            "/api/admin/me/two-factor/confirm", new { code = Kod(sir, Adim(sunucuAni)) });

        Assert.Equal(HttpStatusCode.OK, onay.StatusCode);

        return sir;
    }

    /// <summary>
    /// Parola fazı + ikinci adım. Yanlış kodda değil, **tüketilmiş adımda** yeniden
    /// deniyor: D11 kabul edilen adıma eşit veya küçük olanı reddediyor, yani aynı 30
    /// saniyelik pencerede ikinci bir giriş sıradaki adımın kodunu kullanmak zorunda
    /// (D45'in yazılı sürtünmesi). Adım toleransın ötesine kayarsa penceresinin açılması
    /// beklenir — kırmızı vermek yerine beklemek, testin ölçtüğü şeyi değiştirmiyor.
    /// </summary>
    /// <param name="sunucuAni">
    /// Sunucunun saatini KONTROL EDEN testler bunu verir (`OturumBoruHattiTests` sahte
    /// bir `IClock` enjekte ediyor ve onu ileri sarıyor). Verilmezse gerçek saat
    /// kullanılır. Sahte saatte **beklemek işe yaramaz** — saat kendiliğinden ilerlemez —
    /// bu yüzden bekleme yalnız gerçek saatte yapılır.
    /// </param>
    internal static async Task GirAsync(
        HttpClient client,
        string loginId,
        string parola,
        string sir,
        DateTimeOffset? sunucuAni = null)
    {
        HttpResponseMessage parolaFazi = await client.PostAsJsonAsync(
            "/api/identity/sessions", new { loginId, password = parola });

        Assert.Equal(HttpStatusCode.OK, parolaFazi.StatusCode);

        long adim = Adim(sunucuAni);

        for (int deneme = 0; deneme < 3; deneme++)
        {
            long bekleme = sunucuAni is null ? (adim - 1 - AdimSimdi()) * PeriyotSaniye : 0;

            if (bekleme > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(bekleme + 1));
            }

            HttpResponseMessage ikinciAdim = await client.PostAsJsonAsync(
                "/api/identity/sessions/two-factor", new { code = Kod(sir, adim) });

            if (ikinciAdim.StatusCode == HttpStatusCode.OK)
            {
                return;
            }

            adim++;
        }

        Assert.Fail("İkinci adım üç denemede de geçilemedi; kod hesabı ya da tolerans bozuk.");
    }

    internal static long AdimSimdi() => Adim(null);

    private static long Adim(DateTimeOffset? an) =>
        (an ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / PeriyotSaniye;

    /// <summary>
    /// RFC 6238. Sayaç big-endian; burada `IPAddress.HostToNetworkOrder` ile yazılıyor —
    /// üretim kodu diziyi ters çevirerek yapıyor ve ikisinin aynı sonucu vermesi
    /// aritmetiğin tek bir yazımın kendi hatasına bağlı olmadığını gösteriyor.
    /// </summary>
    internal static string Kod(string sirBase32, long adim)
    {
        byte[] sayac = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(adim));
        byte[] mac = HMACSHA1.HashData(Base32Coz(sirBase32), sayac);

        int ofset = mac[^1] & 0x0F;
        int ikili = ((mac[ofset] & 0x7F) << 24)
            | (mac[ofset + 1] << 16)
            | (mac[ofset + 2] << 8)
            | mac[ofset + 3];

        return (ikili % 1_000_000).ToString(CultureInfo.InvariantCulture).PadLeft(Hane, '0');
    }

    private static byte[] Base32Coz(string deger)
    {
        List<byte> baytlar = [];
        int tampon = 0;
        int bit = 0;

        foreach (char simge in deger.Trim().ToUpperInvariant())
        {
            tampon = (tampon << 5) | Alfabe.IndexOf(simge, StringComparison.Ordinal);
            bit += 5;

            if (bit >= 8)
            {
                bit -= 8;
                baytlar.Add((byte)((tampon >> bit) & 0xFF));
            }
        }

        return [.. baytlar];
    }
}
