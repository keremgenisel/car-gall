namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// R12 — **"Aktif" durumu aktif olmak demektir**; geçici kilitlenme aktifliği etkilemez.
/// Kilitlenme bir durum DEĞİL, ayrı bir sayaçtır (bkz. <see cref="LockoutState"/>):
/// ikisini tek eksende toplamak, kilitli bir Yöneticiyi "pasif" sayıp son Yönetici
/// korumasını sessizce delerdi (AC-45).
/// </summary>
internal enum StaffStatus
{
    Active = 1,
    Deactivated = 2,
}
