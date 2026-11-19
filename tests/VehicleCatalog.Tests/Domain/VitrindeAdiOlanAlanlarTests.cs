using CarGallery.Modules.VehicleCatalog.Domain;

namespace CarGallery.Modules.VehicleCatalog.Tests.Domain;

/// <summary>
/// Kapı: ilan detayının yazabildiği her nitelik anahtarının vitrinde bir ADI vardır.
///
/// <para><b>Hangi hatayı tutuyor.</b> Detay yanıtının <c>Label</c>'ı değerin etiketidir,
/// alanın adı değil; alanın adı vitrinde <c>ListingDetailPage.attributeName</c>'deki
/// <c>case</c>'lerden gelir. Adı olmayan anahtarda ekran sunucunun etiketine düşer ve satır
/// "Değer | Değer" basar — <c>0002</c>'nin 2026-11-19'da bulunan kaçan hatası tam buydu ve
/// hiçbir test kırılmamıştı.</para>
///
/// <para><b>Ne zaman kırılır.</b> <c>VehicleFieldDefinitions</c>'a <c>LocalizedText</c>
/// tipinde bir opsiyonel alan eklenir (projektör vitrine yalnız bu tipi taşır,
/// <c>VitrinTipKapisiTests</c>) ama vitrindeki eşlemeye eklenmezse. Bugün öyle bir alan
/// yok; kapının boş geçmediğini çekirdek altı anahtar ölçer.</para>
///
/// <para><b>Modül sınırı.</b> <c>AlanEtiketiKapisiTests</c> gibi cephe dosyasını okur: kuralın
/// bir yanı kodda, bir yanı vitrinde.</para>
/// </summary>
public sealed class VitrindeAdiOlanAlanlarTests
{
    /// <summary><c>ShowcaseListingDetailQueries.BuildAttributesAsync</c>'in her ilanda yazabildiği anahtarlar.</summary>
    private static readonly string[] CekirdekAnahtarlar =
        ["body_type", "fuel_type", "transmission_type", "drivetrain_type", "trim_level", "color"];

    public static TheoryData<string> VitrineInebilenAnahtarlar()
    {
        TheoryData<string> veri = [.. CekirdekAnahtarlar];

        foreach (VehicleFieldDefinition tanim in VehicleFieldDefinitions.All)
        {
            if (tanim.Kind == FieldValueKind.LocalizedText)
            {
                veri.Add(tanim.FieldKey);
            }
        }

        return veri;
    }

    [Theory(DisplayName = "0002/AC-10: detayın yazabildiği her nitelik anahtarının vitrinde adı vardır")]
    [MemberData(nameof(VitrineInebilenAnahtarlar))]
    public void VardirAdi_HerNitelikAnahtari(string anahtar)
    {
        Assert.True(
            DetaySayfasi.Value.Contains($"case '{anahtar}':", StringComparison.Ordinal),
            $"`{anahtar}` ilan detayında satır olabilir ama `ListingDetailPage.attributeName`'de adı "
            + "yok: ekran başlık sütununa değeri basar (\"Değer | Değer\"). Eşlemeye `case` ve "
            + "`detail.field.*` çevirisi eklenir.");
    }

    private static readonly Lazy<string> DetaySayfasi = new(() =>
    {
        const string Goreceli = "frontend/src/features/catalog/ListingDetailPage.tsx";

        for (DirectoryInfo? dizin = new(AppContext.BaseDirectory); dizin is not null; dizin = dizin.Parent)
        {
            string aday = Path.Combine(dizin.FullName, Goreceli);

            if (File.Exists(aday))
            {
                return File.ReadAllText(aday);
            }
        }

        throw new FileNotFoundException(
            $"`{Goreceli}` {AppContext.BaseDirectory} ve üstündeki hiçbir dizinde bulunamadı.");
    });
}
