using System.Security.Claims;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Endpoints.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Identity.Endpoints;

internal sealed record AddStaffRequest(
    string? DisplayName,
    string? LoginId,
    string? InitialPassword,
    string? Role,
    string? PreferredLocale,
    Guid? BranchId);

internal sealed record ChangeRoleRequest(string? Role);

internal sealed record ResetPasswordRequest(string? NewPassword);

internal sealed record ChangeOwnPasswordRequest(string? CurrentPassword, string? NewPassword);

internal sealed record AddStaffResponse(Guid StaffId);

/// <summary>
/// Yönetim yüzeyi. **Bu grubun altındaki her uç oturum gerektirir** (C18, AC-23) ve bu,
/// tek tek uçlara politika yazarak değil, GRUBA yazarak sağlanır: yeni bir uç eklemek
/// onu otomatik olarak korumalı yapar, unutulabilecek bir adım kalmaz.
///
/// Yönetici işlemleri ayrıca `ManagerPolicy` taşır (R9, AC-25, AC-26, AC-28).
/// </summary>
internal static class AdminEndpoints
{
    internal static void MapAdminEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder admin = builder
            .MapGroup(AdminSurface.RoutePrefix)
            .RequireAuthorization(AdminSurface.AuthenticatedPolicy);

        admin.MapGet("/me", GetCurrent)
            .WithName("GetCurrentStaff")
            .Produces<CurrentSessionResponse>(StatusCodes.Status200OK);

        admin.MapPost("/me/password", ChangeOwnPasswordAsync)
            .WithName("ChangeOwnPassword")
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/staff", ListStaffAsync)
            .WithName("ListStaff")
            .RequireAuthorization(AdminSurface.ManagerPolicy)
            .Produces<IReadOnlyList<StaffListItem>>(StatusCodes.Status200OK);

        admin.MapPost("/staff", AddStaffAsync)
            .WithName("AddStaff")
            .RequireAuthorization(AdminSurface.ManagerPolicy)
            .Produces<AddStaffResponse>(StatusCodes.Status201Created);

        admin.MapPost("/staff/{staffId:guid}/role", ChangeRoleAsync)
            .WithName("ChangeStaffRole")
            .RequireAuthorization(AdminSurface.ManagerPolicy)
            .Produces(StatusCodes.Status204NoContent);

        admin.MapPost("/staff/{staffId:guid}/deactivate", DeactivateAsync)
            .WithName("DeactivateStaff")
            .RequireAuthorization(AdminSurface.ManagerPolicy)
            .Produces(StatusCodes.Status204NoContent);

        admin.MapPost("/staff/{staffId:guid}/password-reset", ResetPasswordAsync)
            .WithName("ResetStaffPassword")
            .RequireAuthorization(AdminSurface.ManagerPolicy)
            .Produces(StatusCodes.Status204NoContent);
    }

    private static Ok<CurrentSessionResponse> GetCurrent(HttpContext httpContext) =>
        TypedResults.Ok(new CurrentSessionResponse(
            httpContext.User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            httpContext.User.FindFirstValue(SessionAuthenticationHandler.PreferredLocaleClaim) ?? string.Empty,
            httpContext.User.FindFirstValue(SessionAuthenticationHandler.MustChangePasswordClaim) == "true"));

    private static async Task<Results<Ok<IReadOnlyList<StaffListItem>>, JsonHttpResult<Dictionary<string, object?>>>>
        ListStaffAsync(StaffCommands commands, CancellationToken cancellationToken) =>
        TypedResults.Ok(await commands.ListAsync(cancellationToken));

    private static async Task<Results<Created<AddStaffResponse>, JsonHttpResult<Dictionary<string, object?>>>>
        AddStaffAsync(AddStaffRequest request, StaffCommands commands, CancellationToken cancellationToken)
    {
        Result<Guid> result = await commands.AddAsync(
            new AddStaffCommand(
                request.DisplayName,
                request.LoginId,
                request.InitialPassword,
                request.Role,
                request.PreferredLocale,
                request.BranchId),
            cancellationToken);

        return result.IsFailure
            ? IdentityProblems.From(result.Error)
            : TypedResults.Created((string?)null, new AddStaffResponse(result.Value));
    }

    private static async Task<Results<NoContent, JsonHttpResult<Dictionary<string, object?>>>> ChangeRoleAsync(
        Guid staffId,
        ChangeRoleRequest request,
        StaffCommands commands,
        CancellationToken cancellationToken) =>
        Map(await commands.ChangeRoleAsync(staffId, request.Role, cancellationToken));

    private static async Task<Results<NoContent, JsonHttpResult<Dictionary<string, object?>>>> DeactivateAsync(
        Guid staffId,
        StaffCommands commands,
        CancellationToken cancellationToken) =>
        Map(await commands.DeactivateAsync(staffId, cancellationToken));

    private static async Task<Results<NoContent, JsonHttpResult<Dictionary<string, object?>>>> ResetPasswordAsync(
        Guid staffId,
        ResetPasswordRequest request,
        StaffCommands commands,
        CancellationToken cancellationToken) =>
        Map(await commands.ResetPasswordAsync(staffId, request.NewPassword, cancellationToken));

    private static async Task<Results<NoContent, JsonHttpResult<Dictionary<string, object?>>>>
        ChangeOwnPasswordAsync(
            ChangeOwnPasswordRequest request,
            HttpContext httpContext,
            StaffCommands commands,
            CancellationToken cancellationToken)
    {
        string? staffId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(staffId, out Guid parsed))
        {
            return IdentityProblems.From(new Error(Domain.IdentityErrors.NoSession));
        }

        return Map(await commands.ChangeOwnPasswordAsync(
            parsed, request.CurrentPassword, request.NewPassword, cancellationToken));
    }

    private static Results<NoContent, JsonHttpResult<Dictionary<string, object?>>> Map(Result result) =>
        result.IsFailure ? IdentityProblems.From(result.Error) : TypedResults.NoContent();
}
