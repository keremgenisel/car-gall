using System.Security.Claims;
using System.Text.Encodings.Web;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Contracts.Sessions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarGallery.Modules.Identity.Endpoints.Authentication;

internal sealed class SessionAuthenticationOptions : AuthenticationSchemeOptions;

/// <summary>
/// Oturumu HER İSTEKTE kaynaktan çözer (C16). Taşıyıcının içinden hiçbir iddia gelmez:
/// rol ve durum veritabanından okunur, bu yüzden pasifleştirme, rol değişikliği ve
/// parola değişikliği bir sonraki istekte etkilidir.
///
/// Şema burada, modülün içinde tanımlıdır; boru hattına takılması Host'un işidir
/// (docs/architecture.md § 2.F). Bu ayrım sessiz bir kayıp riski taşır — Host
/// `UseAuthentication()` çağırmazsa koruma kaybolur — ve bu yüzden `Host.Tests`
/// gerçek boru hattını sınar.
/// </summary>
internal sealed class SessionAuthenticationHandler(
    IOptionsMonitor<SessionAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IStaffSessionQueries sessions) : AuthenticationHandler<SessionAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>Panel kabuğunu basan Host bu anahtardan personelin dilini okur (R16).</summary>
    internal const string PreferredLocaleClaim = "cargallery:preferred_locale";

    internal const string SessionIdClaim = "cargallery:session_id";
    internal const string MustChangePasswordClaim = "cargallery:must_change_password";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = SessionCookie.Read(Context);

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        StaffSessionDto? session = await sessions.ResolveAsync(token, Context.RequestAborted);

        if (session is null)
        {
            // Çözülemeyen her durum aynı sonucu verir; hangi nedenle düştüğü söylenmez.
            return AuthenticateResult.NoResult();
        }

        ClaimsIdentity identity = new(
        [
            new Claim(ClaimTypes.NameIdentifier, session.StaffId.ToString()),
            new Claim(ClaimTypes.Name, session.DisplayName),
            new Claim(ClaimTypes.Role, session.Role),
            new Claim(SessionIdClaim, session.SessionId.ToString()),
            new Claim(PreferredLocaleClaim, session.PreferredLocale),
            new Claim(MustChangePasswordClaim, session.MustChangePassword ? "true" : "false"),
        ], AdminSurface.AuthenticationScheme);

        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), AdminSurface.AuthenticationScheme));
    }
}
