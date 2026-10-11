using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Application.Projection;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Vehicles;

/// <summary>
/// Panelin gönderdiği düzenleme. Çekirdek alanların **tamamını** taşır (R2, C25, **D19**)
/// ve şaseyi de taşır — ama şase DEĞİŞTİRİLEMEZ (**D20/D24**): gövdede bulunması,
/// "değiştirme girişimi"nin ifade edilebilmesi ve reddedilebilmesi içindir (AC-8).
/// </summary>
internal sealed record UpdateVehicleRequest(
    Guid VehicleId,
    Guid BranchId,
    Guid VehicleModelId,
    string BodyType,
    string FuelType,
    string TransmissionType,
    string DrivetrainType,
    string TrimLevel,
    IReadOnlyDictionary<string, string> Color,
    /// <summary>
    /// **Nullable ve bu bilinçli** (C25, AC-5). `int` olduğunda JSON'da bulunmayan alan
    /// sessizce `0` bağlanıyordu: kilometre `0` GEÇERLİ bir değer olduğu için istek
    /// kabul ediliyor ve araç kaydedilmiş kilometresini kaybediyordu. Diğer çekirdek
    /// alanlar tesadüfen korunuyordu — dizeler boşa, `Guid`'ler `Empty`'ye, model yılı
    /// aralık dışına düşüyor. Değer tipi olan alanlarda "gönderilmedi" ile "0 gönderildi"
    /// ancak `null` ile ayrılır.
    /// </summary>
    int? ModelYear,
    int? MileageKilometers,
    decimal? PriceAmount,
    bool PriceOnRequest,
    string Vin,
    IReadOnlyDictionary<string, string> ListingTitle,
    IReadOnlyDictionary<string, string> ListingDescription,
    IReadOnlyList<OptionalFieldInput> OptionalValues);

/// <summary>
/// Opsiyonel alan girdisi. Değer HAM METİN olarak gelir ve alanın beyan edilen tipine göre
/// ayrıştırılır (AC-11, AC-12); locale'li metin ise kendi eşlemesiyle gelir.
///
/// <para>İkisinin de dolu olması bir çağrı hatasıdır ve reddedilir: hangisinin
/// kazandığını sessizce seçmek, veri bozulmasının en sessiz biçimi olurdu.</para>
/// </summary>
internal sealed record OptionalFieldInput(
    string FieldKey,
    string? Value,
    IReadOnlyDictionary<string, string>? Values);

/// <summary>
/// Araç düzenleme. **C17 gereği agregatın üzerine kurulu bir komut katmanıdır**; ortak
/// kurallar `VehicleWriteRules`'ta ve kayıt yoluyla AYNI (tuzak 3).
///
/// <para><b>Semantik (D23):</b> çekirdek alanlar TAM DEĞİŞTİRME, ilan metni TAM
/// DEĞİŞTİRME, opsiyonel alanlar BİRLEŞTİRME. Üçüncüsü F1'in gereği: kapalı alan formda
/// görünmez (R7), istekte taşınmaz ve "gelmeyen = silindi" demek kapatmayı silmeye
/// çevirirdi (AC-17).</para>
/// </summary>
internal sealed class UpdateVehicleCommand(
    VehicleCatalogDbContext context,
    VehicleWriteRules rules,
    ShowcaseProjectionWriter projection,
    VehicleCatalogUnitOfWork unitOfWork,
    IClock clock)
{
    internal async Task<Result> ExecuteAsync(UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // --- BİÇİM (ucuz, ağ yok) ---
        Result color = rules.ValidateColor(request.Color);

        if (color.IsFailure)
        {
            return color;
        }

        Result<VehicleSpecification> specification = VehicleWriteRules.BuildSpecification(
            request.BodyType,
            request.FuelType,
            request.TransmissionType,
            request.DrivetrainType,
            request.TrimLevel);

        if (specification.IsFailure)
        {
            return Result.Failure(specification.Error);
        }

        Result<ListingPrice> price = rules.BuildPrice(request.PriceAmount, request.PriceOnRequest);

        if (price.IsFailure)
        {
            return Result.Failure(price.Error);
        }

        Result<LocalizedText> title = rules.BuildListingText(request.ListingTitle, VehicleFieldNames.ListingTitle);

        if (title.IsFailure)
        {
            return Result.Failure(title.Error);
        }

        Result<LocalizedText> description =
            rules.BuildListingText(request.ListingDescription, VehicleFieldNames.ListingDescription);

        if (description.IsFailure)
        {
            return Result.Failure(description.Error);
        }

        // C25 — eksik gelen alan "dokunulmadı" değil GEÇERSİZ sayılır. Değer tipli iki
        // çekirdek alanda bu ancak burada görülebilir: bağlayıcı onlara varsayılan
        // veriyor ve `0` kilometre geçerli bir kilometredir.
        if (request.MileageKilometers is null)
        {
            return VehicleWriteRules.Failure(VehicleErrors.CoreFieldMissing, VehicleFieldNames.Mileage);
        }

        if (request.ModelYear is null)
        {
            return VehicleWriteRules.Failure(VehicleErrors.CoreFieldMissing, VehicleFieldNames.ModelYear);
        }

        // --- VARLIK ---
        Vehicle? vehicle = await context.Vehicles
            .Include(item => item.OptionalFieldValues)
            .SingleOrDefaultAsync(item => item.Id == new VehicleId(request.VehicleId), cancellationToken);

        if (vehicle is null)
        {
            return VehicleWriteRules.Failure(VehicleErrors.NotFound, VehicleFieldNames.VehicleId);
        }

        // AC-8 — şase fiziksel aracın kimliğidir; değişmesi "aynı kayıt, başka araç"
        // demektir ve A4/A5'in izlediği şeyi bozar (D20).
        if (!string.Equals(vehicle.Vin.Value, request.Vin?.Trim(), StringComparison.Ordinal))
        {
            return VehicleWriteRules.Failure(VehicleErrors.VinImmutable, VehicleFieldNames.Vin);
        }

        Listing? listing = await context.Listings
            .SingleOrDefaultAsync(item => item.VehicleId == vehicle.Id, cancellationToken);

        if (listing is null)
        {
            // Bir araca bir ilan: tekil indeks bunu garanti ediyor ve kayıt yolu ikisini
            // aynı transaction'da yazıyor. Buraya düşmek veri bütünlüğü sorunudur.
            return VehicleWriteRules.Failure(VehicleErrors.NotFound, VehicleFieldNames.VehicleId);
        }

        Result references = await rules.ValidateReferencesAsync(
            request.VehicleModelId,
            request.BodyType,
            request.FuelType,
            request.TransmissionType,
            request.DrivetrainType,
            request.TrimLevel,
            cancellationToken);

        if (references.IsFailure)
        {
            return references;
        }

        Result branch = await rules.ValidateBranchAsync(request.BranchId, cancellationToken);

        if (branch.IsFailure)
        {
            return branch;
        }

        Result<IReadOnlyList<(string FieldKey, OptionalFieldValue Value)>> optional =
            await ResolveOptionalValuesAsync(request.OptionalValues, cancellationToken);

        if (optional.IsFailure)
        {
            return Result.Failure(optional.Error);
        }

        // --- YAZMA ---
        DateTimeOffset now = clock.UtcNow;

        Result core = vehicle.ChangeCoreDetails(
            specification.Value,
            VehicleWriteRules.BuildColor(request.Color),
            request.ModelYear.Value,
            request.MileageKilometers.Value,
            now);

        if (core.IsFailure)
        {
            return VehicleWriteRules.Failure(core.Error.Code, core.Error.Code switch
            {
                Vehicle.NegativeMileage => VehicleFieldNames.Mileage,
                _ => VehicleFieldNames.ModelYear,
            });
        }

        // R5/AC-6 — fiyat DEĞİŞTİYSE geçmişe yazılır. Değişmemiş fiyatı geçmişe yazmak,
        // her kaydetmede bir "değişiklik" uydururdu ve geçmiş anlamını yitirirdi.
        if (vehicle.Price != price.Value)
        {
            vehicle.ChangePrice(price.Value, now);
        }

        // Şube ataması: aynı şubeye atamak bayrağı da bozmaz.
        if (vehicle.BranchId != request.BranchId)
        {
            vehicle.AssignBranch(request.BranchId);
        }

        foreach ((string fieldKey, OptionalFieldValue value) in optional.Value)
        {
            vehicle.SetOptionalFieldValue(fieldKey, value);
        }

        Result text = listing.SetText(title.Value, description.Value);

        if (text.IsFailure)
        {
            // Hata KODU hangi metnin uzun olduğunu söylüyor; koşulsuz başlığa eşlemek,
            // açıklamayı uzun yazan personele yanlış alanı gösteriyordu.
            return VehicleWriteRules.Failure(
                VehicleErrors.TextTooLong,
                text.Error.Code == Listing.DescriptionTooLong
                    ? VehicleFieldNames.ListingDescription
                    : VehicleFieldNames.ListingTitle);
        }

        // D37 — eksik `Touch` çağrısı. Ölçüldü: `Touch`'ın çağıranları yalnız
        // `UpdateVehicleMediaCommand` ve `ChangePrice`'tı; başlık, kilometre veya renk
        // düzeltmesi "son güncellenen" sıralamasında GÖRÜNMÜYORDU. D20 geçişi görünür
        // kılıyor ve aynı borcun düzenleme tarafı burada kapanıyor.
        vehicle.Touch(now);

        // D12/R15 — vitrin satırı AYNI İŞLEMDE güncelleniyor. Bu spec'e kadar hiçbir komut
        // okuma modeline yazmıyordu: personel fiyatı düşürüyor, vitrinde eski fiyat
        // kalıyordu.
        await projection.ProjectAsync(vehicle, listing, cancellationToken);

        // D11 — fiyat ve şube değişikliğinin duyurusu iş verisiyle aynı transaction'da
        // outbox'a yazılıyor (C3).
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Opsiyonel değerleri çözer: alan DEFTERDE mi (AC-9), AÇIK mı (**D27**) ve değer
    /// beyan edilen tipe uyuyor mu (AC-11, AC-12).
    ///
    /// <para><b>Kapalı alana gelen değer REDDEDİLİR, sessizce yutulmaz.</b> Formun o alanı
    /// göstermesi imkânsız (R7); isteği ancak bayat bir sekme ya da elle kurulmuş bir
    /// çağrı taşır. Sessizce atmak bu depoda tekrar eden hata.</para>
    /// </summary>
    private async Task<Result<IReadOnlyList<(string FieldKey, OptionalFieldValue Value)>>>
        ResolveOptionalValuesAsync(IReadOnlyList<OptionalFieldInput> inputs, CancellationToken cancellationToken)
    {
        if (inputs.Count == 0)
        {
            return Result.Success<IReadOnlyList<(string, OptionalFieldValue)>>([]);
        }

        Dictionary<string, FieldFormState> settings = await context.VehicleFieldSettings
            .AsNoTracking()
            .ToDictionaryAsync(setting => setting.FieldKey, setting => setting.FormState, cancellationToken);

        List<(string FieldKey, OptionalFieldValue Value)> resolved = [];

        foreach (OptionalFieldInput input in inputs)
        {
            if (!VehicleFieldDefinitions.TryGet(input.FieldKey, out VehicleFieldDefinition definition))
            {
                return Result.Failure<IReadOnlyList<(string, OptionalFieldValue)>>(
                    Error.With(VehicleErrors.FieldNotDeclared, "field", input.FieldKey));
            }

            if (!settings.TryGetValue(input.FieldKey, out FieldFormState state) || state is FieldFormState.Closed)
            {
                return Result.Failure<IReadOnlyList<(string, OptionalFieldValue)>>(
                    Error.With(VehicleErrors.FieldClosed, "field", input.FieldKey));
            }

            Result<OptionalFieldValue> value = Resolve(definition, input);

            if (value.IsFailure)
            {
                return Result.Failure<IReadOnlyList<(string, OptionalFieldValue)>>(
                    Error.With(VehicleErrors.InvalidFieldValue, "field", input.FieldKey));
            }

            resolved.Add((input.FieldKey, value.Value));
        }

        return Result.Success<IReadOnlyList<(string, OptionalFieldValue)>>(resolved);
    }

    private Result<OptionalFieldValue> Resolve(VehicleFieldDefinition definition, OptionalFieldInput input)
    {
        if (definition.Kind is FieldValueKind.LocalizedText)
        {
            if (input.Values is null)
            {
                return Result.Failure<OptionalFieldValue>(new Error(OptionalFieldValue.KindMismatch));
            }

            Result<LocalizedText> localized = rules.BuildListingText(input.Values, definition.FieldKey);

            return localized.IsFailure
                ? Result.Failure<OptionalFieldValue>(localized.Error)
                : Result.Success(OptionalFieldValue.OfLocalizedText(localized.Value));
        }

        return input.Value is null
            ? Result.Failure<OptionalFieldValue>(new Error(OptionalFieldValue.KindMismatch))
            : OptionalFieldValue.Parse(definition.Kind, input.Value, rules.Currency);
    }
}
