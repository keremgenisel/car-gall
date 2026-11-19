using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.Home;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Content.Contracts.PrivacyNotice;
using CarGallery.Modules.Content.Endpoints;
using CarGallery.Modules.Content.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarGallery.Modules.Content;

/// <summary>`0017` — `Content` modülünün kaydı: şema, komutlar, sorgular ve uçlar.</summary>
public static class ContentModule
{
    /// <summary>Modülü kaydeder; etkin diller kurulum yapılandırmasından gelir (R9).</summary>
    public static IServiceCollection AddContent(
        this IServiceCollection services,
        string connectionString,
        string defaultLocale,
        IReadOnlyList<string> enabledLocales)
    {
        services.AddDbContext<ContentDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ContentDbContext.Schema)));

        services.AddScoped<IModuleSchema>(provider => new ModuleSchema<ContentDbContext>(
            provider.GetRequiredService<ContentDbContext>(),
            "Content"));

        services.TryAddSingleton<IClock, SystemClock>();
        services.AddSingleton(new ContentLocaleOptions(defaultLocale, [.. enabledLocales]));
        services.AddScoped<ContentCommands>();
        services.AddScoped<ContentAdminQueries>();
        services.AddScoped<IContentPublicQueries, ContentPublicQueries>();

        // `0019` — `Leads`'in talep anında okuduğu sürüm yüzeyi. Kaydı BURADA, sahibinde:
        // `Leads` bunun için bir varsayılan kaydetmez (AC-49).
        services.AddScoped<IPrivacyNoticeQueries, PrivacyNoticeQueries>();

        // `0022` — ana sayfa metinleri: panelin okuması ve Host'un okuduğu public yüzey aynı sınıf.
        services.AddScoped<HomeTextCommands>();
        services.AddScoped<HomeTextQueries>();
        services.AddScoped<IHomeTextQueries>(provider => provider.GetRequiredService<HomeTextQueries>());

        return services;
    }

    /// <summary>
    /// `0022` D11 — ana sayfa metinlerini etkin her dilde, her alan sınırında doldurur.
    /// YALNIZ geliştirme ve performans ölçümü içindir (<c>Development:SeedHomeTexts</c>);
    /// üretim yolunda çağrılmaz. Şemayı kurulum adımı uygular, bu yalnız veridir.
    /// </summary>
    public static async Task SeedHomeTextsForDevelopmentAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        HomeTextQueries queries = scope.ServiceProvider.GetRequiredService<HomeTextQueries>();
        ContentLocaleOptions locales = scope.ServiceProvider.GetRequiredService<ContentLocaleOptions>();
        AdminHomeTextsDto mevcut = await queries.GetForAdminAsync(cancellationToken);

        Result<int> sonuc = await scope.ServiceProvider
            .GetRequiredService<HomeTextCommands>()
            .SaveAsync(mevcut.Version, HomeTextDevelopmentSeed.Inputs(locales.Enabled), cancellationToken);

        if (sonuc.IsFailure)
        {
            throw new InvalidOperationException($"Ana sayfa metinleri tohumlanamadı: {sonuc.Error.Code}.");
        }
    }

    /// <summary>Uçları bağlar.</summary>
    public static void MapContent(this IEndpointRouteBuilder builder) => builder.MapContentEndpoints();
}
