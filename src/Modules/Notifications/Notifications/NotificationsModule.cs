using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Notifications.Application;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Notifications;

/// <summary>
/// Modülün tek giriş noktası. **Endpoint kaydı yoktur**: `Notifications` bir yaprak
/// modüldür (C11), dışarıya yüzeyi yok — sistemden dışarı giden mesajların tek kapısı
/// olmak dışında.
/// </summary>
public static class NotificationsModule
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services,
        NotificationsSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

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

        // D4 — gönderici soyutlamanın arkasında. Bugün SMTP; sağlayıcıya geçiş bu tek
        // satırı değiştirir, kuyruk ve tekrar deneme hiç dokunulmadan kalır.
        services.AddScoped<INotificationTransport, SmtpNotificationTransport>();
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
}
