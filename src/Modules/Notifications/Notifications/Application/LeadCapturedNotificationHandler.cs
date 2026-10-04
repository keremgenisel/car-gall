using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Leads.Contracts.Events;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Notifications.Domain;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications.Application;

/// <summary>
/// 0009'un tek tükettiği olay (X4): <c>LeadCaptured</c>.
///
/// <para><b>Bu işleyici GÖNDERMEZ, yalnız KUYRUĞA YAZAR</b> (C10, AC-13). Gönderim arka
/// plan servisinin işi (R10). Ayrım bilinçli: işleyici, olayın outbox dağıtıcısı
/// tarafından teslim edildiği an çalışır ve o dağıtıcı beşinci denemeyi yapacak olan
/// değildir. Tekrar deneme bir kuyruk olmadan karşılanamaz.</para>
///
/// <para><b>İdempotentlik taban sınıfta</b> (C3, AC-19): iş ile inbox kaydı aynı
/// <c>SaveChanges</c> içinde kalıcı olur. Aynı olay iki kez gelse ikinci gelişte ikinci
/// bir kayıt oluşmaz. Vaat edilen budur — tam-bir-kez TESLİM değil (C10).</para>
///
/// <para><b>İki mesaj bağımsızdır</b> (R7, AC-12). Biri kuyruğa giremezse diğeri yine
/// girer; gönderim sırasında da birinin başarısızlığı diğerini engellemez.</para>
/// </summary>
internal sealed class LeadCapturedNotificationHandler(
    NotificationsDbContext context,
    IClock clock,
    ILeadNotificationQueries leads,
    NotificationsOptions options,
    ILogger<LeadCapturedNotificationHandler> logger)
    : IdempotentIntegrationEventHandler<LeadCaptured, NotificationsDbContext>(context, clock)
{
    protected override async Task HandleCoreAsync(
        LeadCaptured integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // Kişisel veri olaydan değil KAYNAKTAN okunur (D1). Talep yoksa — silinmiş veya
        // anonimleştirilmiş — gönderilecek bir mesaj da yoktur.
        LeadNotificationDto? talep = await leads.FindAsync(integrationEvent.LeadId, cancellationToken);

        if (talep is null)
        {
            logger.LogWarning(
                "Talep bulunamadı; bildirim kuyruğa alınmadı: {LeadId}",
                integrationEvent.LeadId);

            return;
        }

        DateTimeOffset now = Clock.UtcNow;

        // Galeri bildirimi KURULUM VARSAYILAN DİLİNDE gider (R5, AC-6): alıcı bir adrestir,
        // bir kişi değil — `PreferredLocale` yoktur. Atama geldiği gün danışmanın diline
        // geçilir (X3).
        Context.Messages.Add(NotificationMessage.Queue(
            talep.LeadId,
            TemplateKeys.LeadReceivedGallery,
            options.DefaultLocale,
            NotificationChannel.Email,
            now));

        // Ziyaretçi karşılığı talebin BIRAKILDIĞI dilde gider (L1, R3, AC-8).
        //
        // E-posta yoksa gönderim denenmez ama kayıt DURUR (R6, AC-11): 0005 R4 telefonu
        // tek iletişim kanalı olarak kabul ediyor ve bu bir hata değil eksik kanaldır.
        // Kaydı tutulmazsa galeri "neden ulaşılmadı" sorusunu hiçbir yerden cevaplayamaz.
        Context.Messages.Add(string.IsNullOrWhiteSpace(talep.Email)
            ? NotificationMessage.NoChannel(
                talep.LeadId,
                TemplateKeys.LeadReceivedVisitor,
                talep.SubmittedLocale,
                NotificationChannel.Email,
                now)
            : NotificationMessage.Queue(
                talep.LeadId,
                TemplateKeys.LeadReceivedVisitor,
                talep.SubmittedLocale,
                NotificationChannel.Email,
                now));
    }
}
