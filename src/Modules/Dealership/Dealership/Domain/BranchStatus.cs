namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// R3 — üç değer. Veritabanında ÇEVRİLMİŞ adı saklanmaz; kod saklanır ve uçta çevrilir
/// (C13, Yasak Listesi 22).
///
/// `Closed` TERMİNALDİR (D3): kapatılmış şube yeniden aktif edilemez. Aynı lokasyon
/// yeniden açılacaksa yeni bir şube kaydı açılır — `Satıldı`'nın A4'te kurduğu desen.
/// Aksi hâlde `BranchReactivated` diye bir olay ve A8'in tersine çevrilmesi gerekirdi.
/// </summary>
internal enum BranchStatus
{
    Active = 1,
    TemporarilyClosed = 2,
    Closed = 3,
}
