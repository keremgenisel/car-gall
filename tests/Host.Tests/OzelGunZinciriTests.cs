using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0027` — özel günler ve "şu an", GERÇEK zincirde: gerçek PostgreSQL, gerçek uçlar, Yönetici oturumu.
/// "Saat T" (spec varsayılanı) Host'un `IClock`'u sabit <c>T</c> döndürür demektir: saat en SONDA
/// değiştirilir (`OturumBoruHattiTests:76-78`, modüller kendi `IClock`'larını kaydediyor). Her senaryo
/// saati kendi anına kurar ve Yöneticiye O ANDA girer (ikinci adım kodu sunucunun anıyla); yani testler
/// takvimden bağımsızdır, hangi gün koşarsa koşsun aynı sonucu verir.
/// </summary>
public sealed class OzelGunZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";
    private const string Galeri = $"{AdminSurface.RoutePrefix}/gallery";
    private const string Subeler = $"{AdminSurface.RoutePrefix}/branches";

    private static readonly string[] Gunler = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private readonly AyarliSaat saat = new();
    private WebApplicationFactory<Program>? factory;
    private string sir = string.Empty;
    private int sayac;

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

            // Saat en SONDA: modüller kendi `IClock`'larını kaydediyor ve son kayıt kazanıyor.
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

        HttpClient yonetici = await YoneticiAsync(saat.UtcNow);
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsJsonAsync(Galeri, new
        {
            version = JsonDocument.Parse(await yonetici.GetStringAsync(Galeri)).RootElement.GetProperty("version").GetInt32(),
            commercialName = "Oto Galeri",
            displayNames = new Dictionary<string, string> { ["tr"] = "Oto Galeri Vitrin" },
        })).StatusCode);
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    // ----- "Şu an" hesabı (R2, R7, R8 · D3, D6, D7, D8) -----

    /// <summary>
    /// Senaryo: etiket · an · program (<c>gün=HH:mm-HH:mm,…;…</c>) · saat dilimi · özel günler
    /// (<c>from..through=aralıklar;…</c>, aralık yoksa tam gün kapalı) · beklenen açık · bugün · next
    /// (<c>YYYY-MM-DD HH:mm</c> ya da null) · beklenen süre (yoksa yalnız 1–90000 ölçülür, AC-83).
    /// </summary>
    public static TheoryData<string, string, string, string, string, bool, string, string?, int?> SuAnSenaryolari() => new()
    {
        { "AC-30", "2026-12-04T09:00:00Z", Programli, "Europe/Istanbul", "", true, "2026-12-04", "2026-12-04 18:00", 21600 },
        { "AC-31", "2026-12-04T16:00:00Z", Programli, "Europe/Istanbul", "", false, "2026-12-04", "2026-12-05 10:00", 18000 },
        { "AC-32", "2026-12-05T12:00:00Z", Programli, "Europe/Istanbul", "", false, "2026-12-05", "2026-12-07 09:00", null },
        { "AC-33", "2026-12-06T11:00:00Z", Kuveyt, "Asia/Kuwait", "", false, "2026-12-06", "2026-12-06 16:00", null },
        { "AC-34", "2027-01-01T09:00:00Z", Programli, "Europe/Istanbul", "2027-01-01..2027-01-01=", false, "2027-01-01", "2027-01-02 10:00", null },
        { "AC-35", "2027-03-09T14:00:00Z", Programli, "Europe/Istanbul", "2027-02-08..2027-03-09=10:00-16:00;2027-03-10..2027-03-12=", false, "2027-03-09", "2027-03-13 10:00", null },
        { "AC-36", "2027-03-09T08:00:00Z", Programli, "Europe/Istanbul", "2027-02-08..2027-03-09=10:00-16:00;2027-03-10..2027-03-12=", true, "2027-03-09", "2027-03-09 16:00", null },
        { "AC-37 (23:30)", "2026-12-04T20:30:00Z", "friday=20:00-23:59;saturday=00:00-02:00,10:00-14:00", "Europe/Istanbul", "", true, "2026-12-04", "2026-12-05 02:00", null },
        { "AC-37 (23:59:30)", "2026-12-04T20:59:30Z", "friday=20:00-23:59;saturday=00:00-02:00,10:00-14:00", "Europe/Istanbul", "", true, "2026-12-04", "2026-12-05 02:00", null },
        { "AC-38", "2026-12-04T09:00:00Z", HerGun, "Europe/Istanbul", "", true, "2026-12-04", null, null },
        { "AC-39", "2027-03-29T07:30:00Z", "monday=09:00-18:00", "Europe/Berlin", "", true, "2027-03-29", "2027-03-29 18:00", null },
        { "AC-84", "2027-03-27T23:00:00Z", "monday=09:00-18:00", "Europe/Berlin", "", false, "2027-03-28", "2027-03-29 09:00", 82800 },
        { "AC-85", "2027-10-30T22:00:00Z", "monday=09:00-18:00", "Europe/Berlin", "", false, "2027-10-31", "2027-11-01 09:00", 90000 },
        { "AC-86", "2027-03-28T00:30:00Z", "sunday=01:00-04:00", "Europe/Berlin", "", true, "2027-03-28", "2027-03-28 04:00", 5400 },
        { "AC-87 (01:45)", "2027-03-28T00:45:00Z", "sunday=02:30-05:00", "Europe/Berlin", "", false, "2027-03-28", "2027-03-28 03:00", 900 },
        { "AC-87 (03:00)", "2027-03-28T01:00:00Z", "sunday=02:30-05:00", "Europe/Berlin", "", true, "2027-03-28", "2027-03-28 05:00", 7200 },
        { "AC-88", "2027-10-31T00:30:00Z", "sunday=01:00-04:00", "Europe/Berlin", "", true, "2027-10-31", "2027-10-31 04:00", 9000 },
        { "AC-89 (ilk 02:15)", "2027-10-31T00:15:00Z", "sunday=02:30-05:00", "Europe/Berlin", "", false, "2027-10-31", "2027-10-31 02:30", 900 },
        { "AC-89 (ikinci 02:15)", "2027-10-31T01:15:00Z", "sunday=02:30-05:00", "Europe/Berlin", "", true, "2027-10-31", "2027-10-31 05:00", 9900 },
        { "AC-90 (açık)", "2027-10-31T00:15:00Z", "sunday=01:00-02:30", "Europe/Berlin", "", true, "2027-10-31", "2027-10-31 02:30", 900 },
        { "AC-90 (ikinci 02:15)", "2027-10-31T01:15:00Z", "sunday=01:00-02:30", "Europe/Berlin", "", false, "2027-10-31", "2027-11-07 01:00", null },
        { "AC-91 (23:30)", "2027-03-27T21:30:00Z", "saturday=20:00-23:59;sunday=00:00-02:00", "Asia/Beirut", "", true, "2027-03-27", "2027-03-28 02:00", 1800 },
        { "AC-91 (01:00)", "2027-03-27T22:00:00Z", "saturday=20:00-23:59;sunday=00:00-02:00", "Asia/Beirut", "", true, "2027-03-28", "2027-03-28 02:00", 3600 },
        { "AC-92", "2026-12-05T02:00:00Z", Programli, "America/New_York", "", false, "2026-12-04", "2026-12-05 10:00", 10800 },
        { "AC-93", "2026-12-04T18:00:00Z", "friday=20:00-23:59", "Europe/Istanbul", "", true, "2026-12-04", "2026-12-05 00:00", 10800 },
        { "AC-94", "2026-12-07T09:00:00Z", PazarKapali, "Europe/Istanbul", "", true, "2026-12-07", "2026-12-13 00:00", 43200 },
        { "AC-95", "2027-03-27T23:30:00Z", "monday=09:00-18:00", "Europe/Berlin", "", false, "2027-03-28", "2027-03-29 09:00", 81000 },
        { "AC-96", "2026-12-05T09:00:00Z", Programli, "Europe/Istanbul", "2026-12-05..2026-12-05=", false, "2026-12-05", "2026-12-07 09:00", null },
        { "AC-97", "2027-01-03T09:00:00Z", Programli, "Europe/Istanbul", "2027-01-04..2027-01-13=", false, "2027-01-03", "2027-01-14 09:00", null },
        { "AC-98 (18:00:00)", "2026-12-04T15:00:00Z", Programli, "Europe/Istanbul", "", false, "2026-12-04", "2026-12-05 10:00", null },
        { "AC-98 (09:00:00)", "2026-12-04T06:00:00Z", Programli, "Europe/Istanbul", "", true, "2026-12-04", "2026-12-04 18:00", null },
        { "AC-98 (17:59:59.5)", "2026-12-04T14:59:59.5Z", Programli, "Europe/Istanbul", "", true, "2026-12-04", "2026-12-04 18:00", 1 },
        { "AC-99 (+7)", "2026-12-07T08:00:00Z", "monday=09:00-10:00", "Europe/Istanbul", "", false, "2026-12-07", "2026-12-14 09:00", null },
        { "AC-99 (through+1)", "2026-12-07T08:00:00Z", "monday=09:00-10:00", "Europe/Istanbul", "2026-12-14..2026-12-20=", false, "2026-12-07", "2026-12-21 09:00", null },
    };

    private const string Programli = "monday=09:00-18:00;tuesday=09:00-18:00;wednesday=09:00-18:00;thursday=09:00-18:00;friday=09:00-18:00;saturday=10:00-14:00";
    private const string Kuveyt = "sunday=09:00-13:00,16:00-21:00;monday=09:00-13:00,16:00-21:00;tuesday=09:00-13:00,16:00-21:00;wednesday=09:00-13:00,16:00-21:00;thursday=09:00-13:00,16:00-21:00";
    private const string HerGun = "monday=00:00-23:59;tuesday=00:00-23:59;wednesday=00:00-23:59;thursday=00:00-23:59;friday=00:00-23:59;saturday=00:00-23:59;sunday=00:00-23:59";
    private const string PazarKapali = "monday=00:00-23:59;tuesday=00:00-23:59;wednesday=00:00-23:59;thursday=00:00-23:59;friday=00:00-23:59;saturday=00:00-23:59";

    [Theory(DisplayName = "0027/AC-30–AC-39, AC-83–AC-99: \"Şu an\" durumu sabit `IClock`'la, şubenin saat diliminde, özel günler dahil; `refreshInSeconds` 1–90000")]
    [MemberData(nameof(SuAnSenaryolari))]
    public async Task SuAn_Senaryo(string etiket, string an, string program, string bolge, string ozel, bool acik, string bugun, string? sonraki, int? sure)
    {
        JsonElement durum = (await PublicSubeAsync(await SubeAcAsync(Zaman(an), Program(program), bolge, OzelGunler(ozel)))).GetProperty("openNow");

        Assert.Equal(acik, durum.GetProperty("open").GetBoolean());
        Assert.Equal(bugun, durum.GetProperty("today").GetString());
        Assert.Equal(
            sonraki,
            durum.GetProperty("next").ValueKind == JsonValueKind.Null
                ? null
                : $"{durum.GetProperty("next").GetProperty("date").GetString()} {durum.GetProperty("next").GetProperty("time").GetString()}");

        // AC-83 — her yanıtta 1–90000.
        int yenileme = durum.GetProperty("refreshInSeconds").GetInt32();
        Assert.InRange(yenileme, 1, 90000);

        if (sure is not null)
        {
            Assert.True(sure == yenileme, $"{etiket}: refreshInSeconds {yenileme}, beklenen {sure}");
        }
    }

    [Fact(DisplayName = "0027/AC-41: Saat dilimi İstanbul'dan Tokyo'ya değişen şubenin kayıtları değişmez; Tokyo'da Cuma 18:00 kapalı, açılış Cumartesi 10:00")]
    public async Task SaatDilimiDegisir_KayitlarAyni()
    {
        DateTimeOffset an = Zaman("2026-12-04T09:00:00Z");
        string ad = await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", OzelGunler("2026-12-31..2026-12-31="));
        Assert.True((await PublicSubeAsync(ad)).GetProperty("openNow").GetProperty("open").GetBoolean());

        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, ad);
        JsonObject govde = await GovdeAsync(yonetici, sube);
        string onceProgram = govde["openingHours"]!.ToJsonString();
        string onceOzel = govde["specialHours"]!.ToJsonString();
        govde["timeZone"] = "Asia/Tokyo";
        await BasariliAsync(await yonetici.PutAsJsonAsync($"{Subeler}/{sube}", govde));

        JsonObject sonra = await GovdeAsync(yonetici, sube);
        Assert.Equal(onceProgram, sonra["openingHours"]!.ToJsonString());
        Assert.Equal(onceOzel, sonra["specialHours"]!.ToJsonString());

        JsonElement durum = (await PublicSubeAsync(ad)).GetProperty("openNow");
        Assert.Equal("2026-12-04", durum.GetProperty("today").GetString());
        Assert.False(durum.GetProperty("open").GetBoolean());
        Assert.Equal("""{"date":"2026-12-05","time":"10:00"}""", durum.GetProperty("next").GetRawText());
    }

    // ----- Ziyaretçiye dönük okuma (R10, C3 · D8) -----

    [Fact(DisplayName = "0027/AC-42, AC-43, AC-47: Şube öğesi `address, name, openNow, openingHours, phone, specialHours`; `openNow` `next, open, refreshInSeconds, today`; saat dilimi yanıtta yok; `Cache-Control: no-store`")]
    public async Task OkumaBicimi()
    {
        string ad = await SubeAcAsync(Zaman("2026-12-04T09:00:00Z"), Program(Programli), "Asia/Tokyo", OzelGunler("2026-12-10..2026-12-10="));

        HttpResponseMessage yanit = await Istemci().GetAsync(new Uri("/api/dealership/gallery?locale=tr", UriKind.Relative));
        Assert.Equal("no-store", yanit.Headers.CacheControl?.ToString());
        string metin = await yanit.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Asia/Tokyo", metin, StringComparison.Ordinal);
        Assert.DoesNotContain("timeZone", metin, StringComparison.OrdinalIgnoreCase);

        JsonElement oge = JsonDocument.Parse(metin).RootElement.GetProperty("branches").EnumerateArray().Single(item => item.GetProperty("name").GetString() == ad);
        Assert.Equal(["address", "name", "openNow", "openingHours", "phone", "specialHours"], oge.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["next", "open", "refreshInSeconds", "today"], oge.GetProperty("openNow").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal("""[{"from":"2026-12-10","through":"2026-12-10","intervals":[]}]""", oge.GetProperty("specialHours").GetRawText());
    }

    [Fact(DisplayName = "0027/AC-40: Programsız şubenin `openNow`'u null, `specialHours`'u [] — özel günleri olsa bile")]
    public async Task Programsiz()
    {
        string ad = await SubeAcAsync(Zaman("2026-12-04T09:00:00Z"), null, "Europe/Istanbul", OzelGunler("2026-12-10..2026-12-10="));
        JsonElement oge = await PublicSubeAsync(ad);

        Assert.Equal(JsonValueKind.Null, oge.GetProperty("openNow").ValueKind);
        Assert.Equal("[]", oge.GetProperty("specialHours").GetRawText());
    }

    [Fact(DisplayName = "0027/AC-44, AC-45: Pencere bugün + 30 gün: 2026-12-31 var, 2027-01-01 yok; süren kayıt (2027-02-08–03-09) 2027-03-01'de var")]
    public async Task Pencere()
    {
        string ad = await SubeAcAsync(Zaman("2026-12-01T09:00:00Z"), Program(Programli), "Europe/Istanbul", OzelGunler("2026-12-31..2026-12-31=;2027-01-01..2027-01-01=;2027-02-08..2027-03-09=10:00-16:00"));
        Assert.Equal(["2026-12-31"], (await PublicSubeAsync(ad)).GetProperty("specialHours").EnumerateArray().Select(item => item.GetProperty("from").GetString()));

        saat.Simdi = Zaman("2027-03-01T09:00:00Z");
        Assert.Equal(["2027-02-08"], (await PublicSubeAsync(ad)).GetProperty("specialHours").EnumerateArray().Select(item => item.GetProperty("from").GetString()));
    }

    [Fact(DisplayName = "0027/AC-46: tr, ar ve en yanıtlarında aynı şubenin `openNow`'u ve `specialHours`'u aynıdır")]
    public async Task DildenBagimsiz()
    {
        string ad = await SubeAcAsync(Zaman("2026-12-04T09:00:00Z"), Program(Programli), "Europe/Istanbul", OzelGunler("2026-12-10..2026-12-12=10:00-13:00"));
        string[] degerler = await Task.WhenAll(new[] { "tr", "ar", "en" }.Select(async dil =>
        {
            JsonElement oge = JsonDocument.Parse(await Istemci().GetStringAsync($"/api/dealership/gallery?locale={dil}")).RootElement
                .GetProperty("branches").EnumerateArray().Single(item => item.GetProperty("phone").GetString() == "+902165554433" && item.GetProperty("openNow").ValueKind != JsonValueKind.Null && item.GetProperty("specialHours").GetArrayLength() == 1);
            return oge.GetProperty("openNow").GetRawText() + oge.GetProperty("specialHours").GetRawText();
        }));

        Assert.Single(degerler.Distinct());
        Assert.NotEmpty(ad);
    }

    // ----- Panel: kaydetme ve okuma (R1, R5 · D2, D13) -----

    [Fact(DisplayName = "0027/AC-2, AC-3, AC-4, AC-5, AC-9, AC-103 (sunucu yarısı): Kayıtlar saklanır ve panel okumasında `from` sırasıyla döner; boş/null/yok bitiş başlangıçla aynı; iki aralık sırasıyla")]
    public async Task Kaydeder_PanelOkumasi()
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));

        JsonArray ozel =
        [
            new JsonObject { ["from"] = "2027-02-08", ["through"] = "2027-03-09", ["intervals"] = Araliklar("10:00-16:00") },
            new JsonObject { ["from"] = "2027-01-10", ["intervals"] = Araliklar("16:00-21:00", "09:00-13:00") },
            new JsonObject { ["from"] = "2027-01-01", ["through"] = null, ["intervals"] = new JsonArray() },
        ];
        await BasariliAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = ozel));

        Assert.Equal(
            """[{"from":"2027-01-01","through":"2027-01-01","intervals":[]},{"from":"2027-01-10","through":"2027-01-10","intervals":[{"start":"09:00","end":"13:00"},{"start":"16:00","end":"21:00"}]},{"from":"2027-02-08","through":"2027-03-09","intervals":[{"start":"10:00","end":"16:00"}]}]""",
            (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString());
    }

    [Fact(DisplayName = "0027/AC-8 (sunucu yarısı): Özel günle açılan şubenin kaydı saklanır")]
    public async Task Acma_OzelGunle()
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", OzelGunler("2027-01-01..2027-01-01="), yonetici));

        Assert.Equal("""[{"from":"2027-01-01","through":"2027-01-01","intervals":[]}]""", (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString());
    }

    public static TheoryData<string, string, string> HataliGovdeler() => new()
    {
        { """[{"from":"2027-02-30","intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"from":"2027-13-01","intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"from":"27.11.2026","intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"from":"2027-1-1","intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"from":"","intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"from":"2027-01-01\n","intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"from":"2027-01\u00000-01","intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"from":null,"intervals":[]}]""", "specialHours.0.from", "special_hours.invalid_date" },
        { """[{"from":"2027-01-01","through":"","intervals":[]}]""", "specialHours.0.through", "special_hours.invalid_date" },
        { """[null]""", "specialHours.0", "special_hours.invalid_entry" },
        { """[{"from":"2027-01-01"}]""", "specialHours.0.intervals", "special_hours.intervals_required" },
        { """[{"from":"2027-01-01","intervals":null}]""", "specialHours.0.intervals", "special_hours.intervals_required" },
        { """[{"from":"2027-01-01","intervals":[null]}]""", "specialHours.0.intervals.0", "special_hours.invalid_entry" },
        { """[{"from":"2027-01-01","intervals":[{"start":null,"end":"13:00"}]}]""", "specialHours.0.intervals.0.start", "opening_hours.invalid_time" },
        { """[{"from":"2027-01-01","intervals":[{"start":"09:00"}]}]""", "specialHours.0.intervals.0.end", "opening_hours.invalid_time" },
        { """[{"from":"2027-03-10","through":"2027-03-09","intervals":[]}]""", "specialHours.0.through", "special_hours.end_before_start" },
        { """[{"from":"2027-01-01","through":"2027-03-02","intervals":[]}]""", "specialHours.0.through", "special_hours.too_long" },
        { """[{"from":"2027-11-29","intervals":[]}]""", "specialHours.0.through", "special_hours.too_far" },
        { """[{"from":"2027-03-08","through":"2027-03-10","intervals":[]},{"from":"2027-03-10","through":"2027-03-12","intervals":[]}]""", "specialHours.1.from", "special_hours.overlap" },
        { """[{"from":"2027-01-01","intervals":[{"start":"9:00","end":"13:00"}]}]""", "specialHours.0.intervals.0.start", "opening_hours.invalid_time" },
        { """[{"from":"2027-01-01","intervals":[{"start":"09:00","end":"13:00"},{"start":"13:00","end":"18:00"}]}]""", "specialHours.0", "opening_hours.overlap" },
        { """[{"from":"2027-01-01","intervals":[{"start":"08:00","end":"09:00"},{"start":"10:00","end":"11:00"},{"start":"12:00","end":"13:00"},{"start":"14:00","end":"15:00"}]}]""", "specialHours.0", "opening_hours.too_many_intervals" },
    };

    [Theory(DisplayName = "0027/AC-10–AC-14, AC-17–AC-19: Hatalı özel gün `422`, belirtilen alan ve kod; hiçbir girdi `500` dönmez (NUL, satır sonu, var olmayan gün, şekil girdileri dâhil). Saat 2026-11-27T09:00Z")]
    [MemberData(nameof(HataliGovdeler))]
    public async Task Doner422_HataliOzelGun(string liste, string alan, string kod)
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));

        JsonElement problem = await ProblemAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = JsonNode.Parse(liste)), HttpStatusCode.UnprocessableEntity);
        Assert.Equal(kod, problem.GetProperty(alan).GetString());
    }

    [Fact(DisplayName = "0027/AC-12, AC-13, AC-15: 60 gün kabul; 366 gün ileri kabul; uç uca değen iki kayıt kabul (Saat 2026-11-27T09:00Z)")]
    public async Task Sinirda_Kabul()
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));

        await BasariliAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = OzelGunler("2027-01-01..2027-03-01=;2027-11-28..2027-11-28=;2027-03-08..2027-03-09=;2027-03-10..2027-03-12=")));
        Assert.Equal(4, (await GovdeAsync(yonetici, sube))["specialHours"]!.AsArray().Count);
    }

    [Fact(DisplayName = "0027/AC-16: 13 güncel/gelecek kayıt `422` ve `specialHours` alanında `special_hours.too_many`")]
    public async Task OnUcKayit()
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));
        string onUc = string.Join(';', Enumerable.Range(1, 13).Select(gun => $"2027-01-{gun:00}..2027-01-{gun:00}="));

        JsonElement problem = await ProblemAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = OzelGunler(onUc)), HttpStatusCode.UnprocessableEntity);
        Assert.Equal("special_hours.too_many", problem.GetProperty("specialHours").GetString());
    }

    [Fact(DisplayName = "0027/AC-20, AC-21: Hatalı özel günle kaydetmede şubenin hiçbir alanı (adres, haftalık program) değişmez; iki kaydın farklı hataları tek yanıtta")]
    public async Task HataliKaydetme_HicbirAlanDegismez()
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));
        string once = (await GovdeAsync(yonetici, sube)).ToJsonString();

        JsonElement problem = await ProblemAsync(
            await KaydetAsync(yonetici, sube, govde =>
            {
                govde["addresses"] = new JsonObject { ["tr"] = "Yeni Adres 9" };
                govde["openingHours"] = Program("monday=10:00-11:00");
                govde["specialHours"] = JsonNode.Parse("""[{"from":"2027-02-30","intervals":[]},{"from":"2027-03-10","through":"2027-03-09","intervals":[]}]""");
            }),
            HttpStatusCode.UnprocessableEntity);

        Assert.Equal("special_hours.invalid_date", problem.GetProperty("specialHours.0.from").GetString());
        Assert.Equal("special_hours.end_before_start", problem.GetProperty("specialHours.1.through").GetString());
        Assert.Equal(once, (await GovdeAsync(yonetici, sube)).ToJsonString());
    }

    [Fact(DisplayName = "0027/AC-23, AC-104: Geçmiş ama geçerli kayıt sessizce düşer (200); geçmiş ama hatalı kayıt `422` alır; indeks gönderilenindir (Saat 2027-01-05)")]
    public async Task Gecmis_DusmedenOnceDogrulama()
    {
        DateTimeOffset an = Zaman("2027-01-05T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));

        await BasariliAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = OzelGunler("2027-01-01..2027-01-01=;2027-02-10..2027-02-10=")));
        Assert.Equal("""[{"from":"2027-02-10","through":"2027-02-10","intervals":[]}]""", (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString());

        JsonElement iki = await ProblemAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = JsonNode.Parse("""[{"from":"2027-01-01","through":"2027-01-01","intervals":[]},{"from":"2027-02-10","through":"2027-02-01","intervals":[]}]""")), HttpStatusCode.UnprocessableEntity);
        Assert.Equal("special_hours.end_before_start", iki.GetProperty("specialHours.1.through").GetString());

        JsonElement tek = await ProblemAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = JsonNode.Parse("""[{"from":"2027-01-03","through":"2027-01-01","intervals":[]}]""")), HttpStatusCode.UnprocessableEntity);
        Assert.Equal("special_hours.end_before_start", tek.GetProperty("specialHours.0.through").GetString());
    }

    [Fact(DisplayName = "0027/AC-24: 2027-01-01'lik kayıt panel okumasında yerel 23:59'da (20:59Z) var, yerel 2027-01-02 00:00'da (21:00Z) yok")]
    public async Task PanelOkumasi_GunDegisimi()
    {
        DateTimeOffset an = Zaman("2026-12-30T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", OzelGunler("2027-01-01..2027-01-01="), yonetici));

        HttpClient once = await YoneticiAsync(Zaman("2027-01-01T20:59:00Z"));
        Assert.Single((await GovdeAsync(once, sube))["specialHours"]!.AsArray());

        HttpClient sonra = await YoneticiAsync(Zaman("2027-01-01T21:00:00Z"));
        Assert.Empty((await GovdeAsync(sonra, sube))["specialHours"]!.AsArray());
    }

    [Fact(DisplayName = "0027/AC-25, AC-26, AC-27: Alan yok ya da null → değişmez; GET→PUT gidiş-dönüşü değiştirmez; [] hepsini siler")]
    public async Task GidisDonus()
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", OzelGunler("2027-01-01..2027-01-01=;2027-02-10..2027-02-12=10:00-13:00"), yonetici));
        string kayitlar = (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString();

        await BasariliAsync(await KaydetAsync(yonetici, sube, govde => govde.Remove("specialHours")));
        Assert.Equal(kayitlar, (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString());

        await BasariliAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = null));
        Assert.Equal(kayitlar, (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString());

        await BasariliAsync(await KaydetAsync(yonetici, sube, _ => { }));
        Assert.Equal(kayitlar, (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString());

        await BasariliAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = new JsonArray()));
        Assert.Empty((await GovdeAsync(yonetici, sube))["specialHours"]!.AsArray());
    }

    [Fact(DisplayName = "0027/AC-28: Eski sürüm `409`; başarılı kaydetme sürümü bir artırır; kapatılmış şubede `409` `branch.closed_read_only`; hiçbirinde outbox'a olay yazılmaz")]
    public async Task Surum_Kapali_Outbox()
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));
        Guid diger = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));
        long outbox = await OutboxAsync();
        int surum = (await GovdeAsync(yonetici, sube))["version"]!.GetValue<int>();

        await BasariliAsync(await KaydetAsync(yonetici, sube, govde => govde["specialHours"] = OzelGunler("2027-01-01..2027-01-01=")));
        Assert.Equal(surum + 1, (await GovdeAsync(yonetici, sube))["version"]!.GetValue<int>());

        JsonElement eski = await ProblemAsync(await KaydetAsync(yonetici, sube, govde =>
        {
            govde["version"] = surum;
            govde["specialHours"] = new JsonArray();
        }), HttpStatusCode.Conflict);
        Assert.Equal("concurrency.lost", eski.GetProperty("errorCode").GetString());
        Assert.Equal(outbox, await OutboxAsync());

        int kapatmaSurumu = (await GovdeAsync(yonetici, diger))["version"]!.GetValue<int>();
        await BasariliAsync(await yonetici.PostAsJsonAsync($"{Subeler}/{diger}/close", new { version = kapatmaSurumu }));
        long kapanistanSonra = await OutboxAsync();
        JsonElement kapali = await ProblemAsync(await KaydetAsync(yonetici, diger, govde => govde["specialHours"] = OzelGunler("2027-01-01..2027-01-01=")), HttpStatusCode.Conflict);
        Assert.Equal("branch.closed_read_only", kapali.GetProperty("errorCode").GetString());
        Assert.Equal(kapanistanSonra, await OutboxAsync());
    }

    [Fact(DisplayName = "0027/AC-29: Satış Danışmanı ve İçerik Editörü `403`, oturumsuz `401`; kayıtlar değişmez; danışmanın şube listesi anahtar kümesi değişmez")]
    public async Task Yetki()
    {
        DateTimeOffset an = Zaman("2026-11-27T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", OzelGunler("2027-01-01..2027-01-01="), yonetici));
        JsonObject govde = await GovdeAsync(yonetici, sube);
        govde["specialHours"] = new JsonArray();
        string kayitlar = (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString();

        HttpClient editor = await PersonelAsync(yonetici, $"editor{Interlocked.Increment(ref sayac)}@ornek.com", "content_editor");
        HttpClient danisman = await PersonelAsync(yonetici, $"danisman{Interlocked.Increment(ref sayac)}@ornek.com", "sales_advisor");

        Assert.Equal(HttpStatusCode.Forbidden, (await editor.PutAsJsonAsync($"{Subeler}/{sube}", govde)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await danisman.PutAsJsonAsync($"{Subeler}/{sube}", govde)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Istemci().PutAsJsonAsync($"{Subeler}/{sube}", govde)).StatusCode);
        Assert.Equal(kayitlar, (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString());

        JsonElement liste = JsonDocument.Parse(await danisman.GetStringAsync(Subeler)).RootElement;
        JsonElement oge = (liste.ValueKind == JsonValueKind.Array ? liste : liste.GetProperty("items")).EnumerateArray().First();
        Assert.DoesNotContain(oge.EnumerateObject(), property => property.Name is "specialHours" or "openNow");
    }

    [Fact(DisplayName = "0027/AC-106: Saklı geçmiş kayıt alansız kaydetmede kalır (veritabanında sayılır); `specialHours` taşıyan kaydetmede silinir")]
    public async Task GecmisKayit_YalnizListeliKaydetmedeSilinir()
    {
        DateTimeOffset kurulum = Zaman("2026-12-20T09:00:00Z");
        HttpClient yonetici = await YoneticiAsync(kurulum);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(kurulum, Program(Programli), "Europe/Istanbul", OzelGunler("2027-01-01..2027-01-01="), yonetici));

        HttpClient sonra = await YoneticiAsync(Zaman("2027-01-05T09:00:00Z"));
        await BasariliAsync(await KaydetAsync(sonra, sube, govde =>
        {
            govde.Remove("specialHours");
            govde["addresses"] = new JsonObject { ["tr"] = "Yeni Adres 10" };
        }));
        Assert.Equal(["2027-01-01"], await SakliAsync(sube));

        await BasariliAsync(await KaydetAsync(sonra, sube, govde => govde["specialHours"] = OzelGunler("2027-02-10..2027-02-10=")));
        Assert.Equal(["2027-02-10"], await SakliAsync(sube));
    }

    [Fact(DisplayName = "0027/AC-107: Saat dilimi ve kayıt tek gövdede: \"bugün\" YENİ saat diliminde — İstanbul'da geçmiş, New York'ta bugün olan kayıt saklanır")]
    public async Task YeniSaatDilimindeBugun()
    {
        DateTimeOffset an = Zaman("2026-11-27T22:30:00Z");
        HttpClient yonetici = await YoneticiAsync(an);
        Guid sube = await KimlikAsync(yonetici, await SubeAcAsync(an, Program(Programli), "Europe/Istanbul", null, yonetici));

        await BasariliAsync(await KaydetAsync(yonetici, sube, govde =>
        {
            govde["timeZone"] = "America/New_York";
            govde["specialHours"] = OzelGunler("2026-11-27..2026-11-27=");
        }));

        Assert.Equal("""[{"from":"2026-11-27","through":"2026-11-27","intervals":[]}]""", (await GovdeAsync(yonetici, sube))["specialHours"]!.ToJsonString());
    }

    // ----- Yardımcılar -----

    private static DateTimeOffset Zaman(string an) => DateTimeOffset.Parse(an, CultureInfo.InvariantCulture);

    /// <summary><c>gün=HH:mm-HH:mm,…;…</c> → yedi günlük harita (yazılmayan gün kapalı).</summary>
    private static JsonObject Program(string metin)
    {
        Dictionary<string, string> gunler = metin.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .ToDictionary(parca => parca.Split('=')[0], parca => parca.Split('=')[1]);
        JsonObject program = [];

        foreach (string gun in Gunler)
        {
            program[gun] = Araliklar(gunler.TryGetValue(gun, out string? araliklar) ? araliklar.Split(',') : []);
        }

        return program;
    }

    private static JsonArray Araliklar(params string[] araliklar) =>
        [.. araliklar.Select(aralik => (JsonNode)new JsonObject { ["start"] = aralik[..5], ["end"] = aralik[6..] })];

    /// <summary><c>from..through=aralıklar;…</c> → özel gün listesi; boş metin → <c>null</c> (alan yok).</summary>
    private static JsonArray? OzelGunler(string metin) =>
        metin.Length == 0
            ? null
            : [.. metin.Split(';').Select(kayit =>
            {
                string[] parca = kayit.Split('=');
                string[] tarih = parca[0].Split("..");
                return (JsonNode)new JsonObject
                {
                    ["from"] = tarih[0],
                    ["through"] = tarih[1],
                    ["intervals"] = parca[1].Length == 0 ? new JsonArray() : Araliklar(parca[1].Split(',')),
                };
            })];

    private async Task<HttpClient> YoneticiAsync(DateTimeOffset an)
    {
        saat.Simdi = an;
        HttpClient yonetici = Istemci();
        await IkinciAdim.GirAsync(yonetici, IdentityTestSettings.SeedAdminLoginId, CalisanParola, sir, an);
        return yonetici;
    }

    /// <summary>Saati <paramref name="an"/>'a kurar ve şube açar; şubenin (benzersiz) adını döner.</summary>
    private async Task<string> SubeAcAsync(DateTimeOffset an, JsonObject? program, string bolge, JsonArray? ozel, HttpClient? yonetici = null)
    {
        yonetici ??= await YoneticiAsync(an);
        saat.Simdi = an;
        string ad = $"Saat Şubesi {Interlocked.Increment(ref sayac)}-{Guid.NewGuid().ToString("N")[..4]}";
        JsonObject govde = new()
        {
            ["names"] = new JsonObject { ["tr"] = ad },
            ["addresses"] = new JsonObject { ["tr"] = "Bağdat Caddesi 1" },
            ["phone"] = "+902165554433",
            ["timeZone"] = bolge,
            ["slug"] = $"saat-{Interlocked.Increment(ref sayac)}-{Guid.NewGuid().ToString("N")[..6]}",
        };

        if (program is not null)
        {
            govde["openingHours"] = program;
        }

        if (ozel is not null)
        {
            govde["specialHours"] = ozel;
        }

        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync(Subeler, govde);
        Assert.True(yanit.StatusCode == HttpStatusCode.Created, $"{(int)yanit.StatusCode}: {await yanit.Content.ReadAsStringAsync()}");
        return ad;
    }

    private async Task<Guid> KimlikAsync(HttpClient yonetici, string ad)
    {
        JsonElement liste = JsonDocument.Parse(await yonetici.GetStringAsync(Subeler)).RootElement;
        JsonElement ogeler = liste.ValueKind == JsonValueKind.Array ? liste : liste.GetProperty("items");
        return ogeler.EnumerateArray()
            .Single(oge => oge.EnumerateObject().Any(p => p.Value.ValueKind == JsonValueKind.String && p.Value.GetString() == ad))
            .GetProperty("branchId").GetGuid();
    }

    private static async Task<JsonObject> GovdeAsync(HttpClient yonetici, Guid sube) =>
        JsonNode.Parse(await yonetici.GetStringAsync($"{Subeler}/{sube}"))!.AsObject();

    private static async Task<HttpResponseMessage> KaydetAsync(HttpClient yonetici, Guid sube, Action<JsonObject> degistir)
    {
        JsonObject govde = await GovdeAsync(yonetici, sube);
        degistir(govde);
        return await yonetici.PutAsync(
            new Uri($"{Subeler}/{sube}", UriKind.Relative),
            new StringContent(govde.ToJsonString(), Encoding.UTF8, "application/json"));
    }

    private static async Task BasariliAsync(HttpResponseMessage yanit) =>
        Assert.True(yanit.IsSuccessStatusCode, $"{(int)yanit.StatusCode}: {await yanit.Content.ReadAsStringAsync()}");

    private async Task<JsonElement> PublicSubeAsync(string ad) =>
        JsonDocument.Parse(await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr")).RootElement
            .GetProperty("branches").EnumerateArray().Single(oge => oge.GetProperty("name").GetString() == ad);

    private async Task<long> OutboxAsync()
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM dealership.outbox_messages";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>Veritabanında saklı kayıtların <c>from</c>'ları (geçmiş olanlar dâhil, okuma süzgeci olmadan).</summary>
    private async Task<List<string>> SakliAsync(Guid sube)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT coalesce(special_hours::text, '[]') FROM dealership.branches WHERE id = @id";
        command.Parameters.AddWithValue("id", sube);
        string json = (string)(await command.ExecuteScalarAsync())!;
        return [.. JsonDocument.Parse(json).RootElement.EnumerateArray().Select(item => item.GetProperty("from").GetString()!)];
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage yanit, HttpStatusCode beklenen)
    {
        string metin = await yanit.Content.ReadAsStringAsync();
        Assert.True(yanit.StatusCode == beklenen, $"{(int)yanit.StatusCode}: {metin}");
        return JsonDocument.Parse(metin).RootElement;
    }

    private async Task<HttpClient> PersonelAsync(HttpClient yonetici, string loginId, string role)
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

    /// <summary>Testin kurduğu an; modüllerin `IClock`'u (en son kayıt, `ConfigureTestServices`).</summary>
    private sealed class AyarliSaat : IClock
    {
        internal DateTimeOffset Simdi { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UtcNow => Simdi;
    }
}
