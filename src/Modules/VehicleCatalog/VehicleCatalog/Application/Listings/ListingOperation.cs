namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// Panelin bir ilan üzerinde çağırabildiği **sekiz** işlem: A3'ün altı geçişi ve
/// `0012c`'nin iki sıralama eylemi.
///
/// <para><b>`ListingAction` ile birleştirilmedi</b> ve gerekçesi ölçülebilir: o enum
/// `docs/domain.md` A3'ün birebir aynasıdır, `GecisTablosuTests` ile korunuyor ve panelin
/// `TRANSITIONS` tablosu ondan türüyor. Öne çıkarma bir geçiş değil — aynı enum'a
/// konsaydı ayna testi anlamını kaybeder ve taslak bir ilanda "öne çıkar" düğmesinin
/// görünürlüğü A3'ün tablosundan türetilmeye çalışılırdı.</para>
/// </summary>
internal enum ListingOperation
{
    Publish = 0,
    Unpublish = 1,
    Reserve = 2,
    Unreserve = 3,
    MarkSold = 4,
    Archive = 5,
    Feature = 6,
    Unfeature = 7,
}

/// <summary>
/// İşlemlerin rota sözcükleri ve `ListingAction` eşlemesi.
///
/// <para>Eşleme ELLE yazılmış: rota adını enum'dan TÜRETMEK `MarkSold` → `mark-sold` gibi
/// bir dönüşüm kuralı gerektirirdi ve o kural adresi enum adının rehinesi yapardı
/// (`0012b`'nin aynı kararı).</para>
/// </summary>
internal static class ListingOperations
{
    /// <summary>Tekil yüzeyin sekiz rotası.</summary>
    internal static readonly (string Route, ListingOperation Operation)[] All =
    [
        ("publish", ListingOperation.Publish),
        ("unpublish", ListingOperation.Unpublish),
        ("reserve", ListingOperation.Reserve),
        ("unreserve", ListingOperation.Unreserve),
        ("sold", ListingOperation.MarkSold),
        ("archive", ListingOperation.Archive),
        ("feature", ListingOperation.Feature),
        ("unfeature", ListingOperation.Unfeature),
    ];

    /// <summary>
    /// **D18** — toplu yüzeyin **altı** rotası. Rezerve etme ve rezervasyonu kaldırma
    /// burada **YOK**: rezervasyon bir müşteriye aittir ve yirmi aracı kimseye rezerve
    /// etmek anlamsızdır.
    ///
    /// <para>Yokluğun HTTP karşılığı `404`'tür ve bu bilinçli: rota hiç yok. `422` dönmek
    /// *"istediğin oldu ama olmadı"* derdi — oysa böyle bir uç tanımlanmadı.</para>
    /// </summary>
    internal static readonly (string Route, ListingOperation Operation)[] Bulk =
    [
        .. All.Where(item => item.Operation
            is not (ListingOperation.Reserve or ListingOperation.Unreserve))
    ];

    /// <summary>İşlem bir öne çıkarma eylemi mi? Öyleyse hedef değeri de söyler.</summary>
    internal static bool IsFeature(this ListingOperation operation, out bool featured)
    {
        featured = operation is ListingOperation.Feature;

        return operation is ListingOperation.Feature or ListingOperation.Unfeature;
    }

    /// <summary>
    /// Geçiş karşılığı. Öne çıkarma işlemleri için çağrılmaz — çağıran önce
    /// <see cref="IsFeature"/> sorar ve iki yol ayrı komutlara gider.
    /// </summary>
    internal static ListingAction ToAction(this ListingOperation operation) => operation switch
    {
        ListingOperation.Publish => ListingAction.Publish,
        ListingOperation.Unpublish => ListingAction.Unpublish,
        ListingOperation.Reserve => ListingAction.Reserve,
        ListingOperation.Unreserve => ListingAction.Unreserve,
        ListingOperation.MarkSold => ListingAction.MarkSold,
        ListingOperation.Archive => ListingAction.Archive,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
    };
}
