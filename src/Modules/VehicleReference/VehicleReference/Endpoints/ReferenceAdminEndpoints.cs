using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.VehicleReference.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.VehicleReference.Endpoints;

/// <summary>
/// `0021` — marka ve model yönetim uçları. Hepsi TEK grupta ve o grup yalnız Yönetici
/// politikasını taşır (R15, D4): Satış Danışmanı ve İçerik Editörü `403`, oturumsuz istek
/// `401` (AC-53, AC-55). Rol kodu burada karşılaştırılmaz; politika `Identity`'nin yayınladığı
/// yüzeydedir (`0020`'nin deseni).
///
/// <para>Araç formunun okuması bu grupta DEĞİL: oturumsuz <c>/api/reference/brands</c> ve
/// <c>/api/reference/models</c> yalnız aktif kayıtları döndürür ve her personel rolüne açıktır
/// (AC-54).</para>
///
/// <para>Gövde incedir: bağlama, komut, sonuç eşleme (Yasak Listesi 20).</para>
/// </summary>
internal static class ReferenceAdminEndpoints
{
    internal const string RoutePrefix = $"{AdminSurface.RoutePrefix}/reference";

    internal static void MapReferenceAdminEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder yonetim = builder.MapGroup(RoutePrefix)
            .RequireAuthorization(AdminSurface.ManagerPolicy);

        yonetim.MapGet("/brands", async (ReferenceAdminQueries queries, CancellationToken ct) =>
            TypedResults.Ok(await queries.ListBrandsAsync(ct)));
        yonetim.MapGet("/brands/{id:guid}", FindBrandAsync);
        yonetim.MapPost("/brands", async (NameBody body, BrandCommands commands, CancellationToken ct) =>
            Created("brands", await commands.CreateAsync(Input(body), ct)));
        yonetim.MapPut("/brands/{id:guid}", (Guid id, SaveNameBody body, BrandCommands commands, CancellationToken ct) =>
            VersionAsync(commands.UpdateAsync(id, body.Version, new ReferenceNameInput(body.CanonicalName, body.DisplayNames), ct)));
        yonetim.MapPost("/brands/{id:guid}/deactivate", (Guid id, VersionBody body, BrandCommands commands, CancellationToken ct) =>
            VersionAsync(commands.SetActiveAsync(id, body.Version, active: false, ct)));
        yonetim.MapPost("/brands/{id:guid}/activate", (Guid id, VersionBody body, BrandCommands commands, CancellationToken ct) =>
            VersionAsync(commands.SetActiveAsync(id, body.Version, active: true, ct)));
        yonetim.MapPut("/brands/{id:guid}/show-on-home", (Guid id, ShowOnHomeBody body, BrandCommands commands, CancellationToken ct) =>
            VersionAsync(commands.SetShowOnHomeAsync(id, body.Version, body.ShowOnHome, ct)));

        yonetim.MapPost("/brands/{id:guid}/models", async (Guid id, NameBody body, ModelCommands commands, CancellationToken ct) =>
            Created("models", await commands.CreateAsync(id, Input(body), ct)));
        yonetim.MapPut("/models/{id:guid}", (Guid id, SaveModelBody body, ModelCommands commands, CancellationToken ct) =>
            VersionAsync(commands.UpdateAsync(
                id,
                body.Version,
                body.BrandId,
                new ReferenceNameInput(body.CanonicalName, body.DisplayNames),
                ct)));
        yonetim.MapPost("/models/{id:guid}/deactivate", (Guid id, VersionBody body, ModelCommands commands, CancellationToken ct) =>
            VersionAsync(commands.SetActiveAsync(id, body.Version, active: false, ct)));
        yonetim.MapPost("/models/{id:guid}/activate", (Guid id, VersionBody body, ModelCommands commands, CancellationToken ct) =>
            VersionAsync(commands.SetActiveAsync(id, body.Version, active: true, ct)));
    }

    private static async Task<Results<Ok<AdminBrandDetailDto>, NotFound>> FindBrandAsync(
        Guid id,
        ReferenceAdminQueries queries,
        CancellationToken cancellationToken)
    {
        AdminBrandDetailDto? brand = await queries.FindBrandAsync(id, cancellationToken);
        return brand is null ? TypedResults.NotFound() : TypedResults.Ok(brand);
    }

    private static IResult Created(string resource, Result<(Guid Id, int Version)> sonuc) =>
        sonuc.IsSuccess
            ? TypedResults.Created($"{RoutePrefix}/{resource}/{sonuc.Value.Id}", new CreatedBody(sonuc.Value.Id, sonuc.Value.Version))
            : ReferenceProblems.From(sonuc.Error);

    private static async Task<IResult> VersionAsync(Task<Result<int>> islem)
    {
        Result<int> sonuc = await islem;
        return sonuc.IsSuccess ? TypedResults.Ok(new VersionBody(sonuc.Value)) : ReferenceProblems.From(sonuc.Error);
    }

    private static ReferenceNameInput Input(NameBody body) => new(body.CanonicalName, body.DisplayNames);
}

/// <summary>Marka ya da model ekleme gövdesi.</summary>
internal sealed record NameBody(string? CanonicalName, Dictionary<string, string?>? DisplayNames);

/// <summary>Marka kaydetme gövdesi; <c>Version</c> okunan sürümdür.</summary>
internal sealed record SaveNameBody(int Version, string? CanonicalName, Dictionary<string, string?>? DisplayNames);

/// <summary>
/// Model kaydetme gövdesi. <c>BrandId</c> opsiyoneldir; gönderilir ve modelin markasından
/// farklıysa istek reddedilir (AC-20).
/// </summary>
internal sealed record SaveModelBody(int Version, Guid? BrandId, string? CanonicalName, Dictionary<string, string?>? DisplayNames);

/// <summary>"Ana sayfada göster" gövdesi.</summary>
internal sealed record ShowOnHomeBody(int Version, bool ShowOnHome);

/// <summary>Durum eylemleri ve yazmaların yanıtı: okunan ya da yeni sürüm.</summary>
internal sealed record VersionBody(int Version);

/// <summary>Eklenen kaydın kimliği ve sürümü.</summary>
internal sealed record CreatedBody(Guid Id, int Version);

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir: kararlı <c>errorCode</c> ve <c>field</c>;
/// çevrilmiş metin yok (C6). Alan hataları `422`; eski sürüm `409`; yok `404`.
/// </summary>
internal static class ReferenceProblems
{
    internal static IResult From(Error error)
    {
        int status = error.Code switch
        {
            ReferenceErrors.BrandNotFound or ReferenceErrors.ModelNotFound => StatusCodes.Status404NotFound,
            ConcurrencyErrors.Lost => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        Dictionary<string, object?> body = new(StringComparer.Ordinal)
        {
            ["type"] = "about:blank",
            ["status"] = status,
            ["errorCode"] = error.Code,
        };

        foreach (KeyValuePair<string, string> parameter in error.Parameters ?? new Dictionary<string, string>())
        {
            body[parameter.Key] = parameter.Value;
        }

        return TypedResults.Json(body, contentType: "application/problem+json", statusCode: status);
    }
}
