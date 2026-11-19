using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Application.Newsletter;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Endpoints;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications;

/// <summary>
/// Modülün tek giriş noktası: DI kaydı ve endpoint kaydı.
///
/// <para>`Notifications` bir yaprak modüldür (`0009` C11, `0018` C12): hiçbir MODÜL ona
/// senkron bağlanmaz, o da olay yayınlamaz. `0018` ile ilk public ucunu (bülten aboneliği)
/// ve ilk zamanlı işini (haftalık dönem) aldı (**D20**); ikisi de ziyaretçiye ya da saate
/// açılır, başka bir modüle değil.</para>
/// </summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services,
        NotificationsSetup setup,
        NewsletterSetup newsletter)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(newsletter);

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(
                setup.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__ef_migrations_history",
                    NotificationsDbContext.Schema)));

        // ADR 0002 — migration'ı çalışan uygulama değil kurulum adımı uygular ve modüller
        // Host'ta elle sayılmaz. Bu kayıt olmadan `notifications` şeması hiç oluşmaz ve
        // eksiklik yalnız ilk sorguda görünürdü; 0006 denetiminde `dealership` şemasıyla
        // tam olarak bu yaşandı.
        services.AddScoped<IModuleSchema>(provider => new ModuleSchema<NotificationsDbContext>(
            provider.GetRequiredService<NotificationsDbContext>(),
            "Notifications"));

        services.AddSingleton<IClock, SystemClock>();

        // C9/AC-27 — yapılandırma KURULUŞTA doğrulanır. Eksikse burada patlar ve uygulama
        // hiç ayağa kalkmaz; çalışma anında patlamaz. Bildirim adresi yapılandırılmamışsa
        // her talep sessizce kaybolur — bu spec'in varlık nedeni tam olarak o durum.
        Result<NotificationsOptions> options = NotificationsOptions.Create(setup);

        if (options.IsFailure)
        {
            throw new InvalidOperationException(
                $"Notifications yapılandırması eksik veya geçersiz: {options.Error.Code}");
        }

        services.AddSingleton(options.Value);
        services.AddScoped<TemplateResolver>();

        // `0018` C14/AC-97 — bülten yapılandırması da KURULUŞTA doğrulanır; imza sırrı,
        // dönem anı, bağlantı süresi ya da üç sınırdan biri eksikse uygulama ayağa kalkmaz.
        Result<NewsletterOptions> newsletterOptions = NewsletterOptions.Create(newsletter);

        if (newsletterOptions.IsFailure)
        {
            throw new InvalidOperationException(
                $"Notifications bülten yapılandırması eksik veya geçersiz: {newsletterOptions.Error.Code}");
        }

        services.AddSingleton(newsletterOptions.Value);
        services.AddSingleton<UnsubscribeTokens>();
        services.AddSingleton<ConfirmationTokens>();

        // D9 — geçerli istek bütçesi süreç ömrü boyunca yaşar; tekil olmalı.
        services.AddSingleton<INewsletterRequestQuota, NewsletterRequestQuota>();
        // 0007 R3 — satır kilidi ortak desenden; süresi modülün kendi seçeneği (0012c D57).
        services.AddSingleton(InvariantGuardOptions<NotificationsDbContext>.Default);
        services.AddScoped<SubscribeToNewsletter>();
        services.AddScoped<ConfirmSubscription>();
        services.AddScoped<UnsubscribeFromNewsletter>();

        // D13 — haftalık dönem: doğrudan çağrılabilen işleyici + onu tetikleyen zamanlı servis.
        services.AddScoped<NewsletterPeriodProcessor>();
        services.AddHostedService(provider => new NewsletterPeriodHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<NewsletterPeriodHostedService>>()));

        // D4 — `VehiclePublished`'ın alıcısı. Dağıtıcı alıcıları DI'dan çözer: bu kayıt
        // olmadan işleyici yazılmış ama KABLOLANMAMIŞ kalır ve `VehicleCatalog`'un dağıtıcısı
        // alıcısı olmayan duyuruyu sessizce işlenmiş sayar (`0003`'ün kaçan hatası).
        services.AddScoped<IIntegrationEventDispatchTarget, VehiclePublishedCandidateHandler>();

        // D9 — kaba istek sınırı boru hattında; politika DI'dan kurulur.
        services.AddRateLimiter(limiter => limiter.AddPolicy<string, NewsletterRateLimitPolicy>(
            NewsletterEndpoints.RateLimitPolicy));

        // D4 — gönderici soyutlamanın arkasında. Bugün SMTP; sağlayıcıya geçiş bu tek
        // satırı değiştirir, kuyruk ve tekrar deneme hiç dokunulmadan kalır.
        services.AddScoped<INotificationTransport, SmtpNotificationTransport>();
        services.AddScoped<NewsletterMessageComposer>();
        services.AddScoped<NotificationDispatcher>();

        // R10/AC-14 — kuyruğu işleyen arka plan servisi. Kayıt veritabanında durduğu için
        // yeniden başlatma bekleyen gönderimleri kaybetmez.
        services.AddHostedService(provider => new NotificationQueueHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<NotificationQueueHostedService>>()));

        // Olay dağıtıcısı hedefleri DI'dan çözüyor; `Leads`'in dağıtıcısı bu kaydı bulur.
        services.AddScoped<IIntegrationEventDispatchTarget, LeadCapturedNotificationHandler>();

        return services;
    }

    /// <summary>`0018` — bülten aboneliğinin üç ucu; Host yalnız bunu çağırır.</summary>
    public static void MapNotifications(this IEndpointRouteBuilder builder) => builder.MapNewsletterEndpoints();
}
