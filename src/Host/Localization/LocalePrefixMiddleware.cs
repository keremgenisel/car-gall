using System.Net;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Identity.Contracts.Sessions;

namespace CarGallery.Host.Localization;

internal sealed class LocalePrefixMiddleware(RequestDelegate next, LocalePrefixResolver resolver, IndexHtmlRenderer renderer)
{
    /// <summary>
    /// Yönetim panelinin uç tarafındaki kökü (C13: aynı Vite uygulamasında `/admin` rota
    /// ağacı). API kökü `AdminSurface.RoutePrefix`'tedir; bu, tarayıcının gittiği adres.
    /// </summary>
    internal const string AdminPathPrefix = "/admin";

    /// <summary>`0014` **D9** — listenin adresi `/{locale}/cars`; ilan detayıyla aynı yol parçası.</summary>
    internal const string ListSegment = "cars";

    /// <summary>`0014` **D17** — listenin okuduğu üç parametre; başkası (`utm_source`) yönlendirmez.</summary>
    private static readonly string[] ListParameters = ["bodyType", "page", "pageSize"];

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

        // 0014 D17, D21 — liste `/{locale}/cars`'a taşındı. Eski liste adresi (`/{locale}/`)
        // listenin okuduğu bir parametre taşıyorsa ziyaretçi AYNI sorguyla listeye gider.
        // HTTP yönlendirmesi tarayıcı geçmişine ara kayıt eklemez (AC-7) ve ana sayfa hiç
        // yüklenmez. Sorgu bayt bayt taşınır: normalizasyon `0001` C14'ün işi (AC-70).
        if (IsLocaleRoot(path, firstSegment) && CarriesListParameter(context.Request.Query))
        {
            context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
            context.Response.Headers.Location = $"/{firstSegment}/{ListSegment}{context.Request.QueryString.Value}";
            return;
        }

        // `0017` D13/D27 — tek parçalı içerik adresi ESKİ bir slug'sa güncel adrese 301.
        // Okuma yalnız ayrılmış olmayan tek parçalı yolda yapılır: ana sayfa, liste ve ilan
        // detayı `Content`'e hiç sormaz (AC-42).
        if (ContentSlugOf(path, firstSegment) is string slug)
        {
            string? guncel = await context.RequestServices
                .GetRequiredService<IContentPublicQueries>()
                .FindCurrentSlugAsync(firstSegment, slug, context.RequestAborted);

            if (guncel is not null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.MovedPermanently;
                context.Response.Headers.Location =
                    $"/{firstSegment}/{Uri.EscapeDataString(guncel)}{context.Request.QueryString.Value}";
                return;
            }
        }

        Locale locale = resolver.Resolve(firstSegment);
        await renderer.WriteAsync(context, locale);
    }

    /// <summary>
    /// `0017` — <c>/{locale}/{x}</c> biçimindeki yolun <c>x</c>'i, ayrılmış değilse; değilse
    /// <c>null</c>. Ayrılmış liste <c>Content.Contracts</c>'tadır (C11); liste bölümü
    /// (<see cref="ListSegment"/>) o listede olmak zorunda (AC-50).
    /// </summary>
    internal static string? ContentSlugOf(PathString path, string localeSegment)
    {
        string[] parcalar = (path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (parcalar.Length != 2 || parcalar[0] != localeSegment)
        {
            return null;
        }

        string aday = Uri.UnescapeDataString(parcalar[1]);

        return ContentReservedSlugs.Contains(aday) ? null : aday;
    }

    private static bool IsLocaleRoot(PathString path, string localeSegment) =>
        path.Value is { } value && (value == $"/{localeSegment}" || value == $"/{localeSegment}/");

    /// <summary>
    /// Anahtar eşleşmesi ORDİNAL. `IQueryCollection.ContainsKey` harfe duyarsızdır; uç ise
    /// `bodyType`'ı harfe duyarlı okur (`listUrlState.ts`). Duyarsız eşleşme `?BodyType=suv`'u
    /// liste parametresi sayıp ziyaretçiyi filtresiz listeye 301'liyordu — ve 301 tarayıcıda
    /// kalıcı önbelleğe girer (QA BULGU-1). Karar, ucun okuduğu anahtarla aynı biçimde verilir.
    /// </summary>
    private static bool CarriesListParameter(IQueryCollection query) =>
        query.Keys.Any(key => ListParameters.Contains(key, StringComparer.Ordinal));
}
