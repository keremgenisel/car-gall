using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0028` **D5** · **D7** · **D8** — ziyaretçi okumasının e-posta ve harita adresleri, GERÇEK zincirde:
/// gerçek PostgreSQL, gerçek uçlar, Yönetici oturumu, kurulum yapılandırmasından okunan anahtar.
/// Her test kendi kurulumunu kurar (<see cref="KurAsync"/>): anahtarlı, anahtarsız, varsayılan dili ar.
/// Günlük kayıtları <see cref="Gunluk"/> ile bütün kategorilerde <c>Trace</c>'ten yakalanır (AC-83).
/// </summary>
public sealed class IletisimZinciriTests : IAsyncLifetime
{
    private const string Anahtar = "test-anahtar";
    private const string CalisanParola = "calisan-parolasi-1";
    private const string Galeri = $"{AdminSurface.RoutePrefix}/gallery";
    private const string Subeler = $"{AdminSurface.RoutePrefix}/branches";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private readonly AyarliSaat saat = new();
    private readonly Gunluk gunluk = new();
    private WebApplicationFactory<Program>? factory;
    private string sir = string.Empty;
    private int sayac;
    private DateTimeOffset girisAni = new(2031, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        await TestSchema.ApplyAsync(container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    /// <summary>
    /// Kurulumu başlatır ve Yöneticinin ikinci adımını kurar. <paramref name="anahtar"/> <c>null</c>
    /// ise yapılandırmada hiç yoktur.
    /// </summary>
    private async Task KurAsync(string? anahtar = Anahtar, string varsayilanDil = "tr")
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
            builder.UseSetting("ConnectionStrings:VehicleCatalog", container.GetConnectionString());
            builder.UseSetting("Localization:DefaultLocale", varsayilanDil);

            if (anahtar is not null)
            {
                builder.UseSetting("GoogleMaps:EmbedApiKey", anahtar);
            }

            IdentityTestSettings.Apply(builder);
            NotificationsTestSettings.Apply(builder);
            MediaTestSettings.Apply(builder);

            // AC-83 — bütün kategoriler, en düşük düzey, kapsamlar dahil.
            // Sağlayıcıya özgü kural yapılandırmanın kategori kurallarından önce seçilir (appsettings'in
            // `Logging:LogLevel`'i bu sağlayıcıyı kısmaz).
            builder.ConfigureLogging(logging => logging
                .SetMinimumLevel(LogLevel.Trace)
                .AddProvider(gunluk)
                .AddFilter<Gunluk>(null, LogLevel.Trace));
            builder.ConfigureTestServices(services => services.AddSingleton<IClock>(saat));
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
        sir = await IkinciAdim.KurAsync(kurulum, saat.UtcNow);
    }

    // ----- Ziyaretçi okuması (R2, R3, C2 · D5, D7) -----

    [Fact(DisplayName = "0028/AC-21, AC-22, AC-23, AC-24, AC-27: Şube öğesi tam `address, email, map, name, openNow, openingHours, phone, specialHours`; `map` tam `embedUrl, linkUrl`; Kadıköy'ün e-postası ve iki adresi tam dize, Bostancı'nın e-postası null; ham koordinat, saat dilimi, WhatsApp ve slug yok")]
    public async Task Okuma_AnahtarliKurulum()
    {
        await KurAsync();
        HttpClient yonetici = await YoneticiAsync();
        string kadikoy = await SubeAcAsync(yonetici, "Bağdat Cad. 1", email: "kadikoy@otogaleri.com.tr", enlem: 40.98765m, boylam: 29.0275m, whatsapp: "+905320000001");
        string bostanci = await SubeAcAsync(yonetici, "Bağdat Cad. No:5");

        string metin = await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr");
        JsonElement[] subeler = [.. JsonDocument.Parse(metin).RootElement.GetProperty("branches").EnumerateArray()];

        Assert.All(subeler, oge => Assert.Equal(["address", "email", "map", "name", "openNow", "openingHours", "phone", "specialHours"], Anahtarlar(oge)));
        Assert.All(subeler, oge => Assert.Equal(["embedUrl", "linkUrl"], Anahtarlar(oge.GetProperty("map"))));

        JsonElement k = Sube(subeler, kadikoy);
        Assert.Equal("kadikoy@otogaleri.com.tr", k.GetProperty("email").GetString());
        Assert.Equal("https://www.google.com/maps/search/?api=1&query=40.98765%2C29.0275", k.GetProperty("map").GetProperty("linkUrl").GetString());
        Assert.Equal("https://www.google.com/maps/embed/v1/place?key=test-anahtar&q=40.98765%2C29.0275&language=tr", k.GetProperty("map").GetProperty("embedUrl").GetString());
        Assert.Equal(JsonValueKind.Null, Sube(subeler, bostanci).GetProperty("email").ValueKind);

        // AC-23 — koordinat yalnız harita adresinin sorgusunda (kodlu) geçer.
        Assert.DoesNotContain("40.98765,", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("latitude", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("timeZone", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Europe/Istanbul", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("+905320000001", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("whatsApp", metin, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("slug", metin, StringComparison.OrdinalIgnoreCase);

        // AC-27 — `locale=en` isteğinde gömme adresinin dili en.
        JsonElement en = Sube([.. JsonDocument.Parse(await Istemci().GetStringAsync("/api/dealership/gallery?locale=en")).RootElement.GetProperty("branches").EnumerateArray()], kadikoy);
        Assert.EndsWith("&language=en", en.GetProperty("map").GetProperty("embedUrl").GetString(), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0028/AC-25: Süreç kültürü tr-TR iken de Kadıköy'ün `linkUrl`'i nokta ondalıklı ve sondaki sıfırsız")]
    public async Task Okuma_KulturTr()
    {
        await KurAsync();
        string ad = await SubeAcAsync(await YoneticiAsync(), "x", enlem: 40.98765m, boylam: 29.0275m);
        CultureInfo onceki = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal("https://www.google.com/maps/search/?api=1&query=40.98765%2C29.0275", (await PublicSubeAsync(ad)).GetProperty("map").GetProperty("linkUrl").GetString());
        }
        finally
        {
            CultureInfo.CurrentCulture = onceki;
            CultureInfo.DefaultThreadCurrentCulture = null;
        }
    }

    [Fact(DisplayName = "0028/AC-26: tr adresi `Bağdat Cad. No:5/1 & #2` olan koordinatsız şubenin `linkUrl`'i tam dize; `locale=ar` isteğinde de aynı (tr adresi)")]
    public async Task Okuma_AdresliSorgu()
    {
        await KurAsync();
        string ad = await SubeAcAsync(await YoneticiAsync(), "Bağdat Cad. No:5/1 & #2", arAdres: "شارع بغداد ٥");
        const string Beklenen = "https://www.google.com/maps/search/?api=1&query=Ba%C4%9Fdat%20Cad.%20No%3A5%2F1%20%26%20%232";

        Assert.Equal(Beklenen, (await PublicSubeAsync(ad)).GetProperty("map").GetProperty("linkUrl").GetString());
        Assert.Equal(Beklenen, (await PublicSubeAsync(ad, "ar")).GetProperty("map").GetProperty("linkUrl").GetString());
    }

    [Theory(DisplayName = "0028/AC-28, AC-81 (sunucu yarısı): Anahtar yokken, boşken ve yalnız boşlukken `map`'in anahtar kümesi tam `linkUrl`; yanıtta `embedUrl` dizesi yok")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Okuma_Anahtarsiz(string? anahtar)
    {
        await KurAsync(anahtar);
        string ad = await SubeAcAsync(await YoneticiAsync(), "Bağdat Cad. 1", enlem: 40.98765m, boylam: 29.0275m);

        string metin = await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr");
        JsonElement harita = Sube([.. JsonDocument.Parse(metin).RootElement.GetProperty("branches").EnumerateArray()], ad).GetProperty("map");

        Assert.Equal(["linkUrl"], Anahtarlar(harita));
        Assert.StartsWith("https://www.google.com/maps/search/", harita.GetProperty("linkUrl").GetString(), StringComparison.Ordinal);
        Assert.DoesNotContain("embedUrl", metin, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0028/AC-29, AC-30: Panelden adres değişince ilk okumada `linkUrl` yeni adresi kullanır; e-posta boşaltılınca `email` null ve ilk `/tr/contact` belgesinde `email` yok")]
    public async Task Okuma_PanelDegisikligi()
    {
        await KurAsync();
        HttpClient yonetici = await YoneticiAsync();
        string ad = await SubeAcAsync(yonetici, "Eski Cad. 1", email: "kadikoy@otogaleri.com.tr");
        Assert.Equal("kadikoy@otogaleri.com.tr", (await DepartmentAsync(ad)).GetProperty("email").GetString());

        await BasariliAsync(await KaydetAsync(yonetici, await KimlikAsync(yonetici, ad), govde =>
        {
            govde["addresses"] = new JsonObject { ["tr"] = "Yeni Cad. 2" };
            govde["email"] = null;
        }));

        JsonElement sube = await PublicSubeAsync(ad);
        Assert.Equal("https://www.google.com/maps/search/?api=1&query=Yeni%20Cad.%202", sube.GetProperty("map").GetProperty("linkUrl").GetString());
        Assert.Equal(JsonValueKind.Null, sube.GetProperty("email").ValueKind);
        Assert.DoesNotContain("email", (await DepartmentAsync(ad)).EnumerateObject().Select(p => p.Name));
    }

    [Fact(DisplayName = "0028/AC-31, 0020/AC-48: Geçici kapalı ve kapatılmış şube yanıtta yok; iletişim belgesinde de yok")]
    public async Task KapaliSubeler()
    {
        await KurAsync();
        HttpClient yonetici = await YoneticiAsync();
        string gecici = await SubeAcAsync(yonetici, "Kapalı Cad. 1");
        string kapali = await SubeAcAsync(yonetici, "Kapalı Cad. 2");
        await DurumAsync(yonetici, gecici, "close-temporarily");
        await DurumAsync(yonetici, kapali, "close");

        string yanit = await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr");
        string belge = await Istemci().GetStringAsync("/tr/contact");

        foreach (string ad in new[] { gecici, kapali })
        {
            Assert.DoesNotContain(ad, yanit, StringComparison.Ordinal);
            Assert.DoesNotContain(ad, belge, StringComparison.Ordinal);
        }
    }

    [Fact(DisplayName = "0028/AC-91, AC-92 (sunucu yarısı): Her karakteri \"–\" olan 300 karakterlik tr adresli koordinatsız şubenin `map`'i null; belgedeki öğesi `hasMap` taşımaz, adresi taşır")]
    public async Task Okuma_SiniriAsanAdres()
    {
        await KurAsync();
        string adres = new('–', 300);
        string ad = await SubeAcAsync(await YoneticiAsync(), adres);

        Assert.Equal(JsonValueKind.Null, (await PublicSubeAsync(ad)).GetProperty("map").ValueKind);
        JsonElement oge = await DepartmentAsync(ad);
        Assert.DoesNotContain("hasMap", oge.EnumerateObject().Select(p => p.Name));
        Assert.Equal(adres, oge.GetProperty("address").GetString());
    }

    [Fact(DisplayName = "0028/AC-92, QA 2 BULGU-3: Adresi yalnız denetim karakteri (`\\u0001\\u0002`) olan koordinatsız şubenin `map`'i null; belgedeki öğesi `hasMap` taşımaz")]
    public async Task Okuma_BosSorgu()
    {
        await KurAsync();
        string ad = await SubeAcAsync(await YoneticiAsync(), "\u0001\u0002");

        Assert.Equal(JsonValueKind.Null, (await PublicSubeAsync(ad)).GetProperty("map").ValueKind);
        Assert.DoesNotContain("hasMap", (await DepartmentAsync(ad)).EnumerateObject().Select(p => p.Name));
    }

    [Fact(DisplayName = "0028/AC-97: tr adresi `Bağdat Cad.\\nNo:5\\t\\t1` olan koordinatsız şubenin sorgusu tam `Ba%C4%9Fdat%20Cad.%20No%3A5%201`")]
    public async Task Okuma_DenetimKarakterleri()
    {
        await KurAsync();
        string ad = await SubeAcAsync(await YoneticiAsync(), "Bağdat Cad.\nNo:5\t\t1");

        Assert.Equal(
            "https://www.google.com/maps/search/?api=1&query=Ba%C4%9Fdat%20Cad.%20No%3A5%201",
            (await PublicSubeAsync(ad)).GetProperty("map").GetProperty("linkUrl").GetString());
    }

    [Fact(DisplayName = "0028/AC-98: Varsayılan dili ar olan kurulumda koordinatsız şubenin sorgusu ar adresinin kodlaması; `locale=tr` isteğinde de")]
    public async Task Okuma_VarsayilanDilAr()
    {
        await KurAsync(varsayilanDil: "ar");
        const string ArAdres = "شارع بغداد ٥";
        string ad = await SubeAcAsync(await YoneticiAsync(), "Bağdat Cad. 5", arAdres: ArAdres, adDili: "ar");
        string beklenen = "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(ArAdres);

        Assert.Equal(beklenen, (await PublicSubeAsync(ad, "ar")).GetProperty("map").GetProperty("linkUrl").GetString());
        Assert.Equal(beklenen, (await PublicSubeAsync(ad, "tr")).GetProperty("map").GetProperty("linkUrl").GetString());
    }

    // ----- Anahtarın gizliliği (PM şartı (a)) -----

    [Fact(DisplayName = "0028/AC-82, AC-83: Anahtar `test-anahtar` iken `/tr/`, `/tr/cars`, görünür bir ilanın detayı, `/tr/contact`, `/ar/contact` ve 404 belgesi anahtarı taşımaz; başlangıçta, okumada ve belgelerde yakalanan hiçbir günlük kaydı (Trace, kapsamlar dahil) taşımaz")]
    public async Task Anahtar_BelgelerdeVeGunlukteYok()
    {
        await KurAsync();
        await SubeAcAsync(await YoneticiAsync(), "Bağdat Cad. 1", enlem: 40.98765m, boylam: 29.0275m);
        string ilan = await IlanAsync();

        // Okuma ucu anahtarı taşır (gömme adresi); belgeler taşımaz.
        Assert.Contains(Anahtar, await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr"), StringComparison.Ordinal);

        foreach (string adres in new[] { "/tr/", "/tr/cars", $"/tr/cars/{ilan}", "/tr/contact", "/ar/contact", "/tr/yok/boyle/bir/yol" })
        {
            HttpResponseMessage yanit = await Istemci().GetAsync(adres);
            string html = await yanit.Content.ReadAsStringAsync();
            Assert.DoesNotContain(Anahtar, html, StringComparison.Ordinal);
            Assert.DoesNotContain(Anahtar, yanit.Headers.ToString() + yanit.Content.Headers, StringComparison.Ordinal);
        }

        // Yakalama kanıtı: düşük düzeyli kayıtlar ve okumanın sorgusu da günlükte (ölçü boş bir kümede yapılmıyor).
        Assert.Contains(gunluk.Kayitlar, kayit => kayit.StartsWith("Debug", StringComparison.Ordinal) || kayit.StartsWith("Trace", StringComparison.Ordinal));
        Assert.Contains(gunluk.Kayitlar, kayit => kayit.Contains("dealership", StringComparison.Ordinal) && kayit.Contains("branches", StringComparison.Ordinal));
        Assert.DoesNotContain(gunluk.Kayitlar, kayit => kayit.Contains(Anahtar, StringComparison.Ordinal));
    }

    [Fact(DisplayName = "0028/AC-83 (hata yolu): Okuma ucu 500 döndüğünde ve iletişim belgesi okumasız basıldığında yakalanan hiçbir günlük kaydı (istisna dahil) anahtarı taşımaz")]
    public async Task Anahtar_HataYolundaGunlukteYok()
    {
        await KurAsync();
        await SubeAcAsync(await YoneticiAsync(), "Bağdat Cad. 1", enlem: 40.98765m, boylam: 29.0275m);
        await SqlAsync("ALTER TABLE dealership.branches RENAME COLUMN latitude TO latitude_bozuk");

        Assert.Equal(HttpStatusCode.InternalServerError, (await Istemci().GetAsync("/api/dealership/gallery?locale=tr")).StatusCode);
        HttpResponseMessage belge = await Istemci().GetAsync("/tr/contact");
        Assert.Equal(HttpStatusCode.OK, belge.StatusCode);
        Assert.DoesNotContain(Anahtar, await belge.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        Assert.Contains(gunluk.Kayitlar, kayit => kayit.Contains("latitude", StringComparison.Ordinal));
        Assert.DoesNotContain(gunluk.Kayitlar, kayit => kayit.Contains(Anahtar, StringComparison.Ordinal));
    }

    // ----- Yardımcılar -----

    private static string[] Anahtarlar(JsonElement oge) => [.. oge.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)];

    private static JsonElement Sube(JsonElement[] subeler, string ad) => subeler.Single(oge => oge.GetProperty("name").GetString() == ad);

    private async Task<HttpClient> YoneticiAsync()
    {
        DateTimeOffset giris = girisAni;
        girisAni = girisAni.AddSeconds(90);
        saat.Simdi = giris;
        HttpClient yonetici = Istemci();
        await IkinciAdim.GirAsync(yonetici, IdentityTestSettings.SeedAdminLoginId, CalisanParola, sir, giris);
        return yonetici;
    }

    /// <summary>Şube açar; benzersiz adını döner. Ad <paramref name="adDili"/>'nde, adres tr'de (ve varsa ar'da).</summary>
    private async Task<string> SubeAcAsync(
        HttpClient yonetici,
        string adres,
        string? email = null,
        decimal? enlem = null,
        decimal? boylam = null,
        string? whatsapp = null,
        string? arAdres = null,
        string adDili = "tr")
    {
        string ad = $"İletişim Şubesi {Interlocked.Increment(ref sayac)}-{Guid.NewGuid().ToString("N")[..4]}";
        JsonObject adresler = new() { ["tr"] = adres };

        if (arAdres is not null)
        {
            adresler["ar"] = arAdres;
        }

        JsonObject govde = new()
        {
            ["names"] = new JsonObject { [adDili] = ad },
            ["addresses"] = adresler,
            ["phone"] = "+902165554433",
            ["timeZone"] = "Europe/Istanbul",
            ["slug"] = $"iletisim-{Interlocked.Increment(ref sayac)}-{Guid.NewGuid().ToString("N")[..6]}",
            ["email"] = email,
            ["whatsApp"] = whatsapp,
            ["latitude"] = enlem,
            ["longitude"] = boylam,
        };

        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync(Subeler, govde);
        Assert.True(yanit.StatusCode == HttpStatusCode.Created, $"{(int)yanit.StatusCode}: {await yanit.Content.ReadAsStringAsync()}");
        return ad;
    }

    private static async Task<Guid> KimlikAsync(HttpClient yonetici, string ad)
    {
        JsonElement liste = JsonDocument.Parse(await yonetici.GetStringAsync(Subeler)).RootElement;
        JsonElement ogeler = liste.ValueKind == JsonValueKind.Array ? liste : liste.GetProperty("items");
        return ogeler.EnumerateArray()
            .Single(oge => oge.EnumerateObject().Any(p => p.Value.ValueKind == JsonValueKind.String && p.Value.GetString() == ad))
            .GetProperty("branchId").GetGuid();
    }

    private static async Task<HttpResponseMessage> KaydetAsync(HttpClient yonetici, Guid sube, Action<JsonObject> degistir)
    {
        JsonObject govde = JsonNode.Parse(await yonetici.GetStringAsync($"{Subeler}/{sube}"))!.AsObject();
        degistir(govde);
        return await yonetici.PutAsync(
            new Uri($"{Subeler}/{sube}", UriKind.Relative),
            new StringContent(govde.ToJsonString(), Encoding.UTF8, "application/json"));
    }

    private static async Task DurumAsync(HttpClient yonetici, string ad, string eylem)
    {
        Guid sube = await KimlikAsync(yonetici, ad);
        int surum = JsonDocument.Parse(await yonetici.GetStringAsync($"{Subeler}/{sube}")).RootElement.GetProperty("version").GetInt32();
        await BasariliAsync(await yonetici.PostAsJsonAsync($"{Subeler}/{sube}/{eylem}", new { version = surum }));
    }

    private static async Task BasariliAsync(HttpResponseMessage yanit) =>
        Assert.True(yanit.IsSuccessStatusCode, $"{(int)yanit.StatusCode}: {await yanit.Content.ReadAsStringAsync()}");

    private async Task<JsonElement> PublicSubeAsync(string ad, string dil = "tr") =>
        Sube([.. JsonDocument.Parse(await Istemci().GetStringAsync($"/api/dealership/gallery?locale={dil}")).RootElement.GetProperty("branches").EnumerateArray()], ad);

    private async Task<JsonElement> DepartmentAsync(string ad) =>
        Assert.Single(new AngleSharp.Html.Parser.HtmlParser().ParseDocument(await Istemci().GetStringAsync("/tr/contact")).JsonLd())
            .GetProperty("department").EnumerateArray().Single(oge => oge.GetProperty("name").GetString() == ad);

    /// <summary>AC-82 — görünür bir ilan: geliştirme tohumunun gerçek yolu (referans verisi + bir ilan).</summary>
    private async Task<string> IlanAsync()
    {
        await factory!.Services.SeedVehicleReferenceForDevelopmentAsync();
        Guid model = await SkalerAsync<Guid>("SELECT id FROM vehicle_reference.vehicle_models LIMIT 1");
        await factory.Services.SeedVehicleCatalogForDevelopmentAsync(1, [model]);
        return await SkalerAsync<string>("SELECT slug FROM vehicle_catalog.showcase_listings LIMIT 1");
    }

    private async Task<T> SkalerAsync<T>(string sql)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private async Task SqlAsync(string sql)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = new(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private HttpClient Istemci() => factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    private sealed class AyarliSaat : IClock
    {
        internal DateTimeOffset Simdi { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UtcNow => Simdi;
    }

    /// <summary>
    /// AC-83 — her kaydın iletisi, istisnası (tam metni) ve o andaki kapsamların hepsi tek bir metne
    /// yazılır; ölçü bu metinlerde anahtarın geçmemesidir.
    /// </summary>
    private sealed class Gunluk : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider kapsamlar = new LoggerExternalScopeProvider();

        internal ConcurrentQueue<string> Kuyruk { get; } = new();

        internal IReadOnlyCollection<string> Kayitlar => Kuyruk;

        public ILogger CreateLogger(string categoryName) => new Yazici(this, categoryName);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider) => kapsamlar = scopeProvider;

        public void Dispose()
        {
        }

        private sealed class Yazici(Gunluk gunluk, string kategori) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => gunluk.kapsamlar.Push(state);

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                StringBuilder metin = new($"{logLevel} {kategori} {formatter(state, exception)} {state} {exception}");
                gunluk.kapsamlar.ForEachScope((kapsam, yazilan) => yazilan.Append(" | ").Append(kapsam), metin);
                gunluk.Kuyruk.Enqueue(metin.ToString());
            }
        }
    }
}
