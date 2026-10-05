using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.VehicleCatalog.Endpoints;

/// <summary>
/// Panelin araç yüzeyi. Uçlar `AdminSurface.RoutePrefix` altında ve
/// `AuthenticatedPolicy` taşıyor — kök adı burada yeniden YAZILMIYOR, `0006`'nın sabitinden
/// geliyor. Kendi kökünü açan bir panel, C18'in rota tarayan testini atlardı (AC-2).
///
/// <para>Gövde İNCE (`docs/conventions.md` § 3): bağlama, komut çağrısı, sonuç eşleme.
/// İş mantığı yok.</para>
/// </summary>
internal static class VehicleAdminEndpoints
{
    internal static void MapVehicleAdminEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder vehicles = builder
            .MapGroup($"{AdminSurface.RoutePrefix}/vehicles")
            .RequireAuthorization(AdminSurface.AuthenticatedPolicy);

        vehicles.MapPost("/", CreateAsync)
            .WithName("CreateVehicle")
            .Produces<CreateVehicleResponse>(StatusCodes.Status201Created);

        vehicles.MapGet("/", ListAsync)
            .WithName("ListVehicles")
            .Produces<IReadOnlyList<VehicleAdminListItemDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateAsync(
        CreateVehicleBody body,
        CreateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        Result<Guid> result = await command.ExecuteAsync(body.ToRequest(), cancellationToken);

        return result.IsFailure
            ? CatalogProblems.From(result.Error)
            : TypedResults.Created($"{AdminSurface.RoutePrefix}/vehicles/{result.Value}", new CreateVehicleResponse(result.Value));
    }

    private static async Task<Ok<IReadOnlyList<VehicleAdminListItemDto>>> ListAsync(
        HttpContext httpContext,
        IVehicleAdminQueries queries,
        ShowcaseLocaleOptions locales,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.ListAsync(
            locales.ResolveOrDefault(httpContext.Request.Query["locale"].ToString()).Code,
            cancellationToken));
}

/// <summary>
/// İstek gövdesi. Para BİRİMİ yoktur — kurulum başına sabittir ve panelden değiştirilemez
/// (C3, P3); personel yalnız tutar gönderir.
/// </summary>
internal sealed record CreateVehicleBody(
    Guid BranchId,
    Guid VehicleModelId,
    string BodyType,
    string FuelType,
    string TransmissionType,
    string DrivetrainType,
    string TrimLevel,
    Dictionary<string, string>? Color,
    int ModelYear,
    int MileageKilometers,
    decimal? PriceAmount,
    bool PriceOnRequest,
    string? Vin)
{
    internal CreateVehicleRequest ToRequest() => new(
        BranchId,
        VehicleModelId,
        BodyType ?? string.Empty,
        FuelType ?? string.Empty,
        TransmissionType ?? string.Empty,
        DrivetrainType ?? string.Empty,
        TrimLevel ?? string.Empty,
        Color ?? [],
        ModelYear,
        MileageKilometers,
        PriceAmount,
        PriceOnRequest,
        Vin ?? string.Empty);
}

internal sealed record CreateVehicleResponse(Guid VehicleId);

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir. Gövde KARARLI bir `errorCode` ve
/// gerekirse hangi ALANIN düştüğünü taşır; çevrilmiş metin taşımaz — uç kendi
/// kataloğundan çevirir (C10, AC-42).
///
/// <para>`title` ve `detail` bilerek yok: ASP.NET'in varsayılan İngilizce başlığı
/// ziyaretçiye gösterilebilecek bir metin gibi görünür ve uçta yanlışlıkla basılabilir —
/// `0006`'da aynı karar verilmişti.</para>
/// </summary>
internal static class CatalogProblems
{
    internal static JsonHttpResult<Dictionary<string, object?>> From(Error error)
    {
        int status = error.Code switch
        {
            // AC-16 — tekillik ihlali bir DURUM ÇAKIŞMASIDIR: istek geçerli, dünya uygun değil.
            VehicleErrors.DuplicateVin => StatusCodes.Status409Conflict,
            VehicleErrors.BranchNotFound => StatusCodes.Status422UnprocessableEntity,
            ConcurrencyErrors.Lost => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        Dictionary<string, object?> body = new(StringComparer.Ordinal)
        {
            ["type"] = "about:blank",
            ["status"] = status,
            ["errorCode"] = error.Code,
        };

        if (error.Parameters is { Count: > 0 })
        {
            foreach (KeyValuePair<string, string> parameter in error.Parameters)
            {
                body[parameter.Key] = parameter.Value;
            }
        }

        return TypedResults.Json(body, contentType: "application/problem+json", statusCode: status);
    }
}
