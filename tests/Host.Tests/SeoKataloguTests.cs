using CarGallery.Host.Seo;

namespace CarGallery.Host.Tests;

/// <summary>
/// `0004` **D12** · **P2** · **P3** — Host'un belge metinleri ve listenin parametre adları ön yüz
/// deposunun dosyalarından. Kapı anahtarı ÜRETEN yerden (Host'un kullandığı anahtar listesi)
/// ölçer; diller sabit yazılmaz, var olan katalog dosyaları taranır (`docs/testing.md` § 3).
/// </summary>
public sealed class SeoKataloguTests
{
    public static TheoryData<string> Diller()
    {
        string[] diller = [.. Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, SeoCatalog.Directory, "seo"), "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()];

        // Sıfır vakalı Theory yeşildir; kapı sessizce açılmasın.
        Assert.NotEmpty(diller);

        return [.. diller];
    }

    [Theory]
    [MemberData(nameof(Diller))]
    public void Bulunur_HostunKullandigiHerAnahtar_HerDilde(string dil)
    {
        SeoCatalog katalog = SeoCatalog.Load(AppContext.BaseDirectory, [dil]);

        Assert.All(SeoCatalog.UsedKeys, anahtar => Assert.False(string.IsNullOrWhiteSpace(katalog.Text(dil, anahtar)), anahtar));
    }

    [Fact]
    public void Baslamaz_BirDildeAnahtarEksikken()
    {
        string kok = Directory.CreateTempSubdirectory("seo-katalog").FullName;
        KopyalaKataloglari(kok);
        File.WriteAllText(Path.Combine(kok, SeoCatalog.Directory, "seo", "ar.json"), "{\"seo.list.title\": \"x\"}");

        InvalidOperationException hata = Assert.Throws<InvalidOperationException>(() => SeoCatalog.Load(kok, ["tr", "ar"]));

        Assert.Contains("ar: seo.list.description", hata.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("tr: ", hata.Message, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "0004/AC-68 (küme yarısı): listenin okuduğu parametreler D23'ün kümesidir ve ön yüzle aynı dosyadan okunur")]
    public void Ayni_ListeParametreleri_D23Kumesi()
    {
        SeoCatalog katalog = SeoCatalog.Load(AppContext.BaseDirectory, ["tr"]);

        Assert.Equal(
            new HashSet<string>(["bodyType", "fuel", "transmission", "brand", "yearMin", "yearMax", "mileageMin", "mileageMax", "priceMin", "priceMax", "sort", "page"]),
            katalog.ListQueryParameters);
    }

    [Fact]
    public void Doldurur_AdVeSayiYerTutucularini()
    {
        SeoCatalog katalog = SeoCatalog.Load(AppContext.BaseDirectory, ["tr"]);

        Assert.Equal("125.000 km", katalog.Format("tr", SeoCatalog.MileageValue, new Dictionary<string, string> { ["kilometers"] = "125.000" }));
    }

    private static void KopyalaKataloglari(string hedef)
    {
        string kaynak = Path.Combine(AppContext.BaseDirectory, SeoCatalog.Directory);

        foreach (string dosya in Directory.GetFiles(kaynak, "*.json", SearchOption.AllDirectories))
        {
            string yeni = Path.Combine(hedef, SeoCatalog.Directory, Path.GetRelativePath(kaynak, dosya));
            Directory.CreateDirectory(Path.GetDirectoryName(yeni)!);
            File.Copy(dosya, yeni);
        }
    }
}
