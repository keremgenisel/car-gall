using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarGallery.Modules.Notifications.Tests.Integration;

/// <summary>
/// AC-24 — gönderim geçmişi alıcının adını, e-posta adresini veya telefonunu HİÇ tutmaz.
///
/// <para><b>Alıcı adresi gönderim ANINDA kaynaktan çözülür</b> (C5) ve kayda hiç yazılmaz.
/// Gerekçe silinme talebidir: geçmişte duran bir e-posta adresi, talep anonimleştirildikten
/// sonra da orada kalır ve "kişisel veri nerede" sorusunun cevabı iki yere bölünür.</para>
///
/// <para>Test ŞEMAYA bakıyor, koda değil: bir alanı sonradan ekleyen kişi testi değil
/// şemayı değiştirir ve kapı tam orada kapanmalı. İkinci yarı davranışı ölçüyor — gerçek
/// bir gönderimden sonra satırın HİÇBİR değerinde alıcının adresi geçmiyor.</para>
/// </summary>
[Collection(NotificationsDatabaseCollection.Name)]
public sealed class GizlilikTests(NotificationsDatabase database)
{
    private static readonly DateTimeOffset An = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid TalepId = Guid.Parse("0192c000-0000-7000-8000-00000000d001");
    private static readonly Guid SubeId = Guid.Parse("0192c000-0000-7000-8000-00000000b001");
    private static readonly Guid IlanId = Guid.Parse("0192c000-0000-7000-8000-00000000c001");

    private const string ZiyaretciAdresi = "ziyaretci@ornek.com";

    [Fact(DisplayName = "0009/AC-24: Gönderim geçmişi alıcının adını, e-posta adresini veya telefonunu hiç tutmaz")]
    public async Task TutmazHic_AdAdresVeTelefon()
    {
        await using NotificationsDbContext context = database.CreateContext();

        List<string> kolonlar = await context.Database
            .SqlQuery<string>($"""
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'notifications' AND table_name = 'messages'
                """)
            .ToListAsync();

        Assert.NotEmpty(kolonlar);

        // Şema yarısı: böyle bir alan HİÇ YOK. Boş bırakılan bir alan, bir gün dolduğunda
        // kimsenin fark etmediği bir gizlilik ihlali olurdu.
        foreach (string yasak in new[] { "name", "email", "phone", "address", "recipient" })
        {
            Assert.DoesNotContain(
                kolonlar,
                kolon => kolon.Contains(yasak, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact(DisplayName = "0009/AC-24: Gönderim geçmişi alıcının adını, e-posta adresini veya telefonunu hiç tutmaz")]
    public async Task TutmazHic_GonderimdenSonraDa()
    {
        await database.ResetMessagesAsync();
        await KuyrugaAsync();
        await IsleAsync();

        await using NotificationsDbContext context = database.CreateContext();

        // Satırın TAMAMI metne çevrilip aranıyor: tek tek alanlara bakmak, yarın eklenen
        // bir alanı atlardı. Mesaj gerçekten gönderildi — yani adres çözüldü, kullanıldı
        // ve hiçbir yere yazılmadı.
        List<string> satirlar = await context.Database
            .SqlQuery<string>($"""SELECT messages::text AS "Value" FROM notifications.messages""")
            .ToListAsync();

        Assert.NotEmpty(satirlar);
        Assert.All(satirlar, satir =>
            Assert.DoesNotContain(ZiyaretciAdresi, satir, StringComparison.OrdinalIgnoreCase));
        Assert.All(satirlar, satir =>
            Assert.DoesNotContain("Ayşe", satir, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            NotificationStatus.Sent,
            await context.Messages.AsNoTracking().Select(message => message.Status).SingleAsync());
    }

    private async Task KuyrugaAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        context.Messages.Add(NotificationMessage.Queue(
            TalepId, TemplateKeys.LeadReceivedVisitor, "tr", NotificationChannel.Email, An));

        await context.SaveChangesAsync();
    }

    private async Task IsleAsync()
    {
        await using NotificationsDbContext context = database.CreateContext();

        NotificationDispatcher dispatcher = new(
            context,
            new SessizTasiyici(),
            new TemplateResolver(context, NullLogger<TemplateResolver>.Instance),
            new SahteTalepSorgusu(),
            new SahteSubeSorgusu(),
            new SahteSlugSorgusu(),
            Secenekler(),
            new SabitSaat(An),
            NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.DispatchDueAsync(CancellationToken.None);
    }

    private static NotificationsOptions Secenekler() => NotificationsOptions.Create(new NotificationsSetup(
        ConnectionString: "yok",
        DefaultLocale: "tr",
        SetupNotificationAddress: "galeri@ornek.com",
        PublicBaseUrl: "https://ornek.com",
        SenderAddress: "bildirim@ornek.com",
        SmtpHost: "localhost",
        SmtpPort: 25,
        SmtpUser: "kullanici",
        SmtpPassword: "sir",
        SmtpUseStartTls: false,
        MaximumAttempts: 3,
        RetryBaseSeconds: 10,
        SendTimeoutSeconds: 10)).Value;

    private sealed class SessizTasiyici : INotificationTransport
    {
        public Task<SendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken) =>
            Task.FromResult(SendResult.Success());
    }

    /// <summary>Ad da adres de KAYNAKTA var; testin sorusu onların kayda SIZIP SIZMADIĞI.</summary>
    private sealed class SahteTalepSorgusu : ILeadNotificationQueries
    {
        public Task<LeadNotificationDto?> FindAsync(Guid leadId, CancellationToken cancellationToken) =>
            Task.FromResult<LeadNotificationDto?>(new LeadNotificationDto(
                TalepId, ZiyaretciAdresi, "tr", "Toyota Corolla", SubeId, IlanId));
    }

    private sealed class SahteSubeSorgusu : IBranchContactQueries
    {
        public Task<BranchNotificationContactDto?> FindContactAsync(
            Guid branchId,
            string locale,
            CancellationToken cancellationToken) =>
            Task.FromResult<BranchNotificationContactDto?>(
                new BranchNotificationContactDto(branchId, "Kadıköy Şubesi", "kadikoy@ornek.com"));
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
