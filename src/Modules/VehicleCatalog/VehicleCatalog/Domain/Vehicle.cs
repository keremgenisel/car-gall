using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain.Events;

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

    /// <summary>`0012a`/AC-8 — aynı varlık bir araca iki kez bağlanamaz.</summary>
    internal const string DuplicateMediaAsset = "vehicle.duplicate_media_asset";

    /// <summary>`0012a`/AC-9 — bağlanmak istenen varlık `Media`'da yok.</summary>
    internal const string MediaAssetNotFound = "vehicle.media_asset_not_found";

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
        UpdatedAt = createdAt;
    }

    /// <summary>Başka modülün (Dealership) kimliği: kısıtsız Guid, foreign key yok (Yasak Listesi 2).</summary>
    internal Guid BranchId { get; private set; }

    /// <summary>Referans verinin (VehicleReference) kimliği; model adı orada yaşar.</summary>
    internal Guid VehicleModelId { get; }

    internal VehicleSpecification Specification { get; private set; }

    /// <summary>Kasa tipi çekirdek alandır ve daraltmanın tek eksenidir (0001 R8).</summary>
    internal BodyTypeSlug BodyType => Specification.BodyType;

    /// <summary>
    /// Renk taksonomi değildir; galeriye göre değişen serbest metindir ve locale başına
    /// taşınır, sessiz fallback'le (docs/architecture.md § 3, docs/domain.md).
    /// </summary>
    internal LocalizedText Color { get; private set; }

    internal int ModelYear { get; private set; }

    /// <summary>A5 — galeri genelinde tekil; `Arşiv` muaf. Her zaman GİZLİ (F4).</summary>
    internal Vin Vin { get; private set; }

    /// <summary>
    /// A5'in muafiyeti: arşivlenmiş aracın şasesi yeniden kullanılabilir (AC-17).
    ///
    /// <para><b>Neden `Vehicle`'da, `Listing.Status`'te değil.</b> Tekillik kısmi bir tekil
    /// İNDEKSLE korunuyor ve indeks yalnız kendi tablosunu görebilir; durum `listings`'te
    /// durduğu sürece `vehicles` üzerindeki indeks muafiyeti uygulayamaz. Bu bayrak o
    /// bilginin araç tarafındaki yansımasıdır ve ilan arşivlendiği anda — `0012`'nin işi —
    /// aynı transaction'da yazılır.</para>
    ///
    /// <para>`0010a`'da hiçbir şey arşivlemez (X2), yani bayrak her zaman `false`; AC-17
    /// doğrudan agregat üzerinden sınanır.</para>
    /// </summary>
    internal bool IsArchived { get; private set; }

    /// <summary>
    /// A6 — silinemez ve düzeltilemez. Kaldırma metodu YOKTUR ve bu bir eksiklik değil,
    /// invariantın kendisidir (AC-24, gözlem noktası C19).
    /// </summary>
    internal IReadOnlyList<PriceChange> PriceChanges => priceChanges;

    internal int MileageKilometers { get; private set; }

    internal ListingPrice Price { get; private set; }

    /// <summary>
    /// 0010b **D25** — listenin "son güncellenen" ekseni. Ayrı bir `CreatedAt` kolonu YOK:
    /// kimlik UUID v7 ve zaten zaman sıralı; aynı bilgiyi ikinci kez saklamak olurdu.
    /// </summary>
    internal DateTimeOffset UpdatedAt { get; private set; }

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
        Result validation = ValidateCore(modelYear, mileageKilometers);

        if (validation.IsFailure)
        {
            return Result.Failure<Vehicle>(validation.Error);
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

    /// <summary>
    /// 0010b **R2/C25** — düzenleme TAM DEĞİŞTİRMEDİR ve çekirdek alanların hepsi birlikte
    /// gelir. Doğrulama `Create` ile AYNI yerden geçiyor (<see cref="ValidateCore"/>):
    /// iki yolun iki ayrı kuralı olsaydı `0010a` D8'in invariantı iki yerde iki farklı şey
    /// olurdu — bu spec'in bilinen tuzaklarından biri (3).
    ///
    /// <para>Şase BURADA YOK ve bu bilinçli: **D20/R4** şaseyi düzenlemeye kapatıyor.
    /// İsteğin taşıdığı şase ile saklanan şasenin aynı olup olmadığı komut katmanının
    /// işi (**D24**) — agregat değiştirilemez bir alanı hiç almaz.</para>
    /// </summary>
    internal Result ChangeCoreDetails(
        VehicleSpecification specification,
        LocalizedText color,
        int modelYear,
        int mileageKilometers,
        DateTimeOffset at)
    {
        Result validation = ValidateCore(modelYear, mileageKilometers);

        if (validation.IsFailure)
        {
            return validation;
        }

        Specification = specification;
        Color = color;
        ModelYear = modelYear;
        MileageKilometers = mileageKilometers;
        Touch(at);

        return Result.Success();
    }

    /// <summary>Her yazma yolundan sonra çağrılır; `UpdatedAt` tek kapıdan geçer (D25).</summary>
    internal void Touch(DateTimeOffset at) => UpdatedAt = at;

    private static Result ValidateCore(int modelYear, int mileageKilometers)
    {
        if (mileageKilometers < 0)
        {
            return Result.Failure(new Error(NegativeMileage));
        }

        return modelYear is < 1900 or > 2200
            ? Result.Failure(new Error(ImplausibleModelYear))
            : Result.Success();
    }

    internal void AttachMediaAsset(Guid mediaAssetId)
    {
        if (!mediaAssetIds.Contains(mediaAssetId))
        {
            mediaAssetIds.Add(mediaAssetId);
        }
    }

    /// <summary>
    /// `0012a` D12/AC-2…6 — aracın sıralı medya listesini **bütünüyle yerine koyar**.
    /// Bağlama, sıralama ve çıkarma tek **idempotent** işlemdir.
    ///
    /// <para><b>Kapak ayrı bir alan DEĞİL</b> (D2): sıralı listenin ilk elemanıdır
    /// (<see cref="CoverAssetId"/>) ve sözlük onu böyle tanımlıyor. İkinci bir "kapak"
    /// alanı aynı olguyu iki yerde tutar ve biri bayatlar; bu yüzden "kapak yap" bir
    /// <b>sıra</b> işlemidir.</para>
    ///
    /// <para><b>Durum ön koşulu yoktur</b> (D14): `işleniyor` ve `başarısız` varlıklar da
    /// bağlanabilir. R5 başarısız varlığın panelde görünmesini istiyor ve görünmesi için
    /// bağlı olması gerekir; **M3** yalnız YAYIN koşulunu `hazır`a bağlıyor. Yasaklamak,
    /// işlemenin sürdüğü dakikalarda paneli kullanılamaz kılardı.</para>
    ///
    /// <para>Varlıkların GERÇEKTEN var olduğunu bu metot bilemez — o `Media`'nın bilgisi
    /// ve kontrol `Application` katmanında (AC-9). Domain yalnız kendi invariantını
    /// koruyor: liste **yinelenen** taşımaz (AC-8).</para>
    /// </summary>
    internal Result ReplaceMediaAssets(IReadOnlyList<Guid> ordered)
    {
        ArgumentNullException.ThrowIfNull(ordered);

        if (ordered.Distinct().Count() != ordered.Count)
        {
            return Result.Failure(new Error(DuplicateMediaAsset));
        }

        mediaAssetIds.Clear();
        mediaAssetIds.AddRange(ordered);

        return Result.Success();
    }

    /// <summary>
    /// A6 — fiyat değişikliği geçmişe YAZILIR ve eski kayıt olduğu gibi kalır. Geçmiş
    /// `Vehicle`'ın içinde yaşıyor çünkü invariant agregatın invariantı: dışarıdan bir
    /// servis yazsaydı, fiyatı değiştirip geçmişi yazmayan bir yol açık kalırdı.
    /// </summary>
    /// <param name="at">0012b **D11** — değişiklik DUYURULUR; olay Application katmanında
    /// entegrasyon olayına çevrilir (§ 2.B). Domain, modül dışındaki sözleşmeyi bilmez.</param>
    internal void ChangePrice(ListingPrice price, DateTimeOffset at)
    {
        Price = price;
        Touch(at);
        Raise(new VehiclePriceWasChanged(Id));

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
    ///
    /// <para>0010b **C23** — değer artık TİPLİ. İmza değişikliği C17'nin adıyla yazılmış
    /// istisnasıdır; davranış aynı kalıyor: aynı anahtar ikinci kez yazılırsa üzerine
    /// yazar, yoksa ekler.</para>
    /// </summary>
    internal void SetOptionalFieldValue(string fieldKey, OptionalFieldValue value)
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

    /// <summary>
    /// Şube ataması. **A7** değişimin duyurulmasını istiyor (`0012b` **D11**); transfer
    /// AKIŞI kapsam dışı (X7) — burada yalnız olgu kaydediliyor.
    ///
    /// <para>Olay yalnız şube GERÇEKTEN değiştiğinde yayılıyor: aynı şubeye atamak bir
    /// taşınma değildir ve `A8`'in bayrağını temizlemek için de bu yol kullanılıyor.</para>
    /// </summary>
    internal void AssignBranch(Guid branchId)
    {
        bool tasindi = BranchId != branchId;

        BranchId = branchId;
        AwaitingBranchAssignment = false;

        if (tasindi)
        {
            Raise(new VehicleWasRelocated(Id));
        }
    }

    internal void MarkAwaitingBranchAssignment() => AwaitingBranchAssignment = true;

    /// <summary>
    /// A5'in muafiyetini açar. `0012` ilanı arşivlediğinde AYNI transaction'da çağırır;
    /// ayrı çağrılırsa bayrak bayatlar ve tekillik indeksi yanlış kararı verir.
    /// </summary>
    internal void MarkArchived() => IsArchived = true;

    /// <summary>
    /// **A2**'nin medya koşulu: en az bir bağlı varlık **`hazır`** durumunda olmalı
    /// (`docs/domain.md` **M3**).
    ///
    /// <para><b>Hazırlık bilgisi PARAMETRE olarak geliyor</b> (`0012a` D11): `Domain` dış
    /// dünyayı bilmez (`docs/conventions.md` § 4) ve `Media`'yı hiç bilemez. Çağıran onu
    /// `Media`'nın okuma yüzeyinden çözüyor (**C4**: olay gövdesinden değil).</para>
    ///
    /// <para><b>Önceki hâli yalnız SAYIYA bakıyordu</b> (<c>mediaAssetIds.Count > 0</c>) ve
    /// `0011` X7 bu bedeli açıkça kabul etmişti: *"yüklenmiş ama işlenmemiş bir görseli
    /// olan araç yayına alınabilir görünür"*. `0012a` D6 şartı genişletiyor; `0011`'in
    /// uyarısı buydu — *"orada yazılı olmazsa sahipsiz kalır"*.</para>
    ///
    /// <para><b>Dört sınır durumu</b> (AC-19…22) bu tek satırdan çıkıyor: hiç varlık yok ·
    /// hepsi `işleniyor` · biri `hazır` ikisi `işleniyor` · hepsi `başarısız`. Kesişimin
    /// boş olup olmaması dördünü birlikte ayırıyor.</para>
    /// </summary>
    /// <param name="readyMediaAssetIds">
    /// `Media`'ya göre `hazır` durumdaki varlıkların kimlikleri. Aracın listesiyle
    /// kesişimi alınıyor: çağıran fazlasını verse bile karar yalnız BAĞLI varlıklara göre
    /// verilir.
    /// </param>
    internal Result EnsurePublishable(IReadOnlySet<Guid> readyMediaAssetIds)
    {
        ArgumentNullException.ThrowIfNull(readyMediaAssetIds);

        return mediaAssetIds.Exists(readyMediaAssetIds.Contains)
            ? Result.Success()
            : Result.Failure(new Error(MediaRequired));
    }
}
