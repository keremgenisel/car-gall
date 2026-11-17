using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Contracts.Gallery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Dealership.Endpoints;

/// <summary>
/// `0014` **D6**, **D27** — modülün İLK public ucu. Oturum gerektirmez (C1, AC-15); yönetim
/// kökünün dışındadır çünkü ziyaretçinin okuduğu veridir. Gövde incedir: bağlama, sorgu,
/// sonuç eşleme.
/// </summary>
internal static class PublicGalleryEndpoints
{
    internal static void MapPublicGalleryEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/dealership/gallery", GetAsync)
            .WithName("GetPublicGallery")
            .Produces<PublicGalleryDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<PublicGalleryDto>, NotFound>> GetAsync(
        HttpContext httpContext,
        IPublicGalleryQueries queries,
        CancellationToken cancellationToken)
    {
        PublicGalleryDto? gallery = await queries.GetAsync(
            httpContext.Request.Query["locale"].ToString(),
            cancellationToken);

        return gallery is null ? TypedResults.NotFound() : TypedResults.Ok(gallery);
    }
}
