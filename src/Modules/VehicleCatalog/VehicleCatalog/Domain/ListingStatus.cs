namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// docs/domain.md sözlüğündeki yayın durumu: Taslak · Yayında · Rezerve · Satıldı · Arşiv.
/// Durum adı veritabanında ÇEVRİLMİŞ hâliyle saklanmaz; kod saklanır, gösterim uçta
/// çevrilir (Yasak Listesi 22).
/// </summary>
internal enum ListingStatus
{
    Draft = 0,
    Published = 1,
    Reserved = 2,
    Sold = 3,
    Archived = 4,
}
