using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Leads.Application;
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
        int rateLimitPermitLimit = LeadRateLimitOptions.DefaultPermitLimit,
        int rateLimitWindowMinutes = LeadRateLimitOptions.DefaultWindowMinutes)
    {
        services.AddDbContext<LeadsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", LeadsDbContext.Schema)));

        services.AddSingleton(BuildLocaleOptions(enabledLocales));
        services.AddSingleton<IClock, SystemClock>();

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

        AddRateLimiting(services, new LeadRateLimitOptions
        {
            PermitLimit = rateLimitPermitLimit,
            WindowMinutes = rateLimitWindowMinutes,
        });

        return services;
    }

    public static void MapLeads(this IEndpointRouteBuilder builder) => builder.MapLeadEndpoints();

    /// <summary>
    /// Şemayı uygular. Üretimde migration'ın nasıl uygulanacağı henüz kararlaştırılmamış
    /// bir konudur (0003'ten devrediyor); bu çağrı bugün geliştirme ve kanıt üretimi
    /// yolundan çağrılır.
    /// </summary>
    public static async Task MigrateLeadsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        LeadsDbContext context = scope.ServiceProvider.GetRequiredService<LeadsDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// C17 — aynı IP'den 10 dakikada en çok 5 talep; altıncı istek reddedilir.
    ///
    /// IP KİŞİSEL VERİDİR: yalnızca bellekteki sayaçta bölüm anahtarı olarak durur, kalıcı
    /// kayda ve loga YAZILMAZ (docs/conventions.md § 12). `X-Forwarded-For` okunmaz —
    /// güvenilmeyen bir başlığa göre bölmek, sınırı istemcinin kapatabilmesi demektir.
    ///
    /// Sınır, handler'dan ÖNCE reddeder: reddedilen istek için kayıt oluşmaz (AC-32).
    /// </summary>
    private static void AddRateLimiting(IServiceCollection services, LeadRateLimitOptions options)
    {
        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(LeadEndpoints.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = TimeSpan.FromMinutes(options.WindowMinutes),
                    QueueLimit = 0,
                }));

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                // AC-33/AC-34 — gövde kararlı bir `errorCode` taşır ve teknik ayrıntı
                // içermez. Varsayılan boş 503 gövdesi bırakılsaydı uç, hız sınırını
                // genel bir ağ hatasından ayıramazdı.
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["status"] = StatusCodes.Status429TooManyRequests,
                        ["errorCode"] = LeadErrors.RateLimited,
                    }),
                    cancellationToken);
            };
        });
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
