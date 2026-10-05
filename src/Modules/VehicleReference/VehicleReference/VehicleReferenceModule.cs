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
    public static IServiceCollection AddVehicleReference(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<VehicleReferenceDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", VehicleReferenceDbContext.Schema)));

        services.AddScoped<IModuleSchema>(provider => new ModuleSchema<VehicleReferenceDbContext>(
            provider.GetRequiredService<VehicleReferenceDbContext>(),
            "VehicleReference"));

        services.AddScoped<IBodyTypeQueries, BodyTypeQueries>();
        services.AddScoped<IVehicleModelQueries, VehicleModelQueries>();
        services.AddScoped<IReferenceTermQueries, ReferenceTermQueries>();

        return services;
    }

    /// <summary>YALNIZCA geliştirme ve kanıt üretimi içindir.</summary>
    public static async Task SeedVehicleReferenceForDevelopmentAsync(
        this IServiceProvider services,
        Guid vehicleModelId,
        CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        VehicleReferenceDbContext context = scope.ServiceProvider.GetRequiredService<VehicleReferenceDbContext>();

        if (await context.VehicleModels.AnyAsync(model => model.Id == vehicleModelId, cancellationToken))
        {
            return;
        }

        context.VehicleModels.Add(new VehicleModel
        {
            Id = vehicleModelId,
            CanonicalName = "Toyota Corolla",
            Translations =
            [
                new VehicleModelTranslation { VehicleModelId = vehicleModelId, Locale = "tr", Name = "Toyota Corolla" },
                new VehicleModelTranslation { VehicleModelId = vehicleModelId, Locale = "en", Name = "Toyota Corolla" },
                new VehicleModelTranslation { VehicleModelId = vehicleModelId, Locale = "ar", Name = "تويوتا كورولا" },
            ],
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    public static void MapVehicleReference(this IEndpointRouteBuilder builder) => builder.MapReferenceEndpoints();
}
