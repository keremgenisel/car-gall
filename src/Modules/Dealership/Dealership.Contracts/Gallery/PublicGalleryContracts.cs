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
/// </summary>
public sealed record PublicBranchDto(
    string Name,
    string Address,
    string Phone,
    IReadOnlyDictionary<string, IReadOnlyList<PublicOpeningIntervalDto>>? OpeningHours = null);

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
