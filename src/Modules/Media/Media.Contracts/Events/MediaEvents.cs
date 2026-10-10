using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Contracts.Assets;

namespace CarGallery.Modules.Media.Contracts.Events;

/// <summary>
/// Üretilmiş bir türevin duyuruda taşınan hâli. Yalnız primitive taşır
/// (`docs/architecture.md` § 2.B).
///
/// <para><c>Width</c>/<c>Height</c> GERÇEKLEŞEN boyuttur, hedeflenen değil: AC-18 gereği
/// kaynak hedeften küçükse büyütme yapılmaz. Hedefi taşımak, "1600" yazıp 900 piksel
/// servis etmek demekti — ve `docs/frontend.md` § 7 açık <c>width</c>/<c>height</c>
/// istiyor (CLS borcu).</para>
/// </summary>
public sealed record MediaDerivativePayload(string Variant, string Url, int Width, int Height);

/// <summary>
/// Medya sahipsizdir — "hangi araca ait" bilgisi VehicleCatalog'dadır
/// (docs/architecture.md § 1).
///
/// <para><b>AC-29 — duyuru TÜREV KÜMESİNİ taşır:</b> her türev için etiket, erişim yolu,
/// genişlik ve yükseklik. <i>Tek bir erişim yolu yeterli değildir.</i></para>
///
/// <para><b>Tek <c>Url</c> alanı kaldırıldı</b> ve bu bilinçli: küme ile yan yana duran
/// bir "asıl URL", iki kaynağın ayrışmasına davettir. Tüketici hangi boyutu istediğini
/// AÇIKÇA seçiyor — kriterin amacı tam olarak bu. `VehicleCatalog`'un kapak görseli
/// gösterim türevini (<c>display</c>) adıyla alıyor.</para>
///
/// <para><b>Kabul edilen bedel (D3):</b> outbox gövdesi büyür ve filigran politikası
/// değiştiğinde <b>yeniden projeksiyon</b> gerekir — S4'ün "eski türevler eski filigranı
/// taşır" bedeliyle aynı aileden.</para>
/// </summary>
public sealed record MediaAssetReady(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MediaAssetId,
    IReadOnlyList<MediaDerivativePayload> Derivatives) : IIntegrationEvent;

/// <summary>
/// D3 — `docs/architecture.md` § 1'in olay akışında yazılıydı, kodda yoktu. 0011 onu ekliyor
/// ve yayıyor (R8).
///
/// <para><b>Neden gerekli:</b> R8 olmazsa personel fotoğrafın yüklendiğini sanır ve araç
/// yayına alınamaz — hata ekranda değil, EKSİKLİKTE görünür. Bu olayla `VehicleCatalog`
/// fotoğrafı bekleyen aracı "eksik bilgi" olarak işaretleyebilir.</para>
///
/// <para><b>Tüketicisi 0011'de zorunlu değil</b> (D17): fotoğrafı araca bağlayan `0012`
/// gelmeden hiçbir araç hiçbir varlık kimliği taşımıyor. Hedefi olmayan olay dağıtıcıda
/// işlenmiş sayılır ve kuyruk birikmez.</para>
///
/// <para>AC-33 — <c>Reason</c> <b>kapalı bir kümedendir</b> ve kullanıcıya gösterilecek
/// metin taşımaz (Yasak Listesi 23); çeviri uçta yapılır. Önceki hâli <c>string</c>'di ve
/// küme yalnızca sabitlerle temsil ediliyordu — tip onu zorlamıyordu. `0010b`'nin "tek
/// `InvalidSlug` dört alanı temsil ediyordu" hatası aynı sınıftandı.</para>
/// </summary>
public sealed record MediaAssetFailed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MediaAssetId,
    MediaFailureReason Reason) : IIntegrationEvent;
