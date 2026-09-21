using CarGallery.Modules.VehicleCatalog.Contracts.FieldSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.VehicleCatalog.Endpoints;

internal static class FieldSettingsEndpoints
{
    internal static void MapFieldSettingsEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/catalog/field-settings", GetFieldSettingsAsync)
            .WithName("GetFieldSettings")
            .Produces<FieldSettingsDto>(StatusCodes.Status200OK);
    }

    private static async Task<Ok<FieldSettingsDto>> GetFieldSettingsAsync(
        IFieldSettingsQueries queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetAsync(cancellationToken));
}
