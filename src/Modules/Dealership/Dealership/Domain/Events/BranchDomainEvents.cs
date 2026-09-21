using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Dealership.Domain.Events;

/// <summary>
/// Modülün İÇİNDE kalan olaylar; aynı transaction'da işlenir (architecture.md § 2.B).
/// Entegrasyon olayına çevrilmeleri Application katmanının işidir — domain, dışarıdaki
/// sözleşmeyi bilmez.
/// </summary>
internal sealed record BranchWasOpened(BranchId BranchId) : IDomainEvent;

/// <summary>
/// R8 — YALNIZCA görünen ad değişince doğar. Adres, telefon, koordinat, saat dilimi ve
/// slug değişikliği bu olayı üretmez: duyurunun bugünkü tek tüketicisi vitrin okuma
/// modelidir ve orada yalnız şube ADI kopyalanır. Kullanılmayan alanı duyuruya koymak,
/// tüketicisi olmayan bir sözleşme yüzeyi yaratırdı.
/// </summary>
internal sealed record BranchDisplayNameWasChanged(BranchId BranchId) : IDomainEvent;

/// <summary>D3 — yalnızca `Kapatılmış`'a geçişte doğar; `Geçici kapalı` üretmez.</summary>
internal sealed record BranchWasClosed(BranchId BranchId) : IDomainEvent;
