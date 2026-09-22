namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// docs/architecture.md § 1 ve docs/domain.md: Yönetici · Satış Danışmanı · İçerik Editörü.
/// Rol adı veritabanında ÇEVRİLMİŞ hâliyle saklanmaz; kod saklanır, gösterim uçta
/// çevrilir (C7, Yasak Listesi 22).
///
/// Bu bir izin motoru DEĞİLDİR ve olmayacaktır: şube bazlı yetki V1'de yoktur (C2,
/// docs/architecture.md § 6). Rol, yapılabilecek işlemleri belirler; personelin
/// `BranchId`'si bir yetki sınırı değil, bir bağlantı bilgisidir.
/// </summary>
internal enum StaffRole
{
    Manager = 1,
    SalesAdvisor = 2,
    ContentEditor = 3,
}
