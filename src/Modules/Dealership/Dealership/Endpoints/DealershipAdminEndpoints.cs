using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Dealership.Application;
using CarGallery.Modules.Dealership.Domain;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Dealership.Endpoints;

/// <summary>
/// `0020` — galeri profili ve şubelerin yönetim uçları. Hepsi TEK grupta ve o grup yalnız
/// Yönetici politikasını taşır (R17, D2): Satış Danışmanı ve İçerik Editörü `403`, oturumsuz
/// istek `401` (AC-66, AC-67). Rol kodu burada karşılaştırılmaz; politika `Identity`'nin
/// yayınladığı yüzeydedir.
///
/// <para>Şube LİSTESİ (<c>GET /api/admin/branches</c>) bu grupta DEĞİL: araç formu onu her
/// rolde okuyor ve o okuma bu kartta değişmez (AC-68). Panelin şubeler ekranı aynı listeyi
/// okur; ayrıntı ve yazma buradadır.</para>
///
/// <para>Gövde incedir: bağlama, komut, sonuç eşleme (Yasak Listesi 20).</para>
/// </summary>
internal static class DealershipAdminEndpoints
{
    internal static void MapDealershipAdminEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder yonetim = builder.MapGroup(AdminSurface.RoutePrefix)
            .RequireAuthorization(AdminSurface.ManagerPolicy);

        yonetim.MapGet("/gallery", GetGalleryAsync);
        yonetim.MapPut("/gallery", (SaveGalleryBody body, GalleryCommands commands, CancellationToken ct) =>
            VersionAsync(commands.UpdateProfileAsync(
                body.Version,
                new GalleryProfileInput(body.CommercialName, body.TaxNumber, body.TradeRegistryNumber, body.DisplayNames, body.SocialLinks),
                ct)));

        yonetim.MapGet("/branches/{id:guid}", FindBranchAsync);
        yonetim.MapPost("/branches", OpenBranchAsync);
        yonetim.MapPut("/branches/{id:guid}", (Guid id, SaveBranchBody body, BranchCommands commands, CancellationToken ct) =>
            VersionAsync(commands.UpdateAsync(id, body.Version, Input(body), ct)));
        yonetim.MapPost("/branches/{id:guid}/close-temporarily", (Guid id, VersionBody body, BranchCommands commands, CancellationToken ct) =>
            VersionAsync(commands.CloseTemporarilyAsync(id, body.Version, ct)));
        yonetim.MapPost("/branches/{id:guid}/reopen", (Guid id, VersionBody body, BranchCommands commands, CancellationToken ct) =>
            VersionAsync(commands.ReopenAsync(id, body.Version, ct)));
        yonetim.MapPost("/branches/{id:guid}/close", (Guid id, VersionBody body, BranchCommands commands, CancellationToken ct) =>
            VersionAsync(commands.CloseAsync(id, body.Version, ct)));
    }

    private static async Task<Results<Ok<AdminGalleryDto>, NotFound>> GetGalleryAsync(
        DealershipAdminQueries queries,
        CancellationToken cancellationToken)
    {
        AdminGalleryDto? gallery = await queries.GetGalleryAsync(cancellationToken);
        return gallery is null ? TypedResults.NotFound() : TypedResults.Ok(gallery);
    }

    private static async Task<Results<Ok<AdminBranchDto>, NotFound>> FindBranchAsync(
        Guid id,
        DealershipAdminQueries queries,
        CancellationToken cancellationToken)
    {
        AdminBranchDto? branch = await queries.FindBranchAsync(id, cancellationToken);
        return branch is null ? TypedResults.NotFound() : TypedResults.Ok(branch);
    }

    private static async Task<IResult> OpenBranchAsync(
        OpenBranchBody body,
        BranchCommands commands,
        CancellationToken cancellationToken)
    {
        Result<(Guid Id, int Version)> sonuc = await commands.OpenAsync(
            new BranchFormInput(
                body.Names,
                body.Addresses,
                body.Phone,
                body.WhatsApp,
                body.Email,
                body.TimeZone,
                body.Slug,
                body.Latitude,
                body.Longitude),
            cancellationToken);

        return sonuc.IsSuccess
            ? TypedResults.Created(
                $"{AdminSurface.RoutePrefix}/branches/{sonuc.Value.Id}",
                new CreatedBody(sonuc.Value.Id, sonuc.Value.Version))
            : DealershipProblems.From(sonuc.Error);
    }

    private static async Task<IResult> VersionAsync(Task<Result<int>> islem)
    {
        Result<int> sonuc = await islem;
        return sonuc.IsSuccess ? TypedResults.Ok(new VersionBody(sonuc.Value)) : DealershipProblems.From(sonuc.Error);
    }

    private static BranchFormInput Input(SaveBranchBody body) => new(
        body.Names,
        body.Addresses,
        body.Phone,
        body.WhatsApp,
        body.Email,
        body.TimeZone,
        body.Slug,
        body.Latitude,
        body.Longitude);
}

/// <summary>
/// Galeri profili kaydetme gövdesi; <c>Version</c> okunan sürümdür (R16).
///
/// <para>`0024` **D4** · P1 — <c>SocialLinks</c> platform kodundan adrese haritadır. Alan yoksa
/// ya da açık <c>null</c>'sa kayıtlı hesaplar değişmez: D4 ikisini AYNI anlama bağladığı için
/// düz nullable tip yeterli; <c>Optional&lt;T&gt;</c> gerekmez. Silmenin tek yolu boş haritadır.</para>
/// </summary>
internal sealed record SaveGalleryBody(
    int Version,
    string? CommercialName,
    string? TaxNumber,
    string? TradeRegistryNumber,
    Dictionary<string, string?>? DisplayNames,
    Dictionary<string, string?>? SocialLinks = null);

/// <summary>Şube açma gövdesi.</summary>
internal sealed record OpenBranchBody(
    Dictionary<string, string?>? Names,
    Dictionary<string, string?>? Addresses,
    string? Phone,
    string? WhatsApp,
    string? Email,
    string? TimeZone,
    string? Slug,
    decimal? Latitude,
    decimal? Longitude);

/// <summary>Şube kaydetme gövdesi; <c>Version</c> okunan sürümdür (R16).</summary>
internal sealed record SaveBranchBody(
    int Version,
    Dictionary<string, string?>? Names,
    Dictionary<string, string?>? Addresses,
    string? Phone,
    string? WhatsApp,
    string? Email,
    string? TimeZone,
    string? Slug,
    decimal? Latitude,
    decimal? Longitude);

/// <summary>Durum eylemleri ve yazmaların yanıtı: okunan ya da yeni sürüm.</summary>
internal sealed record VersionBody(int Version);

/// <summary>Açılan şubenin kimliği ve sürümü.</summary>
internal sealed record CreatedBody(Guid Id, int Version);

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir: kararlı <c>errorCode</c> ve alan
/// parametreleri; çevrilmiş metin yok (C5). Alan hataları `422` (`docs/conventions.md` § 5);
/// eski sürüm, geçersiz durum geçişi, son aktif şube ve kapatılmış şubenin düzenlenmesi
/// DURUM ÇAKIŞMASIDIR (`409`): istek geçerli, dünya uygun değil.
/// </summary>
internal static class DealershipProblems
{
    internal static IResult From(Error error)
    {
        int status = error.Code switch
        {
            BranchCommands.BranchNotFound or Gallery.NotFound => StatusCodes.Status404NotFound,
            ConcurrencyErrors.Lost
                or Branch.InvalidTransition
                or Branch.LastActiveBranch
                or Branch.ClosedReadOnly => StatusCodes.Status409Conflict,
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
