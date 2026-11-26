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
            $"`{anahtar}` ilan detayında satır olabilir ama `attributeName.ts`'de adı "
            + "yok: ekran başlık sütununa değeri basar (\"Değer | Değer\"). Eşlemeye `case` ve "
            + "`detail.field.*` çevirisi eklenir.");
    }

    /// <summary>
    /// `frontend-003` **D11** — eşleme TEK dosyadadır: ilan detayı ve karşılaştırma tablosu onu içe
    /// aktarır. İkinci bir kopya bu kapının görmediği ikinci bir liste olurdu; yeni bir alan detayda
    /// adlanır, kopyada "Değer | Değer" basar (`0002`'nin kaçan hatası).
    /// </summary>
    [Fact(DisplayName = "frontend-003/AC-57: Alan adı eşlemesi `frontend/src` altında tek dosyadadır — `case 'body_type':` tam olarak bir dosyada geçer ve o dosya `attributeName.ts`'dir")]
    public void TekDosyada_AlanAdiEslemesi()
    {
        string kaynak = Path.Combine(DepoKoku.Value, "frontend", "src");
        List<string> dosyalar =
        [
            .. Directory.EnumerateFiles(kaynak, "*.ts*", SearchOption.AllDirectories)
                .Where(dosya => File.ReadAllText(dosya).Contains("case 'body_type':", StringComparison.Ordinal))
                .Select(dosya => Path.GetRelativePath(kaynak, dosya).Replace('\\', '/')),
        ];

        Assert.Equal(["features/catalog/attributeName.ts"], dosyalar);
    }

    private const string Eslemesi = "frontend/src/features/catalog/attributeName.ts";

    private static readonly Lazy<string> DepoKoku = new(() =>
    {
        for (DirectoryInfo? dizin = new(AppContext.BaseDirectory); dizin is not null; dizin = dizin.Parent)
        {
            if (File.Exists(Path.Combine(dizin.FullName, Eslemesi)))
            {
                return dizin.FullName;
            }
        }

        throw new FileNotFoundException(
            $"`{Eslemesi}` {AppContext.BaseDirectory} ve üstündeki hiçbir dizinde bulunamadı.");
    });

    private static readonly Lazy<string> DetaySayfasi = new(() => File.ReadAllText(Path.Combine(DepoKoku.Value, Eslemesi)));
}
