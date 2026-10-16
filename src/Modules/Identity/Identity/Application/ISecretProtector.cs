using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// D3 — doğrulama sırrı veritabanında **açık metin durmaz**; bu port onu şifreler ve
/// çözer. Üç seçenek ölçülmüştü: (a) açık kolon — veritabanı erişimi ikinci adımın
/// tamamını atlatır, (b) uygulama düzeyinde şifreleme + anahtar yapılandırmadan,
/// (c) ASP.NET `DataProtection` anahtar halkası. Karar **(b)**: `Media` ve
/// `Notifications` zaten sırlarını dağıtım yapılandırmasından alıyor
/// (`docs/conventions.md` § 11) ve (c) anahtar halkasının nerede saklanacağı sorusunu
/// açardı.
///
/// Anahtar **döndürme** kapsam dışıdır (§ X7) ama saklanan biçim **sürümlüdür**
/// (`PasswordHash` deseni): bir gün gelecek geçişin eski satırları okuyabilmesi için.
/// </summary>
internal interface ISecretProtector
{
    string Protect(string plaintext);

    Result<string> Unprotect(string cipher);
}
