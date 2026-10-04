using System.Net.Http.Json;
using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Leads.Contracts.Events;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// D6 — kuyruk testlerinin ezici çoğunluğu sahte taşıyıcıyla koşar; <b>bu tek test</b>
/// gerçek bir SMTP sunucusuna (Mailpit) karşı koşar.
///
/// <para><b>Neden gerekli:</b> sahte taşıyıcı, taşıyıcının SMTP'yi doğru konuştuğunu
/// kanıtlayamaz. Yanlış kurulan bir <c>MimeMessage</c>, eksik bir <c>From</c> başlığı veya
/// hiç çağrılmayan bir <c>SendAsync</c> sahte taşıyıcıyla sonsuza kadar yeşil kalır. Bu
/// test, mesajın gerçekten bir sunucuya TESLİM edildiğini sunucunun kendi kaydından okur.</para>
///
/// <para>Olaydan başlar, kuyruğu atlamaz: akışın tamamı — <c>LeadCaptured</c> → kuyruk →
/// gönderici → gerçek SMTP — tek bir testte koşar. Yalnız HTTP ucu dışarıda kalır; onu
/// <c>Host.Tests</c> kapsıyor (AC-1).</para>
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class GercekSmtpTests(NotificationsDatabase database) : IAsyncLifetime
{
    private static readonly DateTimeOffset An = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid TalepId = Guid.Parse("0192c000-0000-7000-8000-00000000d001");
    private static readonly Guid SubeId = Guid.Parse("0192c000-0000-7000-8000-00000000b001");
    private static readonly Guid IlanId = Guid.Parse("0192c000-0000-7000-8000-00000000c001");

    /// <summary>
    /// Mailpit kimlik doğrulamasını varsayılan olarak hiç duyurmaz; taşıyıcı ise
    /// <b>her zaman</b> kimlik doğrular (AC-28, kimliksiz relay bilinçli olarak
    /// desteklenmez). İki env değişkeni sunucuyu herhangi bir kimlikle konuşur hâle getirir.
    /// </summary>
    private readonly IContainer mailpit = new ContainerBuilder("axllent/mailpit:v1.21")
        .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "true")
        .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "true")
        .WithPortBinding(1025, assignRandomHostPort: true)
        .WithPortBinding(8025, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request.ForPath("/readyz").ForPort(8025)))
        .Build();

    public Task InitializeAsync() => mailpit.StartAsync();

    public Task DisposeAsync() => mailpit.DisposeAsync().AsTask();

    [Fact(DisplayName = "0009/AC-2: Bir talep oluşturulduğunda galeri tarafına bir e-posta gönderilir")]
    public async Task Gonderilir_GercekSmtpSunucusuna()
    {
        await database.ResetMessagesAsync();

        NotificationsOptions secenekler = Secenekler();

        await KuyrugaAsync(secenekler);
        await GonderAsync(secenekler);

        // Kayıt tarafı: mesaj gönderildi işaretlendi.
        NotificationMessage galeri = await GaleriMesajiAsync();
        Assert.Equal(NotificationStatus.Sent, galeri.Status);

        // Sunucu tarafı: SUNUCUNUN KENDİ kaydında mesaj var. Asıl kanıt burasıdır —
        // yukarıdaki satır sahte taşıyıcıyla da yeşil kalırdı.
        JsonElement teslim = await TeslimAsync("galeri@ornek.com");

        Assert.Equal("bildirim@ornek.com", teslim.GetProperty("From").GetProperty("Address").GetString());

        string govde = await GovdeAsync(teslim.GetProperty("ID").GetString()!);

        Assert.Contains("Toyota Corolla", govde, StringComparison.Ordinal);
        Assert.Contains("https://ornek.com/tr/cars/toyota-corolla-2019-abc123", govde, StringComparison.Ordinal);
    }

    /// <summary>Olaydan kuyruğa: gerçek işleyici, gerçek veritabanı.</summary>
    private async Task KuyrugaAsync(NotificationsOptions secenekler)
    {
        await using NotificationsDbContext context = database.CreateContext();

        LeadCapturedNotificationHandler isleyici = new(
            context,
            new SabitSaat(An),
            new SahteTalepSorgusu(),
            secenekler,
            NullLogger<LeadCapturedNotificationHandler>.Instance);

        await isleyici.HandleAsync(
            new LeadCaptured(
                Guid.Parse("0192c000-0000-7000-8000-00000000e001"),
                An,
                TalepId,
                SubeId,
                IlanId,
                "InformationRequest",
                "tr"),
            CancellationToken.None);
    }

    /// <summary>
    /// Kuyruktan sunucuya: <b>gerçek</b> <see cref="SmtpNotificationTransport"/>. Bu testin
    /// tek sahtelediği şey modül sınırının ötesindeki üç sorgudur (docs/testing.md § 1).
    /// </summary>
    private async Task GonderAsync(NotificationsOptions secenekler)
    {
        await using NotificationsDbContext context = database.CreateContext();

        NotificationDispatcher gonderici = new(
            context,
            new SmtpNotificationTransport(secenekler, NullLogger<SmtpNotificationTransport>.Instance),
            new TemplateResolver(context, NullLogger<TemplateResolver>.Instance),
            new SahteTalepSorgusu(),
            new SahteSubeSorgusu(),
            new SahteSlugSorgusu(),
            secenekler,
            new SabitSaat(An),
            NullLogger<NotificationDispatcher>.Instance);

        await gonderici.DispatchDueAsync(CancellationToken.None);
    }

    /// <summary>
    /// Mailpit mesajı kendi deposuna yazarken SMTP oturumu zaten kapanmış olabilir; kısa
    /// bir bekleme penceresi olmadan bu test makinenin hızına göre kırılgan olurdu (AP-03).
    /// </summary>
    private async Task<JsonElement> TeslimAsync(string alici)
    {
        using HttpClient http = new() { BaseAddress = ApiTabani };

        for (int deneme = 0; deneme < 25; deneme++)
        {
            JsonElement cevap = await http.GetFromJsonAsync<JsonElement>("/api/v1/messages");

            foreach (JsonElement mesaj in cevap.GetProperty("messages").EnumerateArray())
            {
                bool eslesti = mesaj.GetProperty("To").EnumerateArray().Any(hedef =>
                    hedef.GetProperty("Address").GetString() == alici);

                if (eslesti)
                {
                    return mesaj;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        Assert.Fail($"Mailpit'e {alici} adresine giden bir mesaj ulaşmadı.");

        return default;
    }

    private async Task<string> GovdeAsync(string mesajId)
    {
        using HttpClient http = new() { BaseAddress = ApiTabani };

        JsonElement mesaj = await http.GetFromJsonAsync<JsonElement>($"/api/v1/message/{mesajId}");

        return mesaj.GetProperty("Text").GetString() ?? string.Empty;
    }

    private Uri ApiTabani => new($"http://{mailpit.Hostname}:{mailpit.GetMappedPublicPort(8025)}");

    private NotificationsOptions Secenekler() => NotificationsOptions.Create(new NotificationsSetup(
        ConnectionString: database.ConnectionString,
        DefaultLocale: "tr",
        SetupNotificationAddress: "galeri@ornek.com",
        PublicBaseUrl: "https://ornek.com",
        SenderAddress: "bildirim@ornek.com",
        SmtpHost: mailpit.Hostname,
        SmtpPort: mailpit.GetMappedPublicPort(1025),
        SmtpUser: "kullanici",
        SmtpPassword: "sir",
        SmtpUseStartTls: false,
        MaximumAttempts: 3,
        RetryBaseSeconds: 10,
        SendTimeoutSeconds: 30)).Value;

    private async Task<NotificationMessage> GaleriMesajiAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        return await context.Messages
            .AsNoTracking()
            .SingleAsync(message => message.TemplateKey == TemplateKeys.LeadReceivedGallery);
    }

    private sealed class SahteTalepSorgusu : ILeadNotificationQueries
    {
        public Task<LeadNotificationDto?> FindAsync(Guid leadId, CancellationToken cancellationToken) =>
            Task.FromResult<LeadNotificationDto?>(new LeadNotificationDto(
                TalepId,
                "ziyaretci@ornek.com",
                "tr",
                "Toyota Corolla",
                SubeId,
                IlanId));
    }

    /// <summary>Şubenin e-postası YOK: galeri bildirimi kurulum adresine gider (AC-5).</summary>
    private sealed class SahteSubeSorgusu : IBranchContactQueries
    {
        public Task<BranchNotificationContactDto?> FindContactAsync(
            Guid branchId,
            string locale,
            CancellationToken cancellationToken) =>
            Task.FromResult<BranchNotificationContactDto?>(
                new BranchNotificationContactDto(branchId, "Kadıköy Şubesi", null));
    }

    private sealed class SahteSlugSorgusu : IListingSlugQueries
    {
        public Task<string?> FindSlugAsync(Guid listingId, CancellationToken cancellationToken) =>
            Task.FromResult<string?>("toyota-corolla-2019-abc123");
    }

    private sealed class SabitSaat(DateTimeOffset an) : IClock
    {
        public DateTimeOffset UtcNow { get; } = an;
    }
}
