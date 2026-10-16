using System.Globalization;
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
/// Duyurunun YAZILDIĞI yarı — `0006`/AC-48 ve `0003`/AC-25'in birleştirilmiş uygulamadaki
/// karşılığı: *"değişiklik kaydedildiyse duyurusu da kaydedilmiştir."*
///
/// <para><b>Bu testin yokluğu bir kaçan hatanın bedeliydi.</b> `SubeKapatmaZinciriTests`
/// zincirin dağıtıcı→alıcı yarısını kanıtlıyor ama outbox satırını <b>ham SQL ile</b>
/// kendisi yazıyor (`INSERT INTO dealership.outbox_messages`), yani uygulamanın yazma
/// yolunu hiç koşmuyor. Modül testlerinde de tuzak görünmez: orada tek modül kayıtlıdır
/// ve `IOutboxWriter` doğru `DbContext`'e çözülür. Kusur yalnız <b>birleştirilmiş
/// Host</b>'ta vardı — üç modül paylaşılan arayüzü kendi `DbContext`'iyle kaydediyordu,
/// DI'da son kayıt kazanıyordu ve `Identity`'nin duyurusu `Leads`'in izleyicisine yazılıp
/// hiç kaydedilmiyordu.</para>
///
/// <para>Gözlem noktası bilinçli olarak <b>tablo</b>: "yazıcı doğru tipte mi" sorusunu
/// sormak, aynı kusuru başka bir yazımla geri getirmeye izin verirdi. Sorulan şey
/// duyurunun KENDİ modülünün tablosunda durduğu.</para>
/// </summary>
public sealed class DuyuruYazimiZinciriTests : IAsyncLifetime
{
    private const string CalisanParola = "calisan-parolasi-1";

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

    [Fact(DisplayName = "0006/AC-48: Personel pasifleştirildiğinde duyurusu `identity` outbox'ına yazılır — birleştirilmiş Host'ta, başka modülün tablosuna değil")]
    public async Task Yazilir_Duyuru_KendiModulununOutboxina()
    {
        HttpClient client = await GirisYapmisIstemciAsync();

        HttpResponseMessage eklendi = await client.PostAsJsonAsync($"{AdminSurface.RoutePrefix}/staff", new
        {
            displayName = "Sonda Personeli",
            loginId = "duyuru-sondasi@ornek.com",
            initialPassword = "ilk-parola-1234",
            role = "sales_advisor",
            preferredLocale = "tr",
        });

        Assert.Equal(HttpStatusCode.Created, eklendi.StatusCode);

        Guid personelId = JsonDocument.Parse(await eklendi.Content.ReadAsStringAsync())
            .RootElement.GetProperty("staffId").GetGuid();

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await client.PostAsJsonAsync(
                $"{AdminSurface.RoutePrefix}/staff/{personelId}/deactivate",
                new { })).StatusCode);

        // İŞ VERİSİ kaydedildi: bu satır olmadan aşağıdaki iddia "hiçbir şey olmadı" ile
        // karışırdı. Kusurlu hâlde personel GERÇEKTEN pasifleşiyordu; kaybolan şey yalnız
        // duyuruydu.
        Assert.Equal(1, await SayAsync("select count(*) from identity.staff where normalized_login_id = 'duyuru-sondasi@ornek.com' and status = 2"));

        Assert.Equal(1, await DuyuruSayisiAsync("identity"));

        // Ve BAŞKA modülün tablosunda durmuyor: kusurun ilk hâlinde satır `leads`'in
        // izleyicisine yazılıyordu.
        foreach (string sema in Semalar)
        {
            Assert.Equal(0, await DuyuruSayisiAsync(sema));
        }
    }

    /// <summary>`identity` dışındaki, outbox tablosu olan modüller.</summary>
    private static readonly string[] Semalar = ["leads", "dealership", "media"];

    private async Task<int> DuyuruSayisiAsync(string sema) => await SayAsync(
        $"select count(*) from {sema}.outbox_messages where type like '%StaffDeactivated%'");

    private async Task<int> SayAsync(string sql)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = new(sql, connection);

        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
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
