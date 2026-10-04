using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Notifications.Contracts.Setup;
using CarGallery.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton(setup);

        return services;
    }
}
