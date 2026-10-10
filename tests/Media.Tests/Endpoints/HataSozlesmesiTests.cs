using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Media.Domain;

namespace CarGallery.Media.Tests.Endpoints;

/// <summary>
/// AC-7 — reddedilen istek çevrilmiş metin değil `errorCode` döner (C7, Yasak Listesi 23).
///
/// <para>Hata gövdesi `internal` bir sınıftan üretiliyor ve `InternalsVisibleTo` ile
/// okunuyor; HTTP üzerinden sınamak aynı şeyi bir kat daha uzaktan ölçerdi. Ucun GERÇEK
/// boru hattında oturum gerektirdiği `Host.Tests`'te sınanıyor (AC-2).</para>
/// </summary>
public sealed class HataSozlesmesiTests
{
    [Theory(DisplayName = "0011/AC-8: Reddedilen istek çevrilmiş metin değil `errorCode` döner")]
    // AC-3 / AC-6 — `415` ve `413` AYRI AYRI (`docs/conventions.md` § 5, `0011` ile
    // eklendi): istemcinin yapacağı şey farklı — biri BAŞKA BİR DOSYA seçmek, diğeri
    // AYNI dosyayı küçültmek. Önceki turda ikisini `422`'de toplamıştım.
    [InlineData(MediaErrors.UnsupportedType, 415)]
    [InlineData(MediaErrors.TooLarge, 413)]
    [InlineData(MediaErrors.PixelBudgetExceeded, 422)]
    [InlineData(MediaErrors.NotRetryable, 409)]
    [InlineData(MediaErrors.NotFound, 404)]
    public void Doner_ErrorCode_CevrilmisMetinDegil(string kod, int beklenenDurum)
    {
        Dictionary<string, JsonElement> govde = Govde(new Error(kod), out int durum);

        Assert.Equal(beklenenDurum, durum);
        Assert.Equal(kod, govde["errorCode"].GetString());

        // RFC 7807'nin `title` ve `detail` üyeleri "insan tarafından okunabilir" metindir —
        // yani API'nin DÖNDÜRMEMESİ gereken şey. İkisi de bilinçli olarak yok.
        Assert.False(govde.ContainsKey("title"));
        Assert.False(govde.ContainsKey("detail"));
    }

    [Fact]
    public void Tasir_YapisalParametre_MetinYerine()
    {
        Dictionary<string, JsonElement> govde = Govde(
            Error.With(MediaErrors.PixelBudgetExceeded, "pixels", "540000"), out _);

        Assert.Equal("540000", govde["pixels"].GetString());

        // Gövdede Türkçe/İngilizce hiçbir CÜMLE yok: metin değerlerin hepsi kod ya da sayı.
        // `type` hariç — o RFC 7807'nin sabiti (`about:blank`).
        foreach ((string anahtar, JsonElement deger) in govde)
        {
            if (deger.ValueKind == JsonValueKind.String && anahtar != "type")
            {
                Assert.DoesNotContain(" ", deger.GetString()!, StringComparison.Ordinal);
            }
        }
    }

    /// <summary>
    /// `JsonElement` olarak okunuyor, `object?` olarak değil: `object?` ayrıştırması da
    /// `JsonElement` üretiyor ve bir dizeyle karşılaştırıldığında "Expected: x, Actual: x"
    /// diyen bir hata veriyor — ilk yazımda beş test tam bu yüzden kırmızı verdi.
    /// </summary>
    private static Dictionary<string, JsonElement> Govde(Error error, out int durum)
    {
        Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<Dictionary<string, object?>> sonuc =
            CarGallery.Modules.Media.Endpoints.MediaProblems.From(error);

        durum = sonuc.StatusCode ?? 0;

        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            JsonSerializer.Serialize(sonuc.Value))!;
    }
}
