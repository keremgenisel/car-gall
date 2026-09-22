using CarGallery.Modules.Identity.Domain;

namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// Rolün TEL ÜZERİNDEKİ kararlı kodu. Enum adı değil, ayrı bir sözlük: enum değeri
/// yeniden adlandırılırsa sözleşme sessizce kırılırdı.
///
/// Kod ÇEVRİLMEZ (C7, Yasak Listesi 22): personelin gördüğü rol adı uçta, çeviri
/// kataloğundan gelir. Veritabanında da kod saklanır (AC-52).
/// </summary>
internal static class StaffRoleCode
{
    private static readonly Dictionary<string, StaffRole> ByCode = new(StringComparer.Ordinal)
    {
        ["manager"] = StaffRole.Manager,
        ["sales_advisor"] = StaffRole.SalesAdvisor,
        ["content_editor"] = StaffRole.ContentEditor,
    };

    private static readonly Dictionary<StaffRole, string> ByRole =
        ByCode.ToDictionary(pair => pair.Value, pair => pair.Key);

    internal static bool TryParse(string? code, out StaffRole role)
    {
        role = default;

        return code is not null && ByCode.TryGetValue(code, out role);
    }

    internal static string ToCode(StaffRole role) => ByRole[role];
}

/// <summary>Durumun tel üzerindeki kararlı kodu; enum adı değil (C7).</summary>
internal static class StaffStatusCode
{
    private static readonly Dictionary<StaffStatus, string> ByStatus = new()
    {
        [StaffStatus.Active] = "active",
        [StaffStatus.Deactivated] = "deactivated",
    };

    internal static string ToCode(StaffStatus status) => ByStatus[status];
}
