using CarGallery.BuildingBlocks;
using CarGallery.Modules.Notifications.Application.Newsletter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Notifications.Endpoints;

/// <summary>Abone olma isteğinin gövdesi. Dil gövdede açıkça gelir (**D14**).</summary>
internal sealed record SubscribeRequest(string? Email, string? Locale);

/// <summary>Teyit ve çıkış isteğinin gövdesi: bağlantıdaki anahtar (**D26**).</summary>
internal sealed record NewsletterTokenRequest(string? Token);

/// <summary>
/// **D5** — abone olma isteğinin TEK cevabı: dört durumun dördünde de aynı gövde (AC-16).
/// Adresin durumu hakkında hiçbir alan taşımaz.
/// </summary>
internal sealed record SubscribeResponse(string Status)
{
    internal static readonly SubscribeResponse Accepted = new("accepted");
}

/// <summary>Teyit ve çıkışın başarı gövdesi.</summary>
internal sealed record NewsletterLinkResponse(string Status);

/// <summary>
/// `0018` — `Notifications`'ın İLK public ucu (**D20**). Endpoint gövdesi incedir: bağlama,
/// handler çağrısı, sonuç eşleme (docs/conventions.md § 3).
///
/// <para>Yaprak konumu korunuyor (C12): uçlar ziyaretçiye açıktır; hiçbir MODÜL bu modüle
/// senkron bağlanmaz.</para>
/// </summary>
internal static class NewsletterEndpoints
{
    /// <summary>
    /// **D9** — kaba istek sınırının politikası. Kaydı <c>AddNotifications</c>'ta, uygulaması
    /// Host'un <c>UseRateLimiter()</c> satırında; satır silinirse sınır sessizce kaybolur ve bu
    /// yüzden AC-14 Host.Tests'te, gerçek boru hattında sınanır.
    /// </summary>
    internal const string RateLimitPolicy = "newsletter-subscribe";

    internal static void MapNewsletterEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder group = builder.MapGroup("/api/notifications/newsletter");

        group.MapPost("/subscriptions", SubscribeAsync)
            .WithName("SubscribeToNewsletter")
            .RequireRateLimiting(RateLimitPolicy)
            .Produces<SubscribeResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/confirmations", ConfirmAsync)
            .WithName("ConfirmNewsletterSubscription")
            .Produces<NewsletterLinkResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/unsubscriptions", UnsubscribeAsync)
            .WithName("UnsubscribeFromNewsletter")
            .Produces<NewsletterLinkResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Geçerli istek bütçesinin anahtarı. IP KİŞİSEL VERİDİR: yalnız bellekteki sayaca girer,
    /// kalıcı kayda ve loga yazılmaz (AC-15). `X-Forwarded-For` burada okunmaz; ters proxy
    /// arkasında doğru adresi Host'un `ForwardedHeaders` katmanı yazar.
    /// </summary>
    private static string ClientKeyOf(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async Task<Results<Accepted<SubscribeResponse>, JsonHttpResult<Dictionary<string, object?>>>> SubscribeAsync(
        SubscribeRequest request,
        HttpContext httpContext,
        SubscribeToNewsletter handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(
            new SubscribeCommand(request.Email, request.Locale, ClientKeyOf(httpContext)),
            cancellationToken);

        return result.IsFailure
            ? NewsletterProblems.From(result.Error)
            : TypedResults.Accepted((string?)null, SubscribeResponse.Accepted);
    }

    private static async Task<Results<Ok<NewsletterLinkResponse>, JsonHttpResult<Dictionary<string, object?>>>> ConfirmAsync(
        NewsletterTokenRequest request,
        ConfirmSubscription handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(request.Token, cancellationToken);

        return result.IsFailure
            ? NewsletterProblems.From(result.Error)
            : TypedResults.Ok(new NewsletterLinkResponse("confirmed"));
    }

    private static async Task<Results<Ok<NewsletterLinkResponse>, JsonHttpResult<Dictionary<string, object?>>>> UnsubscribeAsync(
        NewsletterTokenRequest request,
        UnsubscribeFromNewsletter handler,
        CancellationToken cancellationToken)
    {
        Result result = await handler.HandleAsync(request.Token, cancellationToken);

        return result.IsFailure
            ? NewsletterProblems.From(result.Error)
            : TypedResults.Ok(new NewsletterLinkResponse("unsubscribed"));
    }
}
