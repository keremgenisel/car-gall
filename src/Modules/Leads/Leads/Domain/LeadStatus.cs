namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// docs/domain.md **L4**: `Yeni → Atandı → İletişimde → Sonuçlandı`. Bu spec talebi
/// <see cref="New"/> durumunda bırakır; geçişler atama akışıyla birlikte gelir (X2).
/// Değerler şimdiden tanımlı, çünkü sayısal karşılıkları sonradan kaydırmak saklanmış
/// veriyi sessizce bozar.
/// </summary>
internal enum LeadStatus
{
    New = 1,
    Assigned = 2,
    InContact = 3,
    Concluded = 4,
}
