using CarGallery.BuildingBlocks;
using CarGallery.Host.Theme;
using Microsoft.Extensions.Configuration;

namespace CarGallery.Host.Tests.Theme;

/// <summary>
/// Yapılandırma doğrulamasının davranış matrisi.
///
/// <para><b>Buradaki testler çıplak `[Fact]`'tir ve bu bilinçli.</b> Kriterler "uygulama
/// AYAĞA KALKMAZ" diyor; `Create` çağrısı bunu kanıtlamaz, yalnız doğrulamanın sonucunu
/// kanıtlar. Kriterlerin kapısı gerçek boru hattında açılıyor
/// (<see cref="CarGallery.Host.Tests.TemaBaslangicTests"/>). Kriter adını buraya yazmak,
/// kanıtlamadığı bir şeyi iddia etmek olurdu.</para>
/// </summary>
public sealed class TemaYapilandirmasiTests
{
    /// <summary>Dört durumun BİRİNCİSİ — tek sessiz varsayılana düşme yolu (C5).</summary>
    [Fact]
    public void Dusur_Varsayilana_HicDegerVerilmediginde()
    {
        Result<ThemeOptions> sonuc = Olustur([]);

        Assert.True(sonuc.IsSuccess);
        Assert.True(sonuc.Value.IsDefault);
        Assert.Equal(string.Empty, sonuc.Value.StyleAttributeValue);
    }

    /// <summary>İKİNCİSİ — kısmi tema. Hangi token eksikse adıyla söylenir.</summary>
    [Theory]
    [InlineData(ThemeTokens.Ink)]
    [InlineData(ThemeTokens.AccentFill)]
    [InlineData(ThemeTokens.InkMuted)]
    [InlineData(ThemeTokens.DangerSurface)]
    public void Reddeder_KismiTemayi(string eksik)
    {
        Dictionary<string, string> tema = GecerliTema();
        tema.Remove(eksik);

        Error hata = Olustur(tema).Error;

        Assert.Equal(ThemeErrors.TokenMissing, hata.Code);
        Assert.Equal(eksik, hata.Parameters!["token"]);
    }

    /// <summary>ÜÇÜNCÜSÜ, birinci yarı — tabloda olmayan anahtar.</summary>
    [Theory]
    [InlineData("color-lemon")]
    [InlineData("color-accent")]
    [InlineData("renk-zemin")]
    public void Reddeder_TaninmayanAnahtari(string anahtar)
    {
        Dictionary<string, string> tema = GecerliTema();
        tema[anahtar] = "#123456";

        Error hata = Olustur(tema).Error;

        Assert.Equal(ThemeErrors.UnknownToken, hata.Code);
        Assert.Equal(anahtar, hata.Parameters!["token"]);
    }

    /// <summary>
    /// Yalnız RENK yapılandırılabilir. Tipografi, köşe ve aralık anahtarları tabloda
    /// var ama yapılandırılamaz; bu yüzden tanınmayan anahtar dalından reddedilir
    /// (X9/D9). Türetilmiş token'lar da verilemez: türetilmiş bir değeri dışarıdan
    /// vermek, türetmeyi anlamsız kılar.
    /// </summary>
    [Theory]
    [InlineData(ThemeTokens.FontFamilyBase)]
    [InlineData(ThemeTokens.FontFamilyArabic)]
    [InlineData(ThemeTokens.Backdrop)]
    [InlineData(ThemeTokens.OnAccentFill)]
    [InlineData(ThemeTokens.FocusRing)]
    [InlineData("radius-md")]
    [InlineData("space-4")]
    public void Reddeder_YapilandirilamayanAnahtari(string anahtar)
    {
        Dictionary<string, string> tema = GecerliTema();
        tema[anahtar] = "#123456";

        Assert.Equal(ThemeErrors.UnknownToken, Olustur(tema).Error.Code);
    }

    /// <summary>ÜÇÜNCÜSÜ, ikinci yarı — biçim (C11).</summary>
    [Theory]
    [InlineData("#fff")]
    [InlineData("#ffffffff")]
    [InlineData("rgb(11, 12, 14)")]
    [InlineData("black")]
    [InlineData("")]
    public void Reddeder_BicimeUymayanDegeri(string deger)
    {
        Dictionary<string, string> tema = GecerliTema();
        tema[ThemeTokens.Ink] = deger;

        Error hata = Olustur(tema).Error;

        Assert.Equal(ThemeErrors.InvalidColorFormat, hata.Code);
        Assert.Equal(ThemeTokens.Ink, hata.Parameters!["token"]);
    }

    /// <summary>
    /// DÖRDÜNCÜSÜ — kontrast. Her çift için ayrı bir senaryo yazmak yerine tablonun
    /// KENDİSİ sürülüyor: tabloya bir satır eklendiğinde bu test onu da kapsar, elle
    /// güncellenmesi gereken bir liste kalmaz.
    ///
    /// <para><b>Ne kanıtlar, ne kanıtlamaz:</b> her çift için o çifti bozmanın REDDE yol
    /// açtığını kanıtlar. Raporlanan çiftin bozduğum çift OLDUĞUNU kanıtlamaz — token'lar
    /// paylaşıldığı için bir çifti bozmak komşularını da bozar ve kapı listedeki İLK
    /// başarısız çifti bildirir. Tek bir çifti yalıtmak tasarım gereği mümkün değil;
    /// hangi çiftin bildirildiğini ayrı bir test ölçüyor
    /// (<see cref="Soyler_CiftiEsigiVeOlcuElen"/>).</para>
    /// </summary>
    [Fact]
    public void Reddeder_EsikAltindaKalanHerCifti()
    {
        List<string> gecenler = [];

        foreach (ContrastPair cift in ThemeTokens.Pairs)
        {
            Dictionary<string, string> tema = GecerliTema();

            // İki ucu da aynı renge çekmek oranı 1.00'e düşürür: hiçbir eşiği karşılamaz.
            tema[ThemeTokens.Resolve(cift.Foreground)] = "#808080";
            tema[ThemeTokens.Resolve(cift.Background)] = "#808080";

            Error hata = Olustur(tema).Error;

            if (hata.Code != ThemeErrors.ContrastBelowThreshold)
            {
                gecenler.Add($"{cift.Foreground} ↔ {cift.Background}: {hata.Code}");
            }
        }

        Assert.Empty(gecenler);
    }

    /// <summary>AC-22'nin içeriği: hangi çift, hangi eşik, ölçülen ne.</summary>
    [Fact]
    public void Soyler_CiftiEsigiVeOlcuElen()
    {
        Dictionary<string, string> tema = GecerliTema();
        tema[ThemeTokens.AccentText] = "#f0f0f0";

        Error hata = Olustur(tema).Error;

        Assert.Equal(ThemeErrors.ContrastBelowThreshold, hata.Code);
        Assert.Equal(ThemeTokens.AccentText, hata.Parameters!["foreground"]);
        Assert.Equal(ThemeTokens.Surface, hata.Parameters["background"]);
        Assert.Equal("4.5", hata.Parameters["threshold"]);
        Assert.Equal("AC-17", hata.Parameters["criterion"]);
        Assert.NotEqual("0.00", hata.Parameters["measured"]);
    }

    [Fact]
    public void KabulEder_TumEsikleriKarsilayanTemayi()
    {
        Result<ThemeOptions> sonuc = Olustur(GecerliTema());

        Assert.True(sonuc.IsSuccess, sonuc.IsFailure ? sonuc.Error.Code : null);
        Assert.False(sonuc.Value.IsDefault);
    }

    /// <summary>
    /// Enjekte edilen gövde: yalnız YAPILANDIRILABİLİR token'lar, kararlı sırada,
    /// küçük harfe indirgenmiş. Türetilmiş token'lar YOK — onlar `tokens.css`'te
    /// `var()` ile duruyor ve `:root` zaten `&lt;html&gt;`'in kendisi.
    /// </summary>
    [Fact]
    public void Uretir_YalnizYapilandirilabilirleri_KarariSirada()
    {
        string gövde = Olustur(GecerliTema()).Value.StyleAttributeValue;

        Assert.StartsWith("--color-surface:#ffffff;--color-surface-muted:", gövde, StringComparison.Ordinal);
        Assert.DoesNotContain(ThemeTokens.OnAccentFill, gövde, StringComparison.Ordinal);
        Assert.DoesNotContain(ThemeTokens.FocusRing, gövde, StringComparison.Ordinal);
        Assert.Equal(ThemeTokens.ConfigurableKeys.Count, gövde.Split(';').Length);
    }

    /// <summary>
    /// Mockup paleti tablonun HER satırını geçiyor. D4 onaylandıktan sonra (2026-10-05)
    /// bu sözlük artık "mockup + bugünkü yardımcı renkler" karması değil, mockup'ın
    /// paletinin TAMAMI — varsayılanla aynı değerler.
    ///
    /// <para><b>Varsayılanla aynı olması sorun değil, bilinçli:</b> bu test
    /// yapılandırmanın BORU HATTINI ölçüyor (eksik anahtar, geçersiz biçim, kontrast
    /// kapısı), değerlerin farklı olmasını değil. Farklı bir paletle çalıştığının kanıtı
    /// AC-15'in iki tema görüntüsünde.</para>
    /// </summary>
    internal static Dictionary<string, string> GecerliTema() => new(StringComparer.OrdinalIgnoreCase)
    {
        [ThemeTokens.Surface] = "#ffffff",
        [ThemeTokens.SurfaceMuted] = "#f3f4f1",
        [ThemeTokens.Ink] = "#0b0c0e",
        [ThemeTokens.InkMuted] = "#5f656c",
        [ThemeTokens.InkInverse] = "#ffffff",
        [ThemeTokens.AccentFill] = "#cdf564",
        [ThemeTokens.AccentText] = "#4f6b00",
        [ThemeTokens.AccentSurface] = "#edf6d6",
        [ThemeTokens.Border] = "#e4e5e1",
        [ThemeTokens.Danger] = "#b8412a",
        [ThemeTokens.DangerSurface] = "#fbece9",
    };

    private static Result<ThemeOptions> Olustur(Dictionary<string, string> tema) =>
        ThemeOptions.Create(new ConfigurationBuilder()
            .AddInMemoryCollection(tema.Select(pair =>
                new KeyValuePair<string, string?>($"{ThemeTokens.SectionName}:{pair.Key}", pair.Value)))
            .Build());
}
