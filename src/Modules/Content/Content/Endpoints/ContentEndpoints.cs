using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Content.Application;
using CarGallery.Modules.Content.Contracts.Pages;
using CarGallery.Modules.Content.Domain;
using CarGallery.Modules.Identity.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.Content.Endpoints;

/// <summary>
/// `0017` — `Content`'in uçları. Okuma public'tir ve yalnız yayındaki dili döner; yazma
/// `AdminSurface` kökünde ve `ContentPolicy`'dedir: Yönetici + İçerik Editörü, Satış
/// Danışmanı `403`, oturumsuz `401` (C4, D25, AC-109, AC-110).
/// </summary>
internal static class ContentEndpoints
{
    /// <summary>Uçları bağlar.</summary>
    internal static void MapContentEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder vitrin = builder.MapGroup("/api/content");
        vitrin.MapGet("/pages/{slug}", FindPageAsync);
        vitrin.MapGet("/navigation", NavigationAsync);

        RouteGroupBuilder panel = builder.MapGroup($"{AdminSurface.RoutePrefix}/content")
            .RequireAuthorization(AdminSurface.ContentPolicy);

        panel.MapGet("/pages", async (ContentAdminQueries queries, CancellationToken ct) =>
            TypedResults.Ok(await queries.ListPagesAsync(ct)));
        panel.MapGet("/pages/{id:guid}", FindAdminPageAsync);
        panel.MapPost("/pages", CreatePageAsync);
        panel.MapPut("/pages/{id:guid}", SavePageAsync);
        panel.MapPost("/pages/{id:guid}/publish", (Guid id, LocaleActionBody body, ContentCommands commands, CancellationToken ct) =>
            VersionAsync(commands.PublishPageAsync(id, body.Version, body.Locale ?? string.Empty, ct)));
        panel.MapPost("/pages/{id:guid}/unpublish", (Guid id, LocaleActionBody body, ContentCommands commands, CancellationToken ct) =>
            VersionAsync(commands.UnpublishPageAsync(id, body.Version, body.Locale ?? string.Empty, ct)));
        panel.MapDelete("/pages/{id:guid}", (Guid id, int version, ContentCommands commands, CancellationToken ct) =>
            NoContentAsync(commands.DeletePageAsync(id, version, ct)));

        panel.MapGet("/faq", async (ContentAdminQueries queries, CancellationToken ct) =>
            TypedResults.Ok(await queries.ListFaqAsync(ct)));
        panel.MapPost("/faq", CreateFaqAsync);
        panel.MapPut("/faq/{id:guid}", (Guid id, SaveFaqBody body, ContentCommands commands, CancellationToken ct) =>
            VersionAsync(commands.SaveFaqEntryAsync(id, body.Version, FaqTexts(body.Locales), ct)));
        panel.MapPost("/faq/{id:guid}/publish", (Guid id, LocaleActionBody body, ContentCommands commands, CancellationToken ct) =>
            VersionAsync(commands.PublishFaqEntryAsync(id, body.Version, body.Locale ?? string.Empty, ct)));
        panel.MapPost("/faq/{id:guid}/unpublish", (Guid id, LocaleActionBody body, ContentCommands commands, CancellationToken ct) =>
            VersionAsync(commands.UnpublishFaqEntryAsync(id, body.Version, body.Locale ?? string.Empty, ct)));
        panel.MapDelete("/faq/{id:guid}", (Guid id, int version, ContentCommands commands, CancellationToken ct) =>
            NoContentAsync(commands.DeleteFaqEntryAsync(id, version, ct)));
        panel.MapPost("/faq/{id:guid}/move", (Guid id, MoveFaqBody body, ContentCommands commands, CancellationToken ct) =>
            NoContentAsync(commands.MoveFaqEntryAsync(id, string.Equals(body.Direction, "up", StringComparison.Ordinal), ct)));

        // `0022` R9 · D8 — ana sayfa metinleri aynı politikada: Yönetici + İçerik Editörü.
        panel.MapGet("/home", async (HomeTextQueries queries, CancellationToken ct) =>
            TypedResults.Ok(await queries.GetForAdminAsync(ct)));
        panel.MapPut("/home", (SaveHomeTextsBody body, HomeTextCommands commands, CancellationToken ct) =>
            VersionAsync(commands.SaveAsync(body.Version, HomeTextInputs(body.Locales), ct)));
    }

    private static async Task<Results<Ok<ContentPagePublicDto>, NotFound>> FindPageAsync(
        string slug,
        string? locale,
        IContentPublicQueries queries,
        CancellationToken cancellationToken)
    {
        ContentPagePublicDto? page = await queries.FindBySlugAsync(locale ?? string.Empty, slug, cancellationToken);
        return page is null ? TypedResults.NotFound() : TypedResults.Ok(page);
    }

    private static async Task<Ok<IReadOnlyList<ContentNavigationLinkDto>>> NavigationAsync(
        string? locale,
        IContentPublicQueries queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.NavigationAsync(locale ?? string.Empty, cancellationToken));

    private static async Task<Results<Ok<AdminPageDto>, NotFound>> FindAdminPageAsync(
        Guid id,
        ContentAdminQueries queries,
        CancellationToken cancellationToken)
    {
        AdminPageDto? page = await queries.FindPageAsync(id, cancellationToken);
        return page is null ? TypedResults.NotFound() : TypedResults.Ok(page);
    }

    private static async Task<IResult> CreatePageAsync(CreatePageBody body, ContentCommands commands, CancellationToken cancellationToken)
    {
        Result<(Guid Id, int Version)> sonuc = await commands.CreatePageAsync(PageTexts(body.Locales), cancellationToken);

        return sonuc.IsSuccess
            ? TypedResults.Created($"{AdminSurface.RoutePrefix}/content/pages/{sonuc.Value.Id}", new CreatedBody(sonuc.Value.Id, sonuc.Value.Version))
            : ContentProblems.From(sonuc.Error);
    }

    private static Task<IResult> SavePageAsync(Guid id, SavePageBody body, ContentCommands commands, CancellationToken cancellationToken) =>
        VersionAsync(commands.SavePageAsync(id, body.Version, PageTexts(body.Locales), cancellationToken));

    private static async Task<IResult> CreateFaqAsync(CreateFaqBody body, ContentCommands commands, CancellationToken cancellationToken)
    {
        Result<(Guid Id, int Version)> sonuc = await commands.CreateFaqEntryAsync(FaqTexts(body.Locales), cancellationToken);

        return sonuc.IsSuccess
            ? TypedResults.Created($"{AdminSurface.RoutePrefix}/content/faq/{sonuc.Value.Id}", new CreatedBody(sonuc.Value.Id, sonuc.Value.Version))
            : ContentProblems.From(sonuc.Error);
    }

    private static async Task<IResult> VersionAsync(Task<Result<int>> islem)
    {
        Result<int> sonuc = await islem;
        return sonuc.IsSuccess ? TypedResults.Ok(new VersionBody(sonuc.Value)) : ContentProblems.From(sonuc.Error);
    }

    private static async Task<IResult> NoContentAsync(Task<Result> islem)
    {
        Result sonuc = await islem;
        return sonuc.IsSuccess ? TypedResults.NoContent() : ContentProblems.From(sonuc.Error);
    }

    private static List<PageTextInput> PageTexts(List<PageTextBody>? locales) =>
        [.. (locales ?? []).Select(item => new PageTextInput(item.Locale ?? string.Empty, item.Title, item.Body, item.Slug))];

    private static List<HomeTextInput> HomeTextInputs(List<HomeTextBody>? locales) =>
    [
        .. (locales ?? []).Select(item => new HomeTextInput(
            item.Locale ?? string.Empty,
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [HomeTextField.IntroTitleKey] = item.IntroTitle,
                [HomeTextField.IntroBodyKey] = item.IntroBody,
                [HomeTextField.FormTitleKey] = item.FormTitle,
                [HomeTextField.FeaturedTitleKey] = item.FeaturedTitle,
                [HomeTextField.EmptyTitleKey] = item.EmptyTitle,
                [HomeTextField.EmptyBodyKey] = item.EmptyBody,
            })),
    ];

    private static List<FaqTextInput> FaqTexts(List<FaqTextBody>? locales) =>
        [.. (locales ?? []).Select(item => new FaqTextInput(item.Locale ?? string.Empty, item.Question, item.Answer))];
}

/// <summary>Bir dilin sayfa metni.</summary>
internal sealed record PageTextBody(string? Locale, string? Title, string? Body, string? Slug);

/// <summary>Serbest sayfa oluşturma gövdesi.</summary>
internal sealed record CreatePageBody(List<PageTextBody>? Locales);

/// <summary>Sayfa kaydetme gövdesi; <c>Version</c> okunan sürümdür (R10).</summary>
internal sealed record SavePageBody(int Version, List<PageTextBody>? Locales);

/// <summary>Dil başına eylem (yayına alma, kaldırma).</summary>
internal sealed record LocaleActionBody(int Version, string? Locale);

/// <summary>Bir dilin SSS metni.</summary>
internal sealed record FaqTextBody(string? Locale, string? Question, string? Answer);

/// <summary>SSS girdisi oluşturma gövdesi.</summary>
internal sealed record CreateFaqBody(List<FaqTextBody>? Locales);

/// <summary>SSS girdisi kaydetme gövdesi.</summary>
internal sealed record SaveFaqBody(int Version, List<FaqTextBody>? Locales);

/// <summary>SSS sırası: <c>up</c> ya da <c>down</c>.</summary>
internal sealed record MoveFaqBody(string? Direction);

/// <summary>`0022` — bir dilin ana sayfa metinleri; <c>null</c> ve boşluk = boş (varsayılana düşer).</summary>
internal sealed record HomeTextBody(
    string? Locale,
    string? IntroTitle,
    string? IntroBody,
    string? FormTitle,
    string? FeaturedTitle,
    string? EmptyTitle,
    string? EmptyBody);

/// <summary>`0022` — ana sayfa metinlerini kaydetme gövdesi; <c>Version</c> okunan sürümdür (R8).</summary>
internal sealed record SaveHomeTextsBody(int Version, List<HomeTextBody>? Locales);

/// <summary>Yazmanın yeni sürümü.</summary>
internal sealed record VersionBody(int Version);

/// <summary>Oluşturulanın kimliği ve sürümü.</summary>
internal sealed record CreatedBody(Guid Id, int Version);

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir: kararlı <c>errorCode</c> ve alan
/// parametreleri; çevrilmiş metin yok (C5, AC-120).
/// </summary>
internal static class ContentProblems
{
    /// <summary>Hata → problem gövdesi.</summary>
    internal static IResult From(Error error)
    {
        int status = error.Code switch
        {
            ContentErrors.PageNotFound or ContentErrors.FaqEntryNotFound => StatusCodes.Status404NotFound,
            ConcurrencyErrors.Lost => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        Dictionary<string, object?> body = new(StringComparer.Ordinal)
        {
            ["type"] = "about:blank",
            ["status"] = status,
            ["errorCode"] = error.Code,
        };

        foreach (KeyValuePair<string, string> parameter in error.Parameters ?? new Dictionary<string, string>())
        {
            body[parameter.Key] = parameter.Value;
        }

        return TypedResults.Json(body, contentType: "application/problem+json", statusCode: status);
    }
}
