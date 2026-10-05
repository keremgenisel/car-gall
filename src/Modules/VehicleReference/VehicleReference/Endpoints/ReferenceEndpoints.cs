using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.VehicleReference.Endpoints;

/// <summary>
/// Referans verinin okuma yüzeyi. 0010a/R4 altı kümenin hepsinin açılmasını istiyor;
/// bugüne kadar yalnız kasa tipi açıktı ve panel kalan beşini hiçbir yerden okuyamıyordu.
///
/// <para><b>Uçlar oturum GEREKTİRMEZ</b> ve bu bilinçli: referans veri gizli değil, vitrin
/// de kasa tiplerini zaten buradan okuyor (0001). C1 **yazma** uçlarını oturuma bağlıyor;
/// bunlar okuma.</para>
///
/// <para>Etiket personelin locale'inde gelir ve çevirisi olmayan değerde kanonik Latin ada
/// düşülür — ham slug HİÇBİR yerde gösterilmez (AC-13).</para>
/// </summary>
internal static class ReferenceEndpoints
{
    internal static void MapReferenceEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/reference/body-types", GetBodyTypesAsync)
            .WithName("GetBodyTypes")
            .Produces<IReadOnlyList<BodyTypeDto>>(StatusCodes.Status200OK);

        builder.MapGet("/api/reference/models", GetModelsAsync)
            .WithName("GetVehicleModels")
            .Produces<IReadOnlyList<VehicleModelDto>>(StatusCodes.Status200OK);

        MapTerm(builder, "fuel-types", ReferenceTermKinds.FuelType, "GetFuelTypes");
        MapTerm(builder, "transmission-types", ReferenceTermKinds.TransmissionType, "GetTransmissionTypes");
        MapTerm(builder, "drivetrain-types", ReferenceTermKinds.DrivetrainType, "GetDrivetrainTypes");
        MapTerm(builder, "trim-levels", ReferenceTermKinds.TrimLevel, "GetTrimLevels");
    }

    /// <summary>
    /// Dört terim türü AYNI şekle sahip; dört ayrı handler yazmak dört ayrı yerde bakım
    /// demekti. Tür rota adından değil, buradaki sabitten gelir — rota adı değişse bile
    /// sorgu doğru türü sorar.
    /// </summary>
    private static void MapTerm(IEndpointRouteBuilder builder, string segment, string kind, string name) =>
        builder.MapGet(
                $"/api/reference/{segment}",
                async (HttpContext httpContext, IReferenceTermQueries queries, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await queries.GetByKindAsync(
                        kind,
                        httpContext.Request.Query["locale"].ToString(),
                        cancellationToken)))
            .WithName(name)
            .Produces<IReadOnlyList<ReferenceTermDto>>(StatusCodes.Status200OK);

    private static async Task<Ok<IReadOnlyList<BodyTypeDto>>> GetBodyTypesAsync(
        HttpContext httpContext,
        IBodyTypeQueries queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetAllAsync(
            httpContext.Request.Query["locale"].ToString(),
            cancellationToken));

    private static async Task<Ok<IReadOnlyList<VehicleModelDto>>> GetModelsAsync(
        HttpContext httpContext,
        IVehicleModelQueries queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetAllAsync(
            httpContext.Request.Query["locale"].ToString(),
            cancellationToken));
}
