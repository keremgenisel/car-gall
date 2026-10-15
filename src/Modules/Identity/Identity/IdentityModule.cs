using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Contracts.Setup;
using CarGallery.Modules.Identity.Contracts.Sessions;
using CarGallery.Modules.Identity.Domain;
using CarGallery.Modules.Identity.Endpoints;
using CarGallery.Modules.Identity.Endpoints.Authentication;
using CarGallery.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CarGallery.Modules.Identity;

/// <summary>
/// Modülün tek giriş noktası: DI kaydı ve endpoint kaydı. Host yalnızca bunu çağırır
/// (docs/conventions.md § 3).
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentity(this IServiceCollection services, IdentitySetup setup)
    {
        if (string.IsNullOrWhiteSpace(setup.SeedAdminPassword))
        {
            // D2/AC-53 — başlangıç parolası yapılandırmada yoksa uygulama BAŞLAMAZ.
            // Üretilmiş bir varsayılan, bilinen parolalı bir Yönetici hesabı demektir.
            throw new InvalidOperationException(
                "Identity:SeedAdmin:Password yapılandırılmamış; başlangıç Yönetici parolası olmadan uygulama başlayamaz.");
        }

        if (YayinlanmisParolalar.Contains(setup.SeedAdminPassword))
        {
            // ADR 0011 — depo herkese açık; bu üç dize geçmişte depoda SABİT olarak
            // durdu ve git geçmişinde sonsuza kadar kalacak. Birinin bir sunucuda
            // `scripts/dev-up.sh`'ı çalıştırması, parolası KAMUYA AÇIK bir Yönetici
            // hesabı kurması demek olurdu. Koşul ortama bağlı DEĞİL: bu kod tabanında
            // ortam kavramı yok (ölçüldü) ve güvenliği ortam değişkenine bağlamak,
            // yanlış ortamda sessizce açılan bir kapı üretir.
            throw new InvalidOperationException(
                "Identity:SeedAdmin:Password depoda yayınlanmış bir geliştirme varsayılanı; "
                    + "bu parolayla uygulama başlayamaz. Kendi parolanızı yapılandırın "
                    + "(ADR 0011).");
        }

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                setup.ConnectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema)));

        IdentityOptions identityOptions = new()
        {
            AccessLifetime = TimeSpan.FromMinutes(setup.AccessLifetimeMinutes),
            RefreshLifetime = TimeSpan.FromDays(setup.RefreshLifetimeDays),
            FailedAttemptThreshold = setup.FailedAttemptThreshold,
            LockoutDuration = TimeSpan.FromMinutes(setup.LockoutMinutes),
            MinimumPasswordLength = setup.MinimumPasswordLength,
            PasswordIterations = setup.PasswordIterations,
        };

        services.AddSingleton(identityOptions);
        services.AddSingleton(BuildLocaleOptions(setup.DefaultLocale, setup.EnabledLocales));
        services.AddSingleton(setup);
        services.AddSingleton<IClock, BuildingBlocks.SystemClock>();
        services.AddSingleton<IPasswordHasher>(provider =>
            new Pbkdf2PasswordHasher(provider.GetRequiredService<IdentityOptions>()));

        // Şema kaydı diğer üç modülle AYNI desen: uygulayan kurulum adımıdır, çalışan
        // uygulama yalnız doğrular (docs/decisions/0002). Bu satır olmadan `identity`
        // şeması kurulum adımında hiç uygulanmaz ve eksikliği yalnız ilk girişte,
        // "tablo yok" diye görünürdü.
        services.AddScoped<IModuleSchema>(provider =>
            new ModuleSchema<IdentityDbContext>(
                provider.GetRequiredService<IdentityDbContext>(),
                "Identity"));

        services.AddScoped<OutboxWriter<IdentityDbContext>>();
        services.AddScoped<IdentityUnitOfWork>();
        services.AddScoped<IBranchValidator, BranchValidator>();
        services.AddScoped<IStaffSessionQueries, StaffSessionQueries>();
        services.AddScoped<Login>();
        services.AddScoped<Logout>();
        // 0007 — invariant koruması ortak desenden gelir; kilit süresi bir iş kuralı
        // değil bir güvenlik marjıdır, bu yüzden yapılandırmadan değil varsayılandan.
        services.AddSingleton(InvariantGuardOptions<IdentityDbContext>.Default);
        services.AddScoped<StaffCommands>();

        // Dağıtıcı modülün kendi kaydıdır: outbox tablosu bu modülün şemasındadır.
        // Bugün abonesi yok (X6, X7) — dağıtıcı yine de ayakta, çünkü konuşan yazıldı.
        services.AddScoped(provider => new OutboxDispatcher<IdentityDbContext>(
            provider.GetRequiredService<IdentityDbContext>(),
            provider,
            provider.GetRequiredService<IClock>(),
            new OutboxDispatchOptions(),
            provider.GetRequiredService<ILogger<OutboxDispatcher<IdentityDbContext>>>()));

        services.AddHostedService(provider => new OutboxDispatcherHostedService<IdentityDbContext>(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ILogger<OutboxDispatcherHostedService<IdentityDbContext>>>()));

        AddAuthentication(services);
        AddAuthorization(services);
        AddLoginRateLimiting(services, setup);

        return services;
    }

    public static void MapIdentity(this IEndpointRouteBuilder builder)
    {
        builder.MapSessionEndpoints();
        builder.MapAdminEndpoints();
    }

    /// <summary>
    /// Yönetim isteklerinin önünde duran iki ara katman. Host bunları çağırmazsa R10'un
    /// kapısı ve AC-30'un sunucu yarısı sessizce kaybolur — `Host.Tests` bu yüzden
    /// gerçek boru hattını sınar.
    /// </summary>
    public static IApplicationBuilder UseIdentity(this IApplicationBuilder app)
    {
        app.UseMiddleware<CrossOriginGuard>();
        app.UseMiddleware<PasswordChangeGate>();

        return app;
    }

    /// <summary>
    /// Kurulum Yönetici hesabını garanti eder (D2). İdempotenttir: hesap varsa dokunulmaz.
    ///
    /// Şemayı UYGULAMAZ — o iş kurulum adımınındır (docs/decisions/0002) ve bu modül
    /// şemasını `IModuleSchema` ile oraya kaydeder. Burada kalan tek şey bir satırlık
    /// DML'dir: hesap olmadan hiç kimse giriş yapamaz ve sistem kalıcı olarak kapalı
    /// kalırdı (0006/D2, Tuzak 1).
    /// </summary>
    public static async Task EnsureSeedAdministratorAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();

        IdentityDbContext context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        IdentitySetup setup = scope.ServiceProvider.GetRequiredService<IdentitySetup>();

        await SeedAdministrator.EnsureAsync(
            context,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            scope.ServiceProvider.GetRequiredService<IdentityLocaleOptions>(),
            scope.ServiceProvider.GetRequiredService<IClock>(),
            setup.SeedAdminLoginId,
            setup.SeedAdminPassword,
            setup.SeedAdminDisplayName,
            cancellationToken);
    }

    private static void AddAuthentication(IServiceCollection services) =>
        services
            .AddAuthentication(AdminSurface.AuthenticationScheme)
            .AddScheme<SessionAuthenticationOptions, SessionAuthenticationHandler>(
                AdminSurface.AuthenticationScheme,
                _ => { });

    /// <summary>
    /// C19 — oturumsuz istek `401`, yetkisi yetmeyen istek `403`. Politika adları
    /// `Identity.Contracts`'ta yayınlı: sonraki panel spec'leri aynı köke aynı adlarla
    /// takılacak.
    /// </summary>
    private static void AddAuthorization(IServiceCollection services) =>
        services
            .AddAuthorizationBuilder()
            .AddPolicy(AdminSurface.AuthenticatedPolicy, policy => policy
                .AddAuthenticationSchemes(AdminSurface.AuthenticationScheme)
                .RequireAuthenticatedUser())
            .AddPolicy(AdminSurface.ManagerPolicy, policy => policy
                .AddAuthenticationSchemes(AdminSurface.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .RequireRole(StaffRoleCode.ToCode(StaffRole.Manager)));

    /// <summary>
    /// R6'nın İKİNCİ yarısı: giriş ucu ayrıca kaynak IP bazlı hız sınırına tabidir.
    /// Hesap kilidinden farklıdır — o, hesap başına ve doğru parolayı da reddeder;
    /// bu, kaynak başına ve isteği hiç işlemeden keser.
    /// </summary>
    private static void AddLoginRateLimiting(IServiceCollection services, IdentitySetup setup)
    {
        services.AddRateLimiter(limiter => limiter.AddPolicy<string, LoginRateLimitPolicy>(
            SessionEndpoints.RateLimitPolicy));

        // IdentitySetup zaten singleton olarak kayıtlı; politika onu DI'dan alır.
    }

    private static IdentityLocaleOptions BuildLocaleOptions(string defaultLocale, IReadOnlyList<string> enabled)
    {
        Result<Locale> parsedDefault = Locale.Create(defaultLocale);

        if (parsedDefault.IsFailure)
        {
            throw new InvalidOperationException(string.Format(
                CultureInfo.InvariantCulture, "Varsayılan locale geçersiz: '{0}'.", defaultLocale));
        }

        List<Locale> locales = [];

        foreach (string code in enabled)
        {
            Result<Locale> parsed = Locale.Create(code);

            if (parsed.IsFailure)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture, "Etkin locale listesinde geçersiz kod: '{0}'.", code));
            }

            locales.Add(parsed.Value);
        }

        if (locales.Count == 0 || !locales.Any(locale => locale.Code == parsedDefault.Value.Code))
        {
            throw new InvalidOperationException(
                "Etkin locale listesi boş olamaz ve varsayılan locale'i içermelidir.");
        }

        return new IdentityLocaleOptions(parsedDefault.Value, locales);
    }

    /// <summary>
    /// Depoda SABİT olarak yayınlanmış geliştirme/CI parolaları (ADR 0011). Bu dizeler
    /// git geçmişinde kalıcıdır, bu yüzden reddi de kalıcıdır — liste kısalmaz, yalnızca
    /// uzar. `tests/Architecture.Tests/YayinlanmisParolaTests.cs` depoda YENİ bir sabit
    /// parolanın doğmadığını denetliyor.
    /// </summary>
    private static readonly HashSet<string> YayinlanmisParolalar =
        new(StringComparer.Ordinal)
        {
            "gelistirme-parolasi",
            "ci-kurulum-parolasi",
            "perf-kurulum-parolasi",
        };
}
