using CarGallery.BuildingBlocks.Outbox;

namespace CarGallery.Modules.Media.Contracts.Events;

/// <summary>
/// Medya sahipsizdir — "hangi araca ait" bilgisi VehicleCatalog'dadır
/// (docs/architecture.md § 1).
///
/// <para><b>Şekli 0011'de BÜYÜTÜLMEDİ</b> (D11). Hazır varlık üç türev taşıyor ama olay tek
/// <c>Url</c> taşımaya devam ediyor ve o <c>Url</c> gösterim türevidir (800 px). Gerekçe:
/// vitrin bugün tek <c>cover_media_url</c> kolonu tutuyor ve <c>srcset</c> `0016`'nın işi;
/// hiçbir şeyin okumadığı bir alan eklemek spekülasyon olurdu. Boyut kümesine ihtiyaç duyan
/// tüketici <c>IMediaAssetQueries</c>'ten çözer ve olaya alan eklemek sonradan da
/// uyumludur.</para>
/// </summary>
public sealed record MediaAssetReady(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MediaAssetId,
    string Url) : IIntegrationEvent;

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
/// <para><c>Reason</c> bir HATA KODUDUR, kullanıcıya gösterilecek metin değil
/// (Yasak Listesi 23): `media.watermark_unavailable` gibi. Çeviri uçta yapılır.</para>
/// </summary>
public sealed record MediaAssetFailed(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid MediaAssetId,
    string Reason) : IIntegrationEvent;
