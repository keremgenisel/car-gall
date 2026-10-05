using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Dealership.Endpoints;

/// <summary>
/// 0010a — şube listesi. Modülün İLK ucu: `0003` bilinçli olarak hiç uç açmamıştı
/// ("Endpoint kaydı yoktur"), çünkü o spec'te şubeyi okuyan bir ekran yoktu.
///
/// <para>Araç kaydı formu olmadan yapamaz: her aracın tam olarak bir şubesi vardır (A1)
/// ve şubesiz araç kaydedilemez (AC-3). Bu yüzeyin bugüne kadar hiç olmaması bir
/// eksiklikti — personel formu bile `branchId: null` gönderiyordu çünkü seçtirecek bir
/// listesi yoktu.</para>
///
/// <para><b>Yönetim kökü altında</b>: liste panelin ihtiyacı ve C1'in ruhu panel verisini
/// oturumun arkasında tutmak. Kök adı `0006`'nın sabitinden geliyor; kendi kökünü açan bir
/// modül C18'in rota tarayan testini atlardı.</para>
/// </summary>
internal static class BranchEndpoints
{
    internal static void MapBranchEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet($"{AdminSurface.RoutePrefix}/branches", ListAsync)
            .WithName("ListBranches")
            .RequireAuthorization(AdminSurface.AuthenticatedPolicy)
            .Produces<IReadOnlyList<BranchSummaryDto>>(StatusCodes.Status200OK);
    }

    private static async Task<Ok<IReadOnlyList<BranchSummaryDto>>> ListAsync(
        HttpContext httpContext,
        IBranchQueries queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.ListAsync(
            httpContext.Request.Query["locale"].ToString(),
            cancellationToken));
}
