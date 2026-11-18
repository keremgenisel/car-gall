using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.Pages;
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

        return services;
    }

    /// <summary>Uçları bağlar.</summary>
    public static void MapContent(this IEndpointRouteBuilder builder) => builder.MapContentEndpoints();
}
