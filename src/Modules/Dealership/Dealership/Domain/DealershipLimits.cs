namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// `0020` R3a · D22 — metin alanlarının üst sınırları. Şemada sınırı olan alanlar (unvan,
/// vergi ve sicil numarası, telefon, e-posta, saat dilimi, kısaltma) şemanın değerini
/// tekrarlar; sınırı olmayan `jsonb` alanlar (görünen ad, şube adı ve adresi) sınırını
/// buradan alır. Sınır aşıldığında veritabanı hatası değil alan hatası döner (C5).
/// </summary>
internal static class DealershipLimits
{
    internal const int CommercialName = 200;
    internal const int RegistryNumber = 40;
    internal const int DisplayName = 120;
    internal const int BranchName = 120;
    internal const int BranchAddress = 300;
    internal const int TimeZone = 60;
}
