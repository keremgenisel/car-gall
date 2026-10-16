namespace CarGallery.Modules.Identity.Contracts.Sessions;

/// <summary>
/// Bir isteğin arkasındaki personelin O ANKİ gerçeği. Her alan **kaynaktan** okunur;
/// hiçbiri oturum taşıyıcısının içinden gelmez (C16).
///
/// Rol ve durum KOD olarak taşınır, çevrilmiş adıyla değil (C7, Yasak Listesi 22).
/// Parola veya parola özeti BULUNMAZ ve bulunamaz (C6, AC-50).
/// </summary>
public sealed record StaffSessionDto(
    Guid StaffId,
    Guid SessionId,
    string DisplayName,
    string LoginId,
    string Role,
    string PreferredLocale,
    bool MustChangePassword,
    Guid? BranchId,
    /// <param name="TwoFactorEnrolled">
    /// D26/D35 — ikinci adım durumu da **kaynaktan** okunur. Girişte pişirilmiş bir
    /// claim, kurulumu biten personele oturumun kalan otuz dakikası boyunca
    /// *"kurulmamış"* derdi; bu alan her istekte personel satırıyla aynı okumada çözülür
    /// ve rol ile hesap durumuyla aynı mekanizmayı paylaşır (C16).
    /// </param>
    bool TwoFactorEnrolled);

/// <summary>
/// Oturum çözümleme yüzeyi. İki tüketicisi var ve ikisi de `Identity`'nin dışındadır:
/// kimlik doğrulama ara katmanı ve panel kabuğunu basan Host
/// (docs/architecture.md § 2.F — host kompozisyonu).
/// </summary>
public interface IStaffSessionQueries
{
    /// <summary>
    /// Oturumu çözer ve gerekiyorsa SESSİZCE yeniler (R4, AC-16). Çözülemeyen her durum
    /// — token yok, oturum iptal edilmiş, süresi dolmuş, personel pasif, damga
    /// değişmiş — aynı sonucu verir: <c>null</c>. Ayrım yapan bir dal yoktur, çünkü
    /// ayrım yapılmayan şey sızdırılamaz.
    /// </summary>
    Task<StaffSessionDto?> ResolveAsync(string? token, CancellationToken cancellationToken);
}
