using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Tests.Endpoints;

/// <summary>
/// `0013` D30 + D13'ün bedeli: **Yönetici rolünde ikinci adım zorunlu** ve kurulumu
/// olmayan hesap kapılı bir oturum alıyor. Bu, modül API testlerinin tamamını
/// etkiliyordu — yedi test birden `403` aldı ve hiçbiri ikinci adımla ilgili değildi.
///
/// Çözüm D28'in deseninin aynısı: kurulum **bir kez**, ortak bir yardımcıyla yapılır.
/// Yedi yerde ayrı ayrı yazmak aynı mantığın yedi kopyası olurdu (**D34**, **D46**).
/// </summary>
internal static class IkinciAdimAkisi
{
    /// <summary>
    /// Kapılı oturumda kurulumu tamamlar ve sırrı döndürür. Kapının izin verdiği üç
    /// yoldan biri tam olarak bu (D13) — kurulumsuz personel kurulumu ancak böyle
    /// yapabilir.
    /// </summary>
    internal static async Task<string> KurAsync(HttpClient client)
    {
        HttpResponseMessage baslat = await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/two-factor", new { });

        Assert.Equal(HttpStatusCode.OK, baslat.StatusCode);

        string sir = (await baslat.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("secret").GetString()!;

        HttpResponseMessage onay = await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/two-factor/confirm", new { code = Kod(sir) });

        Assert.Equal(HttpStatusCode.OK, onay.StatusCode);

        return sir;
    }

    internal static string Kod(string sirBase32) => TimeBasedCode.Compute(
        TwoFactorSecret.FromBase32(sirBase32).Value.ToBytes(),
        TimeBasedCode.StepAt(DateTimeOffset.UtcNow));

    /// <summary>
    /// Parola fazından sonra gelen ikinci adımı geçer. Giriş yanıtı artık iki dallı
    /// (D15): oturum çerezi ancak bu çağrıdan sonra yazılıyor.
    /// </summary>
    internal static async Task IkinciAdimiGecAsync(HttpClient client, string sirBase32)
    {
        HttpResponseMessage yanit = await client.PostAsJsonAsync(
            "/api/identity/sessions/two-factor", new { code = Kod(sirBase32) });

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }
}
