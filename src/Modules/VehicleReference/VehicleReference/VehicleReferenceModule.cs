using CarGallery.Modules.VehicleReference.Application;
using CarGallery.Modules.VehicleReference.Contracts;
using CarGallery.Modules.VehicleReference.Endpoints;
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

        services.AddScoped<IBodyTypeQueries, BodyTypeQueries>();
        services.AddScoped<IVehicleModelQueries, VehicleModelQueries>();

        return services;
    }

    public static void MapVehicleReference(this IEndpointRouteBuilder builder) => builder.MapBodyTypeEndpoints();
}
