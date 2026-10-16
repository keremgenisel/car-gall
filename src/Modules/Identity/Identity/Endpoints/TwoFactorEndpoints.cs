using System.Security.Claims;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Identity.Endpoints;

internal sealed record ConfirmTwoFactorRequest(string? Code);

internal sealed record DisableTwoFactorRequest(string? CurrentPassword, string? Code);

internal sealed record RegenerateRecoveryCodesRequest(string? Code);

/// <summary>
/// Kurulum yanıtı. Sır **metin** olarak döner — `otpauth://` adresi ve elle giriş için
/// Base32 hâli — ve kare kodu UÇ çizer (D17/C15). Sunucuda üretilen bir görsel sırrı
/// önbellek ve log yüzeylerine taşırdı.
/// </summary>
internal sealed record TwoFactorEnrollmentResponse(string SecretUri, string Secret);

/// <summary>Kurtarma kodu kümesi; **bir kez** döner (D31/AC-7).</summary>
internal sealed record RecoveryCodesResponse(IReadOnlyList<string> Codes);

internal sealed record TwoFactorStatusResponse(bool Enrolled, bool Required, int RemainingRecoveryCodes);

/// <summary>
/// İkinci adımın yönetim yüzeyi. Uçlar **mevcut yüzeyin biçimini izler** (D7): personelin
/// kendi işlemleri `{admin}/me/...` altında (`me/password` emsali), Yöneticinin
/// sıfırlaması `{admin}/staff/{staffId}/two-factor-reset` (`.../password-reset` emsali).
///
/// Hepsi yönetim kökünün altındadır ve grup düzeyindeki oturum politikasını devralır
/// (C14/C18): oturumsuz köke ikinci bir yönetim yüzeyi açılmaz. `me/two-factor*` yolları
/// <see cref="Authentication.TwoFactorSetupGate"/>'in izin verdiği üç yoldan biridir —
/// kurulumu olmayan personel kurulumu ancak böyle yapabilir (D13).
/// </summary>
internal static class TwoFactorEndpoints
{
    internal static void MapTwoFactorEndpoints(this RouteGroupBuilder admin)
    {
        admin.MapGet("/me/two-factor", GetStatusAsync)
            .WithName("GetOwnTwoFactorStatus")
            .Produces<TwoFactorStatusResponse>(StatusCodes.Status200OK);

        admin.MapPost("/me/two-factor", StartEnrollmentAsync)
            .WithName("StartOwnTwoFactorEnrollment")
            .Produces<TwoFactorEnrollmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status409Conflict);

        admin.MapPost("/me/two-factor/confirm", ConfirmEnrollmentAsync)
            .WithName("ConfirmOwnTwoFactorEnrollment")
            .Produces<RecoveryCodesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        admin.MapPost("/me/two-factor/disable", DisableAsync)
            .WithName("DisableOwnTwoFactor")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        admin.MapPost("/me/two-factor/recovery-codes", RegenerateAsync)
            .WithName("RegenerateOwnRecoveryCodes")
            .Produces<RecoveryCodesResponse>(StatusCodes.Status200OK);

        // R6/D32 — Yönetici KENDİSİ DÂHİL başka bir personelin ikinci adımını sıfırlar.
        admin.MapPost("/staff/{staffId:guid}/two-factor-reset", ResetAsync)
            .WithName("ResetStaffTwoFactor")
            .RequireAuthorization(AdminSurface.ManagerPolicy)
            .Produces(StatusCodes.Status204NoContent);
    }

    private static async Task<Results<Ok<TwoFactorStatusResponse>, JsonHttpResult<Dictionary<string, object?>>>>
        GetStatusAsync(
            HttpContext httpContext,
            TwoFactorCommands commands,
            CancellationToken cancellationToken)
    {
        if (!TryReadStaffId(httpContext, out Guid staffId))
        {
            return IdentityProblems.From(new Error(IdentityErrors.NoSession));
        }

        Result<TwoFactorStatus> result = await commands.GetStatusAsync(staffId, cancellationToken);

        return result.IsFailure
            ? IdentityProblems.From(result.Error)
            : TypedResults.Ok(new TwoFactorStatusResponse(
                result.Value.Enrolled,
                result.Value.Required,
                result.Value.RemainingRecoveryCodes));
    }

    private static async Task<Results<Ok<TwoFactorEnrollmentResponse>, JsonHttpResult<Dictionary<string, object?>>>>
        StartEnrollmentAsync(
            HttpContext httpContext,
            TwoFactorCommands commands,
            CancellationToken cancellationToken)
    {
        if (!TryReadStaffId(httpContext, out Guid staffId))
        {
            return IdentityProblems.From(new Error(IdentityErrors.NoSession));
        }

        Result<TwoFactorEnrollmentStarted> result =
            await commands.StartEnrollmentAsync(staffId, cancellationToken);

        return result.IsFailure
            ? IdentityProblems.From(result.Error)
            : TypedResults.Ok(new TwoFactorEnrollmentResponse(result.Value.SecretUri, result.Value.Secret));
    }

    private static async Task<Results<Ok<RecoveryCodesResponse>, JsonHttpResult<Dictionary<string, object?>>>>
        ConfirmEnrollmentAsync(
            ConfirmTwoFactorRequest request,
            HttpContext httpContext,
            TwoFactorCommands commands,
            CancellationToken cancellationToken)
    {
        if (!TryReadStaffId(httpContext, out Guid staffId))
        {
            return IdentityProblems.From(new Error(IdentityErrors.NoSession));
        }

        Result<RecoveryCodeSet> result =
            await commands.ConfirmEnrollmentAsync(staffId, request.Code, cancellationToken);

        return result.IsFailure
            ? IdentityProblems.From(result.Error)
            : TypedResults.Ok(new RecoveryCodesResponse(result.Value.Codes));
    }

    private static async Task<Results<NoContent, JsonHttpResult<Dictionary<string, object?>>>> DisableAsync(
        DisableTwoFactorRequest request,
        HttpContext httpContext,
        TwoFactorCommands commands,
        CancellationToken cancellationToken)
    {
        if (!TryReadStaffId(httpContext, out Guid staffId))
        {
            return IdentityProblems.From(new Error(IdentityErrors.NoSession));
        }

        Result result = await commands.DisableAsync(
            staffId, request.CurrentPassword, request.Code, cancellationToken);

        return result.IsFailure ? IdentityProblems.From(result.Error) : TypedResults.NoContent();
    }

    private static async Task<Results<Ok<RecoveryCodesResponse>, JsonHttpResult<Dictionary<string, object?>>>>
        RegenerateAsync(
            RegenerateRecoveryCodesRequest request,
            HttpContext httpContext,
            TwoFactorCommands commands,
            CancellationToken cancellationToken)
    {
        if (!TryReadStaffId(httpContext, out Guid staffId))
        {
            return IdentityProblems.From(new Error(IdentityErrors.NoSession));
        }

        Result<RecoveryCodeSet> result =
            await commands.RegenerateRecoveryCodesAsync(staffId, request.Code, cancellationToken);

        return result.IsFailure
            ? IdentityProblems.From(result.Error)
            : TypedResults.Ok(new RecoveryCodesResponse(result.Value.Codes));
    }

    private static async Task<Results<NoContent, JsonHttpResult<Dictionary<string, object?>>>> ResetAsync(
        Guid staffId,
        HttpContext httpContext,
        TwoFactorCommands commands,
        CancellationToken cancellationToken)
    {
        if (!TryReadStaffId(httpContext, out Guid actorStaffId))
        {
            return IdentityProblems.From(new Error(IdentityErrors.NoSession));
        }

        Result result = await commands.ResetAsync(actorStaffId, staffId, cancellationToken);

        return result.IsFailure ? IdentityProblems.From(result.Error) : TypedResults.NoContent();
    }

    private static bool TryReadStaffId(HttpContext httpContext, out Guid staffId) =>
        Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out staffId);
}
