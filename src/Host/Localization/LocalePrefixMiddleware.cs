using System.Net;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Identity.Contracts.Sessions;

namespace CarGallery.Host.Localization;

internal sealed class LocalePrefixMiddleware(RequestDelegate next, LocalePrefixResolver resolver, IndexHtmlRenderer renderer)
{
    /// <summary>
    /// Yönetim panelinin uç tarafındaki kökü (C13: aynı Vite uygulamasında `/admin` rota
    /// ağacı). API kökü `AdminSurface.RoutePrefix`'tedir; bu, tarayıcının gittiği adres.
    /// </summary>
    internal const string AdminPathPrefix = "/admin";

    public async Task InvokeAsync(HttpContext context)
    {
        PathString path = context.Request.Path;

        // Bir endpoint zaten eşleştiyse bu bir SPA rotası değildir; karışılmaz.
        // Yalnız "/api" kontrolü yetmez: modüllerin tanımladığı her rota (örn. medya)
        // aksi hâlde locale öneki sanılıp 404 olurdu.
        if (context.GetEndpoint() is not null)
        {
            await next(context);
            return;
        }

        // API rotaları locale öneki taşımaz (docs/conventions.md § 2).
        if (path.StartsWithSegments("/api"))
        {
            await next(context);
            return;
        }

        // Yönetim paneli locale ÖNEKİ TAŞIMAZ: dili URL'den değil personelin tercihinden
        // gelir (R16). Bu dal olmasaydı `/admin` ilk segmenti locale sanılıp 404 olurdu.
        if (path.StartsWithSegments(AdminPathPrefix))
        {
            await context.RequestServices
                .GetRequiredService<AdminShellRenderer>()
                .WriteAsync(
                    context,
                    context.RequestServices.GetRequiredService<IStaffSessionQueries>());

            return;
        }

        if (!path.HasValue || path == "/")
        {
            context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            context.Response.Headers.Location = $"/{resolver.Default.Code}/";
            return;
        }

        string firstSegment = path.Value.TrimStart('/').Split('/')[0];

        // Statik varlıklar (uzantılı yollar) locale önekinin dışındadır.
        if (firstSegment.Contains('.', StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        if (!resolver.IsEnabled(firstSegment))
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        Locale locale = resolver.Resolve(firstSegment);
        await renderer.WriteAsync(context, locale);
    }
}
