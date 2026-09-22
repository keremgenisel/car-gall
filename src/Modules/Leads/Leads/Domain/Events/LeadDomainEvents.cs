using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Leads.Domain.Events;

/// <summary>
/// Modülün İÇİNDE kalan olay; aynı transaction'da işlenir (architecture.md § 2.B).
/// Entegrasyon olayına çevrilmesi Application katmanının işidir — domain, modül dışındaki
/// sözleşmeyi bilmez.
///
/// Kişisel alan TAŞIMAZ ve taşımamalıdır: entegrasyon karşılığı outbox'a kalıcı olarak
/// yazılır; ad veya telefon oraya düşerse `Erase` çalıştığı gün kişisel veri outbox'ta
/// durmaya devam eder ve **L6** kâğıt üstünde kalır.
/// </summary>
internal sealed record LeadWasCaptured(LeadId LeadId) : IDomainEvent;
