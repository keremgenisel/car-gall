using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// spec `0012c` — toplu eylemin HTTP sözleşmesi, gerçek boru hattında.
///
/// <para><b>Neden burada:</b> ölçülen şeylerin hiçbiri komut katmanında görünmez —
/// `200`'ün hepsi başarısız olsa bile dönmesi, istek düzeyi reddin `422` olması, oturumsuz
/// isteğin `401` alması ve <b>olmayan</b> bir toplu rotanın `404` vermesi. Dördü de yanıtın
/// kendisinde yaşıyor.</para>
///
/// <para><b>Seçim sınırı YAPILANDIRMADAN okunuyor</b> (**AC-7**) ve bu test onu üretim
/// varsayılanıyla değil, <b>değiştirilmiş</b> bir değerle ölçüyor: sabit 100'e karşı yazılan
/// bir test, sayı koda gömülü olsa da yeşil kalırdı (**D52** — sınır kriterleri API
/// düzeyinde, panelde karşılığı yok).</para>
/// </summary>
public sealed class TopluEylemZinciriTests : IAsyncLifetime
{
    /// <summary>
    /// Üretim varsayılanı 100; test 3'e çekiyor. Küçük bir sınır hem yapılandırmanın
    /// gerçekten okunduğunu gösteriyor hem de 101 sahte kimlik üretmekten kurtarıyor.
    /// </summary>
    private const int Sinir = 3;

    private const string CalisanParola = "calisan-parolasi-1";

    private static readonly Guid SeedSube = Guid.Parse("00000000-0000-7000-8000-00000000b001");

    private static readonly Guid SeedModel = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;

    /// <summary>
    /// `0013` D30/D34 — kurulum Yöneticisinin ikinci adım sırrı. Kurulum **bir kez**,
    /// `InitializeAsync`'te yapılıyor; testlerin her girişi kodu bundan hesaplıyor
    /// (`IkinciAdim`). D28'in e2e için verdiği kararın backend karşılığı.
    /// </summary>
    private string ikinciAdimSirri = string.Empty;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            builder.UseSetting("AdminPaging:MaxPageSize", Sinir.ToString(System.Globalization.CultureInfo.InvariantCulture));
            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);
        });

        HttpClient kurulum = Istemci();

        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = IdentityTestSettings.SeedAdminPassword,
        })).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await kurulum.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = IdentityTestSettings.SeedAdminPassword, newPassword = CalisanParola }))
            .StatusCode);

        // 0013 D30/D13 — Yöneticide ikinci adım ZORUNLU ve kurulumsuz hesap kapılı bir
        // oturum alıyor. Kurulum bir kez burada yapılır; aksi hâlde bu dosyadaki her
        // yönetim isteği `403 two_factor_setup_required` alır ve testler ikinci adımla
        // ilgisi olmayan bir sebeple kırmızı verirdi (D34).
        // Parola değişimi o personelin TÜM oturumlarını düşürdü (`0006` R11/AC-22), yani
        // eldeki çerez ölü. Yeniden giriş `two_factor_setup_required` döndürüyor ve
        // KAPILI bir oturum veriyor (D13) — kurulumun yapılabildiği tek hâl tam bu.
        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = CalisanParola,
        })).StatusCode);

        ikinciAdimSirri = await IkinciAdim.KurAsync(kurulum);
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    [Fact(DisplayName = "0012c/AC-2: Boş seçim istek düzeyinde reddedilir: yanıt `422` ve `listing.selection_empty` taşır")]
    public async Task Reddeder_BosSecimi()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await TopluAsync(client, "archive", []);

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);
        Assert.Equal("listing.selection_empty", await KodAsync(yanit));
    }

    [Fact(DisplayName = "0012c/AC-5: Üst sınırı aşan seçim **kırpılmaz, reddedilir**: yanıt `422` ve `listing.selection_limit_exceeded` taşır")]
    public async Task Reddeder_SiniriAsanSecimi()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await TopluAsync(client, "archive", Kimlikler(Sinir + 1));

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);

        JsonElement govde = await GovdeAsync(yanit);

        Assert.Equal("listing.selection_limit_exceeded", govde.GetProperty("errorCode").GetString());

        // Sınır yanıtta: personel neyi aştığını öğrenmeden seçimini küçültemez.
        Assert.Equal(
            Sinir.ToString(System.Globalization.CultureInfo.InvariantCulture),
            govde.GetProperty("limit").GetString());
    }

    [Fact(DisplayName = "0012c/AC-6: Üst sınır **dâhildir**: tam sınır kadar araç taşıyan seçim kabul edilir")]
    public async Task Kabul_Eder_TamSinirKadarSecimi()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await TopluAsync(client, "archive", Kimlikler(Sinir));

        // Araçlar yok — ama bu SATIR düzeyi bir olgu (**D27**) ve istek işlendi.
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal(Sinir, (await GovdeAsync(yanit)).GetProperty("results").GetArrayLength());
    }

    /// <summary>
    /// **AC-7**'nin tripwire'ı: sınır `AdminPaging:MaxPageSize`'dan geliyor. Sayı koda
    /// gömülseydi üretim varsayılanı (100) geçerli olurdu ve üç kimlikli bir seçim
    /// reddedilmezdi — bu test tam olarak o farkı ölçüyor.
    /// </summary>
    [Fact(DisplayName = "0012c/AC-7: Üst sınır **yapılandırmadan** okunur; kodda sabit değildir")]
    public async Task Okur_Siniri_Yapilandirmadan()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage gecen = await TopluAsync(client, "archive", Kimlikler(Sinir));
        HttpResponseMessage reddedilen = await TopluAsync(client, "archive", Kimlikler(Sinir + 1));

        Assert.Equal(HttpStatusCode.OK, gecen.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableContent, reddedilen.StatusCode);

        // Üretim varsayılanı 100: sayı kodda sabit olsaydı dört kimlik de geçerdi.
        Assert.True(Sinir < 100);
    }

    [Theory(DisplayName = "0012c/AC-8: Oturumu olmayan biri hiçbir toplu eylemi çağıramaz")]
    [InlineData("publish")]
    [InlineData("unpublish")]
    [InlineData("sold")]
    [InlineData("archive")]
    [InlineData("feature")]
    [InlineData("unfeature")]
    public async Task Cagiramaz_OturumsuzKisi_HicbirTopluEylemi(string eylem)
    {
        HttpResponseMessage yanit = await TopluAsync(Istemci(), eylem, Kimlikler(1));

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    /// <summary>
    /// **D18** — yokluğun HTTP karşılığı `404`'tür ve bu bilinçli: `422` *"istediğin oldu
    /// ama olmadı"* derdi, oysa böyle bir uç hiç tanımlanmadı.
    /// </summary>
    [Theory(DisplayName = "0012c/AC-9: Toplu **rezerve** ve toplu **rezervasyon kaldırma** uçları yoktur")]
    [InlineData("reserve")]
    [InlineData("unreserve")]
    public async Task Yoktur_TopluRezerveUclari(string eylem)
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await TopluAsync(client, eylem, Kimlikler(1));

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
    }

    [Fact(DisplayName = "0012c/AC-13: Araçların **hepsi** başarısız olsa bile yanıt `200` döner")]
    public async Task Doner_IkiYuz_HepsiBasarisizOlsaDa()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage yanit = await TopluAsync(client, "archive", Kimlikler(2));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        JsonElement satirlar = (await GovdeAsync(yanit)).GetProperty("results");

        Assert.Equal(2, satirlar.GetArrayLength());

        foreach (JsonElement satir in satirlar.EnumerateArray())
        {
            Assert.False(satir.GetProperty("applied").GetBoolean());
            Assert.Equal("vehicle.not_found", satir.GetProperty("errorCode").GetString());
        }

        // `200` isteğin işlendiğini söyler, kayıtların başarılı olduğunu DEĞİL. Bedel
        // açıkça kabul edildi: istemci gövdeyi okumak zorunda (`docs/conventions.md` § 5).
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    [Fact(DisplayName = "0012c/AC-28: Arşivlenmiş ilan öne çıkarılamaz: yanıt `listing.cannot_feature.archived` ve `409` taşır")]
    public async Task Alir_DortYuzDokuz_ArsivlenmisIlanOneCikarilinca()
    {
        HttpClient client = await GirisYapmisIstemciAsync();
        Guid arac = await AracOlusturAsync(client, "TOPLU00000000001");

        Assert.Equal(HttpStatusCode.NoContent, (await TekilAsync(client, arac, "archive")).StatusCode);

        HttpResponseMessage yanit = await TekilAsync(client, arac, "feature");

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
        Assert.Equal("listing.cannot_feature.archived", await KodAsync(yanit));
    }

    private static IReadOnlyList<Guid> Kimlikler(int adet) =>
        [.. Enumerable.Range(0, adet).Select(_ => Guid.CreateVersion7())];

    private static Task<HttpResponseMessage> TopluAsync(
        HttpClient client,
        string eylem,
        IReadOnlyList<Guid> araclar) =>
        client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/bulk/{eylem}",
            new { vehicleIds = araclar });

    private static Task<HttpResponseMessage> TekilAsync(HttpClient client, Guid arac, string eylem) =>
        client.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/{eylem}", UriKind.Relative),
            content: null);

    private static async Task<JsonElement> GovdeAsync(HttpResponseMessage yanit) =>
        JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

    private static async Task<string?> KodAsync(HttpResponseMessage yanit) =>
        (await GovdeAsync(yanit)).GetProperty("errorCode").GetString();

    private static async Task<Guid> AracOlusturAsync(HttpClient client, string vin)
    {
        HttpResponseMessage yanit = await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles",
            new
            {
                branchId = SeedSube,
                vehicleModelId = SeedModel,
                bodyType = "suv",
                fuelType = "diesel",
                transmissionType = "automatic",
                drivetrainType = "front-wheel-drive",
                trimLevel = "comfort",
                color = new Dictionary<string, string> { ["tr"] = "Beyaz" },
                modelYear = 2019,
                mileageKilometers = 45_000,
                priceAmount = 750_000m,
                priceOnRequest = false,
                vin,
            });

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);

        return (await GovdeAsync(yanit)).GetProperty("vehicleId").GetGuid();
    }

    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    private async Task<HttpClient> GirisYapmisIstemciAsync()
    {
        HttpClient client = Istemci();

        await IkinciAdim.GirAsync(
            client, IdentityTestSettings.SeedAdminLoginId, CalisanParola, ikinciAdimSirri);

        return client;
    }
}
