using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Leads.Application;
using CarGallery.Modules.Leads.Contracts.Notifications;
using CarGallery.Modules.Leads.Domain;
using CarGallery.Modules.Leads.Endpoints;
using CarGallery.Modules.Leads.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Leads;

/// <summary>
/// Modülün tek giriş noktası: DI kaydı ve endpoint kaydı. Host yalnızca bunu çağırır
/// (docs/conventions.md § 3).
/// </summary>
public static class LeadsModule
{
    public static IServiceCollection AddLeads(
        this IServiceCollection services,
        string connectionString,
        IReadOnlyList<string> enabledLocales,
        int capturedLeadLimit = LeadRateLimitOptions.DefaultCapturedLeadLimit,
        int requestLimit = LeadRateLimitOptions.DefaultRequestLimit,
        int rateLimitWindowMinutes = LeadRateLimitOptions.DefaultWindowMinutes)
    {
        services.AddDbContext<LeadsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", LeadsDbContext.Schema)));

        services.AddSingleton(BuildLocaleOptions(enabledLocales));
        services.AddSingleton<IClock, SystemClock>();

        // Şema kaydı diğer modüllerle AYNI desen: uygulayan kurulum adımıdır, çalışan
        // uygulama yalnız doğrular (docs/decisions/0002). Bu satır olmadan `leads`
        // şeması kurulum adımında hiç uygulanmaz ve eksikliği yalnız ilk talepte,
        // "tablo yok" diye görünürdü.
        // 0009 D1 — bildirim gövdesinin dar okuma yüzeyi.
        services.AddScoped<ILeadNotificationQueries, LeadNotificationQueries>();

        services.AddScoped<IModuleSchema>(provider =>
            new ModuleSchema<LeadsDbContext>(
                provider.GetRequiredService<LeadsDbContext>(),
                "Leads"));

        services.AddScoped<IOutboxWriter, OutboxWriter<LeadsDbContext>>();
        services.AddScoped<LeadsUnitOfWork>();
        services.AddScoped<IListingSnapshotSource, ListingSnapshotSource>();
        services.AddScoped<ICanonicalModelNameSource, CanonicalModelNameSource>();
        services.AddScoped<CaptureLead>();

        // Dağıtıcı modülün kendi kaydıdır: outbox tablosu bu modülün şemasındadır ve onu
        // okuyan da bu modülün DbContext'idir. Bugün abonesi yok (X3) — dağıtıcı yine de
        // ayakta, çünkü "dinleyen var, konuşan yok" durumunun tersi de sessizce birikir.
        services.AddScoped(provider => new OutboxDispatcher<LeadsDbContext>(
            provider.GetRequiredService<LeadsDbContext>(),
            provider,
            provider.GetRequiredService<IClock>(),
            new OutboxDispatchOptions(),
            provider.GetRequiredService<ILogger<OutboxDispatcher<LeadsDbContext>>>()));

        services.AddHostedService(provider => new OutboxDispatcherHostedService<LeadsDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxDispatcherHostedService<LeadsDbContext>>>()));

        LeadRateLimitOptions rateLimits = new()
        {
            CapturedLeadLimit = capturedLeadLimit,
            RequestLimit = requestLimit,
            WindowMinutes = rateLimitWindowMinutes,
        };

        // C17'nin sayacı KAYIT başına ilerler; tekil olmalıdır, çünkü pencere süreç
        // ömrü boyunca yaşar.
        services.AddSingleton<ILeadCaptureQuota>(provider => new LeadCaptureQuota(
            provider.GetRequiredService<IClock>(),
            rateLimits.CapturedLeadLimit,
            TimeSpan.FromMinutes(rateLimits.WindowMinutes)));

        AddRateLimiting(services, rateLimits);

        return services;
    }

    public static void MapLeads(this IEndpointRouteBuilder builder) => builder.MapLeadEndpoints();

    /// <summary>
    /// Boru hattındaki İSTEK sınırı — C17'nin talep sınırı DEĞİL.
    ///
    /// Ayrım şu: bu sınır permit'i handler'dan önce tüketir, yani geçersiz istekleri de
    /// sayar. C17'nin "10 dakikada 5 talep"i buraya konulsaydı, sunucu doğrulamasına
    /// düşen dürüst bir ziyaretçi beş denemede kilitlenir ve hiç talep bırakamazdı.
    /// Burası yalnız KABA TEKRARI keser (R16, Karar D4) ve bu yüzden eşiği gevşektir;
    /// kaydedilen talebin sayacı <see cref="ILeadCaptureQuota"/>'dadır.
    ///
    /// IP KİŞİSEL VERİDİR: yalnızca bellekteki sayaçta bölüm anahtarı olarak durur, kalıcı
    /// kayda ve loga YAZILMAZ (docs/conventions.md § 12).
    /// </summary>
    private static void AddRateLimiting(IServiceCollection services, LeadRateLimitOptions options)
    {
        services.AddRateLimiter(limiter => limiter.AddPolicy<string, LeadRateLimitPolicy>(
            LeadEndpoints.RateLimitPolicy));

        // Politika DI'dan ActivatorUtilities ile kurulur; bağımlılığı çözülebilir olmalı.
        services.AddSingleton(options);
    }

    /// <summary>
    /// Eksik veya geçersiz yapılandırmayla uygulama AYAĞA KALKMAZ, çalışma anında
    /// patlamaz (docs/conventions.md § 11).
    /// </summary>
    private static LeadLocaleOptions BuildLocaleOptions(IReadOnlyList<string> enabledLocales)
    {
        List<Locale> enabled = [];

        foreach (string code in enabledLocales)
        {
            Result<Locale> parsed = Locale.Create(code);

            if (parsed.IsFailure)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "Etkin locale listesinde geçersiz kod: '{0}'.",
                    code));
            }

            enabled.Add(parsed.Value);
        }

        if (enabled.Count == 0)
        {
            throw new InvalidOperationException("Etkin locale listesi boş olamaz.");
        }

        return new LeadLocaleOptions(enabled);
    }
}
