using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0014` — galerinin ziyaretçiye dönük ucu, gerçek boru hattında: kimlik doğrulama
/// ara katmanları, JSON serileştiricisi ve modül kaydı dâhil. Sorgunun kendisi
/// `Dealership.Tests`'te; burada ölçülen, TELDEN çıkan şekil.
/// </summary>
public sealed class GaleriPublicUcTests(HostFactory factory) : IClassFixture<HostFactory>
{
    [Fact(DisplayName = "0014/AC-14, 0024/AC-33, 0025/AC-41: Galerinin ziyaretçiye dönük okuma yanıtı yalnız galerinin adını ve aktif şubelerin adını, adresini ve telefonunu taşır; başka alan yanıtta bulunmaz (null olarak da gelmez)")]
    public async Task Tasir_YalnizIzinListesindekiAlanlari()
    {
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        using JsonDocument belge = JsonDocument.Parse(await client.GetStringAsync("/api/dealership/gallery?locale=tr"));

        // İzin listesi ANAHTAR KÜMESİYLE ölçülür, yasak alanların yokluğuyla değil: yarın
        // eklenen bir alan da bu testi kırar (D15). `0020` D15 — galerinin adı artık görünen
        // addır (`commercialName` → `displayName`); beklenen değişiklik, gerileme değil.
        // `0024` D7 — `socialLinks` bilerek eklendi (Context tablosu); öğeleri ayrıca ölçülür.
        Assert.Equal(["branches", "displayName", "socialLinks"], AnahtarlarSirali(belge.RootElement));
        Assert.Equal(JsonValueKind.Array, belge.RootElement.GetProperty("socialLinks").ValueKind);

        JsonElement subeler = belge.RootElement.GetProperty("branches");
        Assert.True(subeler.GetArrayLength() > 0, "seed en az bir aktif şube taşıyor");

        foreach (JsonElement sube in subeler.EnumerateArray())
        {
            // `0025` D8 — `openingHours` bilerek eklendi (Context tablosu); programsız şubede null.
            Assert.Equal(["address", "name", "openingHours", "phone"], AnahtarlarSirali(sube));
        }
    }

    [Fact(DisplayName = "0014/AC-15: Galeri okuması oturum çerezi olmadan 200 döner")]
    public async Task Doner200_OturumCereziOlmadan()
    {
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        HttpResponseMessage yanit = await client.GetAsync("/api/dealership/gallery");

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    private static List<string> AnahtarlarSirali(JsonElement nesne) =>
        [.. nesne.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
}
