using System.Net;
using CarGallery.BuildingBlocks;

namespace CarGallery.Host.Localization;

internal sealed class LocalePrefixMiddleware(RequestDelegate next, LocalePrefixResolver resolver, IndexHtmlRenderer renderer)
{
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
