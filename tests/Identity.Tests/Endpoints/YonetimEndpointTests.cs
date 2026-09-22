using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Tests.Integration;
using Microsoft.AspNetCore.Http;

namespace CarGallery.Modules.Identity.Tests.Endpoints;

/// <summary>
/// Durum kodu ve hata sözleşmesi (docs/testing.md § 1). C19'un ayrımı burada sınanır:
/// oturumsuz istek `401`, oturumu olup yetkisi yetmeyen istek `403`.
/// </summary>
[Collection(IdentityDatabaseCollection.Name)]
public sealed class YonetimEndpointTests(IdentityDatabase database)
{
    private const string Parola = "cok-guclu-parola";

    [Fact(DisplayName = "0006/AC-23: Yönetim rota kökü altındaki her endpoint, oturumsuz istekte `401` döner; bu, rotaları tarayan bir testle doğrulanır")]
    public async Task Doner401_OturumsuzIstek()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await api.Client.GetAsync($"{AdminSurface.RoutePrefix}/staff");

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact(DisplayName = "0006/AC-28: Yönetici rolündeki personel, personel ekleme ve rol değiştirme işlemlerini yapar")]
    public async Task Yapar_YoneticiRolu()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        await GirisAsync(api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi);
        await ParolaDegistirVeYenidenGirAsync(
            api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi, Parola);

        HttpResponseMessage ekleme = await EkleAsync(api, "danisman@ornek.com", "sales_advisor");

        Assert.Equal(HttpStatusCode.Created, ekleme.StatusCode);

        Guid danismanId = (await ekleme.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("staffId").GetGuid();

        HttpResponseMessage rol = await api.Client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/staff/{danismanId}/role", new { role = "content_editor" });

        Assert.Equal(HttpStatusCode.NoContent, rol.StatusCode);
    }

    [Theory(DisplayName = "0006/AC-25: Satış Danışmanı rolündeki personel, personel ekleme ve rol değiştirme işlemlerini yapamaz; istek `403` ile reddedilir")]
    [InlineData("sales_advisor")]
    [InlineData("content_editor")]
    public async Task Doner403_YoneticiOlmayanRoller(string rol)
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        await GirisAsync(api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi);
        await ParolaDegistirVeYenidenGirAsync(
            api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi, Parola);
        await EkleAsync(api, "personel@ornek.com", rol);

        await GirisAsync(api, "personel@ornek.com", Parola);
        await ParolaDegistirVeYenidenGirAsync(api, "personel@ornek.com", Parola, "personel-parolasi-1");

        HttpResponseMessage listeleme = await api.Client.GetAsync($"{AdminSurface.RoutePrefix}/staff");
        HttpResponseMessage ekleme = await EkleAsync(api, "yeni@ornek.com", "sales_advisor");

        Assert.Equal(HttpStatusCode.Forbidden, listeleme.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, ekleme.StatusCode);
    }

    /// <summary>
    /// R10/AC-33 — zorunlu parola değişimi bekleyen personelin YALNIZCA parola değiştirme
    /// ve çıkış istekleri çalışır. Kurulum hesabı tam olarak bu durumda doğar (D2).
    /// </summary>
    [Fact(DisplayName = "0006/AC-33: Zorunlu parola değişimi bekleyen personelin yalnızca parola değiştirme ve çıkış istekleri çalışır; diğer her yönetim isteği reddedilir")]
    public async Task Reddedilir_ParolaDegisimiBekleyenDigerIstekler()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        await GirisAsync(api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi);

        HttpResponseMessage listeleme = await api.Client.GetAsync($"{AdminSurface.RoutePrefix}/staff");

        Assert.Equal(HttpStatusCode.Forbidden, listeleme.StatusCode);
        Assert.Equal("identity.password_change_required", await HataKoduAsync(listeleme));

        // Parola değiştirme ÇALIŞIR.
        Assert.Equal(HttpStatusCode.NoContent,
            (await ParolaDegistirAsync(api, ModuleApi.SeedYoneticiParolasi, Parola)).StatusCode);

        // Değişiklik tüm oturumları düşürdü (R11); yeniden giriş sonrası diğer istekler
        // de çalışır ve kapı artık kapalı değildir.
        Assert.Equal(HttpStatusCode.OK,
            (await GirisAsync(api, ModuleApi.SeedYoneticiKimligi, Parola)).StatusCode);

        Assert.Equal(HttpStatusCode.OK,
            (await api.Client.GetAsync($"{AdminSurface.RoutePrefix}/staff")).StatusCode);
    }

    /// <summary>
    /// Çıkışın çalışması bir İZİNDEN değil, kapının KAPSAMINDAN geliyor: çıkış yolu
    /// yönetim kökünün dışında ve `PasswordChangeGate` o kökle sınırlı. Bu ayrımın
    /// yazılı olması önemli — bir zamanlar kapıda çıkışı muaf tutan bir koşul vardı,
    /// hiçbir zaman çalışmıyordu ve okuyana var olmayan bir koruma vaat ediyordu.
    /// </summary>
    [Fact(DisplayName = "0006/AC-33: Zorunlu parola değişimi bekleyen personelin yalnızca parola değiştirme ve çıkış istekleri çalışır; diğer her yönetim isteği reddedilir")]
    public async Task Calisir_ParolaDegisimiBekleyeninCikisi()
    {
        // Çıkış yolu yönetim kökünün DIŞINDA: kapı onu hiç görmüyor. Yol bir gün kökün
        // altına taşınırsa bu satır kırmızıya döner ve muafiyet açıkça yazılmak zorunda
        // kalır — sessizce kilitlenen bir çıkış yerine kırmızı bir test.
        Assert.False(new PathString(CikisYolu).StartsWithSegments(AdminSurface.RoutePrefix));

        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        await GirisAsync(api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi);

        HttpResponseMessage cikis = await api.Client.DeleteAsync(CikisYolu);

        Assert.Equal(HttpStatusCode.NoContent, cikis.StatusCode);
    }

    private const string CikisYolu = "/api/identity/sessions/current";

    /// <summary>
    /// C17/AC-29 — token yanıt GÖVDESİNE hiç girmez ve çerez `HttpOnly`'dir; sayfadaki
    /// JavaScript onu okuyamaz.
    /// </summary>
    [Fact(DisplayName = "0006/AC-29: Oturum bilgisi, sayfadaki JavaScript'ten okunabilir bir yerde tutulmaz")]
    public async Task HttpOnlyCerez_VeGovdedeTokenYok()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage giris = await GirisAsync(api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi);
        string cerez = giris.Headers.GetValues("Set-Cookie").Single();
        string govde = await giris.Content.ReadAsStringAsync();

        Assert.Contains("httponly", cerez, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cerez.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cerez, StringComparison.OrdinalIgnoreCase);

        // Aranan şey token ve parola DEĞERLERİDİR; `mustChangePassword` bir alan adıdır
        // ve gövdede bulunması beklenir.
        string token = cerez.Split(';')[0].Split('=', 2)[1];

        Assert.NotEmpty(token);
        Assert.DoesNotContain(token, govde, StringComparison.Ordinal);
        Assert.DoesNotContain(ModuleApi.SeedYoneticiParolasi, govde, StringComparison.Ordinal);
        Assert.DoesNotContain("pbkdf2", govde, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "0006/AC-30: Durum değiştiren bir yönetim isteği başka bir kökenden tetiklendiğinde reddedilir")]
    public async Task Reddedilir_BaskaKokendenDurumDegistirenIstek()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        await GirisAsync(api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi);
        await ParolaDegistirVeYenidenGirAsync(
            api, ModuleApi.SeedYoneticiKimligi, ModuleApi.SeedYoneticiParolasi, Parola);

        HttpRequestMessage istek = new(HttpMethod.Post, $"{AdminSurface.RoutePrefix}/staff")
        {
            Content = JsonContent.Create(new
            {
                displayName = "Sahte", loginId = "sahte@ornek.com", initialPassword = Parola,
                role = "sales_advisor", preferredLocale = "tr", branchId = (Guid?)null,
            }),
        };

        istek.Headers.Add("Origin", "https://kotu-site.test");

        HttpResponseMessage yanit = await api.Client.SendAsync(istek);

        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
    }

    [Fact(DisplayName = "0006/AC-59: Giriş ve personel yönetimi ekranlarının hiçbir hata durumunda HTTP durum kodu, hata kodu, `traceId`, endpoint yolu veya istisna metni görünmez")]
    public async Task IcermezTeknikAyrinti_HataGovdesi()
    {
        await database.ResetAsync();
        await using ModuleApi api = await ModuleApi.StartAsync(database);

        HttpResponseMessage yanit = await GirisAsync(api, "hic-yok@ornek.com", "yanlis-parola-123");
        string govde = await yanit.Content.ReadAsStringAsync();

        foreach (string isaret in new[] { "Exception", "   at ", "Npgsql", "/api/identity", "IdentityDbContext", "title", "detail" })
        {
            Assert.DoesNotContain(isaret, govde, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal("identity.invalid_credentials", await HataKoduAsync(yanit));
    }

    private static Task<HttpResponseMessage> GirisAsync(ModuleApi api, string kimlik, string parola) =>
        api.Client.PostAsJsonAsync("/api/identity/sessions", new { loginId = kimlik, password = parola });

    private static Task<HttpResponseMessage> ParolaDegistirAsync(ModuleApi api, string mevcut, string yeni) =>
        api.Client.PostAsJsonAsync(
            $"{AdminSurface.RoutePrefix}/me/password",
            new { currentPassword = mevcut, newPassword = yeni });

    /// <summary>
    /// R11/AC-22 — parola değiştiğinde o personelin TÜM yenileme hakları geçersizleşir;
    /// buna değişikliği yapan cihazın oturumu da dâhildir. Bu yüzden değişimden sonra
    /// YENİDEN GİRİŞ yapılır. Kriterin "tüm" sözcüğü bunu gerektiriyor; mevcut oturumu
    /// ayrıcalıklı tutmak, sızan parolayla açılmış bir oturumun hayatta kalmasına da
    /// kapı açardı.
    /// </summary>
    private static async Task ParolaDegistirVeYenidenGirAsync(
        ModuleApi api,
        string kimlik,
        string mevcut,
        string yeni)
    {
        Assert.Equal(HttpStatusCode.NoContent, (await ParolaDegistirAsync(api, mevcut, yeni)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GirisAsync(api, kimlik, yeni)).StatusCode);
    }

    private static Task<HttpResponseMessage> EkleAsync(ModuleApi api, string kimlik, string rol) =>
        api.Client.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/staff", new
        {
            displayName = "Yeni Personel",
            loginId = kimlik,
            initialPassword = Parola,
            role = rol,
            preferredLocale = "tr",
            branchId = (Guid?)null,
        });

    private static async Task<string?> HataKoduAsync(HttpResponseMessage yanit) =>
        JsonDocument.Parse(await yanit.Content.ReadAsStringAsync())
            .RootElement.GetProperty("errorCode").GetString();
}
