using CarGallery.Modules.Leads.Domain;

namespace CarGallery.Modules.Leads.Application;

/// <summary>
/// Talep tipinin TEL ÜZERİNDEKİ kararlı kodu. Enum adı değil, ayrı bir sözlük: enum
/// değeri yeniden adlandırılırsa sözleşme sessizce kırılırdı.
///
/// Kod snake_case'dir ve ÇEVRİLMEZ (Yasak Listesi 22): ziyaretçinin gördüğü ad uçta,
/// çeviri kataloğundan gelir.
/// </summary>
internal static class LeadTypeCode
{
    private static readonly Dictionary<string, LeadType> ByCode = new(StringComparer.Ordinal)
    {
        ["information_request"] = LeadType.InformationRequest,
        ["call_back"] = LeadType.CallBack,
        ["test_drive"] = LeadType.TestDrive,
        ["trade_in"] = LeadType.TradeIn,
        ["financing_consultation"] = LeadType.FinancingConsultation,
        ["price_offer"] = LeadType.PriceOffer,
    };

    private static readonly Dictionary<LeadType, string> ByType =
        ByCode.ToDictionary(pair => pair.Value, pair => pair.Key);

    /// <summary>
    /// R3 — takas burada TANINIR (ayrıştırılır) ama kabul edilmez. Ayrım
    /// <see cref="LeadTypePolicy"/>'dedir: tanınmayan tiple tanınan ama kabul edilmeyen
    /// tip ziyaretçiye aynı karşılığı vermemeli (AC-9 ile AC-10 ayrı kriterlerdir).
    /// </summary>
    internal static bool TryParse(string? code, out LeadType type)
    {
        type = default;

        return code is not null && ByCode.TryGetValue(code, out type);
    }

    internal static string ToCode(LeadType type) => ByType[type];
}
