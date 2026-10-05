using CarGallery.Host.Theme;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarGallery.Host.Tests;

/// <summary>
/// **Uygulama AYAĞA KALKMAZ** — kriterlerin söylediği şey bu ve kapısı burada.
/// `ThemeOptions.Create` testleri doğrulamanın sonucunu kanıtlıyor; bu testler o sonucun
/// gerçekten başlangıcı DURDURDUĞUNU kanıtlıyor. İkisi ayrı: doğrulama yazılıp
/// `Program.cs`'e bağlanmasa ilk takım yeşil kalırdı.
///
/// <para>Veritabanı gerekmiyor: doğrulama DI kurulumunda, şema kontrolünden ve bağlantı
/// açılmadan önce patlıyor — zaten iddia edilen de bu (C9).</para>
///
/// <para><b>Kontrast senaryoları aritmetikle kuruldu.</b> Kapı listedeki İLK başarısız
/// çifti bildiriyor, bu yüzden bir kriteri yalıtmak için hedef çiftin ilk düşen olması
/// gerekiyor. Örneğin AC-20 için dolgu `#949494` seçildi: gövde metnine karşı 6.45:1
/// (4.5'i geçer, AC-19 düşmez) ama odak halkasına karşı 2.02:1 — yalnız 3:1 eşiği kırılır.</para>
/// </summary>
public sealed class TemaBaslangicTests
{
    [Fact(DisplayName = "frontend-002/AC-11: Tema kısmen verildiğinde — tablonun yapılandırılabilir sütunundaki bir anahtar eksikken — uygulama ayağa kalkmaz ve hangi token'ın eksik olduğunu söyler")]
    public void Kalkmaz_KismiTemada()
    {
        Dictionary<string, string> tema = Tema();
        tema.Remove(ThemeTokens.InkMuted);

        Assert.Contains(ThemeTokens.InkMuted, Baslatmayi(tema), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "frontend-002/AC-12: Token tablosunda olmayan bir anahtar verildiğinde uygulama ayağa kalkmaz ve tanınmayan anahtarı söyler")]
    public void Kalkmaz_TaninmayanAnahtarda()
    {
        Dictionary<string, string> tema = Tema();
        tema["color-lemon"] = "#cdf564";

        Assert.Contains("color-lemon", Baslatmayi(tema), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "frontend-002/AC-13: Altı haneli hex biçiminde olmayan bir renk değeri verildiğinde uygulama ayağa kalkmaz, hangi token olduğunu söyler ve hiçbir değer enjekte edilmez")]
    public void Kalkmaz_BicimeUymayanDegerde()
    {
        Dictionary<string, string> tema = Tema();
        tema[ThemeTokens.Surface] = "#fff";

        string mesaj = Baslatmayi(tema);

        Assert.Contains(ThemeTokens.Surface, mesaj, StringComparison.Ordinal);

        // "Hiçbir değer enjekte edilmez": uygulama hiç kurulmadı, dolayısıyla enjeksiyonu
        // yapan sınıf hiç oluşturulmadı. Kısmi enjeksiyon diye bir ara durum yok.
        Assert.Contains("#RRGGBB", mesaj, StringComparison.Ordinal);
    }

    [Theory(DisplayName = "frontend-002/AC-16: Yalnız renk yapılandırılabilir: yapılandırmaya tipografi, köşe veya aralık anahtarı verilmesi AC-12 kapsamında reddedilir")]
    [InlineData(ThemeTokens.FontFamilyBase)]
    [InlineData(ThemeTokens.FontFamilyArabic)]
    [InlineData("radius-md")]
    [InlineData("space-4")]
    public void Kalkmaz_TipografiKoseVeyaAralikVerildiginde(string anahtar)
    {
        Dictionary<string, string> tema = Tema();
        tema[anahtar] = "değer";

        Assert.Contains(anahtar, Baslatmayi(tema), StringComparison.Ordinal);
    }

    [Fact(DisplayName = "frontend-002/AC-17: Metin vurgusu, zemin ve yüzey grisinin her birine karşı en az 4.5:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz")]
    public void Kalkmaz_MetinVurgusuEsikAltinda() =>
        Assert.Contains("AC-17", Bozuk(ThemeTokens.AccentText, "#f0f0f0"), StringComparison.Ordinal);

    [Fact(DisplayName = "frontend-002/AC-18: Gövde metin rengi, zemin ve yüzey grisinin her birine karşı en az 4.5:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz")]
    public void Kalkmaz_GovdeMetniEsikAltinda() =>
        Assert.Contains("AC-18", Bozuk(ThemeTokens.Ink, "#f0f0f0"), StringComparison.Ordinal);

    /// <summary>Dolgu, gövde metnine karşı 4.5'i geçecek kadar koyu seçildi ki AC-18 düşmesin.</summary>
    [Fact(DisplayName = "frontend-002/AC-19: Dolgu üzerindeki metin token'ı, dolgu vurgusuna karşı en az 4.5:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz")]
    public void Kalkmaz_DolguUzerindekiMetinEsikAltinda() =>
        Assert.Contains("AC-19", Bozuk(ThemeTokens.AccentFill, "#0c0d0f"), StringComparison.Ordinal);

    [Fact(DisplayName = "frontend-002/AC-20: Odak halkası, zemin, yüzey grisi ve dolgu vurgusunun her birine karşı en az 3:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz")]
    public void Kalkmaz_OdakHalkasiEsikAltinda() =>
        Assert.Contains("AC-20", Bozuk(ThemeTokens.AccentFill, "#949494"), StringComparison.Ordinal);

    /// <summary>
    /// Eşiğin çift tablosundan okunduğunun KANITI: dolgu `#c8c8c8` seçildiğinde odak
    /// halkası ona karşı 3.65:1 veriyor — 3:1'i geçer, 4.5:1'i geçmez. Kapı bu temayı
    /// KABUL ediyor. Eşik çift bazlı okunmasa, halkaya da 4.5 uygulanır ve tema reddedilirdi.
    /// </summary>
    [Fact(DisplayName = "frontend-002/AC-21: Kapı eşiği Context § Çift tablosundan okur; tabloda yazılı olmayan bir eşik uygulanmaz ve tabloda yazılı her çift denetlenir")]
    public void Uygular_UcBirEsigini_YalnizOdakHalkasina()
    {
        Dictionary<string, string> tema = Tema();
        tema[ThemeTokens.AccentFill] = "#c8c8c8";

        Assert.Null(BaslatmayiDene(tema));
    }

    [Fact(DisplayName = "frontend-002/AC-22: Uygulama kontrast nedeniyle ayağa kalkmadığında hangi token çiftinin hangi eşiği karşılamadığını söyler")]
    public void Soyler_CiftiVeEsigi()
    {
        string mesaj = Bozuk(ThemeTokens.AccentText, "#f0f0f0");

        Assert.Contains(ThemeTokens.AccentText, mesaj, StringComparison.Ordinal);
        Assert.Contains(ThemeTokens.Surface, mesaj, StringComparison.Ordinal);
        Assert.Contains("4.5", mesaj, StringComparison.Ordinal);
    }

    [Fact(DisplayName = "frontend-002/AC-31: Yardımcı metin rengi, zemin ve yüzey grisinin her birine karşı en az 4.5:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz")]
    public void Kalkmaz_YardimciMetinEsikAltinda() =>
        Assert.Contains("AC-31", Bozuk(ThemeTokens.InkMuted, "#f0f0f0"), StringComparison.Ordinal);

    [Fact(DisplayName = "frontend-002/AC-32: Metin vurgusu, vurgu yüzeyine karşı en az 4.5:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz")]
    public void Kalkmaz_VurguYuzeyiEsikAltinda() =>
        Assert.Contains("AC-32", Bozuk(ThemeTokens.AccentSurface, "#4f6b00"), StringComparison.Ordinal);

    [Fact(DisplayName = "frontend-002/AC-33: Hata rengi, zemine ve hata yüzeyine karşı en az 4.5:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz")]
    public void Kalkmaz_HataRengiEsikAltinda() =>
        Assert.Contains("AC-33", Bozuk(ThemeTokens.Danger, "#f0f0f0"), StringComparison.Ordinal);

    [Fact(DisplayName = "frontend-002/AC-34: Ters metin rengi, gövde metin rengine karşı en az 4.5:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz")]
    public void Kalkmaz_TersMetinEsikAltinda() =>
        Assert.Contains("AC-34", Bozuk(ThemeTokens.InkInverse, "#0b0c0e"), StringComparison.Ordinal);

    private static Dictionary<string, string> Tema() => Theme.TemaYapilandirmasiTests.GecerliTema();

    private static string Bozuk(string token, string deger)
    {
        Dictionary<string, string> tema = Tema();
        tema[token] = deger;

        return Baslatmayi(tema);
    }

    private static string Baslatmayi(Dictionary<string, string> tema)
    {
        string? mesaj = BaslatmayiDene(tema);

        Assert.NotNull(mesaj);

        return mesaj;
    }

    /// <summary>
    /// Kuruluma kadar gider; tema geçerliyse `null` döner. Veritabanı olmadığı için
    /// geçerli temada ŞEMA hatası beklenir — o yüzden yalnız tema hatası ayıklanıyor.
    /// </summary>
    private static string? BaslatmayiDene(Dictionary<string, string> tema)
    {
        using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseWebRoot(Path.Combine(AppContext.BaseDirectory, "TestWebRoot"));
                IdentityTestSettings.Apply(builder);
                NotificationsTestSettings.Apply(builder);

                foreach (KeyValuePair<string, string> deger in tema)
                {
                    builder.UseSetting($"{ThemeTokens.SectionName}:{deger.Key}", deger.Value);
                }
            });

        try
        {
            factory.CreateClient();

            return null;
        }
        catch (InvalidOperationException exception) when (exception.Message.StartsWith("Tema", StringComparison.Ordinal)
            || exception.Message.StartsWith("Kontrast", StringComparison.Ordinal)
            || exception.Message.StartsWith("Tanınmayan", StringComparison.Ordinal))
        {
            return exception.Message;
        }
#pragma warning disable CA1031 // Şema/veritabanı hatası bu testin konusu değil.
        catch (Exception)
#pragma warning restore CA1031
        {
            // Tema geçerliyse uygulama veritabanına kadar gider ve orada düşer; bu testin
            // iddiası "tema kapısı durdurdu mu", veritabanı değil.
            return null;
        }
    }
}
