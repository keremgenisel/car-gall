using CarGallery.BuildingBlocks;
using CarGallery.Modules.Leads.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Leads.Endpoints;

/// <summary>
/// Controller kullanılmaz; endpoint gövdesi incedir: bağlama, handler çağrısı, sonuç
/// eşleme. İş mantığı yoktur (docs/conventions.md § 3, Yasak Listesi 20).
/// </summary>
internal static class LeadEndpoints
{
    /// <summary>
    /// Hız sınırı politikası modülün kendi adını taşır; kaydı <c>AddLeads</c>'de,
    /// uygulaması Host'un boru hattındadır. Host <c>UseRateLimiter()</c> çağırmazsa sınır
    /// SESSİZCE kaybolur — bu yüzden AC-32/AC-33 Host.Tests'te, gerçek boru hattında
    /// sınanır (0003'ün dersi: kablolanmamış davranış test edilmiş sayılmaz).
    /// </summary>
    internal const string RateLimitPolicy = "lead-capture";

    internal static void MapLeadEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/api/leads", CaptureAsync)
            .WithName("CaptureLead")
            .RequireRateLimiting(RateLimitPolicy)
            .Produces<CaptureLeadResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    /// <summary>
    /// C17'nin sayacını bölen anahtar. IP KİŞİSEL VERİDİR: yalnız bellekteki sayaca
    /// girer, kalıcı kayda ve loga yazılmaz (docs/conventions.md § 12).
    ///
    /// `X-Forwarded-For` burada OKUNMAZ: güvenilmeyen bir başlığa göre bölmek, sınırı
    /// istemcinin kapatabilmesi demektir. Ters proxy arkasında doğru IP'yi
    /// `ForwardedHeaders` ara katmanı yazar ve o, yalnızca AÇIKÇA listelenmiş proxy'lere
    /// güvenir (src/Host/Program.cs).
    /// </summary>
    private static string ResolveClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static async Task<Results<Created<CaptureLeadResponse>, JsonHttpResult<Dictionary<string, object?>>>> CaptureAsync(
        CaptureLeadRequest request,
        HttpContext httpContext,
        CaptureLead handler,
        CancellationToken cancellationToken)
    {
        // R17 — idempotentlik anahtarı BAŞLIKTA gelir (docs/conventions.md § 3), gövdede
        // değil: isteğin kendisine ait bir gerçektir, talebin içeriğine değil.
        string? idempotencyKey = httpContext.Request.Headers["Idempotency-Key"];

        Result<Guid> result = await handler.HandleAsync(
            new CaptureLeadCommand(
                request.ListingSlug,
                request.LeadType,
                request.Locale,
                request.FullName,
                request.Phone,
                request.Email,
                request.Message,
                request.ConsentGiven,
                idempotencyKey,
                ResolveClientKey(httpContext)),
            cancellationToken);

        if (result.IsFailure)
        {
            return LeadProblems.From(result.Error);
        }

        // D1 uyarınca talebin okuma yüzeyi YOK: `Created` bir konum başlığı taşımaz,
        // çünkü gösterilecek bir adres yok. Kimlik yalnız uçun "aynı talep mi" kararı
        // için döner.
        return TypedResults.Created((string?)null, new CaptureLeadResponse(result.Value));
    }
}
