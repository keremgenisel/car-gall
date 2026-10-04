using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using CarGallery.Modules.VehicleCatalog;
using CarGallery.Modules.VehicleReference;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarGallery.Host.Tests;

/// <summary>
/// Zincirin GERÇEK boru hattında koştuğu tek yer. Modülün kendi testleri işleyicinin
/// doğru çalıştığını gösterir; bu test onun <b>kablolanmış</b> olduğunu gösterir.
///
/// <para>0003'ün dersi tam olarak buydu ve 0006'da bir kez daha ödendi: test edilmiş bir
/// alıcı, yayıncısı bulunamıyorsa hiçbir şey kanıtlamaz. <c>AddNotifications</c> satırı
/// silinse veya <c>notifications</c> şeması hiç uygulanmasa modülün 33 testi yeşil
/// kalırdı — ve tek bir bildirim bile gitmezdi.</para>
///
/// <para>Burada <b>gerçek bir SMTP sunucusu yok ve olmaması kasıtlı</b>: sınanan şey
/// sağlayıcı erişilemezken ne olduğudur (AC-1).</para>
/// </summary>
public sealed class TalepBildirimiZinciriTests : IAsyncLifetime
{
    private static readonly Guid SubeId = Guid.Parse("00000000-0000-7000-8000-00000000c001");
    private static readonly Guid ModelId = Guid.Parse("0192c000-0000-7000-8000-00000000000a");

    private const string Slug = "toyota-corolla-2019-bbb222";

    /// <summary>Kapalı port: sağlayıcı ERİŞİLEMEZ. AC-1'in koşulu budur.</summary>
    private const string KapaliPort = "1";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("cargallery")
        .Build();

    private WebApplicationFactory<Program>? factory;

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
            builder.UseSetting("Notifications:Smtp:Port", KapaliPort);
        });

        await factory.Services.SeedVehicleReferenceForDevelopmentAsync(ModelId);
        await factory.Services.SeedVehicleCatalogForDevelopmentAsync(0, ModelId);

        await SeedShowcaseAsync();
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
    /// Bu bugün de doğrudur — <c>LeadCaptured</c> outbox'tan ASENKRON dağıtılıyor ve
    /// bildirim talep işleminin içinde hiç yer almıyor. Kriter yeni davranışın kanıtı
    /// değil, korumanın kaybolmadığının kanıtıdır: bildirim gönderimi bir gün talep
    /// işlemine senkron bağlanırsa bu test kırmızıya döner.
    /// </summary>
    [Fact(DisplayName = "0009/AC-1: Sağlayıcı erişilemezken talep kaydı etkilenmez; talep oluşturma başarılı döner")]
    public async Task Etkilenmez_TalepKaydi_SaglayiciErisilemezken()
    {
        HttpClient client = factory!.CreateClient();

        HttpResponseMessage yanit = await GonderAsync(client, "zincir-1");

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        Assert.Equal(1, await SayAsync("SELECT COUNT(*) FROM leads.leads"));
    }

    /// <summary>
    /// Kablolama testi: <c>AddNotifications</c> kaydı, <c>notifications</c> şeması ve
    /// <c>Leads</c> dağıtıcısının işleyiciyi bulması — üçü birden. Biri eksikse satır hiç
    /// oluşmaz.
    /// </summary>
    [Fact]
    public async Task Olusur_IkiBildirimKaydi_TalepSonrasi()
    {
        HttpClient client = factory!.CreateClient();

        Assert.Equal(HttpStatusCode.Created, (await GonderAsync(client, "zincir-2")).StatusCode);

        // Outbox dağıtıcısı beş saniyelik turlarla çalışıyor; sabit bir bekleme bu testi
        // makinenin hızına göre kırılgan yapardı (AP-03).
        int kayit = await BekleAsync("SELECT COUNT(*) FROM notifications.messages", beklenen: 2);

        Assert.Equal(2, kayit);
    }

    private async Task<int> BekleAsync(string sorgu, int beklenen)
    {
        for (int deneme = 0; deneme < 60; deneme++)
        {
            int sonuc = await SayAsync(sorgu);

            if (sonuc >= beklenen)
            {
                return sonuc;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return await SayAsync(sorgu);
    }

    private static async Task<HttpResponseMessage> GonderAsync(HttpClient client, string anahtar)
    {
        HttpRequestMessage istek = new(HttpMethod.Post, "/api/leads")
        {
            Content = JsonContent.Create(new
            {
                listingSlug = Slug,
                leadType = "information_request",
                locale = "tr",
                fullName = "Ayşe Yılmaz",
                phone = "+905321112233",
                email = "ziyaretci@ornek.com",
                message = (string?)null,
                consentGiven = true,
            }),
        };

        istek.Headers.Add("Idempotency-Key", anahtar);

        return await client.SendAsync(istek);
    }

    private async Task<int> SayAsync(string sorgu)
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = sorgu;

        return Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
    }

    private async Task SeedShowcaseAsync()
    {
        await using NpgsqlConnection connection = new(container.GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO vehicle_catalog.showcase_listings (
                listing_id, vehicle_id, is_reserved, showcase_sort_weight, published_at, slug,
                body_type_slug, fuel_type_slug, transmission_type_slug, drivetrain_type_slug,
                trim_level_slug, color_names, vehicle_model_id, model_year, mileage_kilometers,
                price_amount, price_currency_code, price_minor_unit_exponent, price_on_request,
                branch_id, branch_names, cover_asset_id, cover_media_url)
            VALUES (
                gen_random_uuid(), gen_random_uuid(), false, 0, now(), @slug,
                'sedan', 'diesel', 'automatic', 'front-wheel-drive',
                'comfort', '{"tr":"Beyaz"}'::jsonb, @modelId, 2019, 125000,
                1250000, 'TRY', 2, false,
                @branchId, '{"tr":"Merkez Şube"}'::jsonb, NULL, NULL)
            """;

        command.Parameters.AddWithValue("slug", Slug);
        command.Parameters.AddWithValue("modelId", ModelId);
        command.Parameters.AddWithValue("branchId", SubeId);

        await command.ExecuteNonQueryAsync();
    }
}
