using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Infrastructure;
using CarGallery.Modules.Leads.Tests.Integration;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Leads.Tests.Endpoints;

/// <summary>
/// Durum kodu ve hata sözleşmesi (docs/testing.md § 1). Gövdenin ÇEVRİLMİŞ metin
/// taşımadığı da burada sınanır: API `errorCode` döner, çeviri uçta yapılır.
/// </summary>
[Collection(LeadsDatabaseCollection.Name)]
public sealed class TalepEndpointTests(LeadsDatabase database)
{
    private static readonly string[] TeknikSizintiIsaretleri =
    [
        "Exception", "StackTrace", "   at ", "Npgsql", "SELECT", "INSERT",
        "/api/leads", "LeadsDbContext", "CaptureLead",
    ];

    [Fact(DisplayName = "AC-1: İlan detay sayfasından, adı ve telefonu girilmiş bir bilgi talebi bırakıldığında talep kaydedilir")]
    public async Task Doner201_AdVeTelefonVerildiginde()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await GonderAsync(api, Govde());

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Leads.CountAsync());
    }

    [Fact(DisplayName = "AC-14: Hiç var olmayan bir ilan için gönderilen talep ile görünür olmayan bir ilan için gönderilen talep aynı karşılığı alır: aynı hata kodu, aynı gövde")]
    public async Task AyniGovde_VarOlmayanVeGorunmeyenIlan()
    {
        await database.ResetAsync();

        await using ModuleApi gorunmeyen = await ModuleApi.StartAsync(database);
        gorunmeyen.Ilanlar.VitrindenDusur();
        HttpResponseMessage gorunmeyenYanit = await GonderAsync(gorunmeyen, Govde());
        string gorunmeyenGovde = await gorunmeyenYanit.Content.ReadAsStringAsync();

        await using ModuleApi hicYok = await ModuleApi.StartAsync(database);
        HttpResponseMessage hicYokYanit = await GonderAsync(
            hicYok, Govde(slug: "hic-var-olmayan-ilan-999999"));
        string hicYokGovde = await hicYokYanit.Content.ReadAsStringAsync();

        Assert.Equal(gorunmeyenYanit.StatusCode, hicYokYanit.StatusCode);
        Assert.Equal(gorunmeyenGovde, hicYokGovde);
        Assert.Equal("lead.listing_unavailable", await HataKoduAsync(hicYokYanit));
    }

    [Fact(DisplayName = "AC-12: `Rezerve` durumundaki bir ilan için gönderilen talep sunucuda reddedilir ve kayıt oluşmaz")]
    public async Task Doner422_RezerveIlanIcin()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);
        api.Ilanlar.RezerveYap();

        HttpResponseMessage yanit = await GonderAsync(api, Govde());

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);
        Assert.Equal("lead.listing_reserved", await HataKoduAsync(yanit));

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(0, await context.Leads.CountAsync());
    }

    [Fact(DisplayName = "AC-10: Talep tipi takas olarak gönderilen istek reddedilir; takas sistemde tanınan bir tiptir ama bu spec'te kabul edilmez")]
    public async Task Doner422_TakasTipiyle()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await GonderAsync(api, Govde(tip: "trade_in"));

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);
        Assert.Equal("lead.type_not_accepted", await HataKoduAsync(yanit));
    }

    /// <summary>
    /// AC-40 — hatalı alan ziyaretçiye işaretlenir. Sunucu METİN göndermez, hangi alanın
    /// hatalı olduğunu söyleyen YAPISAL bir parametre gönderir; metni uç kendi
    /// kataloğundan üretir (C10, Yasak Listesi 23).
    /// </summary>
    [Fact(DisplayName = "AC-40: Doğrulama hatası olan alan ziyaretçiye işaretlenir ve hatanın ne olduğu ziyaretçinin dilinde yazılır")]
    public async Task Bildirir_HataliAlanAdini()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await GonderAsync(api, Govde(telefon: "05321112233"));

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);

        JsonElement govde = await GovdeAsync(yanit);
        Assert.Equal("lead.invalid_phone", govde.GetProperty("errorCode").GetString());
        Assert.Equal("phone", govde.GetProperty("field").GetString());
    }

    [Fact(DisplayName = "AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task Doner422_AnahtarBasligiYoksa()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await api.Client.PostAsJsonAsync("/api/leads", Govde());

        Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);
        Assert.Equal("lead.idempotency_key_missing", await HataKoduAsync(yanit));
    }

    [Fact(DisplayName = "AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task TekKayit_AyniAnahtarliIkiIstek()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage ilk = await GonderAsync(api, Govde(), anahtar: "anahtar-a");
        HttpResponseMessage ikinci = await GonderAsync(api, Govde(), anahtar: "anahtar-a");

        Assert.Equal(HttpStatusCode.Created, ilk.StatusCode);
        Assert.Equal(HttpStatusCode.Created, ikinci.StatusCode);

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(1, await context.Leads.CountAsync());
    }

    [Fact(DisplayName = "AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task Doner201_SinirdakiAnahtarBasligiyla()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await GonderAsync(api, Govde(), new string('k', 100));

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
    }

    /// <summary>
    /// Sınırı aşan anahtar eskiden `500` üretiyordu: kolon `character varying(100)` ve
    /// aşan değer PostgreSQL'de patlıyor, istisna tekil indeks çakışmasına benzemediği
    /// için yakalanmıyordu. Ziyaretçi sebebini öğrenemeden talebini kaybediyordu.
    /// </summary>
    [Fact(DisplayName = "AC-35: Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı oluşturmaz")]
    public async Task Doner422_AnahtarBasligiCokUzunsa()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        foreach (int uzunluk in new[] { 101, 5000 })
        {
            HttpResponseMessage yanit = await GonderAsync(api, Govde(), new string('k', uzunluk));

            Assert.Equal(HttpStatusCode.UnprocessableContent, yanit.StatusCode);
            Assert.Equal("lead.idempotency_key_too_long", await HataKoduAsync(yanit));
        }

        await using LeadsDbContext context = database.CreateContext();
        Assert.Equal(0, await context.Leads.CountAsync());
    }

    [Fact(DisplayName = "AC-34: Reddedilen istekte ziyaretçiye teknik ayrıntı gösterilmez")]
    public async Task IcermezTeknikAyrinti_ReddedilenYanit()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);
        api.Ilanlar.VitrindenDusur();

        HttpResponseMessage yanit = await GonderAsync(api, Govde());
        string govde = await yanit.Content.ReadAsStringAsync();

        foreach (string isaret in TeknikSizintiIsaretleri)
        {
            Assert.DoesNotContain(isaret, govde, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gövde, ziyaretçiye gösterilebilecek HİÇBİR metin taşımaz — `title` ve `detail`
    /// dâhil. ASP.NET'in varsayılan İngilizce başlığı ("Unprocessable Content") bırakılsa,
    /// uçta yanlışlıkla basılabilecek bir metin olurdu (Yasak Listesi 23).
    /// </summary>
    [Fact(DisplayName = "AC-34: Reddedilen istekte ziyaretçiye teknik ayrıntı gösterilmez")]
    public async Task TasimazCevrilmisMetin_HataGovdesi()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);
        api.Ilanlar.VitrindenDusur();

        JsonElement govde = await GovdeAsync(await GonderAsync(api, Govde()));

        Assert.False(govde.TryGetProperty("title", out _));
        Assert.False(govde.TryGetProperty("detail", out _));
        Assert.Equal("lead.listing_unavailable", govde.GetProperty("errorCode").GetString());
    }

    private static object Govde(
        string? slug = FakeListingSnapshotSource.GecerliSlug,
        string? tip = "information_request",
        string? locale = "tr",
        string? ad = "Ayşe Yılmaz",
        string? telefon = "+905321112233") => new
        {
            listingSlug = slug,
            leadType = tip,
            locale,
            fullName = ad,
            phone = telefon,
            email = (string?)null,
            message = (string?)null,
            consentGiven = true,
        };

    private static async Task<HttpResponseMessage> GonderAsync(
        ModuleApi api,
        object govde,
        string anahtar = "0192c000-0000-7000-8000-00000000aaaa")
    {
        HttpRequestMessage istek = new(HttpMethod.Post, "/api/leads")
        {
            Content = JsonContent.Create(govde),
        };

        istek.Headers.Add("Idempotency-Key", anahtar);

        return await api.Client.SendAsync(istek);
    }

    private static async Task<JsonElement> GovdeAsync(HttpResponseMessage yanit) =>
        JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

    private static async Task<string?> HataKoduAsync(HttpResponseMessage yanit) =>
        (await GovdeAsync(yanit)).GetProperty("errorCode").GetString();
}
