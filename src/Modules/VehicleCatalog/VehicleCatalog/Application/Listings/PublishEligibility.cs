using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// **A2**'nin yayın anında doğrulanabilen koşulları, **hepsi birlikte** (**D2**).
///
/// <para><b>İlk eksikte durulmuyor</b> ve bu ölçülmüş bir karardan geliyor: tek kod
/// dönmek personeli tek tek denemeye zorlar. Aynı hata `0010a` AC-42'de ölçüldü ve
/// `Listing.TitleTooLong`/`DescriptionTooLong` tam bu yüzden ikiye ayrıldı.</para>
///
/// <para><b>A2'nin dört koşulundan İKİSİ burada YOK</b> ve bu bir eksiklik değil, bir
/// ölçümün sonucu (**D23**, **D42**):</para>
/// <list type="bullet">
/// <item><b>Çekirdek alanlar</b> — `Vehicle.Create` dokuz çekirdek alanı zorunlu parametre
/// alıyor; çekirdek alanı boş bir araç kaydı kurulamaz.</item>
/// <item><b>Fiyat</b> — `ListingPrice`'ın tek kurucusu `private` ve yalnız iki üretici var
/// (`OnRequest()`, `Of(money)`); dönüştürücü de `Amount is null` hâlini `onrequest` olarak
/// yazıp geri okurken yalnız o iki değeri üretiyor. "Fiyatı belirlenmemiş ve fiyat sorunuz
/// da olmayan araç" kurulamıyor, kaydedilemiyor ve okunamıyor.</item>
/// </list>
/// <para>Temsil edilemeyen bir koşulu burada denetlemek <b>vakum bir kontrol</b> üretirdi
/// — hiçbir zaman kırmızıya dönmeyen bir dal, olmayan bir kapıdır. İkisinin temsil
/// edilemezliği `YayinUygunluguTests`'teki bir <b>tripwire</b> ile korunuyor.</para>
///
/// <para><b>Lokasyon ve adres koşulları A2'nin DIŞINDA</b> (**R21**, **D25**) ve burada
/// değil, komutun kendisinde ölçülüyor: biri aracın bayrağına, diğeri referans verisine
/// bakıyor ve ikisi de agregatın elinde olan bir bilgi değil.</para>
/// </summary>
internal static class PublishEligibility
{
    /// <summary>
    /// Sağlanmayan koşulların **hepsi**. Boş liste = yayına alınabilir.
    /// </summary>
    /// <param name="vehicle">Yayına alınacak aracın agregatı.</param>
    /// <param name="readyMediaAssetIds">
    /// `Media`'ya göre `Hazır` varlıkların kimlikleri (**C4**: olay gövdesinden değil,
    /// okuma yüzeyinden). Kesişim `Vehicle.EnsurePublishable` içinde alınıyor.
    /// </param>
    /// <param name="requiredFieldKeys">
    /// O an **zorunlu** işaretli opsiyonel alanların anahtarları. Küme çalışma zamanında
    /// değişiyor; **F2** gereği geriye dönük doğrulama yapılmıyor ama **A2 yayın anında
    /// bağlayıcı**.
    /// </param>
    internal static IReadOnlyList<Error> Evaluate(
        Vehicle vehicle,
        IReadOnlySet<Guid> readyMediaAssetIds,
        IReadOnlySet<string> requiredFieldKeys)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        List<Error> eksikler = [];

        if (vehicle.EnsurePublishable(readyMediaAssetIds).IsFailure)
        {
            eksikler.Add(new Error(PublishErrors.NoMedia));
        }

        IReadOnlyList<string> eksikAlanlar = MissingRequiredFields(vehicle, requiredFieldKeys);

        if (eksikAlanlar.Count > 0)
        {
            // D24 — anahtarlar YAPISAL PARAMETRE, kodun parçası değil. Alan başına kod
            // üretmek, kodu veriye çevirir ve her yeni alan uçta yeni bir çeviri anahtarı
            // ister; oysa uç "şu alanlar eksik" cümlesini tek bir kalıpla kurabilir.
            eksikler.Add(Error.With(
                PublishErrors.RequiredField,
                "fields",
                string.Join(',', eksikAlanlar)));
        }

        return eksikler;
    }

    /// <summary>
    /// **D27** — kural TEK yerde. `VehicleAdminQueries` aynı hesabı liste rozeti için
    /// yapıyordu; ikisi ayrı yazılsaydı aynı invariant iki yerde iki farklı şey olabilirdi
    /// (`0010b`'nin bilinen tuzağı 3). Çekirdek alanlar bu hesaba GİRMEZ: `0010a` D8 onları
    /// kayıt anında zorunlu kılıyor.
    /// </summary>
    internal static IReadOnlyList<string> MissingRequiredFields(
        Vehicle vehicle,
        IReadOnlySet<string> requiredFieldKeys)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(requiredFieldKeys);

        if (requiredFieldKeys.Count == 0)
        {
            return [];
        }

        HashSet<string> dolu = [.. vehicle.OptionalFieldValues.Select(value => value.FieldKey)];

        return [.. requiredFieldKeys.Where(key => !dolu.Contains(key)).OrderBy(key => key, StringComparer.Ordinal)];
    }
}
