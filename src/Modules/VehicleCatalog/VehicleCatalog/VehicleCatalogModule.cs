using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.FieldSettings;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.FieldSettings;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Endpoints;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.VehicleCatalog;

/// <summary>
/// Modülün tek giriş noktası: DI kaydı ve endpoint kaydı. Host yalnızca bunu çağırır
/// (docs/conventions.md § 3).
/// </summary>
public static class VehicleCatalogModule
{
    public static IServiceCollection AddVehicleCatalog(
        this IServiceCollection services,
        string connectionString,
        string defaultLocale,
        IReadOnlyList<string> enabledLocales)
    {
        services.AddDbContext<VehicleCatalogDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleCatalogDbContext.Schema)));

        services.AddSingleton(BuildLocaleOptions(defaultLocale, enabledLocales));
        services.AddScoped<IVehicleModelNameResolver, VehicleModelNameResolver>();
        services.AddScoped<IShowcaseListingQueries, ShowcaseListingQueries>();
        services.AddScoped<IFieldSettingsQueries, FieldSettingsQueries>();

        return services;
    }

    public static void MapVehicleCatalog(this IEndpointRouteBuilder builder)
    {
        builder.MapShowcaseEndpoints();
        builder.MapFieldSettingsEndpoints();
    }

    /// <summary>
    /// Eksik veya geçersiz yapılandırmayla uygulama AYAĞA KALKMAZ, çalışma anında patlamaz
    /// (docs/conventions.md § 11).
    /// </summary>
    private static ShowcaseLocaleOptions BuildLocaleOptions(string defaultLocale, IReadOnlyList<string> enabledLocales)
    {
        Result<Locale> parsedDefault = Locale.Create(defaultLocale);
        if (parsedDefault.IsFailure)
        {
            throw new InvalidOperationException($"Varsayılan locale geçersiz: '{defaultLocale}'.");
        }

        List<Locale> enabled = [];
        foreach (string code in enabledLocales)
        {
            Result<Locale> parsed = Locale.Create(code);
            if (parsed.IsFailure)
            {
                throw new InvalidOperationException($"Etkin locale listesinde geçersiz kod: '{code}'.");
            }

            enabled.Add(parsed.Value);
        }

        if (enabled.Count == 0 || !enabled.Any(locale => locale.Code == parsedDefault.Value.Code))
        {
            throw new InvalidOperationException("Etkin locale listesi boş olamaz ve varsayılan locale'i içermelidir.");
        }

        return new ShowcaseLocaleOptions(parsedDefault.Value, enabled);
    }
}
