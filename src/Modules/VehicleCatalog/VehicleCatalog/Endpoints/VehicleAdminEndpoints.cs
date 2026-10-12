using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Identity.Contracts;
using CarGallery.Modules.VehicleCatalog.Application.Listings;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace CarGallery.Modules.VehicleCatalog.Endpoints;

/// <summary>
/// Panelin araç yüzeyi. Uçlar `AdminSurface.RoutePrefix` altında ve
/// `AuthenticatedPolicy` taşıyor — kök adı burada yeniden YAZILMIYOR, `0006`'nın sabitinden
/// geliyor. Kendi kökünü açan bir panel, C18'in rota tarayan testini atlardı (AC-2).
///
/// <para>Gövde İNCE (`docs/conventions.md` § 3): bağlama, komut çağrısı, sonuç eşleme.
/// İş mantığı yok.</para>
/// </summary>
internal static class VehicleAdminEndpoints
{
    internal static void MapVehicleAdminEndpoints(this IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder vehicles = builder
            .MapGroup($"{AdminSurface.RoutePrefix}/vehicles")
            .RequireAuthorization(AdminSurface.AuthenticatedPolicy);

        vehicles.MapPost("/", CreateAsync)
            .WithName("CreateVehicle")
            .Produces<CreateVehicleResponse>(StatusCodes.Status201Created);

        vehicles.MapGet("/", ListAsync)
            .WithName("ListVehicles")
            .Produces<PagedResult<VehicleAdminListItemDto>>(StatusCodes.Status200OK);

        // R15/AC-23 — özet AYRI bir okuma: sayı o anki sayfaya bağlı değil ve AC-25
        // liste yanıtının `PagedResult` zarfında kalmasını istiyor.
        vehicles.MapGet("/summary", SummaryAsync)
            .WithName("VehicleSummary")
            .Produces<VehicleAdminSummaryDto>(StatusCodes.Status200OK);

        vehicles.MapGet("/{vehicleId:guid}", FindAsync)
            .WithName("FindVehicle")
            .Produces<VehicleAdminDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        // `0012a` — aracın medya yüzeyi. İkisi de aynı yönetim kökünde ve aynı oturum
        // politikasını taşıyor; `0006`/C18'in rota TARAYAN testi ikisini de görüyor.
        vehicles.MapGet("/{vehicleId:guid}/media", MediaAsync)
            .WithName("FindVehicleMedia")
            .Produces<VehicleMediaDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        vehicles.MapPut("/{vehicleId:guid}/media", UpdateMediaAsync)
            .WithName("UpdateVehicleMedia")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status422UnprocessableEntity);

        // Durum geçişleri FİİL rotada. Kuralın kaynağı `docs/conventions.md` § 2 ve
        // örneği birebir bu: `POST /api/catalog/vehicles/{id}/publish`. Bu spec'in ayrı
        // bir kararı YOK — konvansiyon zaten söylüyor.
        foreach ((string yol, ListingOperation islem) in ListingOperations.All)
        {
            vehicles.MapPost($"/{{vehicleId:guid}}/{yol}", (
                    Guid vehicleId,
                    ListingTransitionCommands transitions,
                    ListingFeatureCommands features,
                    CancellationToken ct) =>
                    OperationAsync(vehicleId, islem, transitions, features, ct))
                .WithName($"VehicleListing{islem}")
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status409Conflict)
                .Produces(StatusCodes.Status422UnprocessableEntity);
        }

        // `0012c` **D18** — toplu yüzey ALTI işlemi taşıyor; rezerve ve rezervasyon
        // kaldırma burada yok ve yokluğun karşılığı `404`'tür (rota tanımlanmadı).
        // Sözcükler tekil yüzeyle AYNI tablodan geliyor: iki yüzey hiç ayrışamaz.
        foreach ((string yol, ListingOperation islem) in ListingOperations.Bulk)
        {
            vehicles.MapPost($"/bulk/{yol}", (
                    BulkListingBody body,
                    ListingBulkCommands command,
                    CancellationToken ct) =>
                    BulkAsync(islem, body, command, ct))
                .WithName($"VehicleListingBulk{islem}")
                .Produces<BulkListingResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status422UnprocessableEntity);
        }

        vehicles.MapPut("/{vehicleId:guid}", UpdateAsync)
            .WithName("UpdateVehicle")
            .Produces(StatusCodes.Status204NoContent);

        // Formun kurabilmesi için AÇIK opsiyonel alanlar ve tipleri (R6, R7).
        // Vitrin tarafındaki `GET /api/catalog/field-settings` AYRI bir yüzey: orası
        // ziyaretçinin gördüğü alanları söyler, burası personelin doldurduklarını.
        builder.MapGet($"{AdminSurface.RoutePrefix}/vehicle-fields", FormFieldsAsync)
            .WithName("ListVehicleFormFields")
            .RequireAuthorization(AdminSurface.AuthenticatedPolicy)
            .Produces<IReadOnlyList<VehicleAdminFieldDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> CreateAsync(
        CreateVehicleBody body,
        CreateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        Result<Guid> result = await command.ExecuteAsync(body.ToRequest(), cancellationToken);

        return result.IsFailure
            ? CatalogProblems.From(result.Error)
            : TypedResults.Created($"{AdminSurface.RoutePrefix}/vehicles/{result.Value}", new CreateVehicleResponse(result.Value));
    }

    private static async Task<Ok<PagedResult<VehicleAdminListItemDto>>> ListAsync(
        HttpContext httpContext,
        IVehicleAdminQueries queries,
        ShowcaseLocaleOptions locales,
        CancellationToken cancellationToken)
    {
        IQueryCollection query = httpContext.Request.Query;

        // Tanınmayan `sort` değeri HATA DEĞİL: adres çubuğundan gelen bir ipucu ve
        // varsayılana düşmesi personelin önüne hata ekranı koymaktan iyidir.
        VehicleAdminListSort sort = string.Equals(query["sort"], "recently-added", StringComparison.OrdinalIgnoreCase)
            ? VehicleAdminListSort.RecentlyAdded
            : VehicleAdminListSort.LastUpdated;

        VehicleAdminListFilter filter = new(
            locales.ResolveOrDefault(query["locale"].ToString()).Code,
            int.TryParse(query["page"], out int page) ? page : 1,
            int.TryParse(query["pageSize"], out int pageSize) ? pageSize : null,
            sort,
            // 0012b D19 — arşiv AÇIK bir filtreyle görünür. Tanınmayan değer `false`'a
            // düşüyor: `sort` ile aynı gerekçe — adres çubuğundan gelen bir ipucu, hata
            // ekranının karşılığı değil.
            ArsivIstendi(query));

        return TypedResults.Ok(await queries.ListAsync(filter, cancellationToken));
    }

    private static async Task<Ok<VehicleAdminSummaryDto>> SummaryAsync(
        HttpContext httpContext,
        IVehicleAdminQueries queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetSummaryAsync(
            ArsivIstendi(httpContext.Request.Query),
            cancellationToken));

    private static async Task<Ok<IReadOnlyList<VehicleAdminFieldDto>>> FormFieldsAsync(
        IVehicleAdminQueries queries,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await queries.GetFormFieldsAsync(cancellationToken));

    private static async Task<IResult> FindAsync(
        Guid vehicleId,
        IVehicleAdminQueries queries,
        CancellationToken cancellationToken)
    {
        VehicleAdminDetailDto? vehicle = await queries.FindAsync(vehicleId, cancellationToken);

        return vehicle is null
            ? CatalogProblems.From(Error.With(VehicleErrors.NotFound, "field", VehicleFieldNames.VehicleId))
            : TypedResults.Ok(vehicle);
    }

    /// <summary>
    /// `0012a`/AC-24 — aracın sıralı medyası, `Media`'nın durumlarıyla birleştirilmiş
    /// hâlde. Birleştirme SUNUCUDA (D10): panel tek çağrı yapıyor.
    /// </summary>
    private static async Task<IResult> MediaAsync(
        Guid vehicleId,
        IVehicleMediaQueries queries,
        CancellationToken cancellationToken)
    {
        VehicleMediaDto? sonuc = await queries.FindAsync(vehicleId, cancellationToken);

        return sonuc is null
            ? CatalogProblems.From(Error.With(VehicleErrors.NotFound, "field", VehicleFieldNames.VehicleId))
            : TypedResults.Ok(sonuc);
    }

    /// <summary>
    /// `0012a` D12/AC-2…9 — sıralı kimlik listesinin TAMAMINI yerine koyar. Bağlama,
    /// sıralama ve çıkarma tek idempotent istek; kapak sıranın sonucu (D2).
    ///
    /// <para>Boş liste geçerli: "bütün fotoğrafları çıkar" demek ve varlıkları
    /// SİLMEZ (R3).</para>
    /// </summary>
    private static async Task<IResult> UpdateMediaAsync(
        Guid vehicleId,
        UpdateVehicleMediaBody body,
        UpdateVehicleMediaCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        Result sonuc = await command.ExecuteAsync(
            vehicleId, body.MediaAssetIds ?? [], cancellationToken);

        return sonuc.IsFailure ? CatalogProblems.From(sonuc.Error) : TypedResults.NoContent();
    }

    /// <summary>
    /// `0012b` **D19** — arşiv AÇIK bir filtreyle görünür. Tanınmayan değer `false`'a
    /// düşüyor: `sort` ile aynı gerekçe — adres çubuğundan gelen bir ipucu, hata
    /// ekranının karşılığı değil. Liste ve ÖZET aynı bayrağı okuyor (**D35**).
    /// </summary>
    private static bool ArsivIstendi(IQueryCollection query) =>
        string.Equals(query["archived"], "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// `0012b`/`0012c` — tekil işlemin ince ucu: bağlama, komut çağrısı, sonuç eşleme.
    /// Karar agregatta (**C2**, Yasak Listesi 20).
    ///
    /// <para>Buradaki tek dallanma iki **KOMUT** arasında, iki durum arasında değil: öne
    /// çıkarma bir geçiş değildir ve kendi komutuna gider.</para>
    /// </summary>
    private static async Task<IResult> OperationAsync(
        Guid vehicleId,
        ListingOperation operation,
        ListingTransitionCommands transitions,
        ListingFeatureCommands features,
        CancellationToken cancellationToken)
    {
        Result sonuc = operation.IsFeature(out bool featured)
            ? await features.ExecuteAsync(vehicleId, featured, cancellationToken)
            : await transitions.ExecuteAsync(vehicleId, operation.ToAction(), cancellationToken);

        return sonuc.IsFailure ? CatalogProblems.From(sonuc.Error) : TypedResults.NoContent();
    }

    /// <summary>
    /// `0012c` **R4**/**D6** — toplu eylemin ucu. İstek düzeyi reddi `ProblemDetails`
    /// (`422`), satır düzeyi sonuçlar `200` + liste.
    ///
    /// <para><b>Hepsi başarısız olsa bile `200`</b>: `200` isteğin işlendiğini söyler,
    /// kayıtların başarılı olduğunu değil. Bedeli açıkça kabul edildi — istemci başarıyı
    /// HTTP kodundan okuyamaz, gövdeyi okumak zorundadır (`docs/conventions.md` § 5).</para>
    /// </summary>
    private static async Task<IResult> BulkAsync(
        ListingOperation operation,
        BulkListingBody body,
        ListingBulkCommands command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        Result<IReadOnlyList<BulkListingRow>> sonuc =
            await command.ExecuteAsync(operation, body.VehicleIds ?? [], cancellationToken);

        return sonuc.IsFailure
            ? CatalogProblems.From(sonuc.Error)
            : TypedResults.Ok(new BulkListingResponse(
                [
                    .. sonuc.Value.Select(satir => new BulkListingResultBody(
                        satir.VehicleId,
                        satir.Applied,
                        satir.ErrorCode,
                        satir.Parameters,
                        satir.ListingStatus))
                ]));
    }

    /// <summary>
    /// R2/C25 — düzenleme TAM DEĞİŞTİRMEDİR. Gövde çekirdek alanların tamamını taşır ve
    /// eksik gelen alan "dokunulmadı" değil GEÇERSİZ sayılır (AC-5): kısmi semantik
    /// "boş geldi = silindi mi, dokunulmadı mı" belirsizliğini üretirdi.
    /// </summary>
    private static async Task<IResult> UpdateAsync(
        Guid vehicleId,
        UpdateVehicleBody body,
        UpdateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);

        Result result = await command.ExecuteAsync(body.ToRequest(vehicleId), cancellationToken);

        return result.IsFailure ? CatalogProblems.From(result.Error) : TypedResults.NoContent();
    }
}

/// <summary>
/// İstek gövdesi. Para BİRİMİ yoktur — kurulum başına sabittir ve panelden değiştirilemez
/// (C3, P3); personel yalnız tutar gönderir.
/// </summary>
internal sealed record CreateVehicleBody(
    Guid BranchId,
    Guid VehicleModelId,
    string BodyType,
    string FuelType,
    string TransmissionType,
    string DrivetrainType,
    string TrimLevel,
    Dictionary<string, string>? Color,
    int ModelYear,
    int MileageKilometers,
    decimal? PriceAmount,
    bool PriceOnRequest,
    string? Vin)
{
    internal CreateVehicleRequest ToRequest() => new(
        BranchId,
        VehicleModelId,
        BodyType ?? string.Empty,
        FuelType ?? string.Empty,
        TransmissionType ?? string.Empty,
        DrivetrainType ?? string.Empty,
        TrimLevel ?? string.Empty,
        Color ?? [],
        ModelYear,
        MileageKilometers,
        PriceAmount,
        PriceOnRequest,
        Vin ?? string.Empty);
}

internal sealed record CreateVehicleResponse(Guid VehicleId);

/// <summary>
/// `0012a` D12 — sıralı kimlik listesinin tamamı. Gövde eksikse boş liste okunur:
/// "bütün fotoğrafları çıkar" geçerli bir istek (R3).
/// </summary>
internal sealed record UpdateVehicleMediaBody(IReadOnlyList<Guid>? MediaAssetIds);

/// <summary>
/// Düzenleme gövdesi. Şase TAŞINIR ama değiştirilemez (**D24**): gövdede olmasaydı
/// "değiştirme girişimi" ifade edilemez ve AC-8 ölçülemez olurdu.
/// </summary>
internal sealed record UpdateVehicleBody(
    Guid BranchId,
    Guid VehicleModelId,
    string BodyType,
    string FuelType,
    string TransmissionType,
    string DrivetrainType,
    string TrimLevel,
    Dictionary<string, string>? Color,
    /// <summary>
    /// Nullable — AC-5/C25. `int` olsaydı gövdede BULUNMAYAN alan `0` bağlanır ve
    /// kilometre sessizce sıfırlanırdı; `0` geçerli bir kilometre olduğu için istek de
    /// kabul edilirdi.
    /// </summary>
    int? ModelYear,
    int? MileageKilometers,
    decimal? PriceAmount,
    bool PriceOnRequest,
    string? Vin,
    Dictionary<string, string>? ListingTitle,
    Dictionary<string, string>? ListingDescription,
    List<UpdateVehicleOptionalValue>? OptionalValues)
{
    internal UpdateVehicleRequest ToRequest(Guid vehicleId) => new(
        vehicleId,
        BranchId,
        VehicleModelId,
        BodyType ?? string.Empty,
        FuelType ?? string.Empty,
        TransmissionType ?? string.Empty,
        DrivetrainType ?? string.Empty,
        TrimLevel ?? string.Empty,
        Color ?? [],
        ModelYear,
        MileageKilometers,
        PriceAmount,
        PriceOnRequest,
        Vin ?? string.Empty,
        ListingTitle ?? [],
        ListingDescription ?? [],
        [.. (OptionalValues ?? []).Select(item => new OptionalFieldInput(item.FieldKey, item.Value, item.Values))]);
}

internal sealed record UpdateVehicleOptionalValue(
    string FieldKey,
    string? Value,
    Dictionary<string, string>? Values);

/// <summary>
/// Toplu eylemin isteği. Seçim **araç kimliğiyle** taşınıyor (**D44**): `0012b`'nin uçları
/// araç kimliğiyle çalışıyor ve yönetim sözleşmesinde ilan kimliği diye bir yüzey yok.
/// </summary>
internal sealed record BulkListingBody(IReadOnlyList<Guid>? VehicleIds);

/// <summary>Araç başına sonuç satırı (**D45**). Şekil tek; doluluk değişir.</summary>
internal sealed record BulkListingResultBody(
    Guid VehicleId,
    bool Applied,
    string? ErrorCode,
    IReadOnlyDictionary<string, string>? Parameters,
    string? ListingStatus);

/// <summary>
/// Toplu eylemin yanıtı. Liste bir **zarf** içinde: çıplak dizi dönmek, yanıta bir gün
/// istek düzeyi bir alan eklemeyi (örneğin uygulanan sayısı) kırıcı bir değişiklik yapardı.
/// </summary>
internal sealed record BulkListingResponse(IReadOnlyList<BulkListingResultBody> Results);

/// <summary>
/// `Result`'ın hatasını RFC 7807 gövdesine çevirir. Gövde KARARLI bir `errorCode` ve
/// gerekirse hangi ALANIN düştüğünü taşır; çevrilmiş metin taşımaz — uç kendi
/// kataloğundan çevirir (C10, AC-42).
///
/// <para>`title` ve `detail` bilerek yok: ASP.NET'in varsayılan İngilizce başlığı
/// ziyaretçiye gösterilebilecek bir metin gibi görünür ve uçta yanlışlıkla basılabilir —
/// `0006`'da aynı karar verilmişti.</para>
/// </summary>
internal static class CatalogProblems
{
    internal static JsonHttpResult<Dictionary<string, object?>> From(Error error)
    {
        int status = error.Code switch
        {
            // AC-4 — var olmayan araç: istek geçerli, hedef yok.
            VehicleErrors.NotFound => StatusCodes.Status404NotFound,
            // AC-16 — tekillik ihlali bir DURUM ÇAKIŞMASIDIR: istek geçerli, dünya uygun değil.
            VehicleErrors.DuplicateVin => StatusCodes.Status409Conflict,
            VehicleErrors.BranchNotFound => StatusCodes.Status422UnprocessableEntity,
            // 0012b D26 — reddin HTTP karşılığı SABİT. A3'ün geçiş reddi bir DURUM
            // ÇAKIŞMASIDIR (`409`): istek geçerli, ilan o durumda değil. A2'nin ve R21'in
            // yayın reddi bir DOĞRULAMA hatasıdır (`422`): koşullar eksik. İkisini aynı
            // koda toplamak, panelin "satırı tazele" ile "eksikleri göster" arasında
            // seçim yapmasını imkânsız kılardı.
            Listing.InvalidTransition => StatusCodes.Status409Conflict,
            // 0012c C8/D49 — öne çıkarmanın reddi de bir DURUM ÇAKIŞMASIDIR: istek
            // geçerli, ilan arşivde. Kendi kodunu taşıyor çünkü panel bir SIRALAMA eylemi
            // için "geçersiz durum geçişi" derse personel olmayan bir geçişi arar.
            Listing.CannotFeatureArchived => StatusCodes.Status409Conflict,
            ConcurrencyErrors.Lost => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status422UnprocessableEntity,
        };

        Dictionary<string, object?> body = new(StringComparer.Ordinal)
        {
            ["type"] = "about:blank",
            ["status"] = status,
            ["errorCode"] = error.Code,
        };

        if (error.Parameters is { Count: > 0 })
        {
            foreach (KeyValuePair<string, string> parameter in error.Parameters)
            {
                body[parameter.Key] = parameter.Value;
            }
        }

        return TypedResults.Json(body, contentType: "application/problem+json", statusCode: status);
    }
}
