using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Infrastructure;

/// <summary>
/// C6 — yavaş, parolaya özel, hesaba özel tuzlu. Algoritma ve maliyet yapılandırmadan
/// gelir; saklanan özet kendi algoritmasını ve maliyetini içinde taşır.
/// </summary>
internal sealed class Pbkdf2PasswordHasher(IdentityOptions options) : IPasswordHasher
{
    /// <summary>
    /// Var olmayan hesap için çalıştırılan sahte doğrulama. Tuzu ve maliyeti gerçek bir
    /// kayıtla aynıdır; amacı sonucu değil, HARCANAN SÜREYİ eşitlemektir.
    /// </summary>
    private readonly PasswordHash burnHash =
        PasswordHash.Create("hic-kimsenin-parolasi-degil", options.PasswordIterations);

    public PasswordHash Hash(string password) =>
        PasswordHash.Create(password, options.PasswordIterations);

    public void BurnEquivalentWork(string password) => burnHash.Matches(password);
}
