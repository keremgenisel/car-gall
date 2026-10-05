using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.Detail;
using CarGallery.Modules.VehicleCatalog.Application.Development;
using CarGallery.Modules.VehicleCatalog.Application.EventHandlers;
using CarGallery.Modules.VehicleCatalog.Application.FieldSettings;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.FieldSettings;
using CarGallery.Modules.VehicleCatalog.Contracts.Listings;
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
        IReadOnlyList<string> enabledLocales,
        string currencyCode,
        int currencyMinorUnitExponent)
    {
        services.AddDbContext<VehicleCatalogDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleCatalogDbContext.Schema)));

        services.AddSingleton(BuildLocaleOptions(defaultLocale, enabledLocales));
        services.AddSingleton<IClock, SystemClock>();

        // 0003 C1 — dinleyiciler ilk kez DI'a bağlanıyor. 0001 X10'da kapsam dışı bırakılan
        // bu adım 0003'e devredildi; bugüne kadar "dinleyen var, konuşan yok"tu.
        // Yayıncı bu kayıtları GÖRMEZ: dağıtıcı alıcıları DI'dan çözer (C7, Yasak Listesi 18).
        services.AddScoped<IIntegrationEventDispatchTarget, BranchDetailsChangedHandler>();
        services.AddScoped<IIntegrationEventDispatchTarget, BranchDeactivatedHandler>();
        services.AddScoped<IIntegrationEventHandler<BranchDetailsChanged>, BranchDetailsChangedHandler>();
        services.AddScoped<IIntegrationEventHandler<BranchDeactivated>, BranchDeactivatedHandler>();
        services.AddScoped<IVehicleModelNameResolver, VehicleModelNameResolver>();
        services.AddScoped<IBodyTypeCatalog, BodyTypeCatalog>();
        services.AddScoped<IReferenceLabelResolver, ReferenceLabelResolver>();
        // Şema Host'a ENJEKSİYONLA görünür; elle tutulan bir liste yok. Yeni bir
        // modül eklendiğinde kurulum ve doğrulama yollarına kendiliğinden katılır.
        services.AddScoped<IModuleSchema>(provider => new ModuleSchema<VehicleCatalogDbContext>(
            provider.GetRequiredService<VehicleCatalogDbContext>(),
            "VehicleCatalog"));

        services.AddScoped<IShowcaseListingQueries, ShowcaseListingQueries>();
        services.AddScoped<IShowcaseListingDetailQueries, ShowcaseListingDetailQueries>();
        services.AddScoped<IFieldSettingsQueries, FieldSettingsQueries>();

        // 0005 R7/R8 — talep anlık görüntüsünün kaynağı. Locale almaz, çeviri çözmez.
        services.AddScoped<IListingSnapshotQueries, ListingSnapshotQueries>();

        // 0009 D5 — bildirimdeki detay bağlantısının slug'ı.
        services.AddScoped<IListingSlugQueries, ListingSlugQueries>();

        // 0010a — yazma yüzeyi. Para birimi KURULUM BAŞINA SABİTTİR ve panelden
        // değiştirilemez (C3, P3): personel yalnız tutar girer.
        services.AddSingleton(new CatalogCurrencyOptions(currencyCode, currencyMinorUnitExponent));
        services.AddScoped<CreateVehicleCommand>();
        services.AddScoped<IVehicleAdminQueries, VehicleAdminQueries>();

        return services;
    }

    /// <summary>
    /// Migration'ı uygular ve vitrini örnek veriyle doldurur. YALNIZCA geliştirme ve
    /// kanıt üretimi içindir; üretim yolunda çağrılmaz.
    /// </summary>
    public static async Task SeedVehicleCatalogForDevelopmentAsync(
        this IServiceProvider services,
        int listingCount,
        Guid vehicleModelId,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();

        VehicleCatalogDbContext context = scope.ServiceProvider.GetRequiredService<VehicleCatalogDbContext>();
        ShowcaseSeeder seeder = new(context, scope.ServiceProvider.GetRequiredService<IClock>());
        await seeder.SeedAsync(listingCount, vehicleModelId, cancellationToken);
    }

    public static void MapVehicleCatalog(this IEndpointRouteBuilder builder)
    {
        builder.MapShowcaseEndpoints();
        builder.MapFieldSettingsEndpoints();

        // 0010a — panelin araç yüzeyi. `AdminSurface` kökü altında ve oturum politikasıyla;
        // C18'in rota tarayan testi onu kendiliğinden kapsar.
        builder.MapVehicleAdminEndpoints();
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
