using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Application.Projection;

/// <summary>
/// Vitrinde görünürlük kararının TEK yeri. Gizleme sunum katmanında değil, Catalog'un
/// sorgu tarafında uygulanır (C10, Yasak Listesi 41) — burada karar verilir, okuma
/// modeline yalnızca görünür ilanlar yazılır.
/// </summary>
internal static class ShowcaseListingProjector
{
    /// <summary>
    /// R2: yalnız Yayında ve Rezerve. A8 uyarınca lokasyon ataması bekleyen araç,
    /// ilanı ne olursa olsun vitrinde yer almaz — "sessizce vitrinde kalmazlar".
    /// Görünmeyen ilan için <c>null</c> döner; çağıran satırı siler.
    /// </summary>
    /// <param name="publicOptionalFieldKeys">
    /// O an AÇIK ve PUBLIC olan opsiyonel alanların anahtarları. Listede olmayan alanın
    /// değeri okuma modeline HİÇ yazılmaz — gizleme yansıtma anında uygulanır, sunum
    /// katmanında değil (C4, Yasak Listesi 41). Değer `Vehicle`'da durmaya devam eder (F1).
    /// </param>
    internal static ShowcaseListing? Project(
        Vehicle vehicle,
        Listing listing,
        ListingSlug slug,
        IReadOnlyDictionary<string, string> branchNames,
        string? coverMediaUrl,
        IReadOnlySet<string> publicOptionalFieldKeys)
    {
        if (vehicle.AwaitingBranchAssignment)
        {
            return null;
        }

        if (listing.Status is not (ListingStatus.Published or ListingStatus.Reserved))
        {
            return null;
        }

        if (listing.PublishedAt is null)
        {
            return null;
        }

        return new ShowcaseListing
        {
            ListingId = listing.Id.Value,
            VehicleId = vehicle.Id.Value,
            IsReserved = listing.Status is ListingStatus.Reserved,
            ShowcaseSortWeight = listing.ShowcaseSortWeight,
            PublishedAt = listing.PublishedAt.Value,
            Slug = slug.Value,
            BodyTypeSlug = vehicle.BodyType.Value,
            FuelTypeSlug = vehicle.Specification.FuelTypeSlug,
            TransmissionTypeSlug = vehicle.Specification.TransmissionTypeSlug,
            DrivetrainTypeSlug = vehicle.Specification.DrivetrainTypeSlug,
            TrimLevelSlug = vehicle.Specification.TrimLevelSlug,
            ColorNames = vehicle.Color.ToDictionary(),
            VehicleModelId = vehicle.VehicleModelId,
            ModelYear = vehicle.ModelYear,
            MileageKilometers = vehicle.MileageKilometers,
            PriceAmount = vehicle.Price.Amount?.Amount,
            PriceCurrencyCode = vehicle.Price.Amount?.CurrencyCode,
            PriceMinorUnitExponent = vehicle.Price.Amount?.MinorUnitExponent,
            PriceOnRequest = vehicle.Price.IsOnRequest,
            BranchId = vehicle.BranchId,
            BranchNames = new Dictionary<string, string>(branchNames),
            CoverAssetId = vehicle.CoverAssetId,
            CoverMediaUrl = coverMediaUrl,
            OptionalValues =
            [
                .. vehicle.OptionalFieldValues
                    .Where(value => publicOptionalFieldKeys.Contains(value.FieldKey))
                    .Where(value => IsLocaleSafeAtRest(value.Value.Kind))
                    .Select(value => new ShowcaseListingOptionalValue
                    {
                        ListingId = listing.Id.Value,
                        FieldKey = value.FieldKey,
                        Values = value.Value.Localized!.ToDictionary(),
                    })
            ],
        };
    }

    /// <summary>
    /// 0010b **D28** — hangi tipler vitrin okuma modeline OLDUĞU GİBİ taşınabilir.
    ///
    /// <para>Okuma modeli locale → metin tutuyor ve ziyaretçiye o metin olduğu gibi
    /// gösteriliyor. Tutar, tarih, tam sayı ve evet/hayır değerlerinin metin hâli
    /// LOCALE'E BAĞLIDIR; sunucuda metne çevirmek `docs/conventions.md` § 7'yi deler ve
    /// Arapça'da yanlış ayraç üretir. `seçenek` ise slug taşır ve ham slug gösterilmez.</para>
    ///
    /// <para>Düz `metin` de dışarıda ve bu bilinçli: okuma modeli locale ANAHTARLI bir
    /// sözlük tutuyor, düz metnin ise locale'i yok. Onu boş bir sözlükle yazmak satırı
    /// sessizce değersiz bırakırdı — bu depoda tekrar eden hata.</para>
    ///
    /// <para>Doğru çözüm okuma modeline TİPLİ bir kanal açmaktır; o da `0002`'nin
    /// sözleşmesini değiştirir ve bu spec'in kapsamı dışındadır. Bugün public opsiyonel
    /// alan YOK, yani bedel sıfır — ve bir alan public yapıldığında sessiz kalmaması için
    /// mimari bir tripwire testi var.</para>
    /// </summary>
    private static bool IsLocaleSafeAtRest(FieldValueKind kind) =>
        kind is FieldValueKind.LocalizedText;
}
