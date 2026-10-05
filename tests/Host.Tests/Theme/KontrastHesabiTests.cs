using CarGallery.Host.Theme;

namespace CarGallery.Host.Tests.Theme;

/// <summary>
/// Kontrast hesabı ve renk biçimi. Kriterlerin kapısı değil, kriterlerin DAYANDIĞI hesap:
/// bu yanlışsa AC-17…AC-34'ün hepsi yanlış yerde yeşil veya kırmızı olur.
///
/// Referans değerler WCAG'ın kendi örneklerinden: siyah/beyaz 21:1 ve `#777777`/beyaz
/// 4.5 eşiğinin hemen ALTINDA — eşik sınırını gösteren klasik çift.
/// </summary>
public sealed class KontrastHesabiTests
{
    [Fact]
    public void Dondurur_YirmiBir_SiyahBeyaz()
    {
        decimal oran = ThemeContrast.Ratio(Renk("#000000"), Renk("#ffffff"));

        Assert.Equal(21m, Math.Round(oran, 2));
    }

    /// <summary>
    /// Sıraya bağımlı bir oran, çiftin hangi ucunun metin olduğuna göre farklı sonuç
    /// verir ve eşik karşılaştırması anlamsızlaşır.
    /// </summary>
    [Fact]
    public void Simetriktir_Oran()
    {
        ThemeColor koyu = Renk("#4f6b00");
        ThemeColor acik = Renk("#ffffff");

        Assert.Equal(ThemeContrast.Ratio(koyu, acik), ThemeContrast.Ratio(acik, koyu));
    }

    /// <summary>`#777777` beyaza karşı 4.5'in hemen ALTINDA; `#767676` hemen ÜSTÜNDE.</summary>
    [Theory]
    [InlineData("#777777", false)]
    [InlineData("#767676", true)]
    public void Ayirir_EsikSinirini(string renk, bool karsilarMi)
    {
        decimal oran = ThemeContrast.Ratio(Renk(renk), Renk("#ffffff"));

        Assert.Equal(karsilarMi, oran >= 4.5m);
    }

    [Theory]
    [InlineData("#CDF564", "#0B0C0E", 15.6, 15.8)]
    [InlineData("#4F6B00", "#FFFFFF", 6.0, 6.2)]
    [InlineData("#CDF564", "#FFFFFF", 1.2, 1.3)]
    public void Hesaplar_MockupPaletininOranlarini(string a, string b, double enAz, double enCok)
    {
        decimal oran = ThemeContrast.Ratio(Renk(a), Renk(b));

        Assert.InRange(oran, (decimal)enAz, (decimal)enCok);
    }

    /// <summary>
    /// C11 — biçim TEK TİPTİR. Üç haneli hex de reddedilir: `#fff` ile `#ffffff` aynı rengi
    /// verir ama iki biçime izin vermek, kontrast hesabını ayrıştırma varsayımına bağlar.
    /// </summary>
    [Theory]
    [InlineData("#fff")]
    [InlineData("#ffffffff")]
    [InlineData("rgb(255, 255, 255)")]
    [InlineData("oklch(0.9 0.1 120)")]
    [InlineData("white")]
    [InlineData("#gggggg")]
    [InlineData("ffffff")]
    [InlineData("")]
    [InlineData("  ")]
    public void Reddeder_AltiHaneliOlmayanBicimi(string deger) =>
        Assert.False(ThemeContrast.TryParseSixDigit(deger, out _));

    [Fact]
    public void KabulEder_SekizHaneliyi_YalnizSabitTokenYolunda()
    {
        Assert.False(ThemeContrast.TryParseSixDigit("#16191d66", out _));
        Assert.True(ThemeContrast.TryParseAny("#16191d66", out ThemeColor renk));

        // Alfa taşınıyor ama kontrast hesabına girmiyor: kanal değerleri ilk altı haneden.
        Assert.Equal(Renk("#16191d"), renk);
    }

    private static ThemeColor Renk(string deger)
    {
        Assert.True(ThemeContrast.TryParseAny(deger, out ThemeColor renk), deger);

        return renk;
    }
}
