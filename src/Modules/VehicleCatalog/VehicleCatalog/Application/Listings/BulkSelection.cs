using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// Toplu eylemin **istek düzeyi** reddi (`0012c` **R3**). Boş seçim ve sınır aşımı burada
/// ölçülüyor; satır düzeyindeki her şey komutun işi.
///
/// <para><b>Saf</b> ve bilinçli: kural veritabanı görmeden sınanabiliyor ve sınırın
/// <b>dâhil</b> olması ile tekilleştirmenin sınırdan <b>önce</b> gelmesi tek bir yerde
/// yazılı (**D5**, **D29**).</para>
/// </summary>
internal static class BulkSelectionErrors
{
    /// <summary>
    /// **D5** — boş seçim istek düzeyi hatasıdır (`422`), boş bir sonuç listesi değil.
    /// `docs/conventions.md` § 5 bunu adıyla sayıyor: "Boş seçim ve üst sınırın aşılması
    /// **istek düzeyi** hatadır".
    /// </summary>
    internal const string Empty = "listing.selection_empty";

    /// <summary>
    /// **D29** — seçim **KIRPILMAZ**, reddedilir. § 3'ün `pageSize` kırpması adres
    /// çubuğundan gelen bir <b>ipucu</b> içindir; seçim personelin açık eylemidir ve
    /// kırpmak "17 kayıt uygulanmadı"yı sessizce üretirdi.
    /// </summary>
    internal const string LimitExceeded = "listing.selection_limit_exceeded";
}

/// <summary>
/// Doğrulanmış seçim: boş değil, teklenmiş ve sınırın içinde. Sıra **korunuyor** çünkü
/// satırlar gönderildiği sırada işleniyor ve sonuç listesi aynı sırayı taşıyor (**D47**).
/// </summary>
internal readonly record struct BulkSelection(IReadOnlyList<Guid> VehicleIds)
{
    /// <summary>
    /// <b>Sıra pazarlığa kapalı:</b> önce tekilleştir, sonra sınırı ölç (**D29**). Tersi
    /// olsaydı aynı aracı yirmi kez gönderen bir seçim sınırı aşmış sayılırdı — oysa
    /// personel tek bir araç seçmişti.
    ///
    /// <para><c>Guid.Empty</c> ELENMİYOR: var olmayan bir kimlik olarak kendi satırını
    /// alır ve `vehicle.not_found` döner (**D27**). Sessizce düşürmek, gönderilen satır
    /// sayısıyla dönen satır sayısını ayrıştırırdı.</para>
    ///
    /// <para>Sınır **DÂHİL**: tam sınır kadar araç taşıyan seçim geçer.</para>
    /// </summary>
    internal static Result<BulkSelection> Create(IReadOnlyList<Guid>? vehicleIds, int limit)
    {
        List<Guid> tekil = [.. (vehicleIds ?? []).Distinct()];

        if (tekil.Count == 0)
        {
            return Result.Failure<BulkSelection>(new Error(BulkSelectionErrors.Empty));
        }

        if (tekil.Count > limit)
        {
            return Result.Failure<BulkSelection>(Error.With(
                BulkSelectionErrors.LimitExceeded,
                "limit",
                limit.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        return Result.Success(new BulkSelection(tekil));
    }
}
