using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>Bir ilan üzerinde çalışan durum geçişleri (**A3**).</summary>
internal enum ListingAction
{
    Publish = 0,
    Unpublish = 1,
    Reserve = 2,
    Unreserve = 3,
    MarkSold = 4,
    Archive = 5,
}

/// <summary>
/// **C7** gereği agregatın üzerine kurulu komut katmanı: geçiş kararını `Listing` veriyor,
/// burası yalnız sırayı kuruyor — kilitle, oku, karar ver, yansıt, kaydet.
///
/// <para><b>Eşzamanlılık `InvariantGuard` ile</b> (**D8**, `0007` R3): agregat
/// <b>kilitten SONRA</b> okunuyor. Önce okunsaydı bekleyen isteğin elindeki nesne kilit
/// açıldığında bayat olurdu ve "zaten satıldı" koruması sessizce atlanırdı. Kaybeden
/// <b>kural gereği</b> reddediliyor — kilidin altında yeni durumu okuyor ve
/// `listing.invalid_status_transition` alıyor; `concurrency.lost` yalnız kilit zaman
/// aşımında döner ve ayrımı `0007`/AC-12 şart koşuyor.</para>
///
/// <para><b>Yansıtma her geçişte</b> (**R15**, **D12**) ve <b>aynı işlemde</b>: yayına
/// alma düğmesi hiçbir ziyaretçi sayfasını değiştirmeseydi Intent'in başarı ölçütü
/// ölçülemez kalırdı.</para>
/// </summary>
internal sealed class ListingTransitionCommands(
    VehicleCatalogDbContext context,
    VehicleCatalogUnitOfWork unitOfWork,
    ShowcaseProjectionWriter projection,
    IMediaAssetQueries mediaAssets,
    IVehicleModelNameResolver modelNames,
    InvariantGuardOptions guardOptions,
    IClock clock)
{
    internal Task<Result> ExecuteAsync(
        Guid vehicleId,
        ListingAction action,
        CancellationToken cancellationToken) =>
        InvariantGuard.ExecuteAsync(
            context,
            ListingRow(vehicleId),
            excluded: Guid.Empty,
            async (_, ct) =>
            {
                Vehicle? vehicle = await context.Vehicles
                    .Include(item => item.OptionalFieldValues)
                    .FirstOrDefaultAsync(item => item.Id == new VehicleId(vehicleId), ct);

                Listing? listing = vehicle is null
                    ? null
                    : await context.Listings.FirstOrDefaultAsync(item => item.VehicleId == vehicle.Id, ct);

                if (vehicle is null || listing is null)
                {
                    // Kod `VehicleErrors.NotFound` ile AYNI: "araç yok" tek bir olgudur ve ikinci
                    // bir kod, uçta iki ayrı çeviri anahtarı demekti.
                    return Result.Failure(Error.With(VehicleErrors.NotFound, "field", VehicleFieldNames.VehicleId));
                }

                Result sonuc = await UygulaAsync(vehicle, listing, action, ct);

                if (sonuc.IsFailure)
                {
                    return sonuc;
                }

                // D20 — geçiş aracın "son güncellenme" zamanını da ilerletir. Listenin tek
                // satırı iki agregattan besleniyor; yalnız `Listing`'e yazsaydık personel
                // bir aracı yayına alır ve araç listede yerinden kıpırdamazdı.
                vehicle.Touch(clock.UtcNow);

                await projection.ProjectAsync(vehicle, listing, ct);
                await unitOfWork.SaveChangesAsync(ct);

                return Result.Success();
            },
            guardOptions,
            cancellationToken);

    private async Task<Result> UygulaAsync(
        Vehicle vehicle,
        Listing listing,
        ListingAction action,
        CancellationToken cancellationToken) => action switch
    {
        ListingAction.Publish => await PublishAsync(vehicle, listing, cancellationToken),
        ListingAction.Unpublish => listing.Unpublish(),
        ListingAction.Reserve => listing.Reserve(),
        ListingAction.Unreserve => listing.ReturnToShowcase(),
        ListingAction.MarkSold => listing.MarkSold(),
        _ => Arsivle(vehicle, listing),
    };

    /// <summary>
    /// **D9** — arşivleme aracın bayrağını **AYNI işlemde** yazar. Ayrı bir işlemde
    /// yazılsaydı bayrak bayatlar ve şase tekilliği indeksi (`NOT is_archived`) yanlış
    /// kararı sessizce verirdi; ADR 0006 bunu `0012`'nin borcu olarak yazmıştı.
    /// </summary>
    private static Result Arsivle(Vehicle vehicle, Listing listing)
    {
        Result sonuc = listing.Archive();

        if (sonuc.IsSuccess)
        {
            vehicle.MarkArchived();
        }

        return sonuc;
    }

    /// <summary>
    /// Yayına alma: önce **A2**'nin ölçülebilen koşulları, sonra **R21**'in iki reddi,
    /// sonra geçiş. Eksiklerin <b>hepsi birlikte</b> dönüyor (**D2**).
    /// </summary>
    private async Task<Result> PublishAsync(Vehicle vehicle, Listing listing, CancellationToken cancellationToken)
    {
        if (listing.Status is not ListingStatus.Draft)
        {
            // Geçiş reddi A2'nin ÖNÜNE geçiyor: yayında bir ilana "medyası eksik" demek
            // yanlış cevaptır ve personeli olmayan bir eksikliği aramaya yollar. Kod da
            // farklı bir HTTP karşılığı taşıyor (`409` vs `422`, D26).
            return Result.Failure(new Error(Listing.InvalidTransition));
        }

        List<Error> eksikler = [];

        if (vehicle.AwaitingBranchAssignment)
        {
            eksikler.Add(new Error(PublishErrors.BranchUnassigned));
        }

        IReadOnlySet<Guid> hazir = vehicle.MediaAssetIds.Count == 0
            ? new HashSet<Guid>()
            : await mediaAssets.ReadyAmongAsync(vehicle.MediaAssetIds, cancellationToken);

        IReadOnlySet<string> zorunlu = await ZorunluAlanlarAsync(cancellationToken);

        eksikler.AddRange(PublishEligibility.Evaluate(vehicle, hazir, zorunlu));

        Result<ListingSlug> adres = await AdresAsync(vehicle, listing, cancellationToken);

        if (adres.IsFailure)
        {
            eksikler.Add(new Error(PublishErrors.SlugUnavailable));
        }

        if (eksikler.Count > 0)
        {
            return Result.Failure(PublishErrors.Aggregate(eksikler));
        }

        return listing.Publish(adres.Value, clock);
    }

    /// <summary>
    /// İlanın adresi. **D13** — ilk yayında bir kez üretiliyor; zaten adresi olan ilan
    /// kanonik adı hiç sormuyor ve yeniden yayın aynı adrese dönüyor.
    /// </summary>
    private async Task<Result<ListingSlug>> AdresAsync(
        Vehicle vehicle,
        Listing listing,
        CancellationToken cancellationToken)
    {
        if (listing.Slug is not null)
        {
            return Result.Success(listing.Slug.Value);
        }

        IReadOnlyDictionary<Guid, string> adlar =
            await modelNames.ResolveCanonicalNamesAsync([vehicle.VehicleModelId], cancellationToken);

        return adlar.TryGetValue(vehicle.VehicleModelId, out string? ad)
            ? ListingSlug.Create(ad, vehicle.ModelYear, listing.Id)
            : Result.Failure<ListingSlug>(new Error(ListingSlug.InvalidSource));
    }

    private async Task<IReadOnlySet<string>> ZorunluAlanlarAsync(CancellationToken cancellationToken) =>
        new HashSet<string>(
            await context.VehicleFieldSettings
                .AsNoTracking()
                .Where(setting => !setting.IsCore && setting.FormState == FieldFormState.Required)
                .Select(setting => setting.FieldKey)
                .ToListAsync(cancellationToken),
            StringComparer.Ordinal);

    /// <summary>
    /// Kilitlenecek satır: ilanın KENDİSİ. Yüklem `const`'tur ve değer parametredir;
    /// dizeye gömülmez (`InvariantLock`, `0007`/AC-9).
    /// </summary>
    private static InvariantLock ListingRow(Guid vehicleId) => new(
        VehicleCatalogDbContext.Schema,
        "listings",
        "vehicle_id = {0}",
        [vehicleId]);
}
