using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Media.Application;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Media.Endpoints;

/// <summary>
/// Panelin medya yüzeyi. Uçlar `AdminSurface.RoutePrefix` altında ve `AuthenticatedPolicy`
/// taşıyor — kök adı burada yeniden YAZILMIYOR, `0006`'nın sabitinden geliyor. Kendi kökünü
/// açan bir panel, C18'in rota TARAYAN testini atlardı (AC-2).
///
/// <para><b>Dosya baytını servis eden bir uç YOK</b> ve bu C3'ün sonucu: türevler CDN'den
/// okunur. Uygulamayı görsel yoluna koymak, her kart isteğini sunucudan geçirmek ve CDN'i
/// baştan iptal etmek demekti.</para>
///
/// <para>Gövde İNCE (`docs/conventions.md` § 3): bağlama, komut çağrısı, sonuç eşleme.</para>
/// </summary>
internal static class MediaAdminEndpoints
{
    /// <summary>
    /// Yanıt gövdesi. `status` bir KOD taşır (<c>uploaded</c>), çevrilmiş ad değil
    /// (Yasak Listesi 22/23) — uç onu kendi kataloğundan çevirir.
    /// </summary>
    internal sealed record UploadMediaAssetResponse(Guid Id, string Status);

    internal static void MapMediaAdminEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder assets = builder
            .MapGroup($"{AdminSurface.RoutePrefix}/media/assets")
            .RequireAuthorization(AdminSurface.AuthenticatedPolicy);

        assets.MapPost("/", UploadAsync)
            .WithName("UploadMediaAsset")
            .DisableAntiforgery()
            .Produces<UploadMediaAssetResponse>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status415UnsupportedMediaType)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        assets.MapGet("/{id:guid}", FindAsync)
            .WithName("FindMediaAsset")
            .Produces<MediaAssetDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// D9 — orijinal istek içinde depoya yazılır ve uç <c>202</c> döner: iş kabul edildi,
    /// bitmedi. <c>201</c> dönmek "kaynak hazır" demekti ve türevler henüz yok.
    ///
    /// <para><b>Gövde <c>IFormFile</c> olarak BAĞLANMIYOR ve bu ölçülmüş bir karar.</b>
    /// `IFormFile` parametresi uca <c>multipart/form-data</c> kabul meta verisi ekliyor ve
    /// o meta verinin <c>415</c> kısa devresi <b>yetkilendirmeden ÖNCE</b> koşuyor.
    /// Ölçüldü: oturumsuz bir istek JSON gövdeyle <c>415</c>, multipart gövdeyle
    /// <c>401</c> alıyordu — yani yönetim kökü altındaki bir uç, oturumu olmayan birine
    /// kendi varlığını ve beklediği içerik tipini söylüyordu. `0006`/C18'in rota tarayan
    /// testi tam olarak bunu kırdı.</para>
    ///
    /// <para><c>HttpRequest</c> bağlandığında kabul meta verisi hiç oluşmuyor,
    /// yetkilendirme her zaman önce koşuyor ve içerik tipi denetimi buraya — oturum
    /// doğrulandıktan SONRAYA — taşınıyor. Testi gevşetmek yerine ucu düzeltmek, kuralı
    /// bir sonraki panel için de ayakta tutuyor.</para>
    /// </summary>
    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        UploadMediaAssetCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.HasFormContentType)
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        IFormCollection form = await request.ReadFormAsync(cancellationToken);
        IFormFile? file = form.Files["file"];

        if (file is null)
        {
            return MediaProblems.From(new Error(MediaErrors.UnsupportedType));
        }

        // Gövde belleğe alınıyor çünkü üç tüketicisi var: tür sezgisi, ölçüm ve depoya
        // yazma. Akışı üç kez sarmak yerine bir kez tamponluyoruz — ve bunu güvenli kılan
        // şey boyut sınırının BORU HATTINDA uygulanması (`MediaModule`'deki
        // `RequestSizeLimit`): sınırsız bir gövde burada belleği tüketirdi.
        using MemoryStream tampon = new();
        await file.CopyToAsync(tampon, cancellationToken);

        Result<UploadMediaAssetResult> sonuc = await command.ExecuteAsync(
            tampon.ToArray(), cancellationToken);

        if (sonuc.IsFailure)
        {
            return MediaProblems.From(sonuc.Error);
        }

        return TypedResults.Accepted(
            $"{AdminSurface.RoutePrefix}/media/assets/{sonuc.Value.Id}",
            new UploadMediaAssetResponse(
                sonuc.Value.Id,
                sonuc.Value.Status.ToString().ToLowerInvariant()));
    }

    private static async Task<IResult> FindAsync(
        Guid id,
        IMediaAssetQueries queries,
        CancellationToken cancellationToken)
    {
        MediaAssetDto? asset = await queries.FindAsync(id, cancellationToken);

        return asset is null
            ? MediaProblems.From(new Error(MediaErrors.NotFound))
            : TypedResults.Ok(asset);
    }
}
