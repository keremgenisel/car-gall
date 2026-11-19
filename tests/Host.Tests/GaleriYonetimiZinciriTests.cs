using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0020` — galeri profili ve şubelerin gerçek boru hattı: Yönetici politikası (D2), hata
/// gövdesi (C5, P3), sürüm çakışması (D17), public yanıt ve vitrin belgesindeki görünen ad
/// (D15, P1) ve şube kapanışının `VehicleCatalog`'a ulaşan A8 zinciri.
///
/// <para>Üç rol gerçek personel olarak eklenir ve gerçek oturumla girer. Testler sırayla koşar
/// ve her biri kendi şubesini açar; galeri geneli bir durumu (son aktif şube) değiştiren test
/// onu kendi içinde geri kurar.</para>
/// </summary>
public sealed class GaleriYonetimiZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";

    /// <summary>`SeedInitialVehicleModels` migration'ının ilk satırı.</summary>
    private static readonly Guid SeedModel = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;
    private HttpClient yonetici = null!;
    private HttpClient editor = null!;
    private HttpClient danisman = null!;
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
    }

    public async Task DisposeAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }

        await container.DisposeAsync();
    }

    // ----- Erişim (R17 · D2) -----

    [Fact(DisplayName = "0020/AC-66: Satış Danışmanı ve İçerik Editörü'nün galeri ve şube yazma uçlarına isteği `403` alır ve hiçbir değişiklik yapılmaz.")]
    public async Task Reddeder_DanismanVeEditor_YazmaUclari()
    {
        (Guid sube, _) = await SubeAcAsync();
        string onceGaleri = await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/gallery");
        string onceSube = await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/branches/{sube}");

        foreach (HttpClient rol in new[] { danisman, editor })
        {
            foreach ((HttpMethod yontem, string yol, object? govde) in YazmaUclari(sube))
            {
                HttpResponseMessage yanit = await GonderAsync(rol, yontem, yol, govde);
                Assert.True(yanit.StatusCode == HttpStatusCode.Forbidden, $"{yontem} {yol} → {(int)yanit.StatusCode}");
            }
        }

        Assert.Equal(onceGaleri, await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/gallery"));
        Assert.Equal(onceSube, await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/branches/{sube}"));
        Assert.DoesNotContain("yetkisiz-sube", await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/branches?locale=tr"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-67: Oturumu olmayan isteğe yazma uçları `401` döner.")]
    public async Task Doner401_OturumsuzYazma()
    {
        (Guid sube, _) = await SubeAcAsync();
        HttpClient oturumsuz = Istemci();

        foreach ((HttpMethod yontem, string yol, object? govde) in YazmaUclari(sube))
        {
            HttpResponseMessage yanit = await GonderAsync(oturumsuz, yontem, yol, govde);
            Assert.True(yanit.StatusCode == HttpStatusCode.Unauthorized, $"{yontem} {yol} → {(int)yanit.StatusCode}");
        }
    }

    [Fact(DisplayName = "0020/AC-68: `GET /api/admin/branches`'in bugünkü okuyucuları (araç formu) değişmeden çalışır.")]
    public async Task Okur_SubeListesiniHerRol_AracFormuIcin()
    {
        (Guid sube, _) = await SubeAcAsync();

        foreach (HttpClient rol in new[] { danisman, editor, yonetici })
        {
            HttpResponseMessage yanit = await rol.GetAsync($"{AdminSurface.RoutePrefix}/branches?locale=tr");
            Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

            JsonElement satir = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement
                .EnumerateArray().Single(item => item.GetProperty("branchId").GetGuid() == sube);

            // Bugünkü şekil: kimlik, ad ve durum kodu — başka alan eklenmedi.
            Assert.Equal(["branchId", "name", "status"], satir.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
            Assert.Equal("Active", satir.GetProperty("status").GetString());
        }
    }

    // ----- Hata sözleşmesi (C5 · P3) -----

    [Fact(DisplayName = "0020/AC-8: 200 karakteri aşan ticari unvan, 40 karakteri aşan vergi ya da sicil numarası ve 120 karakteri aşan görünen ad alan hatasıyla reddedilir; `500` dönmez (R3a, D22).")]
    public async Task Doner422_SiniriAsanProfilAlanlari()
    {
        HttpResponseMessage yanit = await yonetici.PutAsJsonAsync($"{AdminSurface.RoutePrefix}/gallery", new
        {
            version = await GaleriSurumuAsync(),
            commercialName = new string('u', 201),
            taxNumber = new string('1', 41),
            tradeRegistryNumber = new string('2', 41),
            displayNames = new Dictionary<string, string> { ["tr"] = new string('a', 121) },
        });

        JsonElement govde = await ProblemAsync(yanit, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("dealership.validation_failed", govde.GetProperty("errorCode").GetString());
        Assert.Equal("gallery.commercial_name_too_long", govde.GetProperty("commercialName").GetString());
        Assert.Equal("gallery.tax_number_too_long", govde.GetProperty("taxNumber").GetString());
        Assert.Equal("gallery.trade_registry_number_too_long", govde.GetProperty("tradeRegistryNumber").GetString());
        Assert.Equal("gallery.display_name_too_long", govde.GetProperty("displayName.tr").GetString());
    }

    /// <summary>
    /// AC-30'un metni `400` diyor; uç `422` döndürüyor (plan turu P3: `docs/conventions.md`
    /// § 5 doğrulama hatasını `422` sayar ve depoda `400` dönen uç yok). Bu sapma PR'da
    /// "karşılanmayan kriterler" altında yazılıdır; test GERÇEK davranışı ölçer.
    /// </summary>
    [Fact(DisplayName = "0020/AC-30: Başka bir şubenin — kapatılmış bir şube dâhil — kullandığı adres kısaltması `422` ve bir alan hata koduyla reddedilir; hata kısaltmanın zaten kullanıldığını söyler.")]
    public async Task Reddeder_KullanilmisKisaltma_AlanKoduyla()
    {
        (Guid kapanacak, int surum) = await SubeAcAsync("kapanan-kisaltma");
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches/{kapanacak}/close", new { version = surum })).StatusCode);

        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/branches", SubeGovdesi("kapanan-kisaltma"));

        JsonElement govde = await ProblemAsync(yanit, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("slug", govde.GetProperty("fields").GetString());
        Assert.Equal("branch.slug_taken", govde.GetProperty("slug").GetString());
    }

    [Fact(DisplayName = "0020/AC-33: 120 karakteri aşan şube adı ve 300 karakteri aşan adres alan hatasıyla reddedilir (R3a, D22).")]
    public async Task Doner422_SiniriAsanSubeAdiVeAdresi()
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/branches", new
        {
            names = new Dictionary<string, string> { ["tr"] = new string('a', 121) },
            addresses = new Dictionary<string, string> { ["tr"] = new string('b', 301) },
            phone = "+902165554433",
            timeZone = "Europe/Istanbul",
            slug = Kisaltma(),
        });

        JsonElement govde = await ProblemAsync(yanit, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("branch.name_too_long", govde.GetProperty("name.tr").GetString());
        Assert.Equal("branch.address_too_long", govde.GetProperty("address.tr").GetString());
    }

    [Fact(DisplayName = "0020/AC-27: IANA bölge kimliği olmayan bir saat dilimi (`+03:00`) sunucuda reddedilir.")]
    public async Task Reddeder_SunucudaIanaOlmayanSaatDilimi()
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches",
            SubeGovdesi(Kisaltma(), saatDilimi: "+03:00"));

        JsonElement govde = await ProblemAsync(yanit, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("time_zone.invalid_zone", govde.GetProperty("timeZone").GetString());
    }

    // ----- Eşzamanlı kaydetme (R16 · D17) -----

    [Fact(DisplayName = "0020/AC-59: Galeri profilini, okunduktan sonra başka bir kaydetmeyle değişmiş bir sürüm üzerinden kaydetmek `409` ve bir hata koduyla reddedilir; kayıtlı değer değişmez.")]
    public async Task Doner409_EskiSurumdenGaleriKaydi()
    {
        int okunan = await GaleriSurumuAsync();
        Assert.Equal(HttpStatusCode.OK, (await GaleriKaydetAsync(okunan, "Önce Kaydeden A.Ş.", "Oto")).StatusCode);

        HttpResponseMessage yanit = await GaleriKaydetAsync(okunan, "Sonra Kaydeden A.Ş.", "Oto");

        JsonElement govde = await ProblemAsync(yanit, HttpStatusCode.Conflict);
        Assert.Equal("concurrency.lost", govde.GetProperty("errorCode").GetString());
        Assert.Contains("Önce Kaydeden A.Ş.", await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/gallery"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-60: Aynı kural şube düzenlemesi için de geçerlidir.")]
    public async Task Doner409_EskiSurumdenSubeKaydi()
    {
        (Guid sube, int okunan) = await SubeAcAsync();
        Assert.Equal(HttpStatusCode.OK, (await SubeKaydetAsync(sube, okunan, telefon: "+902161112233")).StatusCode);

        HttpResponseMessage yanit = await SubeKaydetAsync(sube, okunan, telefon: "+902164445566");

        Assert.Equal("concurrency.lost", (await ProblemAsync(yanit, HttpStatusCode.Conflict)).GetProperty("errorCode").GetString());
        Assert.Contains("+902161112233", await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/branches/{sube}"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-62: Düzenleme formu açıkken şube geçici kapatılırsa formdaki kaydetme `409` ile reddedilir (D17).")]
    public async Task Doner409_FormAcikkenGeciciKapatilanSube()
    {
        (Guid sube, int formunSurumu) = await SubeAcAsync();
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches/{sube}/close-temporarily", new { version = formunSurumu })).StatusCode);

        HttpResponseMessage yanit = await SubeKaydetAsync(sube, formunSurumu, telefon: "+902163334455");

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
    }

    // ----- Şube durumu (R10–R13) -----

    [Fact(DisplayName = "0020/AC-42: Kapatılmış bir şube düzenlenemez: ekran alanları salt okunur gösterir ve uç reddeder (R10, D21).")]
    public async Task Reddeder_KapatilmisSubeninDuzenlenmesi_Uc()
    {
        (Guid sube, int surum) = await SubeAcAsync();
        JsonElement kapanis = JsonDocument.Parse(await (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches/{sube}/close", new { version = surum })).Content.ReadAsStringAsync()).RootElement;

        HttpResponseMessage yanit = await SubeKaydetAsync(sube, kapanis.GetProperty("version").GetInt32(), telefon: "+902169998877");

        Assert.Equal("branch.closed_read_only", (await ProblemAsync(yanit, HttpStatusCode.Conflict)).GetProperty("errorCode").GetString());
    }

    [Fact(DisplayName = "0020/AC-54: Kapatılmış bir şube için yeniden açma ve geçici kapatma eylemleri sunulmaz ve uç onları reddeder.")]
    public async Task Reddeder_KapatilmisSubedeYenidenAcmaVeGeciciKapatma_Uc()
    {
        (Guid sube, int surum) = await SubeAcAsync();
        await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/branches/{sube}/close", new { version = surum });
        int kapali = await SubeSurumuAsync(sube);

        foreach (string eylem in new[] { "reopen", "close-temporarily" })
        {
            HttpResponseMessage yanit = await yonetici.PostAsJsonAsync(
                $"{AdminSurface.RoutePrefix}/branches/{sube}/{eylem}", new { version = kapali });

            Assert.Equal("branch.invalid_status_transition", (await ProblemAsync(yanit, HttpStatusCode.Conflict)).GetProperty("errorCode").GetString());
        }
    }

    [Fact(DisplayName = "0020/AC-50: Galerinin son aktif şubesini kalıcı kapatma reddedilir; panel bunun nedenini söyler ve şube aktif kalır.")]
    public async Task Reddeder_SonAktifSubeninKaliciKapatilmasi_Uc()
    {
        (Guid sube, int surum) = await SubeAcAsync();

        // Galeri geneli durum: bu şube dışındaki bütün aktif şubeler geçici kapatılır ve test
        // sonunda geri açılır (sınıftaki testler sırayla koşar).
        List<Guid> digerleri = await AktifSubelerAsync(haric: sube);
        await DurumYazAsync(digerleri, 2);

        try
        {
            HttpResponseMessage yanit = await yonetici.PostAsJsonAsync(
                $"{AdminSurface.RoutePrefix}/branches/{sube}/close", new { version = surum });

            Assert.Equal("branch.last_active_branch", (await ProblemAsync(yanit, HttpStatusCode.Conflict)).GetProperty("errorCode").GetString());
            Assert.Equal("Active", (await SubeAsync(sube)).GetProperty("status").GetString());
        }
        finally
        {
            await DurumYazAsync(digerleri, 1);
        }
    }

    // ----- Vitrin (R4–R7, R14 · D15, P1) -----

    [Fact(DisplayName = "0020/AC-16: Ticari unvan, vergi numarası ve ticaret sicil numarası hiçbir vitrin sayfasında ve vitrinin okuduğu hiçbir public yanıtta yer almaz; public galeri yanıtının anahtar kümesi `displayName` ve `branches`'tır (D15).")]
    public async Task Tasimaz_UnvanVeSicil_VitrinYanitlarinda()
    {
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsJsonAsync($"{AdminSurface.RoutePrefix}/gallery", new
        {
            version = await GaleriSurumuAsync(),
            commercialName = "Gizli Unvan Ticaret Ltd",
            taxNumber = "9876543210",
            tradeRegistryNumber = "SICIL-777",
            displayNames = new Dictionary<string, string> { ["tr"] = "Vitrin Adı" },
        })).StatusCode);

        HttpClient ziyaretci = Istemci();
        string yanit = await ziyaretci.GetStringAsync("/api/dealership/gallery?locale=tr");
        using JsonDocument belge = JsonDocument.Parse(yanit);

        Assert.Equal(["branches", "displayName"], belge.RootElement.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));

        foreach (string adres in new[] { "/tr/", "/tr/cars", "/tr/hakkimizda", "/api/dealership/gallery?locale=ar" })
        {
            string metin = await ziyaretci.GetStringAsync(adres);
            Assert.DoesNotContain("Gizli Unvan", metin, StringComparison.Ordinal);
            Assert.DoesNotContain("9876543210", metin, StringComparison.Ordinal);
            Assert.DoesNotContain("SICIL-777", metin, StringComparison.Ordinal);
        }
    }

    [Fact(DisplayName = "0020/AC-14: Yönetici görünen adı değiştirip kaydettikten sonra açılan ilk vitrin sayfası yeni adı gösterir.")]
    public async Task Gosterir_YeniAdi_KaydettiktenSonrakiIlkSayfa()
    {
        HttpClient ziyaretci = Istemci();
        Assert.Equal(HttpStatusCode.OK, (await GaleriKaydetAsync(await GaleriSurumuAsync(), "Oto Galeri", "Eski Vitrin Adı")).StatusCode);
        Assert.Contains("content=\"Eski Vitrin Adı\"", await ziyaretci.GetStringAsync("/tr/"), StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.OK, (await GaleriKaydetAsync(await GaleriSurumuAsync(), "Oto Galeri", "Yeni Vitrin Adı")).StatusCode);

        Assert.Contains("<meta name=\"gallery-display-name\" content=\"Yeni Vitrin Adı\" />", await ziyaretci.GetStringAsync("/tr/cars"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-9: Vitrinin ana sayfasında, araç listesinde, ilan detayında ve bir içerik sayfasında üst çubuk ziyaretçinin dilindeki görünen adı gösterir.")]
    public async Task Basar_GorunenAdi_DortVitrinSayfasinda()
    {
        Assert.Equal(HttpStatusCode.OK, (await GaleriKaydetAsync(await GaleriSurumuAsync(), "Oto Galeri", "Dört Sayfa Adı")).StatusCode);
        HttpClient ziyaretci = Istemci();

        foreach (string adres in new[] { "/tr/", "/tr/cars", "/tr/cars/toyota-corolla-2019-abc123", "/tr/hakkimizda" })
        {
            Assert.Contains("<meta name=\"gallery-display-name\" content=\"Dört Sayfa Adı\" />", await ziyaretci.GetStringAsync(adres), StringComparison.Ordinal);
        }
    }

    [Fact(DisplayName = "0020/AC-12: Arapça sayfada üst çubuktaki ad Arapça görünen adı gösterir ve sağdan sola düzende doğru hizalanır.")]
    public async Task Basar_ArapcaGorunenAdi_ArapcaSayfada()
    {
        Assert.Equal(HttpStatusCode.OK, (await GaleriKaydetAsync(await GaleriSurumuAsync(), "Oto Galeri", "Oto", ar: "أوتو غاليري")).StatusCode);

        string html = await Istemci().GetStringAsync("/ar/");

        Assert.Contains("<html lang=\"ar\" dir=\"rtl\"", html, StringComparison.Ordinal);
        Assert.Contains("content=\"أوتو غاليري\"", html, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-10: Ziyaretçinin dilinde görünen ad yoksa üst çubuk varsayılan dildeki görünen adı gösterir.")]
    public async Task Basar_VarsayilanDildekiAdi_ZiyaretcininDilindeYoksa()
    {
        Assert.Equal(HttpStatusCode.OK, (await GaleriKaydetAsync(await GaleriSurumuAsync(), "Oto Galeri", "Yalnız Türkçe Ad")).StatusCode);

        Assert.Contains("content=\"Yalnız Türkçe Ad\"", await Istemci().GetStringAsync("/en/"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-23: Yeni aktif şube, ziyaretçinin bir sonraki ana sayfa açışında altlıkta adı, adresi ve telefonuyla görünür.")]
    public async Task Gorunur_YeniAktifSube_Altlikta()
    {
        string kisaltma = Kisaltma();
        await SubeAcAsync(kisaltma, ad: $"Yeni Şube {kisaltma}", telefon: "+902161230001");

        JsonElement sube = await PublicSubeAsync($"Yeni Şube {kisaltma}");

        Assert.Equal("Bağdat Caddesi 1", sube.GetProperty("address").GetString());
        Assert.Equal("+902161230001", sube.GetProperty("phone").GetString());
    }

    [Fact(DisplayName = "0020/AC-38: Değiştirilen adres ve telefon ziyaretçinin bir sonraki ana sayfa açışında altlıkta görünür.")]
    public async Task Gorunur_DegisenAdresVeTelefon_Altlikta()
    {
        string kisaltma = Kisaltma();
        (Guid sube, int surum) = await SubeAcAsync(kisaltma, ad: $"Adresi Değişen {kisaltma}");

        Assert.Equal(HttpStatusCode.OK, (await SubeKaydetAsync(sube, surum, kisaltma, $"Adresi Değişen {kisaltma}", "+902169870000", "Moda Caddesi 7")).StatusCode);

        JsonElement okunan = await PublicSubeAsync($"Adresi Değişen {kisaltma}");
        Assert.Equal("Moda Caddesi 7", okunan.GetProperty("address").GetString());
        Assert.Equal("+902169870000", okunan.GetProperty("phone").GetString());
    }

    [Fact(DisplayName = "0020/AC-43: Aktif bir şube geçici kapatıldığında listede **geçici kapalı** görünür ve altlıkta görünmez.")]
    public async Task GeciciKapaliGorunur_ListedeVeAltliktaYok()
    {
        string kisaltma = Kisaltma();
        (Guid sube, int surum) = await SubeAcAsync(kisaltma, ad: $"Geçici {kisaltma}");

        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches/{sube}/close-temporarily", new { version = surum })).StatusCode);

        Assert.Equal("TemporarilyClosed", await ListedekiDurumAsync(sube));
        Assert.DoesNotContain($"Geçici {kisaltma}", await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr"), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0020/AC-46: Geçici kapalı bir şube yeniden açıldığında **aktif** olur ve altlıkta yeniden görünür.")]
    public async Task AktifOlur_YenidenAcilinca_AltliktaGorunur()
    {
        string kisaltma = Kisaltma();
        (Guid sube, int surum) = await SubeAcAsync(kisaltma, ad: $"Yeniden {kisaltma}");
        await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/branches/{sube}/close-temporarily", new { version = surum });

        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches/{sube}/reopen", new { version = await SubeSurumuAsync(sube) })).StatusCode);

        Assert.Equal("Active", await ListedekiDurumAsync(sube));
        await PublicSubeAsync($"Yeniden {kisaltma}");
    }

    [Fact(DisplayName = "0020/AC-45: Geçici kapatma şubedeki araçları yayından kaldırmaz.")]
    public async Task YayindanKaldirmaz_GeciciKapatma()
    {
        (Guid sube, int surum) = await SubeAcAsync();
        Guid arac = await YayindakiAracAsync(sube, "GECICI0000000001");
        Assert.True(await VitrindeMiAsync(arac));

        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches/{sube}/close-temporarily", new { version = surum })).StatusCode);

        // Geçici kapatma kapanış duyurusu ÜRETMEZ — A8'in tetiği yok; dağıtıcıya bir tur süre
        // tanındıktan sonra da ilan vitrinde ve araç bayraksız.
        Assert.Equal(0, await SayAsync(
            "SELECT count(*) FROM dealership.outbox_messages WHERE type LIKE '%BranchDeactivated' AND payload->>'BranchId' = @id",
            sube));
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.True(await VitrindeMiAsync(arac));
        Assert.Equal(0, await SayAsync("SELECT count(*) FROM vehicle_catalog.vehicles WHERE id = @id AND awaiting_branch_assignment", arac));
    }

    [Fact(DisplayName = "0020/AC-49: Kalıcı kapatılan şubedeki yayındaki ve rezerve araçlar vitrinden kalkar ve şubedeki araçlar şube ataması bekler olarak işaretlenir (A8).")]
    public async Task VitrindenKalkar_VeSubeAtamasiBekler_KaliciKapatmada()
    {
        (Guid sube, int surum) = await SubeAcAsync();
        Guid yayinda = await YayindakiAracAsync(sube, "KALICI0000000001");
        Guid rezerve = await YayindakiAracAsync(sube, "KALICI0000000002");
        Assert.Equal(HttpStatusCode.NoContent, (await yonetici.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{rezerve}/reserve", UriKind.Relative), null)).StatusCode);
        Guid taslak = await AracOlusturAsync(sube, "KALICI0000000003");

        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches/{sube}/close", new { version = await SubeSurumuAsync(sube) })).StatusCode);

        // Zincir GERÇEKTEN çalışıyor: duyuru outbox'tan dağıtıcıyla `VehicleCatalog`'a ulaşır.
        await BekleAsync(async () => !await VitrindeMiAsync(yayinda) && !await VitrindeMiAsync(rezerve));

        foreach (Guid arac in new[] { yayinda, rezerve, taslak })
        {
            await BekleAsync(async () => await SayAsync(
                "SELECT count(*) FROM vehicle_catalog.vehicles WHERE id = @id AND awaiting_branch_assignment", arac) == 1);
        }
    }

    // ----- Yardımcılar -----

    private IEnumerable<(HttpMethod Yontem, string Yol, object? Govde)> YazmaUclari(Guid sube)
    {
        yield return (HttpMethod.Put, $"{AdminSurface.RoutePrefix}/gallery", new
        {
            version = 1,
            commercialName = "Yetkisiz",
            displayNames = new Dictionary<string, string> { ["tr"] = "Yetkisiz" },
        });
        yield return (HttpMethod.Post, $"{AdminSurface.RoutePrefix}/branches", SubeGovdesi("yetkisiz-sube"));
        yield return (HttpMethod.Put, $"{AdminSurface.RoutePrefix}/branches/{sube}", SubeGovdesi("yetkisiz-sube"));
        yield return (HttpMethod.Post, $"{AdminSurface.RoutePrefix}/branches/{sube}/close-temporarily", new { version = 1 });
        yield return (HttpMethod.Post, $"{AdminSurface.RoutePrefix}/branches/{sube}/reopen", new { version = 1 });
        yield return (HttpMethod.Post, $"{AdminSurface.RoutePrefix}/branches/{sube}/close", new { version = 1 });
        yield return (HttpMethod.Get, $"{AdminSurface.RoutePrefix}/gallery", null);
        yield return (HttpMethod.Get, $"{AdminSurface.RoutePrefix}/branches/{sube}", null);
    }

    private static Task<HttpResponseMessage> GonderAsync(HttpClient client, HttpMethod yontem, string yol, object? govde)
    {
        HttpRequestMessage istek = new(yontem, yol);

        if (govde is not null)
        {
            istek.Content = JsonContent.Create(govde, govde.GetType());
        }

        return client.SendAsync(istek);
    }

    private string Kisaltma() => $"sube-{Interlocked.Increment(ref sayac)}-{Guid.NewGuid().ToString("N")[..6]}";

    private static SubeIstegi SubeGovdesi(
        string kisaltma,
        string ad = "Test Şubesi",
        string telefon = "+902165554433",
        string adres = "Bağdat Caddesi 1",
        string saatDilimi = "Europe/Istanbul") => new(
            new Dictionary<string, string> { ["tr"] = ad },
            new Dictionary<string, string> { ["tr"] = adres },
            telefon,
            saatDilimi,
            kisaltma);

    private async Task<(Guid Id, int Version)> SubeAcAsync(string? kisaltma = null, string ad = "Test Şubesi", string telefon = "+902165554433")
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/branches",
            SubeGovdesi(kisaltma ?? Kisaltma(), ad, telefon));

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        JsonElement govde = JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement;

        return (govde.GetProperty("id").GetGuid(), govde.GetProperty("version").GetInt32());
    }

    private async Task<HttpResponseMessage> SubeKaydetAsync(
        Guid sube,
        int surum,
        string? kisaltma = null,
        string ad = "Test Şubesi",
        string telefon = "+902165554433",
        string adres = "Bağdat Caddesi 1")
    {
        kisaltma ??= (await SubeAsync(sube)).GetProperty("slug").GetString();

        return await yonetici.PutAsJsonAsync($"{AdminSurface.RoutePrefix}/branches/{sube}", new
        {
            version = surum,
            names = new Dictionary<string, string> { ["tr"] = ad },
            addresses = new Dictionary<string, string> { ["tr"] = adres },
            phone = telefon,
            timeZone = "Europe/Istanbul",
            slug = kisaltma,
        });
    }

    private async Task<JsonElement> SubeAsync(Guid sube) =>
        JsonDocument.Parse(await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/branches/{sube}")).RootElement;

    private async Task<int> SubeSurumuAsync(Guid sube) => (await SubeAsync(sube)).GetProperty("version").GetInt32();

    private async Task<string?> ListedekiDurumAsync(Guid sube) =>
        JsonDocument.Parse(await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/branches?locale=tr")).RootElement
            .EnumerateArray().Single(item => item.GetProperty("branchId").GetGuid() == sube)
            .GetProperty("status").GetString();

    private async Task<JsonElement> PublicSubeAsync(string ad)
    {
        JsonElement galeri = JsonDocument.Parse(await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr")).RootElement;
        return galeri.GetProperty("branches").EnumerateArray().Single(item => item.GetProperty("name").GetString() == ad);
    }

    private async Task<int> GaleriSurumuAsync() =>
        JsonDocument.Parse(await yonetici.GetStringAsync($"{AdminSurface.RoutePrefix}/gallery")).RootElement
            .GetProperty("version").GetInt32();

    private async Task<HttpResponseMessage> GaleriKaydetAsync(int surum, string unvan, string tr, string? ar = null)
    {
        Dictionary<string, string> adlar = new() { ["tr"] = tr };

        if (ar is not null)
        {
            adlar["ar"] = ar;
        }

        return await yonetici.PutAsJsonAsync($"{AdminSurface.RoutePrefix}/gallery", new
        {
            version = surum,
            commercialName = unvan,
            displayNames = adlar,
        });
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage yanit, HttpStatusCode beklenen)
    {
        string metin = await yanit.Content.ReadAsStringAsync();
        Assert.True(yanit.StatusCode == beklenen, $"{(int)yanit.StatusCode}: {metin}");
        JsonElement govde = JsonDocument.Parse(metin).RootElement;

        // C5 — cümle yok: yalnız kod ve alan parametreleri.
        Assert.False(govde.TryGetProperty("title", out _));
        Assert.False(govde.TryGetProperty("detail", out _));
        return govde;
    }

    private async Task<List<Guid>> AktifSubelerAsync(Guid haric)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM dealership.branches WHERE status = 1 AND id <> @id";
        command.Parameters.AddWithValue("id", haric);

        List<Guid> kimlikler = [];
        await using NpgsqlDataReader okuyucu = await command.ExecuteReaderAsync();

        while (await okuyucu.ReadAsync())
        {
            kimlikler.Add(okuyucu.GetGuid(0));
        }

        return kimlikler;
    }

    private async Task DurumYazAsync(List<Guid> subeler, int durum)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE dealership.branches SET status = @durum WHERE id = ANY(@ids)";
        command.Parameters.AddWithValue("durum", durum);
        command.Parameters.AddWithValue("ids", subeler.ToArray());
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> SayAsync(string sql, Guid id)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("id", sql.Contains("->>", StringComparison.Ordinal) ? id.ToString() : id);

        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<bool> VitrindeMiAsync(Guid arac) =>
        await SayAsync("SELECT count(*) FROM vehicle_catalog.showcase_listings WHERE vehicle_id = @id", arac) == 1;

    private async Task<Guid> AracOlusturAsync(Guid sube, string vin)
    {
        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/vehicles", new
        {
            branchId = sube,
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
        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement.GetProperty("vehicleId").GetGuid();
    }

    private async Task<Guid> YayindakiAracAsync(Guid sube, string vin)
    {
        Guid arac = await AracOlusturAsync(sube, vin);
        Guid varlik = await HazirMedyaEkleAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await yonetici.PutAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/vehicles/{arac}/media", new { mediaAssetIds = new[] { varlik } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await yonetici.PostAsync(
            new Uri($"{AdminSurface.RoutePrefix}/vehicles/{arac}/publish", UriKind.Relative), null)).StatusCode);

        return arac;
    }

    private async Task<Guid> HazirMedyaEkleAsync()
    {
        Guid varlik = Guid.CreateVersion7();

        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO media.assets (
                id, status, content_type, byte_size, source_width, source_height,
                original_object_key, alt_text, attempt_count, uploaded_at, updated_at)
            VALUES (@id, 3, 'image/jpeg', 1024, 1200, 900, @key, '{}'::jsonb, 1, now(), now());
            INSERT INTO media.asset_derivatives (asset_id, variant_code, width, height, object_key)
            VALUES (@id, 'display', 800, 600, @derivative);
            """;
        command.Parameters.AddWithValue("id", varlik);
        command.Parameters.AddWithValue("key", $"private/originals/{varlik:N}.jpg");
        command.Parameters.AddWithValue("derivative", $"public/assets/{varlik:D}/display.webp");
        await command.ExecuteNonQueryAsync();

        return varlik;
    }

    private static async Task BekleAsync(Func<Task<bool>> kosul)
    {
        DateTimeOffset son = DateTimeOffset.UtcNow.AddSeconds(30);

        while (DateTimeOffset.UtcNow < son)
        {
            if (await kosul())
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("Zincir 30 saniyede tamamlanmadı: duyuru teslim edilmedi.");
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

    private sealed record SubeIstegi(
        Dictionary<string, string> Names,
        Dictionary<string, string> Addresses,
        string Phone,
        string TimeZone,
        string Slug);
}
