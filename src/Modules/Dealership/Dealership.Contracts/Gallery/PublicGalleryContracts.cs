using System.Text.Json.Serialization;

namespace CarGallery.Modules.Dealership.Contracts.Gallery;

/// <summary>
/// `0014` **D15** — galerinin ziyaretçiye dönük okuması bir İZİN LİSTESİDİR: yanıt yalnız
/// galerinin GÖRÜNEN adını ve aktif şubelerin adını, adresini ve telefonunu taşır. Vergi ve sicil
/// numarası, logo kimliği; şubenin e-postası, WhatsApp'ı, koordinatı, slug'ı ve durumu burada
/// YOKTUR — null olarak da yoktur (AC-14). Yasak listesi sonradan eklenen bir alanı yakalamaz;
/// izin listesi yakalar.
///
/// <para>`0020` **D15** — `commercialName` yerine `displayName`: ziyaretçinin dilindeki
/// görünen ad, yoksa varsayılan dildeki (D4, AC-13, AC-16). Ticari unvan yasal addır ve hiçbir
/// public yanıtta yer almaz. Görünen ad okunamazsa boş metindir (R5a).</para>
///
/// <para>`0024` **D7** — izin listesi bilerek genişledi: <c>SocialLinks</c> her hesap için YALNIZ
/// platform kodunu ve adresi taşır, `0024` D1'in sırasıyla. Hiç hesap yoksa dizi boştur,
/// <c>null</c> değildir. Dilden bağımsızdır.</para>
/// </summary>
public sealed record PublicGalleryDto(
    string DisplayName,
    IReadOnlyList<PublicBranchDto> Branches,
    IReadOnlyList<PublicSocialLinkDto> SocialLinks);

/// <summary>`0024` R6 — platform KODU (<c>instagram</c>; çevrilmiş ad değil, C8) ve kayıtlı adres.</summary>
public sealed record PublicSocialLinkDto(string Platform, string Url);

/// <summary>
/// Telefon E.164 biçimindedir; uç onu bir <c>tel:</c> bağlantısına çevirir (AC-45).
///
/// <para>`0025` **D8** — izin listesi bilerek genişledi: <c>OpeningHours</c> programı girilmemiş
/// şubede <c>null</c>; girilmişse TAM yedi gün kodu (<c>monday</c> … <c>sunday</c>, bu sırayla),
/// kapalı gün boş dizi, aralıklar başlangıca göre sıralı. Saatler şubenin yerel duvar saatidir
/// (`0025` D2) ve dilden bağımsızdır.</para>
///
/// <para>`0028` **D5** — izin listesi bilerek bir kez daha genişledi: <c>Email</c> kayıtlı adres ya
/// da <c>null</c> (D8, SS2); <c>Map</c> iki harita adresi ya da <c>null</c> (D7). Ham koordinat,
/// saat dilimi, WhatsApp ve slug yine YOKTUR (C2, AC-23).</para>
/// </summary>
public sealed record PublicBranchDto(
    string Name,
    string Address,
    string Phone,
    IReadOnlyDictionary<string, IReadOnlyList<PublicOpeningIntervalDto>>? OpeningHours = null,
    PublicOpenNowDto? OpenNow = null,
    string? Email = null,
    PublicBranchMapDto? Map = null)
{
    /// <summary>
    /// `0027` **D8** — penceredeki özel günler (<c>through</c> ≥ bugün, <c>from</c> ≤ bugün + 30),
    /// <c>from</c>'a göre sıralı. Hiç <c>null</c> olmaz; programsız şubede boştur.
    /// </summary>
    public IReadOnlyList<PublicSpecialDayDto> SpecialHours { get; init; } = [];
}

/// <summary>
/// `0027` **D8** — şubenin "şu an" durumu; şubenin saat diliminde sunucuda hesaplanır (D6). Saat dilimi
/// yanıtta yoktur. <c>Next</c> sürekli açık şubede <c>null</c>'dır (anahtar yine yazılır).
/// </summary>
public sealed record PublicOpenNowDto(bool Open, string Today, PublicNextChangeDto? Next, int RefreshInSeconds);

/// <summary>`0027` D8 — sıradaki değişimin şubenin yerel tarihi (<c>YYYY-MM-DD</c>) ve saati (<c>HH:mm</c>).</summary>
public sealed record PublicNextChangeDto(string Date, string Time);

/// <summary>`0027` D2 · D8 — bir özel gün; aralık yoksa tam gün kapalı.</summary>
public sealed record PublicSpecialDayDto(string From, string Through, IReadOnlyList<PublicOpeningIntervalDto> Intervals);

/// <summary>
/// `0028` **D5** · **D7** — "Google Haritalar'da aç" adresi ve, anahtar yapılandırılmışsa, tıkla-yükle
/// haritanın gömme adresi. Anahtar yokken <c>EmbedUrl</c> anahtarı yanıtta HİÇ yoktur, <c>null</c>
/// olarak da yoktur (PM şartı (a), AC-28).
/// </summary>
public sealed record PublicBranchMapDto(string LinkUrl)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? EmbedUrl { get; init; }
}

/// <summary>`0028` **D13** — iletişim belgesinin <c>geo</c>'su; değerler ölçeği atılmış <c>decimal</c>.</summary>
public sealed record PublicGeoDto(decimal Latitude, decimal Longitude);

/// <summary>
/// `0028` **P1** — iletişim belgesinin okuması: ziyaretçi okumasının AYNISI ve her şubenin koordinatı
/// (aynı sıra). Yalnız Host'un belgesi içindir; ziyaretçi ucu bu okumayı çağırmaz, yani ham koordinat
/// izin listesine hiç girmez (C2, AC-23).
/// </summary>
public sealed record ContactGalleryDto(PublicGalleryDto Gallery, IReadOnlyList<PublicGeoDto?> Geo);

/// <summary>`0025` D8 — bir aralık; ikisi de <c>HH:mm</c>.</summary>
public sealed record PublicOpeningIntervalDto(string Start, string End);

/// <summary>
/// `0020` **D16** — Host'un `index.html`'e bastığı görünen ad (C6: üst çubuk ilk boyamada
/// adla gelir). Dar bir yüzey: yalnız ad, yalnız bir dil. Önbellek yoktur (D6) — profil
/// değişikliği bir sonraki sayfa açışında görünür (AC-14).
/// </summary>
public interface IGalleryDisplayNameQueries
{
    /// <summary>
    /// Ziyaretçinin dilindeki görünen ad; o dilde yoksa varsayılan dildeki (AC-10). Galeri
    /// profili ya da varsayılan dildeki ad yoksa <c>null</c>.
    /// </summary>
    Task<string?> FindDisplayNameAsync(string locale, CancellationToken cancellationToken);
}

/// <summary>
/// `0014` — ana sayfanın galeri adı (`0020`'den beri görünen ad) ve altlığın şube bloğu (R1, R7).
/// Yalnız vitrinde görünen (`Active`) şubeler döner.
///
/// <para>`0004` **P9** — modülün içinden sözleşmeye taşındı: Host ana sayfanın `AutoDealer`
/// yapısal verisini (**D15**) aynı okumadan kurar; ikinci bir "public şubeler" okuması açılmadı.</para>
/// </summary>
public interface IPublicGalleryQueries
{
    /// <summary>Galeri profili yoksa <c>null</c>.</summary>
    Task<PublicGalleryDto?> GetAsync(string locale, CancellationToken cancellationToken);

    /// <summary>
    /// `0028` P1 — <see cref="GetAsync"/> ve şubelerin koordinatı; galeri profili yoksa <c>null</c>.
    /// Varsayılan gövde koordinat bilmeyen uygulamalar içindir (test taslakları): hiçbir şubede
    /// <c>geo</c> yok. Modülün uygulaması aynı sorgudan koordinatı da verir.
    /// </summary>
    async Task<ContactGalleryDto?> GetContactAsync(string locale, CancellationToken cancellationToken) =>
        await GetAsync(locale, cancellationToken) is { } gallery
            ? new ContactGalleryDto(gallery, [.. gallery.Branches.Select(_ => (PublicGeoDto?)null)])
            : null;
}

/// <summary>`0026` D17 — görünen ad (yoksa <c>null</c>) ve atanmış logonun kimliği (yoksa <c>null</c>).</summary>
public sealed record GalleryBrandingDto(string? DisplayName, Guid? LogoMediaAssetId);

/// <summary>
/// `0026` **D17** · AC-75, AC-76 — Host'un belgeyi kurduğu marka okuması: görünen ad ve logo kimliği
/// TEK sorguda. <see cref="IGalleryDisplayNameQueries"/> değişmez; onun tüketicisi bülten
/// (`Notifications`) ve dar bir arayüzü genişletmek bülteni ilgisiz bir değişikliğe bağlardı.
/// Logonun türevleri burada YOK: `Dealership` `Media`'yı okumaz (C1); Host çözer (D3).
/// </summary>
public interface IGalleryBrandingQueries
{
    /// <summary>
    /// Ziyaretçinin dilindeki görünen ad (yoksa varsayılan dildeki) ve logo kimliği. Galeri profili
    /// yoksa <c>null</c>.
    /// </summary>
    Task<GalleryBrandingDto?> FindBrandingAsync(string locale, CancellationToken cancellationToken);
}
