namespace CarGallery.Modules.VehicleCatalog.Contracts.Admin;

/// <summary>
/// **Yönetim yüzeyi — vitrin yüzeyinden AYRI bir sözleşmedir** (C20).
///
/// <para>İki sözleşme aynı DTO ile taşınmaz ve bu bir biçim tercihi değil: gizli alanlar
/// vitrin yanıtında <b>hiç bulunmaz</b>, boş değer olarak da bulunmaz (Yasak Listesi 41,
/// AC-18). Tek bir DTO paylaşılsaydı şase alanı vitrin yanıtında `null` olarak görünürdü
/// ve "gizli" sözü kâğıt üstünde kalırdı.</para>
///
/// <para>Buradaki her yanıt oturum gerektirir (AC-19, C1): tip
/// `AdminSurface.RoutePrefix` altındaki uçlardan döner.</para>
/// </summary>
public sealed record VehicleAdminListItemDto(
    Guid VehicleId,
    string ModelName,
    int ModelYear,
    int MileageKilometers,
    VehicleAdminPriceDto Price,
    string ListingStatus,
    string Vin);

/// <summary>
/// "Fiyat sorunuz" AYRI bir durumdur, `0` değildir (P4). Tutar, para kodu ve minor unit
/// üssü birlikte taşınır; üs kaybolursa üç ondalıklı para birimleri sessizce bozulur (P2).
/// </summary>
public sealed record VehicleAdminPriceDto(
    bool IsOnRequest,
    decimal? Amount,
    string? CurrencyCode,
    int? MinorUnitExponent);

public interface IVehicleAdminQueries
{
    /// <summary>
    /// 0010a/R18 — sade liste. Sayfalama, sıralama ve göstergeler `0010b`'dedir (X18);
    /// burası yalnız "kaydım göründü mü" sorusunu cevaplar.
    /// </summary>
    Task<IReadOnlyList<VehicleAdminListItemDto>> ListAsync(string locale, CancellationToken cancellationToken);
}
