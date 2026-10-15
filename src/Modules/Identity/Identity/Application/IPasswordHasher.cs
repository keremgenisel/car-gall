using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// Parola özetleme portu. Algoritma ve maliyet parametresi dağıtım yapılandırmasından
/// gelir (C6); uygulaması Infrastructure'dadır.
/// </summary>
internal interface IPasswordHasher
{
    PasswordHash Hash(string password);

    /// <summary>
    /// Var olmayan bir hesap için de çalıştırılacak "boşa çalışma". R3 ayırt edilemez
    /// karşılık istiyor; hesap yokken parola doğrulamayı ATLAMAK, yanıt gövdesi aynı olsa
    /// bile ölçülebilir bir süre farkı bırakır ve hangi e-postaların kayıtlı olduğunu
    /// zamanlama üzerinden sızdırır.
    /// </summary>
    void BurnEquivalentWork(string password);

    /// <summary>
    /// D2/D38 — kurtarma kodunun özeti PAROLADAN AYRI bir maliyetle alınır. Ayrı bir
    /// metot olmasının nedeni ölçülmüş: girilen bir kodu saklanan on özetin hepsiyle
    /// karşılaştırmak **on özetleme** demek ve üretim parola değeriyle (210.000) bu tek
    /// girişte saniyeler sürer. Düşük maliyet burada güvenli, çünkü kod sistem üretimi
    /// ve yüksek entropilidir — tahmin edilebilir bir insan parolası değil.
    /// </summary>
    PasswordHash HashRecoveryCode(string code);
}
