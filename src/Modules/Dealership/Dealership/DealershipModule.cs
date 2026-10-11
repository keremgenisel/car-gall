using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Branches;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Dealership.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CarGallery.Modules.Dealership.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Dealership;

/// <summary>
/// Modülün tek giriş noktası. **Endpoint kaydı yoktur**: D1 uyarınca bu spec'te yazma ucu
/// açılmaz ve okuma yüzeyi yalnız `Contracts` üzerinden tüketilir (D6).
/// </summary>
public static class DealershipModule
{
    public static IServiceCollection AddDealership(
        this IServiceCollection services,
        string connectionString,
        string defaultLocale)
    {
        services.AddDbContext<DealershipDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", DealershipDbContext.Schema)));

        Result<Locale> parsed = Locale.Create(defaultLocale);

        if (parsed.IsFailure)
        {
            throw new InvalidOperationException($"Varsayılan locale geçersiz: '{defaultLocale}'.");
        }

        services.AddScoped<IModuleSchema>(provider => new ModuleSchema<DealershipDbContext>(
            provider.GetRequiredService<DealershipDbContext>(),
            "Dealership"));

        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<OutboxWriter<DealershipDbContext>>();
        services.AddScoped<DealershipUnitOfWork>();

        // 0007 — invariant koruması ortak desenden gelir; süre yapılandırmadan değil
        // varsayılandan, çünkü kilit süresi bir iş kuralı değil bir güvenlik marjıdır.
        services.AddSingleton(InvariantGuardOptions.Default);
        services.AddScoped<BranchCommands>();
        services.AddScoped(provider => new OutboxDispatcher<DealershipDbContext>(
            provider.GetRequiredService<DealershipDbContext>(),
            provider,
            provider.GetRequiredService<IClock>(),
            new OutboxDispatchOptions(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OutboxDispatcher<DealershipDbContext>>>()));

        // Arka plan dağıtıcısı modülün kendi kaydıdır: outbox tablosu bu modülün
        // şemasındadır ve onu okuyan da bu modülün DbContext'idir (D9).
        services.AddHostedService(provider => new OutboxDispatcherHostedService<DealershipDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxDispatcherHostedService<DealershipDbContext>>>()));

        services.AddScoped<IBranchQueries>(provider => new BranchQueries(
            provider.GetRequiredService<DealershipDbContext>(),
            parsed.Value));

        // 0009 D2 — aynı sınıf, ayrı ve dar bir yüzey (D7).
        services.AddScoped<IBranchContactQueries>(provider => new BranchQueries(
            provider.GetRequiredService<DealershipDbContext>(),
            parsed.Value));

        return services;
    }

    /// <summary>
    /// 0010a — modülün İLK uç kaydı. `0003` bilinçli olarak hiç uç açmamıştı; araç kaydı
    /// formu şube seçimi istediği için açıldı.
    /// </summary>
    public static void MapDealership(this IEndpointRouteBuilder builder) => builder.MapBranchEndpoints();
}
