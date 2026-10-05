using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>Panelin gönderdiği araç kaydı. Para BİRİMİ yoktur — o yapılandırmadan gelir (C3).</summary>
internal sealed record CreateVehicleRequest(
    Guid BranchId,
    Guid VehicleModelId,
    string BodyType,
    string FuelType,
    string TransmissionType,
    string DrivetrainType,
    string TrimLevel,
    IReadOnlyDictionary<string, string> Color,
    int ModelYear,
    int MileageKilometers,
    decimal? PriceAmount,
    bool PriceOnRequest,
    string Vin);

/// <summary>
/// Araç kaydı oluşturma. **C17 gereği agregatın üzerine kurulu bir komut katmanıdır**:
/// `Vehicle.Create` ve `Listing.CreateDraft` olduğu gibi çağrılıyor, kararları değişmiyor.
///
/// <para><b>Doğrulama sırası bilinçli:</b> önce BİÇİM (ucuz, ağ yok), sonra VARLIK
/// (referans ve şube sorguları), en son yazma. Tersi olsaydı biçimsiz bir istek için altı
/// referans sorgusu atılırdı.</para>
///
/// <para><b>Altı referans alanı AYRI AYRI doğrulanır</b> (AC-5) ve hata hangi alanın
/// düştüğünü söyler: "bir şey yanlış" diyen bir kod personele hangisini düzelteceğini
/// söylemez (AC-42).</para>
/// </summary>
internal sealed class CreateVehicleCommand(
    VehicleCatalogDbContext context,
    IBranchQueries branches,
    IBodyTypeQueries bodyTypes,
    IVehicleModelQueries models,
    IReferenceTermQueries terms,
    CatalogCurrencyOptions currency,
    ShowcaseLocaleOptions locales,
    IClock clock)
{
    /// <summary>R17 — yeni kayıt VARSAYILAN vitrin sıralama ağırlığıyla açılır (X17).</summary>
    internal const int DefaultShowcaseSortWeight = 0;

    /// <summary>R11 — renk serbest metindir ve üst sınırı vardır.</summary>
    internal const int MaximumColorLength = 60;

    /// <summary>PostgreSQL: tekillik ihlali.</summary>
    private const string UniqueViolation = "23505";

    internal async Task<Result<Guid>> ExecuteAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<Vin> vin = Vin.Create(request.Vin);

        if (vin.IsFailure)
        {
            // Üç başarısızlık nedeni (boş · uzun · iç boşluk) tek koda eşlenmiyor: boş
            // bırakılmış bir alana "uzunluk aşıldı" demek personeli yanlış düzeltmeye
            // yöneltiyordu — şaseyi kısaltmaya çalışıyor, oysa sorun boşluk.
            return Failure(
                vin.Error.Code == Vin.TooLong ? VehicleErrors.TextTooLong : VehicleErrors.InvalidVin,
                VehicleFieldNames.Vin);
        }

        if (request.Color.Values.Any(value => value.Length > MaximumColorLength))
        {
            return Failure(VehicleErrors.TextTooLong, VehicleFieldNames.Color);
        }

        // Etkin olmayan locale SESSİZCE elenmez. Daha önce filtre değeri yutuyordu ve
        // personel girdiği rengin kaybolduğunu hiçbir yerde göremiyordu.
        if (request.Color.Keys.Any(code => !locales.IsEnabled(code)))
        {
            return Failure(VehicleErrors.LocaleNotEnabled, VehicleFieldNames.Color);
        }

        // R2 + R10 + AC-6 — renk ÇEKİRDEK alandır ve en az bir etkin locale'de dolu
        // olmalıdır. "En az bir" bilinçli: R10 çok dilli metni yarıda bırakmaya izin
        // veriyor, yani kalan diller düzenleme ekranında (`0010b`) doldurulabilir.
        if (!request.Color.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
        {
            return Failure(VehicleErrors.CoreFieldMissing, VehicleFieldNames.Color);
        }

        Result<BodyTypeSlug> bodyType = BodyTypeSlug.Create(request.BodyType);

        if (bodyType.IsFailure)
        {
            return Failure(VehicleErrors.CoreFieldMissing, VehicleFieldNames.BodyType);
        }

        Result<VehicleSpecification> specification = VehicleSpecification.Create(
            bodyType.Value,
            request.FuelType,
            request.TransmissionType,
            request.DrivetrainType,
            request.TrimLevel);

        if (specification.IsFailure)
        {
            return Failure(VehicleErrors.CoreFieldMissing, VehicleFieldNames.FuelType);
        }

        Result<ListingPrice> price = BuildPrice(request);

        if (price.IsFailure)
        {
            return Result.Failure<Guid>(price.Error);
        }

        Result<Guid> references = await ValidateReferencesAsync(request, cancellationToken);

        if (references.IsFailure)
        {
            return references;
        }

        if (request.BranchId == Guid.Empty)
        {
            return Failure(VehicleErrors.BranchRequired, VehicleFieldNames.BranchId);
        }

        // C6 — şube kimliğinin geçerliliği `Dealership.Contracts` üzerinden, TEK ATLAMA
        // ve salt okuma. Şube adı burada saklanmıyor (Yasak Listesi 33).
        if (await branches.FindAsync(request.BranchId, locales.Default.Code, cancellationToken) is null)
        {
            return Failure(VehicleErrors.BranchNotFound, VehicleFieldNames.BranchId);
        }

        Result<Vehicle> vehicle = Vehicle.Create(
            request.BranchId,
            request.VehicleModelId,
            specification.Value,
            Color(request),
            request.ModelYear,
            request.MileageKilometers,
            price.Value,
            vin.Value,
            clock.UtcNow);

        if (vehicle.IsFailure)
        {
            // Agregatın hatası alan adı TAŞIMAZ — `Domain` panelin alan adlarını bilmez.
            // Eşlemeyi burada yapmak AC-42'nin "hatalı alan işaretlenir" sözünü kilometre
            // ve model yılı için de tutturuyor; aksi hâlde form genel hataya düşerdi ve
            // personel hangisini düzelteceğini bilemezdi.
            return Failure(vehicle.Error.Code, vehicle.Error.Code switch
            {
                Vehicle.NegativeMileage => VehicleFieldNames.Mileage,
                _ => VehicleFieldNames.ModelYear,
            });
        }

        context.Vehicles.Add(vehicle.Value);
        context.Listings.Add(Listing.CreateDraft(vehicle.Value.Id, DefaultShowcaseSortWeight));

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsVinConflict(exception))
        {
            // AC-16 — tekilliği VERİTABANI söylüyor. Önce "var mı" diye bakıp sonra yazmak
            // iki eşzamanlı isteğin ikisinin de geçmesine izin verirdi: okuma ile yazma
            // arasında kilit yok ve kilitlenecek bir satır da henüz yok.
            return Failure(VehicleErrors.DuplicateVin, VehicleFieldNames.Vin);
        }

        return Result.Success(vehicle.Value.Id.Value);
    }

    private static bool IsVinConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres
        && postgres.SqlState == UniqueViolation
        && postgres.ConstraintName == "ux_vehicles_vin";

    private static Result<Guid> Failure(string code, string field) =>
        Result.Failure<Guid>(Error.With(code, "field", field));

    /// <summary>
    /// Anahtarların hepsi bu noktada ETKİN (doğrulama yukarıda yapıldı), yani burada
    /// eleme yok. Elenen tek şey BOŞ değer: boş bir dize "o dili doldurmadım" demektir ve
    /// locale'i boş metinle kaydetmek, dolu sanılan bir alan üretirdi.
    /// </summary>
    private static LocalizedText Color(CreateVehicleRequest request) =>
        LocalizedText.From(
            request.Color
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => new KeyValuePair<Locale, string>(
                    Locale.Create(pair.Key).Value,
                    pair.Value.Trim())));

    /// <summary>
    /// P4 — "fiyat sorunuz" AYRI bir durumdur, `0` değildir. İkisi birlikte verilirse
    /// istek reddedilir: hangisinin kazandığını sessizce seçmek, veri bozulmasının en
    /// sessiz biçimi olurdu.
    /// </summary>
    private Result<ListingPrice> BuildPrice(CreateVehicleRequest request)
    {
        if (request.PriceOnRequest)
        {
            return request.PriceAmount is null
                ? Result.Success(ListingPrice.OnRequest())
                : Result.Failure<ListingPrice>(Error.With(VehicleErrors.InvalidPrice, "field", VehicleFieldNames.Price));
        }

        if (request.PriceAmount is null)
        {
            return Result.Failure<ListingPrice>(
                Error.With(VehicleErrors.CoreFieldMissing, "field", VehicleFieldNames.Price));
        }

        Result<Money> money = currency.Money(request.PriceAmount.Value);

        return money.IsSuccess
            ? Result.Success(ListingPrice.Of(money.Value))
            : Result.Failure<ListingPrice>(Error.With(VehicleErrors.InvalidPrice, "field", VehicleFieldNames.Price));
    }

    /// <summary>
    /// AC-5 — altı küme. Terimler TEK sorguda, model ve kasa tipi ayrı: üçü farklı
    /// tablolarda ve tek bir sorguya sığmıyor. Terim sorgusu eksik olanları döndüğü için
    /// hangi alanın düştüğü doğrudan okunuyor.
    /// </summary>
    private async Task<Result<Guid>> ValidateReferencesAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        if (!await models.ExistsAsync(request.VehicleModelId, cancellationToken))
        {
            return Failure(VehicleErrors.ReferenceNotFound, VehicleFieldNames.VehicleModelId);
        }

        if (!await bodyTypes.ExistsAsync(request.BodyType, cancellationToken))
        {
            return Failure(VehicleErrors.ReferenceNotFound, VehicleFieldNames.BodyType);
        }

        (string Kind, string Slug)[] istenen =
        [
            (ReferenceTermKinds.FuelType, request.FuelType),
            (ReferenceTermKinds.TransmissionType, request.TransmissionType),
            (ReferenceTermKinds.DrivetrainType, request.DrivetrainType),
            (ReferenceTermKinds.TrimLevel, request.TrimLevel),
        ];

        IReadOnlyList<(string Kind, string Slug)> eksikler =
            await terms.FindMissingAsync(istenen, cancellationToken);

        if (eksikler.Count == 0)
        {
            return Result.Success(Guid.Empty);
        }

        return Failure(VehicleErrors.ReferenceNotFound, FieldNameOf(eksikler[0].Kind));
    }

    private static string FieldNameOf(string kind) => kind switch
    {
        ReferenceTermKinds.FuelType => VehicleFieldNames.FuelType,
        ReferenceTermKinds.TransmissionType => VehicleFieldNames.TransmissionType,
        ReferenceTermKinds.DrivetrainType => VehicleFieldNames.DrivetrainType,
        _ => VehicleFieldNames.TrimLevel,
    };
}
