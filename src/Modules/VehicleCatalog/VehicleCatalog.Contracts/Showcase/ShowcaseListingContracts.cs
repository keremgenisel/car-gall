using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

/// <summary>
/// R9: kart yalnız bu alanları taşır. Opsiyonel alanlar, şase, plaka ve iç notlar burada
/// YOKTUR — boş değer olarak da yoktur (R10, F3). Şeklin sabitliği sözleşme testiyle
/// korunur; gizleme Catalog'un sorgu tarafında uygulanır, sunum katmanında değil (C10).
///
/// ListingId ve IsReserved R9'un altı alanına ektir ve zorunludur: ilki karttan detaya
/// gidiş bağlantısı için (X9), ikincisi rezerve rozeti için (AC-2). İkisi de araç verisi
/// değil, ilanın kimliği ve yayın hâlidir.
/// </summary>
public sealed record ShowcaseListingDto(
    Guid ListingId,
    bool IsReserved,
    string? CoverImageUrl,
    string ModelName,
    int ModelYear,
    int MileageKilometers,
    ShowcasePriceDto Price,
    string BranchName);

/// <summary>
/// Sunucu ham sayı + para birimi kodu döner; biçimlendirme uçta yapılır
/// (docs/conventions.md § 7). "Fiyat sorunuz" 0 DEĞİLDİR: tutar null'dır (P4).
/// </summary>
public sealed record ShowcasePriceDto(
    bool OnRequest,
    decimal? Amount,
    string? CurrencyCode,
    int? MinorUnitExponent)
{
    public static ShowcasePriceDto OnRequestPrice() => new(true, null, null, null);
}

/// <summary>
/// pageSize opsiyoneldir ve üst sınırı 24'tür; aşan istek üst sınıra kırpılır, hata dönmez
/// (docs/conventions.md § 3). Uç hiçbir zaman göndermez, böylece C13'ün kanonik URL biçimi
/// bozulmaz ve R6'nın "sayfa başına 24" kuralı vitrinde her zaman geçerli kalır.
/// </summary>
public sealed record ShowcaseListingFilter(
    IReadOnlyList<string> BodyTypes,
    int Page,
    string Locale,
    int? PageSize = null);

public interface IShowcaseListingQueries
{
    Task<PagedResult<ShowcaseListingDto>> GetAsync(ShowcaseListingFilter filter, CancellationToken cancellationToken);
}
