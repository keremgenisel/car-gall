using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Content.Contracts.PrivacyNotice;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0019` — talebin aydınlatma metni sürümüne bağlanması, uçtan uca ve GERÇEK `Content` ile
/// (Bilinen tuzak 1): metin panelin gerçek uçlarıyla yazılır ve yayına alınır, talep gerçek
/// talep ucuna bırakılır, sürüm `Content`'in sözleşmesinden geri okunur. Her test `privacy`'yi
/// yeni kurulum hâline döndürüp kendi durumunu kurar.
/// </summary>
public sealed class AydinlatmaMetniZinciriTests : IAsyncLifetime
{
    private const string Parola = "calisan-parolasi-19";

    private AydinlatmaMetniOrtami ortam = null!;
    private WebApplicationFactory<Program> fabrika = null!;
    private HttpClient yonetici = null!;
    private HttpClient ziyaretci = null!;

    public async Task InitializeAsync()
    {
        ortam = await AydinlatmaMetniOrtami.KurAsync();

        // Talep testleri bütçeyi ölçmüyor; spec'in 5'i burada onları birbirine bağlardı.
        fabrika = ortam.Fabrika(talepSiniri: 1_000);
        ziyaretci = Istemci();

        HttpClient kurulum = Istemci();
        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = IdentityTestSettings.SeedAdminPassword,
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await kurulum.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = IdentityTestSettings.SeedAdminPassword, newPassword = Parola })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await kurulum.PostAsJsonAsync("/api/identity/sessions", new
        {
            loginId = IdentityTestSettings.SeedAdminLoginId,
            password = Parola,
        })).StatusCode);
        string sir = await IkinciAdim.KurAsync(kurulum);

        yonetici = Istemci();
        await IkinciAdim.GirAsync(yonetici, IdentityTestSettings.SeedAdminLoginId, Parola, sir);
    }

    public async Task DisposeAsync() => await ortam.DisposeAsync();

    [Fact(DisplayName = "0019/AC-18: privacy tr'de yayındayken tr dilinde bırakılan talep, o anki tr sürümüne bağlanır")]
    public Task Baglanir_TrSurumune() => BaglanirAsync("tr", "KVKK Aydınlatma Metni", "Kişisel verileriniz Kanun kapsamında işlenir.", "kvkk");

    [Fact(DisplayName = "0019/AC-19: privacy ar'da yayındayken ar dilinde bırakılan talep, o anki ar sürümüne bağlanır")]
    public Task Baglanir_ArSurumune() => BaglanirAsync("ar", "إشعار الخصوصية", "تتم معالجة بياناتك الشخصية وفق القانون.", "khususiya");

    [Fact(DisplayName = "0019/AC-20: privacy en'de yayındayken en dilinde bırakılan talep, o anki en sürümüne bağlanır")]
    public Task Baglanir_EnSurumune() => BaglanirAsync("en", "Privacy Notice", "Your personal data is processed under the Law.", "privacy");

    [Fact(DisplayName = "0019/AC-21: tr ve en'de farklı metinler yayındayken en dilinde bırakılan talep en sürümüne bağlanır, tr sürümüne bağlanmaz")]
    public async Task Baglanir_KendiDilineDigerineDegil()
    {
        await ortam.YeniKurulumAsync();
        await YayindaAsync("tr", "KVKK Metni", "tr gövdesi.", "kvkk");
        await YayindaAsync("en", "Privacy Notice", "en body.", "privacy");
        Guid trSurumu = (await GuncelAsync("tr")).VersionId!.Value;

        await KabulAsync("en", "ac21-en");

        (_, Guid? surum, _) = await ortam.BagAsync("ac21-en");
        PrivacyNoticeVersionDto okunan = (await AydinlatmaMetniOrtami.SurumAsync(fabrika, surum!.Value))!;
        Assert.NotEqual(trSurumu, surum);
        Assert.Equal(("en", "Privacy Notice", "en body."), (okunan.Locale, okunan.Title, okunan.Body));
    }

    [Fact(DisplayName = "0019/AC-22: İletişim izni verilmeden bırakılan talep de o dilin yayındaki sürümüne bağlanır ve izni \"verilmedi\" olarak kalır")]
    public async Task Baglanir_IzinVerilmeden()
    {
        await ortam.YeniKurulumAsync();
        await YayindaAsync("tr", "KVKK Metni", "İzinsiz talepte de bu metin.", "kvkk");
        Guid guncel = (await GuncelAsync("tr")).VersionId!.Value;

        await KabulAsync("tr", "ac22-izinsiz", izin: false);

        Assert.Equal(("published", (Guid?)guncel, false), await ortam.BagAsync("ac22-izinsiz"));
    }

    [Fact(DisplayName = "0019/AC-23: Uygulamanın kaydettiği hiçbir talep \"kayıt tutulmuyordu\" durumunu taşımaz: privacy yayındayken, yayında değilken ve hiç metni yokken bırakılan talepler durumlarını (\"sürüm\" ya da \"yayında değildi\") açıkça taşır (D15)")]
    public async Task Tasimaz_KayitTutulmuyordu()
    {
        await ortam.YeniKurulumAsync();
        await KabulAsync("tr", "ac23-metinsiz");
        await KaydetAsync("tr", "KVKK Metni", "Taslak gövde.", "kvkk");
        await KabulAsync("tr", "ac23-taslak");
        await YayinlaAsync("tr");
        await KabulAsync("tr", "ac23-yayinda");

        Assert.Equal("not_published", (await ortam.BagAsync("ac23-metinsiz")).Durum);
        Assert.Equal("not_published", (await ortam.BagAsync("ac23-taslak")).Durum);
        Assert.Equal("published", (await ortam.BagAsync("ac23-yayinda")).Durum);
        Assert.Equal(0, await ortam.SayAsync("SELECT count(*) FROM leads.leads WHERE privacy_notice_status = 'not_recorded'"));
    }

    [Fact(DisplayName = "0019/AC-24: tr metni \"M1\" iken talep A bırakılır, metin \"M2\" olarak kaydedilir, talep B bırakılır: talep A'nın sürümünden okunan başlık ve gövde M1'in birebir aynısıdır, talep B'ninki M2'ninki")]
    public async Task Okunur_HerTalebinKendiMetni()
    {
        await IkiTalepIkiMetinAsync("ac24");

        Assert.Equal(("M1 başlık", "M1 gövde."), await MetinAsync("ac24-a"));
        Assert.Equal(("M2 başlık", "M2 gövde."), await MetinAsync("ac24-b"));
    }

    [Fact(DisplayName = "0019/AC-25: AC-24'ten sonra tr yayından kaldırıldığında talep A'nın ve talep B'nin sürümleri yine dil, başlık, gövde ve anla okunur")]
    public async Task Okunur_YayindanKalktiktanSonra()
    {
        await IkiTalepIkiMetinAsync("ac25");
        PrivacyNoticeVersionDto a = (await SurumuAsync("ac25-a"))!;
        PrivacyNoticeVersionDto b = (await SurumuAsync("ac25-b"))!;

        await KaldirAsync("tr");

        Assert.Equal(a, await SurumuAsync("ac25-a"));
        Assert.Equal(b, await SurumuAsync("ac25-b"));
        Assert.Equal(("tr", "M1 başlık", "M1 gövde."), (a.Locale, a.Title, a.Body));
        Assert.Equal(("tr", "M2 başlık", "M2 gövde."), (b.Locale, b.Title, b.Body));
        Assert.True(b.RecordedAt > DateTimeOffset.MinValue);
    }

    [Fact(DisplayName = "0019/AC-27: Ziyaretçinin privacy sayfasında okuduğu tr başlığı ve gövdesi, metin değişmeden bırakılan bir tr talebinin sürümünden okunan başlık ve gövdeyle birebir aynıdır")]
    public async Task Esittir_ZiyaretcininOkudugu_TalebinSurumu()
    {
        await ortam.YeniKurulumAsync();
        await YayindaAsync("tr", "KVKK Aydınlatma Metni", "Gövde [bağlantı](https://ornek.com) ve sondaki boşluk ", "kvkk");
        JsonElement sayfa = JsonDocument.Parse(await ziyaretci.GetStringAsync("/api/content/pages/kvkk?locale=tr")).RootElement;

        await KabulAsync("tr", "ac27");

        Assert.Equal(
            (sayfa.GetProperty("title").GetString(), sayfa.GetProperty("body").GetString()),
            await MetinAsync("ac27"));
    }

    [Fact(DisplayName = "0019/AC-28: privacy ar'da taslakken ar dilinde bırakılan talep kabul edilir ve kaydı \"yayında değildi\" durumunu taşır; hiçbir sürüme bağlanmaz")]
    public async Task Tasir_YayindaDegildi_ArTaslakken()
    {
        await ArTaslakTrYayindaAsync();

        await KabulAsync("ar", "ac28-ar");

        Assert.Equal(("not_published", (Guid?)null), Durum(await ortam.BagAsync("ac28-ar")));
    }

    [Fact(DisplayName = "0019/AC-29: AC-28'de privacy tr'de yayında olsa da ar talebi tr sürümüne bağlanmaz")]
    public async Task Baglanmaz_TrSurumune_ArTalebi()
    {
        Guid trSurumu = await ArTaslakTrYayindaAsync();

        await KabulAsync("ar", "ac29-ar");

        (string durum, Guid? surum, _) = await ortam.BagAsync("ac29-ar");
        Assert.NotEqual((Guid?)trSurumu, surum);
        Assert.Equal("not_published", durum);
    }

    [Fact(DisplayName = "0019/AC-30: privacy'nin hiçbir dilde metni yokken (yeni kurulum) bırakılan tr talebi \"yayında değildi\" durumunu taşır")]
    public async Task Tasir_YayindaDegildi_YeniKurulumda()
    {
        await ortam.YeniKurulumAsync();

        await KabulAsync("tr", "ac30");

        Assert.Equal(("not_published", (Guid?)null), Durum(await ortam.BagAsync("ac30")));
    }

    [Fact(DisplayName = "0019/AC-31: tr yayından kaldırıldıktan sonra bırakılan tr talebi \"yayında değildi\" durumunu taşır; kaldırmadan önceki sürüme bağlanmaz")]
    public async Task Tasir_YayindaDegildi_KaldirildiktanSonra()
    {
        await ortam.YeniKurulumAsync();
        await YayindaAsync("tr", "KVKK Metni", "Kaldırılacak metin.", "kvkk");
        await KaldirAsync("tr");

        await KabulAsync("tr", "ac31");

        Assert.Equal(("not_published", (Guid?)null), Durum(await ortam.BagAsync("ac31")));
    }

    /// <summary>
    /// D22/P10 — "yazılmış ama işlenmemiş" hâl GERÇEK panel kaydıyla kurulur: testin bu
    /// veritabanına eklediği ertelenmiş kısıt tetikleyicisi sürüm satırı yazıldıktan SONRA,
    /// commit anında testin tuttuğu danışma kilidini bekler. Talep, bekleyen kilit
    /// `pg_locks`'ta görüldükten sonra bırakılır — uyku yok, sıra kanıtlı.
    /// </summary>
    [Fact(DisplayName = "0019/AC-35: Gerçek PostgreSQL'de Content'te yeni bir tr sürümünü yazmış ama henüz işlenmemiş (commit edilmemiş) bir işlem açıkken bırakılan tr talebi önceki sürüme, işlem işlendikten sonra bırakılan tr talebi yeni sürüme bağlanır; iki talepte de sürümden okunan başlık ve gövde bağlanılan sürümünkiyle birebir aynıdır")]
    public async Task Baglanir_IslenmisSurume_YayinlaAyniAnda()
    {
        const long Kilit = 190_035;
        await ortam.YeniKurulumAsync();
        await YayindaAsync("tr", "Önceki başlık", "Önceki gövde.", "kvkk");
        await ortam.SqlAsync(
            $"""
            CREATE OR REPLACE FUNCTION content.test_0019_commit_bekle() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN PERFORM pg_advisory_xact_lock({Kilit}); RETURN NULL; END; $$;
            CREATE CONSTRAINT TRIGGER test_0019_commit_bekle AFTER INSERT ON content.privacy_notice_versions
            DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION content.test_0019_commit_bekle();
            """);

        try
        {
            await using NpgsqlConnection kilit = new(ortam.ConnectionString);
            await kilit.OpenAsync();
            await using (NpgsqlCommand al = new($"SELECT pg_advisory_lock({Kilit})", kilit))
            {
                await al.ExecuteScalarAsync();
            }

            int belirtec = await BelirtecAsync();
            Task<HttpResponseMessage> kayit = yonetici.PutAsJsonAsync(
                $"{AdminSurface.RoutePrefix}/content/pages/{AydinlatmaMetniOrtami.Privacy}",
                new { version = belirtec, locales = new[] { new { locale = "tr", title = "Yeni başlık", body = "Yeni gövde.", slug = "kvkk" } } });

            await BekleyenKilitAsync(Kilit, kayit);
            await KabulAsync("tr", "ac35-acikken");

            await using (NpgsqlCommand birak = new($"SELECT pg_advisory_unlock({Kilit})", kilit))
            {
                await birak.ExecuteScalarAsync();
            }

            Assert.Equal(HttpStatusCode.OK, (await kayit).StatusCode);
            await KabulAsync("tr", "ac35-sonra");
        }
        finally
        {
            await ortam.SqlAsync(
                """
                DROP TRIGGER IF EXISTS test_0019_commit_bekle ON content.privacy_notice_versions;
                DROP FUNCTION IF EXISTS content.test_0019_commit_bekle();
                """);
        }

        Assert.Equal(("Önceki başlık", "Önceki gövde."), await MetinAsync("ac35-acikken"));
        Assert.Equal(("Yeni başlık", "Yeni gövde."), await MetinAsync("ac35-sonra"));
    }

    [Fact(DisplayName = "0019/AC-37: Aynı tekrar anahtarıyla gönderilen ikinci istek — araya yeni bir tr yayını girmiş olsa da — yeni talep oluşturmaz ve ilk talebin sürüm bağı değişmez")]
    public async Task Degismez_Bag_AyniAnahtarlaTekrarda()
    {
        await ortam.YeniKurulumAsync();
        await YayindaAsync("tr", "İlk başlık", "İlk gövde.", "kvkk");
        HttpResponseMessage ilk = await KabulAsync("tr", "ac37");
        (string, Guid?, bool) once = await ortam.BagAsync("ac37");

        await KaydetAsync("tr", "Araya giren başlık", "Araya giren gövde.", "kvkk");
        HttpResponseMessage tekrar = await AydinlatmaMetniOrtami.TalepAsync(ziyaretci, "tr", "ac37");

        Assert.Equal(await ilk.Content.ReadAsStringAsync(), await tekrar.Content.ReadAsStringAsync());
        Assert.Equal(once, await ortam.BagAsync("ac37"));
        Assert.Equal(1, await ortam.SayAsync("SELECT count(*) FROM leads.leads WHERE idempotency_key = 'ac37'"));
        Assert.Equal(("İlk başlık", "İlk gövde."), await MetinAsync("ac37"));
    }

    private async Task BaglanirAsync(string locale, string title, string body, string slug)
    {
        await ortam.YeniKurulumAsync();
        await YayindaAsync(locale, title, body, slug);
        Guid guncel = (await GuncelAsync(locale)).VersionId!.Value;

        await KabulAsync(locale, $"baglanir-{locale}");

        (string durum, Guid? surum, _) = await ortam.BagAsync($"baglanir-{locale}");
        PrivacyNoticeVersionDto okunan = (await AydinlatmaMetniOrtami.SurumAsync(fabrika, surum!.Value))!;
        Assert.Equal(("published", (Guid?)guncel), (durum, surum));
        Assert.Equal((locale, title, body), (okunan.Locale, okunan.Title, okunan.Body));
    }

    private async Task IkiTalepIkiMetinAsync(string onek)
    {
        await ortam.YeniKurulumAsync();
        await YayindaAsync("tr", "M1 başlık", "M1 gövde.", "kvkk");
        await KabulAsync("tr", $"{onek}-a");
        await KaydetAsync("tr", "M2 başlık", "M2 gövde.", "kvkk");
        await KabulAsync("tr", $"{onek}-b");
    }

    private async Task<Guid> ArTaslakTrYayindaAsync()
    {
        await ortam.YeniKurulumAsync();
        await YayindaAsync("tr", "KVKK Metni", "tr yayında.", "kvkk");
        await KaydetAsync("ar", "مسودة", "نص المسودة.", "khususiya");
        return (await GuncelAsync("tr")).VersionId!.Value;
    }

    private static (string, Guid?) Durum((string Durum, Guid? Surum, bool Izin) bag) => (bag.Durum, bag.Surum);

    private async Task<(string Title, string Body)> MetinAsync(string anahtar)
    {
        PrivacyNoticeVersionDto surum = (await SurumuAsync(anahtar))!;
        return (surum.Title, surum.Body);
    }

    private async Task<PrivacyNoticeVersionDto?> SurumuAsync(string anahtar)
    {
        (string durum, Guid? surum, _) = await ortam.BagAsync(anahtar);
        Assert.Equal("published", durum);
        return await AydinlatmaMetniOrtami.SurumAsync(fabrika, surum!.Value);
    }

    private async Task<PrivacyNoticeLookup> GuncelAsync(string locale)
    {
        using IServiceScope scope = fabrika.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IPrivacyNoticeQueries>().FindPublishedAsync(locale, CancellationToken.None);
    }

    private async Task<HttpResponseMessage> KabulAsync(string locale, string anahtar, bool izin = true)
    {
        HttpResponseMessage yanit = await AydinlatmaMetniOrtami.TalepAsync(ziyaretci, locale, anahtar, izin);
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return yanit;
    }

    private async Task KaydetAsync(string locale, string title, string body, string slug)
    {
        HttpResponseMessage yanit = await yonetici.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/content/pages/{AydinlatmaMetniOrtami.Privacy}",
            new { version = await BelirtecAsync(), locales = new[] { new { locale, title, body, slug } } });
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    private async Task YayinlaAsync(string locale) =>
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/content/pages/{AydinlatmaMetniOrtami.Privacy}/publish",
            new { version = await BelirtecAsync(), locale })).StatusCode);

    private async Task KaldirAsync(string locale) =>
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/content/pages/{AydinlatmaMetniOrtami.Privacy}/unpublish",
            new { version = await BelirtecAsync(), locale })).StatusCode);

    private async Task YayindaAsync(string locale, string title, string body, string slug)
    {
        await KaydetAsync(locale, title, body, slug);
        await YayinlaAsync(locale);
    }

    private async Task<int> BelirtecAsync() =>
        JsonDocument.Parse(await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/content/pages/{AydinlatmaMetniOrtami.Privacy}"))
            .RootElement.GetProperty("version").GetInt32();

    /// <summary>Panel kaydı commit eşiğinde testin kilidini bekleyene kadar bekler; kayıt erken biterse test kırılır.</summary>
    private async Task BekleyenKilitAsync(long kilit, Task kayit)
    {
        System.Diagnostics.Stopwatch sure = System.Diagnostics.Stopwatch.StartNew();

        while (await ortam.SayAsync($"SELECT count(*) FROM pg_locks WHERE locktype = 'advisory' AND NOT granted AND objid = {kilit}") == 0)
        {
            Assert.False(kayit.IsCompleted, "Panel kaydı kilidi beklemeden bitti; ara hâl kurulamadı.");
            Assert.True(sure.Elapsed < TimeSpan.FromSeconds(15), "Panel kaydı commit eşiğine 15 saniyede ulaşmadı.");
            await Task.Delay(20);
        }
    }

    private HttpClient Istemci() => fabrika.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });
}
