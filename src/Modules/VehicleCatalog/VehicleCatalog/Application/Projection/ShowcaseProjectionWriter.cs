using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Projection;

/// <summary>
/// Vitrin okuma modelini **aynı işlemde** güncelleyen tek yer (**R15**, **D12**).
///
/// <para><b>Karar `ShowcaseListingProjector`'da kalıyor</b> — o saf ve test edilebilir;
/// burası yalnızca kararın gerektirdiği veriyi toplayıp satırı yazıyor ya da siliyor.
/// **C12**: görünürlük kararını veren tek yer yansıtmadır. Kopyalanmış kolonları besleyen
/// olay işleyicileri (kapak URL'i, şube adları) bu kararı vermez ve bu yüzden
/// dışarıdadır (**D34**).</para>
///
/// <para><b>İki modülden senkron okuma yapıyor</b> ve ikisi de § 2.A'nın saydığı meşru
/// okumalar: şube adları `Dealership`, kapak görselinin adresi `Media`. <b>Bedel
/// açıkça kabul edildi</b> (**D12**): `Dealership` erişilemezken yayına alma düşer.</para>
///
/// <para>Şube adı <b>locale → ad haritası</b> olarak okunuyor (**D40**): okuma modelinin
/// kolonu bir sözlük ve tek dille yazmak aracı diğer dillerde şubesiz gösterirdi.</para>
/// </summary>
internal sealed class ShowcaseProjectionWriter(
    VehicleCatalogDbContext context,
    IBranchQueries branches,
    IMediaAssetQueries mediaAssets)
{
    /// <summary>
    /// Aracın vitrin satırını yazar, günceller ya da siler. Çağıran bunu
    /// <c>SaveChanges</c>'ten ÖNCE çağırır; yazma ile yansıtma aynı transaction'a girer.
    /// </summary>
    internal async Task ProjectAsync(Vehicle vehicle, Listing listing, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(listing);

        ShowcaseListing? mevcut = await context.ShowcaseListings
            .Include(row => row.OptionalValues)
            .FirstOrDefaultAsync(row => row.ListingId == listing.Id.Value, cancellationToken);

        ShowcaseListing? hedef = listing.Slug is null
            // Adresi olmayan ilan hiç yayınlanmamıştır; yansıtmanın sorusu bile doğmaz.
            ? null
            : ShowcaseListingProjector.Project(
                vehicle,
                listing,
                listing.Slug.Value,
                await SubeAdlariAsync(vehicle.BranchId, cancellationToken),
                await KapakAdresiAsync(vehicle.CoverAssetId, cancellationToken),
                await PublicAlanlarAsync(cancellationToken));

        if (hedef is null)
        {
            if (mevcut is not null)
            {
                context.ShowcaseListings.Remove(mevcut);
            }

            return;
        }

        if (mevcut is null)
        {
            context.ShowcaseListings.Add(hedef);
            return;
        }

        // Satır VAR: anahtar ve değişmezler korunuyor, gerisi üzerine yazılıyor. Silip
        // yeniden eklemek `ListingId`'yi anahtar olarak kullanan bir tabloda aynı turda
        // iki işlem demekti ve EF'in izleyicisi onu çakışma olarak görürdü.
        Guncelle(mevcut, hedef);
    }

    /// <summary>Aracın ilanını bulur ve yansıtır; ilan yoksa sessizce çıkar.</summary>
    internal async Task ProjectAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        Listing? listing = await context.Listings
            .FirstOrDefaultAsync(item => item.VehicleId == vehicle.Id, cancellationToken);

        if (listing is not null)
        {
            await ProjectAsync(vehicle, listing, cancellationToken);
        }
    }

    private static void Guncelle(ShowcaseListing mevcut, ShowcaseListing hedef)
    {
        mevcut.IsReserved = hedef.IsReserved;
        mevcut.ShowcaseSortWeight = hedef.ShowcaseSortWeight;
        mevcut.PublishedAt = hedef.PublishedAt;
        mevcut.ColorNames = hedef.ColorNames;
        mevcut.ModelYear = hedef.ModelYear;
        mevcut.MileageKilometers = hedef.MileageKilometers;
        mevcut.PriceAmount = hedef.PriceAmount;
        mevcut.PriceCurrencyCode = hedef.PriceCurrencyCode;
        mevcut.PriceMinorUnitExponent = hedef.PriceMinorUnitExponent;
        mevcut.PriceOnRequest = hedef.PriceOnRequest;
        mevcut.BranchId = hedef.BranchId;
        mevcut.BranchNames = hedef.BranchNames;
        mevcut.CoverAssetId = hedef.CoverAssetId;
        mevcut.CoverMediaUrl = hedef.CoverMediaUrl;
        mevcut.OptionalValues = hedef.OptionalValues;
    }

    /// <summary>
    /// Kapak görselinin türev adı. `Media`'nın `MediaVariant.Display` sabiti o modülün
    /// `internal`'ı (Yasak Listesi 6); sınırın bu yanında dize olarak tekrarlanıyor ve
    /// tekrar BİLİNÇLİ (Yasak Listesi 10). `MediaAssetReadyHandler` aynı sabiti aynı
    /// gerekçeyle taşıyor.
    /// </summary>
    private const string DisplayVariant = "display";

    private async Task<IReadOnlyDictionary<string, string>> SubeAdlariAsync(
        Guid branchId,
        CancellationToken cancellationToken) =>
        await branches.FindNamesAsync(branchId, cancellationToken)
        ?? new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Kapak görselinin GÖSTERİM türevi. Varlık yoksa ya da henüz hazır değilse
    /// <c>null</c> döner — `MediaAssetReady` geldiğinde kolon o yoldan tazelenir
    /// (`0011` AC-36, **D34**).
    /// </summary>
    private async Task<string?> KapakAdresiAsync(Guid? coverAssetId, CancellationToken cancellationToken)
    {
        if (coverAssetId is null)
        {
            return null;
        }

        IReadOnlyList<MediaAssetDto> varliklar =
            await mediaAssets.FindManyAsync([coverAssetId.Value], cancellationToken);

        return varliklar
            .FirstOrDefault()?.Derivatives
            .FirstOrDefault(derivative => derivative.Variant == DisplayVariant)?.Url;
    }

    private async Task<IReadOnlySet<string>> PublicAlanlarAsync(CancellationToken cancellationToken) =>
        new HashSet<string>(
            await context.VehicleFieldSettings
                .AsNoTracking()
                .Where(setting => !setting.IsCore && setting.FormState != FieldFormState.Closed)
                .Where(setting => setting.PublicVisibility == FieldPublicVisibility.Visible)
                .Select(setting => setting.FieldKey)
                .ToListAsync(cancellationToken),
            StringComparer.Ordinal);
}
