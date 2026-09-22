using System.Security.Claims;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.Identity.Contracts.Sessions;

namespace CarGallery.Host.Localization;

/// <summary>
/// R16/AC-54/AC-55 — **yönetim paneli personelin kendi tercih ettiği dilde açılır;
/// ziyaretçi locale'inden bağımsızdır.** Vitrin dili URL önekinden gelir, panel dili
/// oturumdan; ikisi ayrı eksendir ve karıştırmak, Arapça vitrinden panele geçen Türk
/// danışmanın paneli Arapça görmesine yol açar (Tuzak 5).
///
/// Yön `&lt;html dir&gt;` ile SUNUCUDAN basılır, uçta yeniden hesaplanmaz
/// (docs/frontend.md § 6). Bu yüzden dilin sunucuda biliniyor olması gerekiyor ve bilgi
/// yalnızca oturumda var — kabuk bu yüzden oturumu çözmek zorunda. Bu bir kompozisyon
/// çağrısıdır: host yalnız public contract'ı çağırıp birleştirir, kendi iş kuralını
/// yazmaz (docs/architecture.md § 2.F).
///
/// Oturum yoksa — giriş ekranı — kurulum varsayılan diline düşülür. Giriş yapmamış
/// birinin tercih ettiği dil diye bir şey yoktur.
/// </summary>
internal sealed class AdminShellRenderer(
    IndexHtmlRenderer renderer,
    LocalePrefixResolver resolver)
{
    internal async Task WriteAsync(HttpContext context, IStaffSessionQueries sessions)
    {
        Locale locale = await ResolveLocaleAsync(context, sessions);

        await renderer.WriteAsync(context, locale);
    }

    private async Task<Locale> ResolveLocaleAsync(HttpContext context, IStaffSessionQueries sessions)
    {
        if (!context.Request.Cookies.TryGetValue(AdminSurface.SessionCookieName, out string? token))
        {
            return resolver.Default;
        }

        StaffSessionDto? session = await sessions.ResolveAsync(token, context.RequestAborted);

        if (session is null)
        {
            return resolver.Default;
        }

        Result<Locale> preferred = Locale.Create(session.PreferredLocale);

        // Etkin olmayan bir dil kaydedilemez (AC-40), ama yapılandırma daraltılmış olabilir;
        // o hâlde de panel açılmalıdır.
        return preferred.IsSuccess && resolver.IsEnabled(preferred.Value.Code)
            ? preferred.Value
            : resolver.Default;
    }
}
