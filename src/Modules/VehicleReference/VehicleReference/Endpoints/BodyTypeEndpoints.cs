using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.VehicleReference.Endpoints;

internal static class BodyTypeEndpoints
{
    internal static void MapBodyTypeEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/reference/body-types", GetBodyTypesAsync)
            .WithName("GetBodyTypes")
            .Produces<IReadOnlyList<BodyTypeDto>>(StatusCodes.Status200OK);
    }

    private static async Task<Ok<IReadOnlyList<BodyTypeDto>>> GetBodyTypesAsync(
        HttpContext httpContext,
        IBodyTypeQueries queries,
        CancellationToken cancellationToken)
    {
        string locale = httpContext.Request.Query["locale"].ToString();
        return TypedResults.Ok(await queries.GetAllAsync(locale, cancellationToken));
    }
}
