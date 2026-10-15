namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// R7/D30 — ikinci adımın hangi rol için **zorunlu** olduğu tek yerde yaşar.
///
/// Zorunluluğu **rol** söylüyor ve personel satırı rolü zaten taşıyor; bu yüzden API
/// sözleşmesine ikinci bir "zorunlu mu" alanı eklenmedi (D25). Politika değiştiğinde
/// değişen tek yer burasıdır — sözleşme değil.
///
/// Yönetici, personel ekleyip rol değiştirebilen tek roldür (`0006` R9) ve o hesabın ele
/// geçmesi sistemin tamamını verir. Satış Danışmanı ve İçerik Editörü kendi isteğiyle
/// açar (R15 kapatmayı da onlara verir) — `0006`'nın *"küçük ekipte sürtünme"* gerekçesi
/// tümden yok sayılmasın.
/// </summary>
internal static class TwoFactorPolicy
{
    internal static bool IsRequiredFor(StaffRole role) => role is StaffRole.Manager;
}
