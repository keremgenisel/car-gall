using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Galerinin elindeki fiziksel araç. Kasa tipi ÇEKİRDEK alandır ve A2 uyarınca yayındaki
/// her araçta doludur; bu yüzden bu spec'te "eksensiz ilan" kenar durumu hiç doğmaz (X3).
/// </summary>
internal sealed class Vehicle : AggregateRoot<VehicleId>
{
    internal const string NegativeMileage = "vehicle.negative_mileage";
    internal const string ImplausibleModelYear = "vehicle.implausible_model_year";
    internal const string MediaRequired = "vehicle.media_required";

    private readonly List<Guid> mediaAssetIds = [];
    private readonly List<VehicleOptionalFieldValue> optionalFieldValues = [];
    private readonly List<PriceChange> priceChanges = [];

    /// <summary>
    /// YALNIZCA EF içindir (0010a **D10**). Agregat bu spec'e kadar hiç kaydedilmemişti;
    /// kalıcılık tesisatı C17'nin yasakladığı "davranış yeniden yazımı" DEĞİLDİR — hiçbir
    /// domain metodunun davranışı değişmiyor.
    /// </summary>
    private Vehicle()
        : base(default)
    {
        Specification = default;
        Color = LocalizedText.Empty;
        Vin = default;
    }

    private Vehicle(
        VehicleId id,
        Guid branchId,
        Guid vehicleModelId,
        VehicleSpecification specification,
        LocalizedText color,
        int modelYear,
        int mileageKilometers,
        ListingPrice price,
        Vin vin,
        DateTimeOffset createdAt)
        : base(id)
    {
        Vin = vin;
        priceChanges.Add(new PriceChange
        {
            Id = Guid.CreateVersion7(),
            VehicleId = id,
            Price = price,
            ChangedAt = createdAt,
        });

        BranchId = branchId;
        VehicleModelId = vehicleModelId;
        Specification = specification;
        Color = color;
        ModelYear = modelYear;
        MileageKilometers = mileageKilometers;
        Price = price;
    }

    /// <summary>Başka modülün (Dealership) kimliği: kısıtsız Guid, foreign key yok (Yasak Listesi 2).</summary>
    internal Guid BranchId { get; private set; }

    /// <summary>Referans verinin (VehicleReference) kimliği; model adı orada yaşar.</summary>
    internal Guid VehicleModelId { get; }

    internal VehicleSpecification Specification { get; }

    /// <summary>Kasa tipi çekirdek alandır ve daraltmanın tek eksenidir (0001 R8).</summary>
    internal BodyTypeSlug BodyType => Specification.BodyType;

    /// <summary>
    /// Renk taksonomi değildir; galeriye göre değişen serbest metindir ve locale başına
    /// taşınır, sessiz fallback'le (docs/architecture.md § 3, docs/domain.md).
    /// </summary>
    internal LocalizedText Color { get; }

    internal int ModelYear { get; }

    /// <summary>A5 — galeri genelinde tekil; `Arşiv` muaf (D11). Her zaman GİZLİ (F4).</summary>
    internal Vin Vin { get; private set; }

    /// <summary>
    /// A6 — silinemez ve düzeltilemez. Kaldırma metodu YOKTUR ve bu bir eksiklik değil,
    /// invariantın kendisidir (AC-24, gözlem noktası C19).
    /// </summary>
    internal IReadOnlyList<PriceChange> PriceChanges => priceChanges;

    internal int MileageKilometers { get; private set; }

    internal ListingPrice Price { get; private set; }

    /// <summary>
    /// A8: şube kapatıldığında araç yayından kalkar ve "lokasyon ataması bekliyor"
    /// durumuna geçer. Bu altıncı bir yayın durumu DEĞİLDİR — ListingStatus A3'teki beş
    /// değerde kalır, bekleme aracın kendi bayrağıdır.
    /// </summary>
    internal bool AwaitingBranchAssignment { get; private set; }

    internal IReadOnlyList<Guid> MediaAssetIds => mediaAssetIds;

    /// <summary>Sıralı medya listesinin ilk elemanı (docs/domain.md, Kapak Görseli).</summary>
    internal Guid? CoverAssetId => mediaAssetIds.Count > 0 ? mediaAssetIds[0] : null;

    internal IReadOnlyList<VehicleOptionalFieldValue> OptionalFieldValues => optionalFieldValues;

    /// <summary>
    /// D8 — "taslak" çekirdek alanların HEPSİ girilmiş kayıttır. Yedi parametrenin hiçbiri
    /// nullable değil ve bu bilinçli: alanları nullable yapıp doğrulamayı yayına taşımak
    /// `0007`'nin invariant korumasına dokunurdu.
    ///
    /// <para>Şase ve oluşturma zamanı `0010a`'da eklendi (D2, D3). İlk fiyat da geçmişe
    /// yazılır: "eski kayıt olduğu gibi kalır" (AC-23) ancak karşılaştırılacak bir ilk
    /// kayıt varsa ölçülebilir.</para>
    /// </summary>
    internal static Result<Vehicle> Create(
        Guid branchId,
        Guid vehicleModelId,
        VehicleSpecification specification,
        LocalizedText color,
        int modelYear,
        int mileageKilometers,
        ListingPrice price,
        Vin vin,
        DateTimeOffset createdAt)
    {
        if (mileageKilometers < 0)
        {
            return Result.Failure<Vehicle>(new Error(NegativeMileage));
        }

        if (modelYear is < 1900 or > 2200)
        {
            return Result.Failure<Vehicle>(new Error(ImplausibleModelYear));
        }

        return Result.Success(new Vehicle(
            VehicleId.New(),
            branchId,
            vehicleModelId,
            specification,
            color,
            modelYear,
            mileageKilometers,
            price,
            vin,
            createdAt));
    }

    internal void AttachMediaAsset(Guid mediaAssetId)
    {
        if (!mediaAssetIds.Contains(mediaAssetId))
        {
            mediaAssetIds.Add(mediaAssetId);
        }
    }

    /// <summary>
    /// A6 — fiyat değişikliği geçmişe YAZILIR ve eski kayıt olduğu gibi kalır. Geçmiş
    /// `Vehicle`'ın içinde yaşıyor çünkü invariant agregatın invariantı: dışarıdan bir
    /// servis yazsaydı, fiyatı değiştirip geçmişi yazmayan bir yol açık kalırdı.
    /// </summary>
    internal void ChangePrice(ListingPrice price, DateTimeOffset at)
    {
        Price = price;

        priceChanges.Add(new PriceChange
        {
            Id = Guid.CreateVersion7(),
            VehicleId = Id,
            Price = price,
            ChangedAt = at,
        });
    }

    /// <summary>
    /// Alan kapatıldığında değer SİLİNMEZ (F1); yalnız yanıta girmez (F3).
    /// Bu yüzden değer yazmak alan ayarına bakmaz — ayrım sorgu tarafında yapılır (C4).
    /// </summary>
    internal void SetOptionalFieldValue(string fieldKey, LocalizedText value)
    {
        VehicleOptionalFieldValue? mevcut =
            optionalFieldValues.Find(item => item.FieldKey == fieldKey);

        if (mevcut is null)
        {
            optionalFieldValues.Add(new VehicleOptionalFieldValue { FieldKey = fieldKey, Value = value });
            return;
        }

        mevcut.Value = value;
    }

    internal Result ChangeMileage(int mileageKilometers)
    {
        if (mileageKilometers < 0)
        {
            return Result.Failure(new Error(NegativeMileage));
        }

        MileageKilometers = mileageKilometers;
        return Result.Success();
    }

    internal void AssignBranch(Guid branchId)
    {
        BranchId = branchId;
        AwaitingBranchAssignment = false;
    }

    internal void MarkAwaitingBranchAssignment() => AwaitingBranchAssignment = true;

    /// <summary>A2'nin bu spec'te doğrulanabilen kısmı: en az bir medya varlığı hazır olmalı.</summary>
    internal Result EnsurePublishable() => mediaAssetIds.Count > 0
        ? Result.Success()
        : Result.Failure(new Error(MediaRequired));
}
