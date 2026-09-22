namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// docs/domain.md sözlüğündeki talep tipi: bilgi talebi · geri arama · test sürüşü · takas ·
/// kredi görüşmesi · fiyat teklifi. Durum/tip adı veritabanında ÇEVRİLMİŞ hâliyle
/// saklanmaz; kod saklanır, gösterim uçta çevrilir (Yasak Listesi 22).
///
/// <see cref="TradeIn"/> burada BULUNUR çünkü sistem onu TANIR (R3): tanınmayan bir tiple
/// tanınan ama kabul edilmeyen bir tip ziyaretçiye aynı karşılığı vermez. Kabul kararı
/// bu enum'da değil, <see cref="LeadTypePolicy"/>'de yaşar — 0005 sonrası takası açmak
/// tek bir yerin değişmesidir (X1, Karar D3).
/// </summary>
internal enum LeadType
{
    InformationRequest = 1,
    CallBack = 2,
    TestDrive = 3,
    TradeIn = 4,
    FinancingConsultation = 5,
    PriceOffer = 6,
}

internal static class LeadTypePolicy
{
    /// <summary>R3 — bu spec'te kabul edilen beş tip. Takas tanınır ama kabul edilmez.</summary>
    private static readonly LeadType[] Accepted =
    [
        LeadType.InformationRequest,
        LeadType.CallBack,
        LeadType.TestDrive,
        LeadType.FinancingConsultation,
        LeadType.PriceOffer,
    ];

    internal static bool IsAccepted(LeadType type) => Array.IndexOf(Accepted, type) >= 0;
}
