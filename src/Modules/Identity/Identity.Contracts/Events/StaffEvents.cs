using CarGallery.BuildingBlocks.Outbox;

namespace CarGallery.Modules.Identity.Contracts.Events;

/// <summary>
/// Entegrasyon olayı: geçmiş zaman adı, `record`, yalnız primitive
/// (docs/architecture.md § 2.B, C5). Modül sınırını yalnızca bu geçer.
///
/// docs/architecture.md § 1'in olay haritası bunu
/// `Identity → StaffDeactivated → Leads (açık talepleri yeniden ata), Audit` diye
/// yazıyor. Bugün **hiçbir alıcısı yok** (X6, X7) — bu spec yayıncı tarafını üretir.
/// Yayıncı abonelerini bilmez (Yasak Listesi 18).
///
/// Yalnız <see cref="StaffId"/> taşır: personelin kim olduğu `Identity`'nin verisidir ve
/// sınırı geçmez (C1). Duyuruya ad veya e-posta koymak, outbox satırında kalıcılaşan bir
/// kişisel veri kopyası üretirdi.
/// </summary>
public sealed record StaffDeactivated(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid StaffId) : IIntegrationEvent;
