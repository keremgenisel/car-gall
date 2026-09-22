using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain.Events;

/// <summary>
/// Modülün İÇİNDE kalan olay; aynı transaction'da işlenir (architecture.md § 2.B).
/// Entegrasyon olayına çevrilmesi Application katmanının işidir — domain, modül
/// dışındaki sözleşmeyi bilmez.
/// </summary>
internal sealed record StaffWasDeactivated(StaffId StaffId) : IDomainEvent;
