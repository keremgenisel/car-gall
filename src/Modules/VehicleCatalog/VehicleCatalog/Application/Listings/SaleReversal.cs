using CarGallery.BuildingBlocks;
using CarGallery.BuildingBlocks.Concurrency;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Application.Vehicles;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>`0023` — satışı geri almanın istek düzeyi kararlı kodları.</summary>
internal static class SaleReversalErrors
{
    /// <summary>
    /// **D16** — pencerenin gösterdiği hedef ile sunucunun şimdi hesapladığı hedef farklı
    /// (pencere açıkken şube kapandı, fotoğraf silindi). Bir DURUM ÇAKIŞMASIDIR (`409`):
    /// istek geçerli, dünya değişti. Yanıt güncel hedefi `targetStatus` parametresinde
    /// taşır ki yönetici onu görsün.
    /// </summary>
    internal const string TargetChanged = "listing.sale_reversal.target_changed";

    /// <summary>
    /// İstek beklenen durumu taşımıyor ya da tanınmayan bir değer taşıyor. **D16** beklenen
    /// durumu isteğin parçası yaptı; onsuz bir geri alma "ne olacağını görmeden onaylamak"
    /// demektir.
    /// </summary>
    internal const string ExpectedStatusInvalid = "listing.sale_reversal.expected_status_invalid";
}

/// <summary>
/// Satışı geri almanın tek sorusunun cevabı: araç <b>bugün</b> vitrine çıkabilir mi?
/// </summary>
/// <param name="BranchClosed">Aracın şubesi kalıcı kapatılmış (ya da bulunamıyor) — **D12**.</param>
/// <param name="Reasons">
/// Vitrine çıkamamanın nedenleri, yayın reddinin kodlarıyla (`PublishErrors`). Boşsa
/// araç vitrine dönebilir. Pencere bu kodları "neden Taslak" diye gösterir.
/// </param>
/// <param name="MissingFields">Zorunlu olup boş kalan alan anahtarları (**D13**).</param>
internal sealed record SaleRevertAssessment(
    bool BranchClosed,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> MissingFields)
{
    internal bool CanReturnToShowcase => Reasons.Count == 0;
}

/// <summary>
/// `0023` **D5**, **D12**, **D13** — geri alınan ilanın vitrine dönüp dönemeyeceğini ölçen
/// TEK yer. Onay penceresinin önizlemesi ve geri alma komutu ikisi de buradan geçer; iki
/// ayrı hesap, pencerenin bir şey vaat edip sunucunun başka bir şey yapması demekti.
///
/// <para><b>Şube durumu `Dealership`'ten okunur</b> (**D12**), aracın bayrağından değil:
/// A8 satılmış ilanları atlıyor ve bayrak bu durumda hiç yazılmamış olur. Yalnız bayrağa
/// bakmak aracı kapalı bir şubeyle vitrine çıkarırdı. Okuma § 2.A'nın meşru biçimi —
/// çağıranın işlemi için zorunlu bir doğrulama, tek atlama.</para>
/// </summary>
internal sealed class SaleRevertAssessor(
    VehicleCatalogDbContext context,
    IBranchQueries branches,
    IMediaAssetQueries mediaAssets,
    ShowcaseLocaleOptions locales)
{
    /// <summary>
    /// `Dealership`'in kalıcı kapatma kodu. Sözleşme durumu kod olarak taşıyor
    /// (`BranchSummaryDto.Status`) ve bu modülün o enum'u görmesi yasak (Yasak Listesi);
    /// karşılaştırma bu yüzden yayınlanmış koda karşı.
    /// </summary>
    internal const string ClosedBranchStatus = "Closed";

    internal async Task<SaleRevertAssessment> AssessAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        List<string> nedenler = [];
        bool subeKapali = false;

        if (vehicle.AwaitingBranchAssignment)
        {
            nedenler.Add(PublishErrors.BranchUnassigned);
        }
        else
        {
            // Locale yalnız okuma yüzeyinin imzası için; ad kullanılmıyor, durum kodu
            // locale'den bağımsız. Varsayılan dil kurulum yapılandırmasından gelir.
            BranchSummaryDto? sube = await branches.FindAsync(vehicle.BranchId, locales.Default.Code, cancellationToken);

            // Bulunamayan şube A1'e göre olmaması gereken bir hâl; vitrine çıkmak yerine
            // kapalı şube gibi davranmak "sessizce vitrinde kalmazlar"ın güvenli yanı.
            if (sube is null || string.Equals(sube.Status, ClosedBranchStatus, StringComparison.Ordinal))
            {
                subeKapali = true;
                nedenler.Add(PublishErrors.BranchUnassigned);
            }
        }

        IReadOnlySet<Guid> hazir = vehicle.MediaAssetIds.Count == 0
            ? new HashSet<Guid>()
            : await mediaAssets.ReadyAmongAsync(vehicle.MediaAssetIds, cancellationToken);

        IReadOnlySet<string> zorunlu = await PublishEligibility.RequiredFieldKeysAsync(context, cancellationToken);

        IReadOnlyList<Error> eksikler = PublishEligibility.Evaluate(vehicle, hazir, zorunlu);

        nedenler.AddRange(eksikler.Select(eksik => eksik.Code));

        return new SaleRevertAssessment(
            subeKapali,
            nedenler,
            PublishEligibility.MissingRequiredFields(vehicle, zorunlu));
    }
}

/// <summary>Satışı geri alma isteği. Personel kimliği ve adı oturumdan gelir, gövdeden değil.</summary>
internal sealed record RevertSaleRequest(
    Guid VehicleId,
    string? Reason,
    string? ExpectedStatus,
    Guid StaffId,
    string StaffDisplayName);

/// <summary>
/// `0023` — satışı geri alma komutu. `ListingTransitionCommands`'ın sırası: kilitle, oku,
/// karar ver, yansıt, kaydet (**C1**).
///
/// <para><b>Aynı satır kilidi</b> (`ListingLock`): geri alma ile arşivleme, ve iki geri
/// alma, aynı ilan üzerinde serileşir (**R11**, `0007`). Kaybeden kilidin altında yeni
/// durumu okur ve kural gereği `listing.invalid_status_transition` alır.</para>
///
/// <para><b>`ListingAction`'a eklenmedi</b>: o enum A3'ün tek tıklık geçişlerinin aynası
/// ve toplu yüzey ondan türüyor. Geri alma gerekçe ve beklenen durum taşıyor, yalnız
/// Yöneticiye açık ve toplu değil (**D9**) — aynı enum'a girseydi toplu rotası
/// kendiliğinden doğardı.</para>
/// </summary>
internal sealed class ListingSaleReversalCommand(
    VehicleCatalogDbContext context,
    VehicleCatalogUnitOfWork unitOfWork,
    ShowcaseProjectionWriter projection,
    SaleRevertAssessor assessor,
    InvariantGuardOptions<VehicleCatalogDbContext> guardOptions,
    IClock clock)
{
    internal async Task<Result> ExecuteAsync(RevertSaleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Biçim denetimleri kilitten ÖNCE: ucuz, veritabanı görmüyor ve reddedilen istek
        // ilanı hiç kilitlemiyor (AC-14, AC-19).
        Result<string> gerekce = ListingSaleReversal.NormalizeReason(request.Reason);

        if (gerekce.IsFailure)
        {
            return Result.Failure(gerekce.Error);
        }

        if (!SaleRevertStatuses.TryParse(request.ExpectedStatus, out ListingStatus beklenen))
        {
            return Result.Failure(new Error(SaleReversalErrors.ExpectedStatusInvalid));
        }

        return await InvariantGuard.ExecuteAsync(
            context,
            ListingLock.Row(request.VehicleId),
            excluded: Guid.Empty,
            async (_, ct) =>
            {
                Vehicle? vehicle = await context.Vehicles
                    .Include(item => item.OptionalFieldValues)
                    .FirstOrDefaultAsync(item => item.Id == new VehicleId(request.VehicleId), ct);

                Listing? listing = vehicle is null
                    ? null
                    : await context.Listings.FirstOrDefaultAsync(item => item.VehicleId == vehicle.Id, ct);

                if (vehicle is null || listing is null)
                {
                    return Result.Failure(Error.With(VehicleErrors.NotFound, "field", VehicleFieldNames.VehicleId));
                }

                // R10 — yalnız `Satıldı`dan. Değerlendirme (iki modül okuması) satılmamış
                // bir ilan için hiç yapılmıyor. Kararı agregat veriyor (C2).
                if (!listing.CanRevertSale)
                {
                    return Result.Failure(new Error(Listing.InvalidTransition));
                }

                SaleRevertAssessment degerlendirme = await assessor.AssessAsync(vehicle, ct);
                ListingStatus hedef = listing.SaleRevertTarget(degerlendirme.CanReturnToShowcase);

                // D16 — pencerenin gösterdiği hedef değiştiyse geri alma YAPILMAZ ve kayıt
                // yazılmaz. Yönetici güncel hedefi görür ve yeniden onaylar.
                if (hedef != beklenen)
                {
                    return Result.Failure(Error.With(
                        SaleReversalErrors.TargetChanged,
                        "targetStatus",
                        hedef.ToString()));
                }

                Result gecis = listing.RevertSale(degerlendirme.CanReturnToShowcase);

                if (gecis.IsFailure)
                {
                    return gecis;
                }

                // D12 — kapalı şubenin bayrağı A8 satılmış ilanı atladığı için hiç
                // yazılmamıştı; geri alma onu yazar ve araç yeni şube atanana kadar
                // yayına alınamaz.
                if (degerlendirme.BranchClosed)
                {
                    vehicle.MarkAwaitingBranchAssignment();
                }

                DateTimeOffset an = clock.UtcNow;

                Result<ListingSaleReversal> kayit = ListingSaleReversal.Record(
                    listing, request.StaffId, request.StaffDisplayName, gerekce.Value, an);

                if (kayit.IsFailure)
                {
                    return Result.Failure(kayit.Error);
                }

                context.SaleReversals.Add(kayit.Value);
                vehicle.Touch(an);

                await projection.ProjectAsync(vehicle, listing, read: null, ct);
                await unitOfWork.SaveChangesAsync(ct);

                return Result.Success();
            },
            guardOptions,
            cancellationToken);
    }
}

/// <summary>Panelin geri alma yüzeyi: önizleme ve kayıtlar (**R7**, **R9**).</summary>
internal sealed record SaleReversalOverview(
    string ListingStatus,
    string? TargetStatus,
    IReadOnlyList<string> DraftReasons,
    IReadOnlyList<string> MissingFields,
    IReadOnlyList<SaleReversalRecordView> Records);

internal sealed record SaleReversalRecordView(
    Guid Id,
    string StaffDisplayName,
    DateTimeOffset RevertedAt,
    string Reason,
    string RestoredStatus);

/// <summary>
/// `0023` — geri alma penceresinin önizlemesi ve ilanın kayıtları, tek okuma.
///
/// <para><b>Önizleme kilitsiz</b>: yalnız göstermek için. Pencere açıkken dünya değişirse
/// komut kilidin altında yeniden hesaplar ve farkı `409` ile söyler (**D16**).</para>
/// </summary>
internal sealed class ListingSaleReversalQueries(VehicleCatalogDbContext context, SaleRevertAssessor assessor)
{
    internal async Task<SaleReversalOverview?> FindAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        Vehicle? vehicle = await context.Vehicles
            .AsNoTracking()
            .Include(item => item.OptionalFieldValues)
            .FirstOrDefaultAsync(item => item.Id == new VehicleId(vehicleId), cancellationToken);

        Listing? listing = vehicle is null
            ? null
            : await context.Listings.AsNoTracking().FirstOrDefaultAsync(item => item.VehicleId == vehicle.Id, cancellationToken);

        if (vehicle is null || listing is null)
        {
            return null;
        }

        string? hedef = null;
        IReadOnlyList<string> nedenler = [];
        IReadOnlyList<string> alanlar = [];

        if (listing.CanRevertSale)
        {
            SaleRevertAssessment degerlendirme = await assessor.AssessAsync(vehicle, cancellationToken);

            hedef = listing.SaleRevertTarget(degerlendirme.CanReturnToShowcase).ToString();
            nedenler = degerlendirme.Reasons.Distinct(StringComparer.Ordinal).ToList();
            alanlar = degerlendirme.MissingFields;
        }

        // R7/AC-20 — en yenisi üstte. Eşit anda kimlik (v7, zamana göre sıralı) kırıcıdır.
        List<ListingSaleReversal> kayitlar = await context.SaleReversals
            .AsNoTracking()
            .Where(reversal => reversal.VehicleId == vehicle.Id)
            .OrderByDescending(reversal => reversal.RevertedAt)
            .ThenByDescending(reversal => reversal.Id)
            .ToListAsync(cancellationToken);

        return new SaleReversalOverview(
            listing.Status.ToString(),
            hedef,
            nedenler,
            alanlar,
            [
                .. kayitlar.Select(reversal => new SaleReversalRecordView(
                    reversal.Id.Value,
                    reversal.StaffDisplayName,
                    reversal.RevertedAt,
                    reversal.Reason,
                    reversal.RestoredStatus.ToString()))
            ]);
    }
}
