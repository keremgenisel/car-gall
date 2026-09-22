using CarGallery.BuildingBlocks.Outbox;

namespace CarGallery.Modules.Leads.Contracts.Events;

/// <summary>
/// Entegrasyon olayı: geçmiş zaman adı, `record`, yalnız primitive
/// (docs/architecture.md § 2.B). Modül sınırını yalnızca bu geçer.
///
/// **KİŞİSEL ALAN TAŞIMAZ — ve taşımamalıdır.** Outbox satırı yayıncının kendi şemasında
/// `jsonb` olarak KALICIDIR. Ad, telefon, e-posta veya mesaj bu gövdeye girerse,
/// `Lead.Anonymize()` çalıştığı gün kişisel veri `leads.outbox_messages` içinde durmaya
/// devam eder ve docs/domain.md **L6** kâğıt üstünde kalır. Alan SONRADAN EKLEMEK ucuz,
/// geçmişten SİLMEK imkânsızdır.
///
/// Bugün tüketicisi yok (X3 — `Notifications` modülü mevcut değil). Geldiğinde talebi
/// <see cref="LeadId"/> ile okur; kişisel veriyi olaydan değil kaynaktan alır.
///
/// <see cref="LeadType"/> ve <see cref="SubmittedLocale"/> KOD olarak taşınır, çevrilmiş
/// adıyla değil (Yasak Listesi 22).
/// </summary>
public sealed record LeadCaptured(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid LeadId,
    Guid BranchId,
    Guid ListingId,
    string LeadType,
    string SubmittedLocale) : IIntegrationEvent;
