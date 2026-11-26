using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AngleSharp.Html.Parser;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0024` — sosyal hesapların gerçek boru hattı: gövde (D4, P1), alan hatası (D11), sürüm (R3),
/// Yönetici politikası (D10), panel okuması (AC-32), ziyaretçi okuması (D7) ve ana sayfa belgesinin
/// <c>sameAs</c>'ı (D8). Gerçek PostgreSQL; üç rol gerçek personel olarak girer.
///
/// <para>Testler aynı galeriyi paylaşır ve sırayla koşar (sınıf içi paralellik yok): her test önce
/// kümeyi kendi bildiği hâle getirir, önceki testin bıraktığına güvenmez.</para>
/// </summary>
public sealed class SosyalHesapZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";
    private const string Galeri = $"{AdminSurface.RoutePrefix}/gallery";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;
    private HttpClient yonetici = null!;
    private HttpClient editor = null!;
    private HttpClient danisman = null!;

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
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
            new { currentPassword = IdentityTestSettings.SeedAdminPassword, newPassword = CalisanParola })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = CalisanParola,
        })).StatusCode);
        string sir = await IkinciAdim.KurAsync(kurulum);

        yonetici = Istemci();
        await IkinciAdim.GirAsync(yonetici, IdentityTestSettings.SeedAdminLoginId, CalisanParola, sir);

        editor = await PersonelAsync("editor@ornek.com", "content_editor");
        danisman = await PersonelAsync("danisman@ornek.com", "sales_advisor");

        // Görünen ad zorunlu (0020 AC-5) ve belge kriterleri `AutoDealer` bloğuna bakar (D8).
        Assert.Equal(HttpStatusCode.OK, (await KaydetAsync(new Dictionary<string, string?>())).StatusCode);
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    // ----- Giriş, değiştirme, silme (R1, R3 · D3, D4) -----

    [Fact(DisplayName = "0024/AC-3, AC-32: Boş Instagram'a yazılan geçerli adres kaydedilir; panel okuması onu kod → adres haritasında taşır.")]
    public async Task Kaydeder_YeniAdresi_PanelOkumasiTasir()
    {
        await KumeAsync([]);
        Assert.Empty(await PanelHaritasiAsync());

        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/otogaleri" });

        Assert.Equal(new Dictionary<string, string> { ["instagram"] = "https://www.instagram.com/otogaleri" }, await PanelHaritasiAsync());
    }

    [Fact(DisplayName = "0024/AC-4: Kayıtlı Instagram adresini değiştirmek eskisinin yerine yenisini koyar; okumada tek `instagram` öğesi yeni adresi taşır.")]
    public async Task Degistirir_KayitliAdresi()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/eski" });

        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/yeni" });

        JsonElement oge = Assert.Single(await PublicHesaplarAsync("tr"));
        Assert.Equal("instagram", oge.GetProperty("platform").GetString());
        Assert.Equal("https://www.instagram.com/yeni", oge.GetProperty("url").GetString());
    }

    [Theory(DisplayName = "0024/AC-5, AC-6: Alanı boşaltmak ya da yalnız boşlukla kaydetmek o hesabı siler.")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Siler_BosaltilanHesabi(string? deger)
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/silinir", ["youtube"] = "https://www.youtube.com/@kalir" });

        await KumeAsync(new() { ["instagram"] = deger, ["youtube"] = "https://www.youtube.com/@kalir" });

        Assert.Equal(new Dictionary<string, string> { ["youtube"] = "https://www.youtube.com/@kalir" }, await PanelHaritasiAsync());
    }

    [Fact(DisplayName = "0024/AC-7, AC-13: Baştaki ve sondaki boşluk kırpılır; harf büyüklüğü farklı ana makine girildiği gibi saklanır.")]
    public async Task Kirpar_VeGirildigiGibiSaklar()
    {
        await KumeAsync(new() { ["instagram"] = "  https://WWW.Instagram.COM/otogaleri \t" });

        Assert.Equal("https://WWW.Instagram.COM/otogaleri", (await PanelHaritasiAsync())["instagram"]);
    }

    // ----- Doğrulama (R2 · D2, D11) -----

    [Theory(DisplayName = "0024/AC-8, AC-9, AC-10, AC-11, AC-12, AC-72, AC-73, AC-74, AC-75, AC-76: Biçimsel olarak hatalı adres `422` ve `socialLinks.instagram` alanında `gallery.social_link_invalid` döner.")]
    [InlineData("http://www.instagram.com/otogaleri")]
    [InlineData("https://www.facebook.com/otogaleri")]
    [InlineData("https://www.instagram.com/")]
    [InlineData("https://kullanici@www.instagram.com/otogaleri")]
    [InlineData("https://www.instagram.com:8443/otogaleri")]
    [InlineData("https://instagram.com.kotu.site/x")]
    [InlineData("https://www.instagram.com%2F@kotu.site/x")]
    [InlineData("https://www.instagrаm.com/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://www.instagram.com:443/x")]
    public async Task Doner422_BicimselOlarakHataliAdres(string adres)
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/onceki" });

        JsonElement hata = await ProblemAsync(await KaydetAsync(new() { ["instagram"] = adres }), HttpStatusCode.UnprocessableEntity);

        Assert.Equal("dealership.validation_failed", hata.GetProperty("errorCode").GetString());
        Assert.Equal("gallery.social_link_invalid", hata.GetProperty("socialLinks.instagram").GetString());
        Assert.Equal("https://www.instagram.com/onceki", (await PanelHaritasiAsync())["instagram"]);
    }

    [Fact(DisplayName = "0024/AC-14: UTF-8 olarak 200 baytlık ASCII adres kabul edilir; 201 bayt `422` ve `gallery.social_link_too_long` döner.")]
    public async Task Sinirlar_AsciiAdresi()
    {
        string sinirda = "https://www.instagram.com/" + new string('a', 200 - 26);
        await KumeAsync(new() { ["instagram"] = sinirda });
        Assert.Equal(sinirda, (await PanelHaritasiAsync())["instagram"]);

        JsonElement hata = await ProblemAsync(await KaydetAsync(new() { ["instagram"] = sinirda + "a" }), HttpStatusCode.UnprocessableEntity);
        Assert.Equal("gallery.social_link_too_long", hata.GetProperty("socialLinks.instagram").GetString());
    }

    [Fact(DisplayName = "0024/AC-77: 87 Arapça harfli yol (200 bayt) kabul edilir; 88 harf (202 bayt) `422` ve `gallery.social_link_too_long` döner.")]
    public async Task Sinirlar_ArapcaYolu()
    {
        string sinirda = "https://www.instagram.com/" + new string('ب', 87);
        Assert.Equal(200, Encoding.UTF8.GetByteCount(sinirda));
        await KumeAsync(new() { ["instagram"] = sinirda });
        Assert.Equal(sinirda, (await PanelHaritasiAsync())["instagram"]);

        JsonElement hata = await ProblemAsync(await KaydetAsync(new() { ["instagram"] = sinirda + "ب" }), HttpStatusCode.UnprocessableEntity);
        Assert.Equal("gallery.social_link_too_long", hata.GetProperty("socialLinks.instagram").GetString());
    }

    [Fact(DisplayName = "0024/AC-15: `https://www.facebook.com/profile.php?id=100012345` Facebook alanında kabul edilir.")]
    public async Task KabulEder_FacebookSorguDizesi()
    {
        await KumeAsync(new() { ["facebook"] = "https://www.facebook.com/profile.php?id=100012345" });

        Assert.Equal("https://www.facebook.com/profile.php?id=100012345", (await PanelHaritasiAsync())["facebook"]);
    }

    [Fact(DisplayName = "0024/AC-16: Gövdede `myspace` kodu `422` ve `socialLinks.myspace` alanında `gallery.social_platform_unknown` döner.")]
    public async Task Doner422_BilinmeyenPlatform()
    {
        JsonElement hata = await ProblemAsync(await KaydetAsync(new() { ["myspace"] = "https://myspace.com/x" }), HttpStatusCode.UnprocessableEntity);

        Assert.Equal("gallery.social_platform_unknown", hata.GetProperty("socialLinks.myspace").GetString());
    }

    [Fact(DisplayName = "0024/AC-17, AC-18: Hatalı adresli kaydetmede hiçbir alan değişmez — görünen ad ve başka platformun geçerli adresi de; iki hatalı alanın ikisi de yanıtta.")]
    public async Task Degistirmez_HicbirAlani_HataliKaydetmede()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/onceki" });
        string once = await yonetici.GetStringAsync(Galeri);

        JsonElement hata = await ProblemAsync(await yonetici.PutAsJsonAsync(Galeri, new
        {
            version = await SurumAsync(),
            commercialName = "Oto Galeri",
            displayNames = new Dictionary<string, string> { ["tr"] = "Değişmemesi Gereken Ad" },
            socialLinks = new Dictionary<string, string?>
            {
                ["instagram"] = "http://www.instagram.com/x",
                ["youtube"] = "https://www.youtube.com/@gecerli",
                ["x"] = "https://x.com/",
            },
        }), HttpStatusCode.UnprocessableEntity);

        Assert.Equal("gallery.social_link_invalid", hata.GetProperty("socialLinks.instagram").GetString());
        Assert.Equal("gallery.social_link_invalid", hata.GetProperty("socialLinks.x").GetString());
        Assert.Equal(once, await yonetici.GetStringAsync(Galeri));
    }

    // ----- Eşzamanlı yazma (R3 · D4) -----

    [Fact(DisplayName = "0024/AC-20, AC-21: Eski sürümle sosyal hesap değiştiren kaydetme `409` döner ve hesaplar değişmez; başarılı kaydetme sürümü bir artırır ve yeni sürümü döner.")]
    public async Task Doner409_EskiSurum_VeArtirir_Basarida()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/ilk" });
        int surum = await SurumAsync();

        HttpResponseMessage basarili = await KaydetAsync(new() { ["instagram"] = "https://www.instagram.com/ikinci" }, surum);
        Assert.Equal(HttpStatusCode.OK, basarili.StatusCode);
        Assert.Equal(surum + 1, JsonDocument.Parse(await basarili.Content.ReadAsStringAsync()).RootElement.GetProperty("version").GetInt32());
        Assert.Equal(surum + 1, await SurumAsync());

        JsonElement hata = await ProblemAsync(await KaydetAsync(new() { ["instagram"] = "https://www.instagram.com/ucuncu" }, surum), HttpStatusCode.Conflict);
        Assert.Equal("concurrency.lost", hata.GetProperty("errorCode").GetString());
        Assert.Equal("https://www.instagram.com/ikinci", (await PanelHaritasiAsync())["instagram"]);
    }

    [Fact(DisplayName = "0024/AC-22: `0020`'nin sosyal alansız gövdesiyle kaydetme kayıtlı hesapları değiştirmez.")]
    public async Task Degistirmez_AlansizGovde()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/kalir", ["linkedin"] = "https://www.linkedin.com/company/kalir" });

        // `0020`'nin istemcisinin gövdesi, birebir: `socialLinks` anahtarı yok.
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsJsonAsync(Galeri, new
        {
            version = await SurumAsync(),
            commercialName = "Oto Galeri",
            taxNumber = "1234567890",
            tradeRegistryNumber = (string?)null,
            displayNames = new Dictionary<string, string> { ["tr"] = "Oto Galeri Vitrin" },
        })).StatusCode);

        Assert.Equal(2, (await PanelHaritasiAsync()).Count);
    }

    [Fact(DisplayName = "0024/AC-71: `\"socialLinks\": null` taşıyan kaydetme kayıtlı hesapları değiştirmez.")]
    public async Task Degistirmez_AcikNull()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/kalir" });

        string govde = $$"""{"version":{{await SurumAsync()}},"commercialName":"Oto Galeri","displayNames":{"tr":"Oto Galeri Vitrin"},"socialLinks":null}""";
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsync(Galeri, new StringContent(govde, Encoding.UTF8, "application/json"))).StatusCode);

        Assert.Equal("https://www.instagram.com/kalir", (await PanelHaritasiAsync())["instagram"]);
    }

    [Fact(DisplayName = "0024/AC-23: Boş sosyal hesap haritası kayıtlı hesapların hepsini siler.")]
    public async Task Siler_Hepsini_BosHarita()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/a", ["tiktok"] = "https://www.tiktok.com/@b" });

        await KumeAsync([]);

        Assert.Empty(await PanelHaritasiAsync());
        Assert.Empty(await PublicHesaplarAsync("tr"));
    }

    // ----- Yetki (R7 · D10) -----

    [Fact(DisplayName = "0024/AC-24, AC-25: Satış Danışmanı ve İçerik Editörü'nün sosyal hesaplı kaydetmesi `403` döner ve hesaplar değişmez.")]
    public async Task Doner403_DanismanVeEditor()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/yonetici" });
        int surum = await SurumAsync();

        foreach (HttpClient rol in new[] { danisman, editor })
        {
            HttpResponseMessage yanit = await rol.PutAsJsonAsync(Galeri, Govde(new() { ["instagram"] = "https://www.instagram.com/yetkisiz" }, surum));
            Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
        }

        Assert.Equal("https://www.instagram.com/yonetici", (await PanelHaritasiAsync())["instagram"]);
        Assert.Equal(surum, await SurumAsync());
    }

    [Fact(DisplayName = "0024/AC-26: Oturumsuz aynı istek `401` döner.")]
    public async Task Doner401_Oturumsuz()
    {
        HttpResponseMessage yanit = await Istemci().PutAsJsonAsync(Galeri, Govde(new() { ["instagram"] = "https://www.instagram.com/x" }, 1));

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    // ----- Ziyaretçiye dönük okuma (R6 · C2, D7) -----

    [Fact(DisplayName = "0024/AC-33: Public yanıtın kök anahtarları tam olarak `branches`, `displayName`, `socialLinks`; her öğenin anahtarları tam olarak `platform`, `url`.")]
    public async Task Tasir_YalnizIzinListesi()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/a", ["x"] = "https://x.com/b", ["linkedin"] = "https://www.linkedin.com/company/c" });

        using JsonDocument belge = JsonDocument.Parse(await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr"));

        Assert.Equal(["branches", "displayName", "socialLinks"], Anahtarlar(belge.RootElement));
        JsonElement[] ogeler = [.. belge.RootElement.GetProperty("socialLinks").EnumerateArray()];
        Assert.Equal(3, ogeler.Length);
        Assert.All(ogeler, oge => Assert.Equal(["platform", "url"], Anahtarlar(oge)));
    }

    [Fact(DisplayName = "0024/AC-34: Önce YouTube, sonra Instagram girilmiş galerinin `socialLinks` sırası `instagram`, `youtube`'dur.")]
    public async Task Siralar_D1eGore_GirilmeSirasinaDegil()
    {
        await KumeAsync([]);
        await KumeAsync(new() { ["youtube"] = "https://www.youtube.com/@kanal" });
        await KumeAsync(new() { ["youtube"] = "https://www.youtube.com/@kanal", ["instagram"] = "https://www.instagram.com/sonra" });

        Assert.Equal(["instagram", "youtube"], (await PublicHesaplarAsync("tr")).Select(oge => oge.GetProperty("platform").GetString()));
    }

    [Fact(DisplayName = "0024/AC-35: Hiç hesabı olmayan galerinin yanıtında `socialLinks` boş bir dizidir.")]
    public async Task Doner_BosDizi_HesapYokken()
    {
        await KumeAsync([]);

        JsonElement hesaplar = JsonDocument.Parse(await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr")).RootElement.GetProperty("socialLinks");

        Assert.Equal(JsonValueKind.Array, hesaplar.ValueKind);
        Assert.Equal(0, hesaplar.GetArrayLength());
    }

    [Fact(DisplayName = "0024/AC-36: `locale=tr`, `locale=ar` ve `locale=en` yanıtlarının `socialLinks` dizileri aynıdır.")]
    public async Task Ayni_HerDilde()
    {
        await KumeAsync(new() { ["instagram"] = "https://www.instagram.com/a", ["tiktok"] = "https://www.tiktok.com/@b" });

        string[] diziler = await Task.WhenAll(new[] { "tr", "ar", "en" }.Select(async locale =>
            JsonDocument.Parse(await Istemci().GetStringAsync($"/api/dealership/gallery?locale={locale}")).RootElement.GetProperty("socialLinks").GetRawText()));

        Assert.Equal(diziler[0], diziler[1]);
        Assert.Equal(diziler[0], diziler[2]);
        Assert.Contains("tiktok", diziler[0], StringComparison.Ordinal);
    }

    // ----- Yapısal veri (R5 · D8) -----

    [Fact(DisplayName = "0024/AC-52: Yönetici bir hesabı kaydettikten sonra istenen ilk `/tr/` belgesinin `sameAs`'ı o adresi taşır.")]
    public async Task SameAs_KaydettiktenSonrakiIlkBelge()
    {
        await KumeAsync([]);
        HttpClient ziyaretci = Istemci();
        Assert.DoesNotContain("sameAs", Assert.Single(await JsonLdAsync(ziyaretci)).Anahtarlar());

        await KumeAsync(new() { ["tiktok"] = "https://www.tiktok.com/@ilkbelge" });

        JsonElement dealer = Assert.Single(await JsonLdAsync(ziyaretci));
        Assert.Equal(["https://www.tiktok.com/@ilkbelge"], dealer.GetProperty("sameAs").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact(DisplayName = "0024/AC-34: D1'de olmayan kodla saklanmış bir satır (D3) okumalarda yok sayılır; sıra yine D1'inkidir.")]
    public async Task YokSayar_D1deOlmayanKodu()
    {
        await KumeAsync(new() { ["linkedin"] = "https://www.linkedin.com/company/c", ["facebook"] = "https://www.facebook.com/f" });

        await using (NpgsqlConnection connection = new(container.GetConnectionString()))
        {
            await connection.OpenAsync();
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO dealership.gallery_social_links (gallery_id, platform, url)
                SELECT id, 'myspace', 'https://myspace.com/eski' FROM dealership.galleries;
                """;
            Assert.Equal(1, await command.ExecuteNonQueryAsync());
        }

        Assert.Equal(["facebook", "linkedin"], (await PublicHesaplarAsync("tr")).Select(oge => oge.GetProperty("platform").GetString()));
        Assert.Equal(["facebook", "linkedin"], (await PanelHaritasiAsync()).Keys);
        Assert.DoesNotContain("myspace", await Istemci().GetStringAsync("/tr/"), StringComparison.Ordinal);

        // Bir sonraki kaydetme kümeyi bütünüyle yerine koyar (D3): yabancı satır da gider.
        await KumeAsync([]);
        Assert.Empty(await PanelHaritasiAsync());
    }

    // ----- Yardımcılar -----

    private static async Task<IReadOnlyList<JsonElement>> JsonLdAsync(HttpClient istemci) =>
        new HtmlParser().ParseDocument(await istemci.GetStringAsync("/tr/")).JsonLd();

    private static List<string> Anahtarlar(JsonElement nesne) =>
        [.. nesne.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];


    private async Task KumeAsync(Dictionary<string, string?> hesaplar)
    {
        HttpResponseMessage yanit = await KaydetAsync(hesaplar);
        Assert.True(yanit.StatusCode == HttpStatusCode.OK, $"{(int)yanit.StatusCode}: {await yanit.Content.ReadAsStringAsync()}");
    }

    private async Task<HttpResponseMessage> KaydetAsync(Dictionary<string, string?> hesaplar, int? surum = null) =>
        await yonetici.PutAsJsonAsync(Galeri, Govde(hesaplar, surum ?? await SurumAsync()));

    private static object Govde(Dictionary<string, string?> hesaplar, int surum) => new
    {
        version = surum,
        commercialName = "Oto Galeri",
        displayNames = new Dictionary<string, string> { ["tr"] = "Oto Galeri Vitrin" },
        socialLinks = hesaplar,
    };

    private async Task<int> SurumAsync() =>
        JsonDocument.Parse(await yonetici.GetStringAsync(Galeri)).RootElement.GetProperty("version").GetInt32();

    private async Task<Dictionary<string, string>> PanelHaritasiAsync() =>
        JsonDocument.Parse(await yonetici.GetStringAsync(Galeri)).RootElement.GetProperty("socialLinks")
            .EnumerateObject().ToDictionary(item => item.Name, item => item.Value.GetString()!);

    private async Task<List<JsonElement>> PublicHesaplarAsync(string locale) =>
        [.. JsonDocument.Parse(await Istemci().GetStringAsync($"/api/dealership/gallery?locale={locale}")).RootElement
            .GetProperty("socialLinks").EnumerateArray()];

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage yanit, HttpStatusCode beklenen)
    {
        string metin = await yanit.Content.ReadAsStringAsync();
        Assert.True(yanit.StatusCode == beklenen, $"{(int)yanit.StatusCode}: {metin}");
        JsonElement govde = JsonDocument.Parse(metin).RootElement;

        // C9 — çevrilmiş metin yok: yalnız kod ve alan anahtarı.
        Assert.False(govde.TryGetProperty("title", out _));
        Assert.False(govde.TryGetProperty("detail", out _));
        return govde;
    }

    private async Task<HttpClient> PersonelAsync(string loginId, string role)
    {
        Assert.Equal(HttpStatusCode.Created, (await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/staff", new
        {
            displayName = "Galeri Personeli",
            loginId,
            initialPassword = "ilk-parola-1234",
            role,
            preferredLocale = "tr",
            branchId = (Guid?)null,
        })).StatusCode);

        HttpClient client = Istemci();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/identity/sessions", new { loginId, password = "ilk-parola-1234" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = "ilk-parola-1234", newPassword = PersonelParola })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/identity/sessions", new { loginId, password = PersonelParola })).StatusCode);

        return client;
    }

    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });
}
