using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// Kayıt ve düzenlemenin ORTAK kuralları.
///
/// <para><b>Neden tek yerde.</b> `0010b`'nin bilinen tuzaklarından biri: "düzenlemede
/// çekirdek alanı gevşetmek — aynı invariant iki yerde iki farklı şey olur". İki komut aynı
/// kuralları iki kez yazsaydı, birinin düzeltilmesi diğerini bayatlatırdı ve fark sessiz
/// olurdu: iki yol da yeşil test verir, ikisi farklı şeyi kabul ederdi.</para>
///
/// <para><b>Doğrulama sırası bilinçli:</b> önce BİÇİM (ucuz, ağ yok), sonra VARLIK
/// (referans ve şube sorguları). Tersi olsaydı biçimsiz bir istek için altı referans
/// sorgusu atılırdı.</para>
/// </summary>
internal sealed class VehicleWriteRules(
    IBranchQueries branches,
    IBodyTypeQueries bodyTypes,
    IVehicleModelQueries models,
    IReferenceTermQueries terms,
    CatalogCurrencyOptions currency,
    ShowcaseLocaleOptions locales)
{
    /// <summary>R11 — renk serbest metindir ve üst sınırı vardır.</summary>
    internal const int MaximumColorLength = 60;

    internal CatalogCurrency Currency => new(currency.Code, currency.MinorUnitExponent);

    internal IReadOnlyList<Locale> EnabledLocales => locales.Enabled;

    /// <summary>
    /// Renk: uzunluk, etkin locale ve "en az bir dilde dolu" (R2, AC-6).
    /// Etkin olmayan locale SESSİZCE elenmez — personelin girdiği değeri hiçbir uyarı
    /// vermeden atmak bu depoda tekrar eden ders.
    /// </summary>
    internal Result ValidateColor(IReadOnlyDictionary<string, string> color)
    {
        if (color.Values.Any(value => value.Length > MaximumColorLength))
        {
            return Failure(VehicleErrors.TextTooLong, VehicleFieldNames.Color);
        }

        if (color.Keys.Any(code => !locales.IsEnabled(code)))
        {
            return Failure(VehicleErrors.LocaleNotEnabled, VehicleFieldNames.Color);
        }

        return color.Values.Any(value => !string.IsNullOrWhiteSpace(value))
            ? Result.Success()
            : Failure(VehicleErrors.CoreFieldMissing, VehicleFieldNames.Color);
    }

    /// <summary>Anahtarların hepsi bu noktada etkin; elenen tek şey BOŞ değer.</summary>
    internal static LocalizedText BuildColor(IReadOnlyDictionary<string, string> color) =>
        LocalizedText.From(
            color
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => new KeyValuePair<Locale, string>(
                    Locale.Create(pair.Key).Value,
                    pair.Value.Trim())));

    /// <summary>
    /// Çekirdek slug'ların BİÇİM denetimi; varlık denetimi ayrı (C6).
    ///
    /// <para><b>Dört slug tek tek denetleniyor ve bu bilinçli.</b>
    /// `VehicleSpecification.Create` dördünü bir döngüde geziyor ve tek bir `InvalidSlug`
    /// dönüyor — hangisinin düştüğünü söylemiyor. Önceki hâli o hatayı koşulsuz
    /// `fuelType`'a eşliyordu: vitesi boş bırakan personele YAKIT alanı kırmızı
    /// gösteriliyordu. Yanlış alan işaretlemesi personeli yanlış düzeltmeye yöneltir
    /// (`0010a` AC-42).</para>
    ///
    /// <para>Buradaki denetim `Domain`'in yerine GEÇMEZ: `Create` yine çağrılıyor ve
    /// kuralın sahibi o. Bu geçiş yalnız alanı adlandırmak için; ikisi bir gün ayrışırsa
    /// `Create` yine reddeder, sadece alan adı genel kalır.</para>
    /// </summary>
    internal static Result<VehicleSpecification> BuildSpecification(
        string bodyType,
        string fuelType,
        string transmissionType,
        string drivetrainType,
        string trimLevel)
    {
        Result<BodyTypeSlug> slug = BodyTypeSlug.Create(bodyType);

        if (slug.IsFailure)
        {
            return Result.Failure<VehicleSpecification>(
                Error.With(VehicleErrors.CoreFieldMissing, "field", VehicleFieldNames.BodyType));
        }

        (string Slug, string Field)[] slugs =
        [
            (fuelType, VehicleFieldNames.FuelType),
            (transmissionType, VehicleFieldNames.TransmissionType),
            (drivetrainType, VehicleFieldNames.DrivetrainType),
            (trimLevel, VehicleFieldNames.TrimLevel),
        ];

        foreach ((string deger, string alan) in slugs)
        {
            if (!BodyTypeSlug.TryParse(deger, out _))
            {
                return Result.Failure<VehicleSpecification>(
                    Error.With(VehicleErrors.CoreFieldMissing, "field", alan));
            }
        }

        Result<VehicleSpecification> specification = VehicleSpecification.Create(
            slug.Value,
            fuelType,
            transmissionType,
            drivetrainType,
            trimLevel);

        return specification.IsFailure
            ? Result.Failure<VehicleSpecification>(
                Error.With(VehicleErrors.CoreFieldMissing, "field", VehicleFieldNames.FuelType))
            : specification;
    }

    /// <summary>
    /// P4 — "fiyat sorunuz" AYRI bir durumdur, `0` değildir. İkisi birlikte verilirse
    /// istek reddedilir: hangisinin kazandığını sessizce seçmek, veri bozulmasının en
    /// sessiz biçimi olurdu.
    /// </summary>
    internal Result<ListingPrice> BuildPrice(decimal? amount, bool onRequest)
    {
        if (onRequest)
        {
            return amount is null
                ? Result.Success(ListingPrice.OnRequest())
                : Result.Failure<ListingPrice>(Error.With(VehicleErrors.InvalidPrice, "field", VehicleFieldNames.Price));
        }

        if (amount is null)
        {
            return Result.Failure<ListingPrice>(
                Error.With(VehicleErrors.CoreFieldMissing, "field", VehicleFieldNames.Price));
        }

        Result<Money> money = currency.Money(amount.Value);

        return money.IsSuccess
            ? Result.Success(ListingPrice.Of(money.Value))
            : Result.Failure<ListingPrice>(Error.With(VehicleErrors.InvalidPrice, "field", VehicleFieldNames.Price));
    }

    /// <summary>
    /// AC-5 — altı küme. Terimler TEK sorguda, model ve kasa tipi ayrı: üçü farklı
    /// tablolarda ve tek bir sorguya sığmıyor. Terim sorgusu eksik olanları döndüğü için
    /// hangi alanın düştüğü doğrudan okunuyor.
    /// </summary>
    internal async Task<Result> ValidateReferencesAsync(
        Guid vehicleModelId,
        string bodyType,
        string fuelType,
        string transmissionType,
        string drivetrainType,
        string trimLevel,
        CancellationToken cancellationToken)
    {
        if (!await models.ExistsAsync(vehicleModelId, cancellationToken))
        {
            return Failure(VehicleErrors.ReferenceNotFound, VehicleFieldNames.VehicleModelId);
        }

        if (!await bodyTypes.ExistsAsync(bodyType, cancellationToken))
        {
            return Failure(VehicleErrors.ReferenceNotFound, VehicleFieldNames.BodyType);
        }

        (string Kind, string Slug)[] istenen =
        [
            (ReferenceTermKinds.FuelType, fuelType),
            (ReferenceTermKinds.TransmissionType, transmissionType),
            (ReferenceTermKinds.DrivetrainType, drivetrainType),
            (ReferenceTermKinds.TrimLevel, trimLevel),
        ];

        IReadOnlyList<(string Kind, string Slug)> eksikler =
            await terms.FindMissingAsync(istenen, cancellationToken);

        return eksikler.Count == 0
            ? Result.Success()
            : Failure(VehicleErrors.ReferenceNotFound, FieldNameOf(eksikler[0].Kind));
    }

    /// <summary>
    /// C6 — şube kimliğinin geçerliliği `Dealership.Contracts` üzerinden, TEK ATLAMA ve
    /// salt okuma. Şube adı burada saklanmıyor (Yasak Listesi 33).
    /// </summary>
    internal async Task<Result> ValidateBranchAsync(Guid branchId, CancellationToken cancellationToken)
    {
        if (branchId == Guid.Empty)
        {
            return Failure(VehicleErrors.BranchRequired, VehicleFieldNames.BranchId);
        }

        return await branches.FindAsync(branchId, locales.Default.Code, cancellationToken) is null
            ? Failure(VehicleErrors.BranchNotFound, VehicleFieldNames.BranchId)
            : Result.Success();
    }

    /// <summary>
    /// İlan metni: etkin locale ve locale BAŞINA uzunluk (R10, R11, AC-20).
    /// Uzunluk sınırını `Listing` uyguluyor; buradaki iş locale denetimi.
    /// </summary>
    internal Result<LocalizedText> BuildListingText(IReadOnlyDictionary<string, string> values, string field)
    {
        if (values.Keys.Any(code => !locales.IsEnabled(code)))
        {
            return Result.Failure<LocalizedText>(Error.With(VehicleErrors.LocaleNotEnabled, "field", field));
        }

        return Result.Success(LocalizedText.From(
            values
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                .Select(pair => new KeyValuePair<Locale, string>(
                    Locale.Create(pair.Key).Value,
                    pair.Value.Trim()))));
    }

    internal static Result Failure(string code, string field) =>
        Result.Failure(Error.With(code, "field", field));

    private static string FieldNameOf(string kind) => kind switch
    {
        ReferenceTermKinds.FuelType => VehicleFieldNames.FuelType,
        ReferenceTermKinds.TransmissionType => VehicleFieldNames.TransmissionType,
        ReferenceTermKinds.DrivetrainType => VehicleFieldNames.DrivetrainType,
        _ => VehicleFieldNames.TrimLevel,
    };
}
