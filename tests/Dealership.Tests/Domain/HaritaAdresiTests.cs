using System.Globalization;
using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Tests.Domain;

/// <summary>
/// `0028` **D7** · **P2** — harita adreslerinin saf işlevi (<see cref="BranchMapAddress"/>). Aynı kriterler
/// Host'ta gerçek uçla da ölçülür (`Host.Tests/IletisimSayfasiTests`); burası kodlamanın bütün
/// dallarını hızlıca tarar.
/// </summary>
public sealed class HaritaAdresiTests
{
    private static readonly Coordinate Kadikoy = Coordinate.Create(40.98765000m, 29.02750000m).Value;

    [Fact(DisplayName = "0028/AC-24, AC-27: Koordinatlı şubenin iki adresi tam dize; sondaki sıfırlar yok")]
    public void Koordinatli_sube()
    {
        (string link, string? embed) = BranchMapAddress.Build(Kadikoy, "Kadıköy", "tr", "test-anahtar")!.Value;

        Assert.Equal("https://www.google.com/maps/search/?api=1&query=40.98765%2C29.0275", link);
        Assert.Equal("https://www.google.com/maps/embed/v1/place?key=test-anahtar&q=40.98765%2C29.0275&language=tr", embed);
        Assert.EndsWith("&language=en", BranchMapAddress.Build(Kadikoy, "Kadıköy", "en", "test-anahtar")!.Value.EmbedUrl);
    }

    [Fact(DisplayName = "0028/AC-25: Süreç kültürü tr-TR iken de nokta ondalık")]
    public void Kultur_tr()
    {
        CultureInfo onceki = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            Assert.Equal(
                "https://www.google.com/maps/search/?api=1&query=40.98765%2C29.0275",
                BranchMapAddress.Build(Kadikoy, "x", "tr", null)!.Value.LinkUrl);
        }
        finally
        {
            CultureInfo.CurrentCulture = onceki;
        }
    }

    [Fact(DisplayName = "0028/AC-26: Koordinatsız şube adresi Uri.EscapeDataString ile; boşluk %20, & ve # kodlu")]
    public void Adresli_sube()
    {
        Assert.Equal(
            "https://www.google.com/maps/search/?api=1&query=Ba%C4%9Fdat%20Cad.%20No%3A5%2F1%20%26%20%232",
            BranchMapAddress.Build(null, "Bağdat Cad. No:5/1 & #2", "ar", null)!.Value.LinkUrl);
    }

    [Theory(DisplayName = "0028/AC-28: Anahtar yok, boş ya da yalnız boşluk → gömme adresi yok, bağlantı dolu")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Anahtarsiz(string? anahtar)
    {
        (string link, string? embed) = BranchMapAddress.Build(Kadikoy, "x", "tr", anahtar)!.Value;

        Assert.Null(embed);
        Assert.StartsWith("https://www.google.com/maps/search/?api=1&query=", link);
    }

    [Fact(DisplayName = "0028/AC-91: 300 karakterlik \"–\" adresi 2748 karakterlik bağlantı ister → harita null; ölçü D7'deki gibi")]
    public void Sinir_asimi()
    {
        string adres = new('–', 300);

        Assert.Equal(2748, ("https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(adres)).Length);
        Assert.Null(BranchMapAddress.Build(null, adres, "tr", null));
        Assert.Null(BranchMapAddress.Build(null, adres, "tr", "test-anahtar"));
    }

    [Fact(DisplayName = "0028/AC-91 (sınırın altı): 300 Türkçe harfli adres bağlantı 1848, 39 karakterlik anahtarla gömme 1901")]
    public void Sinirin_alti()
    {
        string adres = new('ğ', 300);
        (string link, string? embed) = BranchMapAddress.Build(null, adres, "tr", new string('k', 39))!.Value;

        Assert.Equal(1848, link.Length);
        Assert.Equal(1901, embed!.Length);
    }

    [Fact(DisplayName = "0028/AC-91 (gömme yarısı): bağlantı sığıp gömme 2048'i aşarsa da harita null")]
    public void Yalniz_gomme_asar()
    {
        // Bağlantı 48 + 6×330 = 2028 karakter; gömme adresi anahtar ve dil eklerle 2048'i aşar.
        string adres = new('ğ', 330);

        Assert.Equal(2028, BranchMapAddress.Build(null, adres, "tr", null)!.Value.LinkUrl.Length);
        Assert.Null(BranchMapAddress.Build(null, adres, "tr", "test-anahtar"));
    }

    [Fact(DisplayName = "0028/AC-97: Satır sonu ve sekmeler tek boşluk; tam sorgu dizesi")]
    public void Denetim_karakterleri()
    {
        Assert.Equal(
            "https://www.google.com/maps/search/?api=1&query=Ba%C4%9Fdat%20Cad.%20No%3A5%201",
            BranchMapAddress.Build(null, "Bağdat Cad.\nNo:5\t\t1", "tr", null)!.Value.LinkUrl);
        Assert.Equal("a b", BranchMapAddress.Normalize("\0 a \u0001\r\n b \u007f"));
    }

    [Fact(DisplayName = "0028/AC-15 yardımcısı: koordinat metni ölçeksiz, eksi işaretli değer de nokta ondalık")]
    public void Sayi_bicimi()
    {
        Assert.Equal("-33.8688", BranchMapAddress.Number(-33.86880000m));
        Assert.Equal("0", BranchMapAddress.Number(0.00000000m));
        Assert.Equal("29.0275", BranchMapAddress.Query(Kadikoy, "x")[9..]);
    }
}
