using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Detail;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.VehicleCatalog.Endpoints;

/// <summary>
/// Controller kullanılmaz; endpoint gövdesi incedir: bağlama, handler çağrısı, sonuç
/// eşleme. İş mantığı yoktur (docs/conventions.md § 3, Yasak Listesi 20).
/// </summary>
internal static class ShowcaseEndpoints
{
    internal static void MapShowcaseEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/catalog/showcase-listings", GetShowcaseListingsAsync)
            .WithName("GetShowcaseListings")
            .Produces<PagedResult<ShowcaseListingDto>>(StatusCodes.Status200OK);

        builder.MapGet("/api/catalog/showcase-listings/{slug}", GetShowcaseListingDetailAsync)
            .WithName("GetShowcaseListingDetail")
            .Produces<ShowcaseListingDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<Ok<PagedResult<ShowcaseListingDto>>> GetShowcaseListingsAsync(
        HttpContext httpContext,
        IShowcaseListingQueries queries,
        CancellationToken cancellationToken)
    {
        // page ve pageSize STRING olarak okunur, int? olarak değil: "abc" veya "-3" için
        // model bağlama 400 üretirdi, oysa C14 geçersiz girdinin hata OLMADIĞINI söylüyor
        // ve sunucu her zaman 200 + normalize edilmiş liste döner (AC-15, AC-16).
        IQueryCollection query = httpContext.Request.Query;

        ShowcaseListingFilter filter = new(
            [.. query["bodyType"].Where(value => value is not null).Select(value => value!)],
            ParsePositiveInteger(query["page"], fallback: 1),
            ResolveLocale(httpContext),
            ParseOptionalPositiveInteger(query["pageSize"]));

        PagedResult<ShowcaseListingDto> page = await queries.GetAsync(filter, cancellationToken);
        return TypedResults.Ok(page);
    }

    private static async Task<Results<Ok<ShowcaseListingDetailDto>, NotFound>> GetShowcaseListingDetailAsync(
        string slug,
        HttpContext httpContext,
        IShowcaseListingDetailQueries queries,
        CancellationToken cancellationToken)
    {
        ShowcaseListingDetailDto? detail =
            await queries.FindAsync(slug, ResolveLocale(httpContext), cancellationToken);

        // Görünmeyen ilan ile hiç var olmayan ilan AYNI karşılığı alır: aynı durum kodu,
        // aynı (boş) gövde. Ayrım yapan bir dal yok — ayrım yapılmadığı için sızdırılamaz
        // (C13, AC-8).
        return detail is null ? TypedResults.NotFound() : TypedResults.Ok(detail);
    }

    private static int ParsePositiveInteger(string? raw, int fallback) =>
        int.TryParse(raw, out int parsed) && parsed >= 1 ? parsed : fallback;

    private static int? ParseOptionalPositiveInteger(string? raw) =>
        int.TryParse(raw, out int parsed) && parsed >= 1 ? parsed : null;

    /// <summary>
    /// API rotaları locale öneki taşımaz; dil açık <c>locale</c> parametresiyle veya
    /// <c>Accept-Language</c> başlığıyla gelir (docs/conventions.md § 2).
    /// </summary>
    private static string ResolveLocale(HttpContext httpContext)
    {
        string? explicitLocale = httpContext.Request.Query["locale"];
        if (!string.IsNullOrWhiteSpace(explicitLocale))
        {
            return explicitLocale;
        }

        string? header = httpContext.Request.Headers.AcceptLanguage;
        return string.IsNullOrWhiteSpace(header) ? string.Empty : header.Split(',')[0].Split('-')[0].Trim();
    }
}
