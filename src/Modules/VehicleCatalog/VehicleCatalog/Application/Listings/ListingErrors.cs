using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Application.Listings;

/// <summary>
/// Yayın reddinin kararlı kodları (`0012b` **D2**, **D25**, **D33**).
///
/// <para>Ortak önek bilinçli: yanıt bir <b>liste</b> taşıyor ve personele "yayına
/// alınamadı, şunlar eksik" demek için çağıranın kodları tek bir aileye göre
/// gruplayabilmesi gerekiyor (`docs/conventions.md` § 5).</para>
///
/// <para><b><c>NoMedia</c> yeniden adlandırılmış bir koddur</b> (**D33**): `Vehicle`'ın
/// içinde `vehicle.media_required` olarak duruyordu, <b>sıfır tüketicisi</b> vardı ve
/// dokümanlar zaten iki yerde bu adı yazıyordu (`conventions.md:121`, `frontend.md:111`).
/// Konvansiyonun "yayınlanmış kodun anlamı değişmez" kuralı bağlamıyor — bu kodu bugün
/// hiçbir uç dönmüyordu.</para>
/// </summary>
internal static class PublishErrors
{
    /// <summary>
    /// Reddin ÜST kodu. Gövde bunu ve `reasons` parametresinde **koşul başına bir kodu**
    /// taşır (**D2**): tek kod dönmek personeli tek tek denemeye zorlardı ve aynı hata
    /// `0010a` AC-42'de ölçüldü.
    /// </summary>
    internal const string CannotPublish = "vehicle.cannot_publish";
    /// <summary>A2 — en az bir medya varlığı `Hazır` olmalı (**M3**).</summary>
    internal const string NoMedia = "vehicle.cannot_publish.no_media";

    /// <summary>
    /// A2 — zorunlu işaretli opsiyonel alanlar dolu olmalı. **TEK kod** ve eksik alanların
    /// anahtarları yapısal parametre olarak geliyor (**D24**): alan kümesi çalışma
    /// zamanında değişiyor ve her alan için ayrı bir kod üretmek, kodu veriye çevirirdi.
    /// </summary>
    internal const string RequiredField = "vehicle.cannot_publish.required_field";

    /// <summary>
    /// **R21**/**D25** — A2 dışındaki birinci red: araç lokasyon ataması bekliyor (**A8**).
    /// Personel bunu düzeltebilir — düzenleme ekranından şube atanır.
    /// </summary>
    internal const string BranchUnassigned = "vehicle.cannot_publish.branch_unassigned";

    /// <summary>
    /// **R21**/**D25** — A2 dışındaki ikinci red: ilanın adresi üretilemedi. Kanonik model
    /// adı Latin olmak zorunda (`ListingSlug.InvalidSource`) ve o ad referans veridedir;
    /// **personelin düzeltebileceği bir şey değil** ve panel bunu böyle söyler.
    /// </summary>
    internal const string SlugUnavailable = "vehicle.cannot_publish.slug_unavailable";

    /// <summary>
    /// Eksikleri TEK bir `Error`'da toplar: üst kod + `reasons` listesi + alt kodların
    /// kendi yapısal parametreleri.
    ///
    /// <para>Virgülle birleştirme bu spec'in kendi deseni (**D24**): alan anahtarları da
    /// aynı biçimde geliyor. Listeyi `Error`'a yapısal olarak gömmek `BuildingBlocks`'u
    /// zenginleştirirdi (Yasak Listesi 11) ve kazancı yoktu — uç zaten bir dizeyi ayırıp
    /// kataloğundan çeviriyor.</para>
    /// </summary>
    internal static Error Aggregate(IReadOnlyList<Error> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);

        Dictionary<string, string> parametreler = new(StringComparer.Ordinal)
        {
            ["reasons"] = string.Join(',', reasons.Select(reason => reason.Code)),
        };

        foreach (Error reason in reasons)
        {
            foreach (KeyValuePair<string, string> parametre in
                reason.Parameters ?? new Dictionary<string, string>(StringComparer.Ordinal))
            {
                parametreler[parametre.Key] = parametre.Value;
            }
        }

        return new Error(CannotPublish, parametreler);
    }
}
