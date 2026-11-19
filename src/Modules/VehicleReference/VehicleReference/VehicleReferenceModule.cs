using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleReference.Application;
using CarGallery.BuildingBlocks.Schema;
using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Endpoints;
using CarGallery.Modules.VehicleReference.Domain;
using CarGallery.Modules.VehicleReference.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarGallery.Modules.VehicleReference;

public static class VehicleReferenceModule
{
    /// <summary>
    /// Kurulumla gelen modellerin kimlikleri (`SeedInitialVehicleModels`). Geliştirme ve
    /// performans tohumu ilanlarını bu modellere — dolayısıyla dokuz markaya — yayar
    /// (`0021` D24): tek markalı tohum marka eksenini ölçemezdi.
    /// </summary>
    private static readonly Guid[] InitialModelIds =
    [
        Guid.Parse("0192c000-0000-7000-8000-00000000000a"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a1"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a2"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a3"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a4"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a5"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a6"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a7"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a8"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000a9"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000aa"),
        Guid.Parse("0192c000-0000-7000-8000-0000000000ab"),
    ];

    /// <summary>Geliştirme tohumunda "ana sayfada göster" işaretli markalar (`0021` R13'ün kanıtı).</summary>
    private static readonly string[] DevelopmentHomeBrandSlugs = ["honda", "renault", "toyota"];

    public static IServiceCollection AddVehicleReference(
        this IServiceCollection services,
        string connectionString,
        IReadOnlyList<string> enabledLocales)
    {
        List<Locale> enabled = [];

        foreach (string code in enabledLocales)
        {
            Result<Locale> locale = Locale.Create(code);

            if (locale.IsFailure)
            {
                throw new InvalidOperationException($"Etkin locale listesinde geçersiz kod: '{code}'.");
            }

            enabled.Add(locale.Value);
        }

        services.AddDbContext<VehicleReferenceDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleReferenceDbContext.Schema)));

        services.AddScoped<IModuleSchema>(provider => new ModuleSchema<VehicleReferenceDbContext>(
            provider.GetRequiredService<VehicleReferenceDbContext>(),
            "VehicleReference"));

        services.AddScoped<IBodyTypeQueries, BodyTypeQueries>();
        services.AddScoped<IVehicleModelQueries, VehicleModelQueries>();
        services.AddScoped<IVehicleBrandQueries, VehicleBrandQueries>();
        services.AddScoped<IReferenceTermQueries, ReferenceTermQueries>();

        // `0021` — yönetim yüzeyi (yalnız Yönetici; politika uç grubunda).
        services.AddSingleton(new ReferenceLocales(enabled));
        services.AddScoped<ReferenceAdminQueries>();
        services.AddScoped<BrandCommands>();
        services.AddScoped<ModelCommands>();

        return services;
    }

    /// <summary>
    /// YALNIZCA geliştirme ve kanıt üretimi içindir. Modelleri kurulum adımı yazar
    /// (`SeedInitialVehicleModels`, `AddVehicleBrands`); tohum yalnız hiçbir marka ana sayfada
    /// işaretli değilse üç markayı işaretler — yönetici bir işareti kaldırdıysa yeniden
    /// başlatma onu geri koymaz. Dönen kimlikler catalog tohumunun yayılacağı modellerdir.
    /// </summary>
    public static async Task<IReadOnlyList<Guid>> SeedVehicleReferenceForDevelopmentAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        VehicleReferenceDbContext context = scope.ServiceProvider.GetRequiredService<VehicleReferenceDbContext>();

        if (!await context.VehicleBrands.AnyAsync(brand => brand.ShowOnHome, cancellationToken))
        {
            foreach (VehicleBrand brand in await context.VehicleBrands
                .Where(brand => DevelopmentHomeBrandSlugs.Contains(brand.Slug))
                .ToListAsync(cancellationToken))
            {
                brand.ShowOnHome = true;
                brand.Version++;
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        return await context.VehicleModels
            .AsNoTracking()
            .Where(model => InitialModelIds.Contains(model.Id))
            .OrderBy(model => model.Id)
            .Select(model => model.Id)
            .ToListAsync(cancellationToken);
    }

    public static void MapVehicleReference(this IEndpointRouteBuilder builder)
    {
        builder.MapReferenceEndpoints();
        builder.MapReferenceAdminEndpoints();
    }
}
