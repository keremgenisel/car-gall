using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Contracts.Admin;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// Yönetim listesi ve düzenleme okuması. Şase BURADA bulunur ve bu doğrudur
/// (`0010a` AC-19): yanıt oturum gerektiren bir uçtan dönüyor ve personel şaseyi kendi
/// giriyor.
///
/// <para><b>İki sıralama ekseni</b> (R14): "son güncellenen" `updated_at`'ten,
/// "yeni eklenen" araç kimliğinden — kimlik UUID v7 ve zaten zaman sıralı. İkisinde de
/// eşitliği ARAÇ KİMLİĞİ kırıyor; kırıcı olmadan aynı dakikada güncellenmiş iki araç
/// kararsız sıralanır ve aynı araç iki sayfada görünebilir (`0001`'in dersi, AC-29).</para>
///
/// <para><b>Göstergeler SAKLANMIYOR, hesaplanıyor</b> (C21): "eksik bilgi" ayar
/// değiştiğinde hiçbir yazma olmadan değişmek zorunda ve bayrak saklamak C7'nin
/// yasakladığı geriye dönük doğrulama olurdu.</para>
/// </summary>
internal sealed class VehicleAdminQueries(
    VehicleCatalogDbContext context,
    IVehicleModelQueries models,
    ShowcaseLocaleOptions locales,
    CatalogAdminPagingOptions paging) : IVehicleAdminQueries
{
    public async Task<PagedResult<VehicleAdminListItemDto>> ListAsync(
        VehicleAdminListFilter filter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        VehicleAdminListRequest request = VehicleAdminListRequest.Normalize(filter, paging);

        int totalCount = await context.Vehicles.CountAsync(cancellationToken);

        IQueryable<Vehicle> sirali = request.Sort is VehicleAdminListSort.RecentlyAdded
            ? context.Vehicles.OrderByDescending(vehicle => vehicle.Id)
            : context.Vehicles
                .OrderByDescending(vehicle => vehicle.UpdatedAt)
                .ThenByDescending(vehicle => vehicle.Id);

        List<Vehicle> vehicles = await sirali
            .AsNoTracking()
            .Include(vehicle => vehicle.OptionalFieldValues)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyList<VehicleAdminListItemDto> items = vehicles.Count == 0
            ? []
            : await MapAsync(vehicles, filter.Locale, cancellationToken);

        return new PagedResult<VehicleAdminListItemDto>(items, request.Page, request.PageSize, totalCount);
    }

    /// <summary>
    /// AC-23 — sayı TÜM araçlar üzerinden ve o anki sayfaya bağlı değil (**D26**).
    ///
    /// <para><b>Bu bir tam taramadır ve bilerek öyle.</b> `LocalizedText` bir değer
    /// dönüştürücüsüyle saklanıyor; EF onu dize olarak görür ve `jsonb` operatörlerine
    /// çeviremez. İki kolonluk dar bir projeksiyon bir galeri ölçeğinde ucuz; ölçek
    /// büyüdüğünde doğru yol ham SQL (`title ? 'tr' AND description ? 'tr'`) olur.</para>
    /// </summary>
    public async Task<VehicleAdminSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        int totalCount = await context.Vehicles.CountAsync(cancellationToken);

        // Gerçekten İKİ KOLON: önceki hâli tüm `Listing` varlığını çekiyordu (durum,
        // ağırlık, yayın zamanı dâhil) ve üstündeki yorum "dar projeksiyon" diyordu.
        // Yorumla kod ayrışınca, bir sonraki okuyucu buraya hiç bakmaz.
        var metinler = await context.Listings
            .AsNoTracking()
            .Select(listing => new { listing.Title, listing.Description })
            .ToListAsync(cancellationToken);

        int eksik = metinler.Count(metin =>
            Listing.MissingLocalesOf(metin.Title, metin.Description, locales.Enabled).Count > 0);

        return new VehicleAdminSummaryDto(totalCount, eksik);
    }

    public async Task<VehicleAdminDetailDto?> FindAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        Vehicle? vehicle = await context.Vehicles
            .AsNoTracking()
            .Include(item => item.OptionalFieldValues)
            .SingleOrDefaultAsync(item => item.Id == new VehicleId(vehicleId), cancellationToken);

        if (vehicle is null)
        {
            return null;
        }

        Listing? listing = await context.Listings
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.VehicleId == vehicle.Id, cancellationToken);

        if (listing is null)
        {
            return null;
        }

        HashSet<string> acikAlanlar = await AcikAlanlarAsync(cancellationToken);

        return new VehicleAdminDetailDto(
            vehicle.Id.Value,
            vehicle.BranchId,
            vehicle.VehicleModelId,
            vehicle.Specification.BodyType.Value,
            vehicle.Specification.FuelTypeSlug,
            vehicle.Specification.TransmissionTypeSlug,
            vehicle.Specification.DrivetrainTypeSlug,
            vehicle.Specification.TrimLevelSlug,
            vehicle.Color.ToDictionary(),
            vehicle.ModelYear,
            vehicle.MileageKilometers,
            Price(vehicle.Price),
            vehicle.Vin.Value,
            // C11 — çevrilmiş durum adı değil KOD döner; çeviri uçta yapılır.
            listing.Status.ToString(),
            listing.Title.ToDictionary(),
            listing.Description.ToDictionary(),
            [
                // F3 — kapalı alanın değeri yanıta GİRMEZ; veritabanında durmaya devam
                // eder (F1). Ayrım sorgu tarafında (Yasak Listesi 41).
                .. vehicle.OptionalFieldValues
                    .Where(value => acikAlanlar.Contains(value.FieldKey))
                    .OrderBy(value => value.FieldKey, StringComparer.Ordinal)
                    .Select(value => OptionalValue(value))
            ],
            vehicle.UpdatedAt);
    }

    public async Task<IReadOnlyList<VehicleAdminFieldDto>> GetFormFieldsAsync(CancellationToken cancellationToken)
    {
        List<VehicleFieldSetting> settings = await context.VehicleFieldSettings
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return
        [
            .. settings
                // Çekirdek alanlar agregatın kendi alanları; opsiyonel alan formunda yer
                // almazlar. Kapalı alanlar da listede bulunmaz (R7).
                .Where(setting => !setting.IsCore && setting.IsOpen)
                .Select(setting => VehicleFieldDefinitions.TryGet(setting.FieldKey, out VehicleFieldDefinition tanim)
                    ? new VehicleAdminFieldDto(
                        setting.FieldKey,
                        tanim.Kind.ToString(),
                        setting.FormState is FieldFormState.Required)
                    : null)
                .Where(field => field is not null)
                .Select(field => field!)
                .OrderBy(field => field.FieldKey, StringComparer.Ordinal)
        ];
    }

    private async Task<IReadOnlyList<VehicleAdminListItemDto>> MapAsync(
        List<Vehicle> vehicles,
        string locale,
        CancellationToken cancellationToken)
    {
        // Durum ve metin sorgusu ELDEKİ araçlarla sınırlı: koşulsuz okuma sayfalamanın
        // kazancını sessizce iptal ederdi.
        List<VehicleId> ids = [.. vehicles.Select(vehicle => vehicle.Id)];

        Dictionary<VehicleId, Listing> listings = await context.Listings
            .AsNoTracking()
            .Where(listing => ids.Contains(listing.VehicleId))
            .ToDictionaryAsync(listing => listing.VehicleId, cancellationToken);

        // Model adı REFERANS VERİDE yaşıyor; burada saklanmıyor (Yasak Listesi 33).
        IReadOnlyDictionary<Guid, string> names = await models.ResolveNamesAsync(
            [.. vehicles.Select(vehicle => vehicle.VehicleModelId).Distinct()],
            locale,
            cancellationToken);

        HashSet<string> zorunluAlanlar = await ZorunluOpsiyonelAlanlarAsync(cancellationToken);

        return
        [
            .. vehicles.Select(vehicle => new VehicleAdminListItemDto(
                vehicle.Id.Value,
                names.TryGetValue(vehicle.VehicleModelId, out string? name) ? name : string.Empty,
                vehicle.ModelYear,
                vehicle.MileageKilometers,
                Price(vehicle.Price),
                (listings.TryGetValue(vehicle.Id, out Listing? listing) ? listing.Status : ListingStatus.Draft).ToString(),
                vehicle.Vin.Value,
                vehicle.UpdatedAt,
                [
                    .. (listing is null ? locales.Enabled : listing.MissingLocales(locales.Enabled))
                        .Select(item => item.Code)
                ],
                EksikBilgiVar(vehicle, zorunluAlanlar)))
        ];
    }

    /// <summary>
    /// R9/AC-16 — zorunlu işaretli bir opsiyonel alanı boş olan araç "eksik bilgi"dir.
    /// Çekirdek alanlar bu hesaba GİRMEZ: `0010a` D8 onları kayıt anında zorunlu kılıyor,
    /// yani her zaman doludurlar.
    /// </summary>
    private static bool EksikBilgiVar(Vehicle vehicle, HashSet<string> zorunluAlanlar) =>
        zorunluAlanlar.Count > 0
        && zorunluAlanlar.Except(vehicle.OptionalFieldValues.Select(value => value.FieldKey)).Any();

    private async Task<HashSet<string>> ZorunluOpsiyonelAlanlarAsync(CancellationToken cancellationToken) =>
        [
            .. await context.VehicleFieldSettings
                .AsNoTracking()
                .Where(setting => !setting.IsCore && setting.FormState == FieldFormState.Required)
                .Select(setting => setting.FieldKey)
                .ToListAsync(cancellationToken)
        ];

    private async Task<HashSet<string>> AcikAlanlarAsync(CancellationToken cancellationToken) =>
        [
            .. await context.VehicleFieldSettings
                .AsNoTracking()
                .Where(setting => !setting.IsCore && setting.FormState != FieldFormState.Closed)
                .Select(setting => setting.FieldKey)
                .ToListAsync(cancellationToken)
        ];

    private static VehicleAdminOptionalValueDto OptionalValue(VehicleOptionalFieldValue value) =>
        value.Value.Kind switch
        {
            FieldValueKind.LocalizedText => new VehicleAdminOptionalValueDto(
                value.FieldKey,
                value.Value.Kind.ToString(),
                null,
                value.Value.Localized!.ToDictionary()),

            // Biçimlendirme UÇTA (conventions § 7): sunucu ham değeri taşır, metne
            // çevirmez — ama ondalık ÖLÇEK para biriminin üssünden gelmeli.
            //
            // **Ölçüldü:** kolon `numeric(19,4)` ve okunan değer ÖLÇEĞİYLE geliyor;
            // ham `ToString()` iki ondalıklı bir kurulumda `9900.0000` üretiyordu.
            // Sayı doğru ama ölçek yanlış ve forma dönen metin de yanlış: personel
            // `9900.0000` görürdü. `Money` üssü taşıyor, biçim ondan geliyor.
            FieldValueKind.Amount => new VehicleAdminOptionalValueDto(
                value.FieldKey,
                value.Value.Kind.ToString(),
                value.Value.Amount!.Value.Amount.ToString(
                    $"F{value.Value.Amount.Value.MinorUnitExponent}",
                    System.Globalization.CultureInfo.InvariantCulture),
                null),

            FieldValueKind.Integer => new VehicleAdminOptionalValueDto(
                value.FieldKey,
                value.Value.Kind.ToString(),
                value.Value.Integer!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                null),

            FieldValueKind.Boolean => new VehicleAdminOptionalValueDto(
                value.FieldKey,
                value.Value.Kind.ToString(),
                value.Value.Boolean!.Value ? "true" : "false",
                null),

            FieldValueKind.Date => new VehicleAdminOptionalValueDto(
                value.FieldKey,
                value.Value.Kind.ToString(),
                value.Value.Date!.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                null),

            FieldValueKind.Option => new VehicleAdminOptionalValueDto(
                value.FieldKey,
                value.Value.Kind.ToString(),
                value.Value.Option,
                null),

            _ => new VehicleAdminOptionalValueDto(
                value.FieldKey,
                value.Value.Kind.ToString(),
                value.Value.Text,
                null),
        };

    private static VehicleAdminPriceDto Price(ListingPrice price) => price.IsOnRequest
        ? new VehicleAdminPriceDto(true, null, null, null)
        : new VehicleAdminPriceDto(
            false,
            price.Amount!.Value.Amount,
            price.Amount.Value.CurrencyCode,
            price.Amount.Value.MinorUnitExponent);
}
