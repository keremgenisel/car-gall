using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AngleSharp.Html.Parser;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0025` — şube çalışma programının gerçek boru hattı: gövde (D11, P1), alan hatası (D12),
/// sürüm ve kapatılmış şube (R6), Yönetici politikası (R10), panel okuması (D14), ziyaretçi okuması
/// (D8) ve ana sayfa belgesinin `openingHoursSpecification`'ı (D9). Gerçek PostgreSQL; üç rol
/// gerçek personel olarak girer. Her test kendi şubesini açar.
/// </summary>
public sealed class CalismaSaatleriZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";
    private const string PersonelParola = "personel-parolasi-1";
    private const string Galeri = $"{AdminSurface.RoutePrefix}/gallery";
    private const string Subeler = $"{AdminSurface.RoutePrefix}/branches";

    private static readonly string[] Gunler = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

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

        // Belge kriterleri `AutoDealer` bloğuna bakar: görünen ad zorunlu (`0004` D15).
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

    // ----- Panel: giriş (R1, R6 · D5) -----

    [Fact(DisplayName = "0025/AC-3, AC-40, AC-42: Pazartesiye 09:00–18:00 kaydedilir; panel okuması D8'in biçiminde yedi gün taşır, diğer günler boş dizidir.")]
    public async Task Kaydeder_Pazartesi_PanelOkumasiD8()
    {
        Guid sube = await SubeAcAsync();

        await ProgramYazAsync(sube, Program(("monday", [("09:00", "18:00")])));

        JsonElement program = (await SubeAsync(sube)).GetProperty("openingHours");
        Assert.Equal(Gunler, program.EnumerateObject().Select(gun => gun.Name));
        Assert.Equal("""[{"start":"09:00","end":"18:00"}]""", program.GetProperty("monday").GetRawText());
        Assert.All(Gunler.Skip(1), gun => Assert.Equal(0, program.GetProperty(gun).GetArrayLength()));
    }

    [Fact(DisplayName = "0025/AC-4, AC-6, AC-19: İki aralık sıralı saklanır ve döner; bir aralığı kaldırmak onu siler, sonuncusunu kaldırmak günü kapalı yapar.")]
    public async Task IkiAralik_KaldirmaVeSira()
    {
        Guid sube = await SubeAcAsync();

        await ProgramYazAsync(sube, Program(("tuesday", [("16:00", "21:00"), ("09:00", "13:00")])));
        Assert.Equal("""[{"start":"09:00","end":"13:00"},{"start":"16:00","end":"21:00"}]""", (await SubeAsync(sube)).GetProperty("openingHours").GetProperty("tuesday").GetRawText());

        await ProgramYazAsync(sube, Program(("tuesday", [("09:00", "13:00")]), ("friday", [("10:00", "12:00")])));
        Assert.Equal(1, (await SubeAsync(sube)).GetProperty("openingHours").GetProperty("tuesday").GetArrayLength());

        await ProgramYazAsync(sube, Program(("friday", [("10:00", "12:00")])));
        Assert.Equal(0, (await SubeAsync(sube)).GetProperty("openingHours").GetProperty("tuesday").GetArrayLength());
    }

    [Fact(DisplayName = "0025/AC-8, AC-9: Programla açılan şubenin programı saklanır; programsız açılan şubenin okumasında `openingHours` null'dır.")]
    public async Task Acma_ProgramliVeProgramsiz()
    {
        Guid programsiz = await SubeAcAsync();
        Assert.Equal(JsonValueKind.Null, (await SubeAsync(programsiz)).GetProperty("openingHours").ValueKind);

        Guid programli = await SubeAcAsync(Program(("saturday", [("10:00", "14:00")])));
        Assert.Equal("10:00", (await SubeAsync(programli)).GetProperty("openingHours").GetProperty("saturday")[0].GetProperty("start").GetString());
    }

    // ----- Doğrulama (R5 · D3, D12) -----

    [Theory(DisplayName = "0025/AC-10, AC-11, AC-12, AC-13, AC-14: Hatalı aralık `422` ve aralığın alanında kararlı kod döner.")]
    [InlineData("9:00", "18:00", "openingHours.monday.0.start", "opening_hours.invalid_time")]
    [InlineData("09:00", "24:00", "openingHours.monday.0.end", "opening_hours.invalid_time")]
    [InlineData("12:60", "18:00", "openingHours.monday.0.start", "opening_hours.invalid_time")]
    [InlineData("18:00", "09:00", "openingHours.monday.0.end", "opening_hours.end_not_after_start")]
    [InlineData("09:00", "09:00", "openingHours.monday.0.end", "opening_hours.end_not_after_start")]
    public async Task Doner422_HataliAralik(string baslangic, string bitis, string alan, string kod)
    {
        Guid sube = await SubeAcAsync();

        JsonElement hata = await ProblemAsync(await KaydetAsync(sube, Program(("monday", [(baslangic, bitis)]))), HttpStatusCode.UnprocessableEntity);

        Assert.Equal("dealership.validation_failed", hata.GetProperty("errorCode").GetString());
        Assert.Equal(kod, hata.GetProperty(alan).GetString());
    }

    [Fact(DisplayName = "0025/AC-15, AC-16, AC-17: Dört aralık `too_many_intervals`, çakışma ve uç uca değme `overlap` — gün alanında.")]
    public async Task Doner422_GunDuzeyi()
    {
        Guid sube = await SubeAcAsync();

        JsonElement dort = await ProblemAsync(await KaydetAsync(sube, Program(("monday", [("08:00", "09:00"), ("10:00", "11:00"), ("12:00", "13:00"), ("14:00", "15:00")]))), HttpStatusCode.UnprocessableEntity);
        Assert.Equal("opening_hours.too_many_intervals", dort.GetProperty("openingHours.monday").GetString());

        foreach (string ikinci in new[] { "12:00", "13:00" })
        {
            JsonElement cakisma = await ProblemAsync(await KaydetAsync(sube, Program(("monday", [("09:00", "13:00"), (ikinci, "18:00")]))), HttpStatusCode.UnprocessableEntity);
            Assert.Equal("opening_hours.overlap", cakisma.GetProperty("openingHours.monday").GetString());
        }
    }

    [Fact(DisplayName = "0025/AC-18, AC-70: `funday` ve `Monday` kodları kendi alanlarında `opening_hours.unknown_day` döner.")]
    public async Task Doner422_BilinmeyenGun()
    {
        Guid sube = await SubeAcAsync();
        JsonObject program = Program();
        program["funday"] = new JsonArray();
        program["Monday"] = Araliklar([("09:00", "18:00")]);

        JsonElement hata = await ProblemAsync(await KaydetAsync(sube, program), HttpStatusCode.UnprocessableEntity);

        Assert.Equal("opening_hours.unknown_day", hata.GetProperty("openingHours.funday").GetString());
        Assert.Equal("opening_hours.unknown_day", hata.GetProperty("openingHours.Monday").GetString());
    }

    [Fact(DisplayName = "0025/AC-20, AC-21: Hatalı programlı kaydetmede şubenin hiçbir alanı değişmez (geçerli adres değişikliği de); Pazartesi ve Çarşamba'nın hataları birlikte gelir.")]
    public async Task Degistirmez_HicbirAlani()
    {
        Guid sube = await SubeAcAsync(Program(("friday", [("10:00", "12:00")])));
        string once = await yonetici.GetStringAsync($"{Subeler}/{sube}");

        JsonObject govde = await GovdeAsync(sube, Program(("monday", [("9:00", "18:00")]), ("wednesday", [("18:00", "09:00")])));
        govde["addresses"] = new JsonObject { ["tr"] = "Yeni Adres Sokak 9" };

        JsonElement hata = await ProblemAsync(await yonetici.PutAsJsonAsync($"{Subeler}/{sube}", govde), HttpStatusCode.UnprocessableEntity);

        Assert.Equal("opening_hours.invalid_time", hata.GetProperty("openingHours.monday.0.start").GetString());
        Assert.Equal("opening_hours.end_not_after_start", hata.GetProperty("openingHours.wednesday.0.end").GetString());
        Assert.Equal(once, await yonetici.GetStringAsync($"{Subeler}/{sube}"));
    }

    [Fact(DisplayName = "0025/AC-23: Pazartesi 00:00–23:59 kabul edilir.")]
    public async Task KabulEder_TumGun()
    {
        Guid sube = await SubeAcAsync();

        await ProgramYazAsync(sube, Program(("monday", [("00:00", "23:59")])));

        Assert.Equal("23:59", (await SubeAsync(sube)).GetProperty("openingHours").GetProperty("monday")[0].GetProperty("end").GetString());
    }

    // ----- Opsiyonellik, saat dilimi, sürüm (R2, R3, R4, R6 · D2, D4, D11) -----

    [Fact(DisplayName = "0025/AC-24, AC-72: Yedi boş gün programı siler: okumada `openingHours` null, belgede o şubenin öğesinde `openingHoursSpecification` yok; `missing_day` dönmez.")]
    public async Task Siler_YediBosGun()
    {
        string ad = Benzersiz("Silinen Şube");
        Guid sube = await SubeAcAsync(Program(("monday", [("09:00", "18:00")])), ad);

        await ProgramYazAsync(sube, Program());

        Assert.Equal(JsonValueKind.Null, (await SubeAsync(sube)).GetProperty("openingHours").ValueKind);
        Assert.Equal(JsonValueKind.Null, (await PublicSubeAsync(ad)).GetProperty("openingHours").ValueKind);
        Assert.DoesNotContain("openingHoursSpecification", (await DepartmentAsync(ad)).EnumerateObject().Select(p => p.Name));
    }

    [Fact(DisplayName = "0025/AC-25, AC-66, AC-67: Program alanı olmayan, açık null taşıyan ve GET yanıtını aynen geri gönderen kaydetme programı değiştirmez; programsız şube programsız kalır.")]
    public async Task Degistirmez_AlansizNullVeGidisDonus()
    {
        Guid sube = await SubeAcAsync(Program(("monday", [("09:00", "18:00")])));
        string program = (await SubeAsync(sube)).GetProperty("openingHours").GetRawText();

        // `0020`'nin gövdesi: `openingHours` anahtarı yok.
        JsonObject alansiz = await GovdeAsync(sube, null);
        alansiz.Remove("openingHours");
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsJsonAsync($"{Subeler}/{sube}", alansiz)).StatusCode);
        Assert.Equal(program, (await SubeAsync(sube)).GetProperty("openingHours").GetRawText());

        JsonObject acikNull = await GovdeAsync(sube, null);
        acikNull["openingHours"] = null;
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsJsonAsync($"{Subeler}/{sube}", acikNull)).StatusCode);
        Assert.Equal(program, (await SubeAsync(sube)).GetProperty("openingHours").GetRawText());

        // AC-67 — GET yanıtı olduğu gibi (sürüm dâhil) geri gönderilir.
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsync($"{Subeler}/{sube}", new StringContent(await yonetici.GetStringAsync($"{Subeler}/{sube}"), Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(program, (await SubeAsync(sube)).GetProperty("openingHours").GetRawText());

        Guid programsiz = await SubeAcAsync();
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsync($"{Subeler}/{programsiz}", new StringContent(await yonetici.GetStringAsync($"{Subeler}/{programsiz}"), Encoding.UTF8, "application/json"))).StatusCode);
        Assert.Equal(JsonValueKind.Null, (await SubeAsync(programsiz)).GetProperty("openingHours").ValueKind);
    }

    [Fact(DisplayName = "0025/AC-68, AC-69: `sunday`'siz program `openingHours.sunday`'de, boş nesne yedi günün her birinde `missing_day` döner; kayıtlı program değişmez.")]
    public async Task Doner422_EksikGun()
    {
        Guid sube = await SubeAcAsync(Program(("monday", [("09:00", "18:00")])));
        string program = (await SubeAsync(sube)).GetProperty("openingHours").GetRawText();

        JsonObject pazarsiz = Program();
        pazarsiz.Remove("sunday");
        JsonElement hata = await ProblemAsync(await KaydetAsync(sube, pazarsiz), HttpStatusCode.UnprocessableEntity);
        Assert.Equal("opening_hours.missing_day", hata.GetProperty("openingHours.sunday").GetString());

        JsonElement bos = await ProblemAsync(await KaydetAsync(sube, new JsonObject()), HttpStatusCode.UnprocessableEntity);
        Assert.All(Gunler, gun => Assert.Equal("opening_hours.missing_day", bos.GetProperty($"openingHours.{gun}").GetString()));

        Assert.Equal(program, (await SubeAsync(sube)).GetProperty("openingHours").GetRawText());
    }

    [Fact(DisplayName = "0025/AC-26: Saat dilimi `Asia/Kuwait`'e değişince Pazartesi 09:00–18:00 okumada ve belgede 09:00–18:00 kalır.")]
    public async Task SaatDilimi_DuvarSaatiniDegistirmez()
    {
        string ad = Benzersiz("Kuveyt Şubesi");
        Guid sube = await SubeAcAsync(Program(("monday", [("09:00", "18:00")])), ad);

        JsonObject govde = await GovdeAsync(sube, null);
        govde.Remove("openingHours");
        govde["timeZone"] = "Asia/Kuwait";
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PutAsJsonAsync($"{Subeler}/{sube}", govde)).StatusCode);

        Assert.Equal("Asia/Kuwait", (await SubeAsync(sube)).GetProperty("timeZone").GetString());
        Assert.Equal("""[{"start":"09:00","end":"18:00"}]""", (await PublicSubeAsync(ad)).GetProperty("openingHours").GetProperty("monday").GetRawText());
        JsonElement oge = (await DepartmentAsync(ad)).GetProperty("openingHoursSpecification")[0];
        Assert.Equal(("09:00", "18:00"), (oge.GetProperty("opens").GetString(), oge.GetProperty("closes").GetString()));
    }

    [Fact(DisplayName = "0025/AC-27, AC-28: Eski sürümle program değiştiren kaydetme `409` ve program değişmez; başarılı kaydetme sürümü bir artırır ve yeni sürümü döner.")]
    public async Task Doner409_VeArtirir()
    {
        Guid sube = await SubeAcAsync();
        int surum = await SurumAsync(sube);

        HttpResponseMessage basarili = await KaydetAsync(sube, Program(("monday", [("09:00", "18:00")])), surum);
        Assert.Equal(HttpStatusCode.OK, basarili.StatusCode);
        Assert.Equal(surum + 1, JsonDocument.Parse(await basarili.Content.ReadAsStringAsync()).RootElement.GetProperty("version").GetInt32());

        JsonElement hata = await ProblemAsync(await KaydetAsync(sube, Program(("monday", [("10:00", "18:00")])), surum), HttpStatusCode.Conflict);
        Assert.Equal("concurrency.lost", hata.GetProperty("errorCode").GetString());
        Assert.Equal("09:00", (await SubeAsync(sube)).GetProperty("openingHours").GetProperty("monday")[0].GetProperty("start").GetString());
    }

    [Fact(DisplayName = "0025/AC-29: Kapatılmış şubenin programını değiştiren kaydetme `409` ve `branch.closed_read_only`; program değişmez.")]
    public async Task Doner409_KapatilmisSube()
    {
        Guid sube = await SubeAcAsync(Program(("monday", [("09:00", "18:00")])));
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync($"{Subeler}/{sube}/close", new { version = await SurumAsync(sube) })).StatusCode);

        JsonElement hata = await ProblemAsync(await KaydetAsync(sube, Program(("monday", [("10:00", "18:00")]))), HttpStatusCode.Conflict);

        Assert.Equal("branch.closed_read_only", hata.GetProperty("errorCode").GetString());
        Assert.Equal("09:00", (await SubeAsync(sube)).GetProperty("openingHours").GetProperty("monday")[0].GetProperty("start").GetString());
    }

    [Fact(DisplayName = "0025/AC-30, AC-57: Geçici kapalı şubenin programı değiştirilebilir; kapalıyken `department`'ta yoktur, yeniden açıldıktan sonraki ilk `/tr/` belgesi yeni programı taşır.")]
    public async Task GeciciKapali_DegistirilebilirVeAcilincaGorunur()
    {
        string ad = Benzersiz("Geçici Şube");
        Guid sube = await SubeAcAsync(Program(("monday", [("09:00", "18:00")])), ad);
        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync($"{Subeler}/{sube}/close-temporarily", new { version = await SurumAsync(sube) })).StatusCode);

        await ProgramYazAsync(sube, Program(("monday", [("08:00", "12:00")])));
        Assert.DoesNotContain(await DepartmentlarAsync(), oge => oge.GetProperty("name").GetString() == ad);

        Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync($"{Subeler}/{sube}/reopen", new { version = await SurumAsync(sube) })).StatusCode);

        Assert.Equal("08:00", (await DepartmentAsync(ad)).GetProperty("openingHoursSpecification")[0].GetProperty("opens").GetString());
    }

    [Fact(DisplayName = "0025/AC-31: Programı değiştiren kaydetme outbox'a hiçbir entegrasyon olayı yazmaz.")]
    public async Task OutboxaYazmaz()
    {
        Guid sube = await SubeAcAsync();
        long once = await OutboxAsync();

        await ProgramYazAsync(sube, Program(("monday", [("09:00", "18:00")])));
        await ProgramYazAsync(sube, Program());

        Assert.Equal(once, await OutboxAsync());
    }

    // ----- Yetki (R10) -----

    [Fact(DisplayName = "0025/AC-32, AC-33, AC-34: Satış Danışmanı ve İçerik Editörü `403`, oturumsuz istek `401`; program değişmez.")]
    public async Task Yetki()
    {
        Guid sube = await SubeAcAsync(Program(("monday", [("09:00", "18:00")])));
        JsonObject govde = await GovdeAsync(sube, Program(("monday", [("10:00", "11:00")])));

        foreach (HttpClient rol in new[] { danisman, editor })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await rol.PutAsJsonAsync($"{Subeler}/{sube}", govde)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await Istemci().PutAsJsonAsync($"{Subeler}/{sube}", govde)).StatusCode);
        Assert.Equal("09:00", (await SubeAsync(sube)).GetProperty("openingHours").GetProperty("monday")[0].GetProperty("start").GetString());
    }

    [Fact(DisplayName = "0025/AC-36: Satış Danışmanının okuduğu şube listesinin öğe anahtarları bu karttan önceki kümedir (`branchId`, `name`, `status`).")]
    public async Task SubeListesi_Degismez()
    {
        await SubeAcAsync(Program(("monday", [("09:00", "18:00")])));

        JsonElement liste = JsonDocument.Parse(await danisman.GetStringAsync($"{Subeler}?locale=tr")).RootElement;

        Assert.All(liste.EnumerateArray(), oge => Assert.Equal(["branchId", "name", "status"], oge.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)));
    }

    // ----- Ziyaretçiye dönük okuma ve belge (R8, R9 · D8, D9) -----

    [Fact(DisplayName = "0025/AC-41, AC-42, AC-43: Public şube öğesinin anahtarları tam olarak `address, name, openingHours, phone`; programlı şubede yedi gün, `sunday` boş, `saturday` 10:00–14:00; tr/ar/en aynı.")]
    public async Task PublicOkuma()
    {
        string ad = Benzersiz("Programlı Şube");
        await SubeAcAsync(Programli(), ad);

        JsonElement galeri = JsonDocument.Parse(await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr")).RootElement;
        Assert.Equal(["branches", "displayName", "socialLinks"], galeri.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.All(galeri.GetProperty("branches").EnumerateArray(), oge => Assert.Equal(["address", "name", "openingHours", "phone"], oge.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal)));

        JsonElement program = (await PublicSubeAsync(ad)).GetProperty("openingHours");
        Assert.Equal(Gunler, program.EnumerateObject().Select(p => p.Name));
        Assert.Equal("[]", program.GetProperty("sunday").GetRawText());
        Assert.Equal("""[{"start":"10:00","end":"14:00"}]""", program.GetProperty("saturday").GetRawText());

        string[] diller = await Task.WhenAll(new[] { "tr", "ar", "en" }.Select(async locale =>
            JsonDocument.Parse(await Istemci().GetStringAsync($"/api/dealership/gallery?locale={locale}")).RootElement
                .GetProperty("branches").EnumerateArray().Select(oge => oge.GetProperty("openingHours").GetRawText()).Order(StringComparer.Ordinal).Aggregate((a, b) => a + b)));
        Assert.Equal(diller[0], diller[1]);
        Assert.Equal(diller[0], diller[2]);
    }

    [Fact(DisplayName = "0025/AC-52, AC-58: Yönetici programı kaydettikten sonraki ilk `/tr/` belgesinde şubenin öğesi üç gruplu `openingHoursSpecification` taşır.")]
    public async Task YapisalVeri_KaydettiktenSonra()
    {
        string ad = Benzersiz("Belge Şubesi");
        Guid sube = await SubeAcAsync(null, ad);
        Assert.DoesNotContain("openingHoursSpecification", (await DepartmentAsync(ad)).EnumerateObject().Select(p => p.Name));

        await ProgramYazAsync(sube, Programli());

        JsonElement[] ogeler = [.. (await DepartmentAsync(ad)).GetProperty("openingHoursSpecification").EnumerateArray()];
        Assert.Equal(3, ogeler.Length);
        Assert.Equal("""["Monday","Tuesday","Wednesday","Thursday","Friday"]""", ogeler[0].GetProperty("dayOfWeek").GetRawText());
        Assert.Equal(("10:00", "14:00"), (ogeler[1].GetProperty("opens").GetString(), ogeler[1].GetProperty("closes").GetString()));
        Assert.Equal(("00:00", "00:00"), (ogeler[2].GetProperty("opens").GetString(), ogeler[2].GetProperty("closes").GetString()));
    }

    // ----- Yardımcılar -----

    private static JsonObject Programli() => Program(
        ("monday", [("09:00", "18:00")]),
        ("tuesday", [("09:00", "18:00")]),
        ("wednesday", [("09:00", "18:00")]),
        ("thursday", [("09:00", "18:00")]),
        ("friday", [("09:00", "18:00")]),
        ("saturday", [("10:00", "14:00")]));

    private static JsonObject Program(params (string Gun, (string Baslangic, string Bitis)[] Araliklar)[] gunler)
    {
        JsonObject program = [];

        foreach (string gun in Gunler)
        {
            program[gun] = Araliklar(gunler.FirstOrDefault(item => item.Gun == gun).Araliklar ?? []);
        }

        return program;
    }

    private static JsonArray Araliklar((string Baslangic, string Bitis)[] araliklar) =>
        [.. araliklar.Select(aralik => (JsonNode)new JsonObject { ["start"] = aralik.Baslangic, ["end"] = aralik.Bitis })];

    private string Benzersiz(string onek) => $"{onek} {Interlocked.Increment(ref sayac)}-{Guid.NewGuid().ToString("N")[..4]}";

    private async Task<Guid> SubeAcAsync(JsonObject? program = null, string? ad = null)
    {
        JsonObject govde = new()
        {
            ["names"] = new JsonObject { ["tr"] = ad ?? Benzersiz("Test Şubesi") },
            ["addresses"] = new JsonObject { ["tr"] = "Bağdat Caddesi 1" },
            ["phone"] = "+902165554433",
            ["timeZone"] = "Europe/Istanbul",
            ["slug"] = $"sube-{Interlocked.Increment(ref sayac)}-{Guid.NewGuid().ToString("N")[..6]}",
        };

        if (program is not null)
        {
            govde["openingHours"] = program;
        }

        HttpResponseMessage yanit = await yonetici.PostAsJsonAsync(Subeler, govde);
        Assert.True(yanit.StatusCode == HttpStatusCode.Created, $"{(int)yanit.StatusCode}: {await yanit.Content.ReadAsStringAsync()}");
        return JsonDocument.Parse(await yanit.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();
    }

    /// <summary>GET yanıtından PUT gövdesi; `program` null ise `openingHours` okunduğu gibi kalır.</summary>
    private async Task<JsonObject> GovdeAsync(Guid sube, JsonObject? program, int? surum = null)
    {
        JsonObject govde = JsonNode.Parse(await yonetici.GetStringAsync($"{Subeler}/{sube}"))!.AsObject();

        if (program is not null)
        {
            govde["openingHours"] = program;
        }

        if (surum is not null)
        {
            govde["version"] = surum;
        }

        return govde;
    }

    private async Task<HttpResponseMessage> KaydetAsync(Guid sube, JsonObject program, int? surum = null) =>
        await yonetici.PutAsJsonAsync($"{Subeler}/{sube}", await GovdeAsync(sube, program, surum));

    private async Task ProgramYazAsync(Guid sube, JsonObject program)
    {
        HttpResponseMessage yanit = await KaydetAsync(sube, program);
        Assert.True(yanit.StatusCode == HttpStatusCode.OK, $"{(int)yanit.StatusCode}: {await yanit.Content.ReadAsStringAsync()}");
    }

    private async Task<JsonElement> SubeAsync(Guid sube) =>
        JsonDocument.Parse(await yonetici.GetStringAsync($"{Subeler}/{sube}")).RootElement;

    private async Task<int> SurumAsync(Guid sube) => (await SubeAsync(sube)).GetProperty("version").GetInt32();

    private async Task<JsonElement> PublicSubeAsync(string ad) =>
        JsonDocument.Parse(await Istemci().GetStringAsync("/api/dealership/gallery?locale=tr")).RootElement
            .GetProperty("branches").EnumerateArray().Single(oge => oge.GetProperty("name").GetString() == ad);

    private async Task<List<JsonElement>> DepartmentlarAsync() =>
        [.. Assert.Single(new HtmlParser().ParseDocument(await Istemci().GetStringAsync("/tr/")).JsonLd()).GetProperty("department").EnumerateArray()];

    private async Task<JsonElement> DepartmentAsync(string ad) =>
        (await DepartmentlarAsync()).Single(oge => oge.GetProperty("name").GetString() == ad);

    private async Task<long> OutboxAsync()
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM dealership.outbox_messages";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage yanit, HttpStatusCode beklenen)
    {
        string metin = await yanit.Content.ReadAsStringAsync();
        Assert.True(yanit.StatusCode == beklenen, $"{(int)yanit.StatusCode}: {metin}");
        JsonElement govde = JsonDocument.Parse(metin).RootElement;

        // C8 — çevrilmiş metin yok: yalnız kod ve alan anahtarı.
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
