using System.Text.RegularExpressions;
using CarGallery.Host.Theme;

namespace CarGallery.Host.Tests.Theme;

/// <summary>
/// R7'nin İKİNCİ kapısı: varsayılan palet (AC-24).
///
/// <para><b>Neden iki kapı var.</b> R6 host'un varsayılanları bilmemesini doğru nedenle
/// şart koşuyor — bilseydi palet iki yerde yaşardı. Ama sonucu, tema verilmediğinde
/// çalışan paletin runtime kapısına HİÇ girmemesidir. Tek kapı varsayılanı kapsamaz.</para>
///
/// <para><b>R6 ihlal edilmiyor</b> (D11): `tokens.css`'i okuyan şey UYGULAMA değil, bu
/// TEST. Bir testin spec'i bilmesi zaten testin işidir. Ve eşik tablosu tek yerde kalıyor —
/// bu test de <see cref="ThemeTokens.Pairs"/>'ı okuyor, kendi eşiğini tanımlamıyor (C12).</para>
/// </summary>
public sealed partial class VarsayilanPaletTests
{
    /// <summary>Tablonun DEĞERİNİ verdiği satırlar. Altı satır D4'ü bekliyor ve burada yok.</summary>
    private static readonly Dictionary<string, string> TablodakiDegerler = new(StringComparer.Ordinal)
    {
        [ThemeTokens.Surface] = "#ffffff",
        [ThemeTokens.SurfaceMuted] = "#f3f4f1",
        [ThemeTokens.Ink] = "#0b0c0e",
        [ThemeTokens.InkInverse] = "#ffffff",
        [ThemeTokens.AccentFill] = "#cdf564",
        [ThemeTokens.AccentText] = "#4f6b00",
        [ThemeTokens.OnAccentFill] = "#0b0c0e",
        [ThemeTokens.FocusRing] = "#4f6b00",
    };

    [Fact(DisplayName = "frontend-002/AC-24: Varsayılan palet, yapılandırılan temayla aynı eşik tablosunu okuyan ve CI'da koşan bir testle doğrulanır; eşiklerden biri ihlal edilirse test kırmızıya döner")]
    public void Karsilar_Esikleri_VarsayilanPalet()
    {
        Dictionary<string, string> palet = Palet();
        List<string> ihlaller = [];

        foreach (ContrastPair cift in ThemeTokens.Pairs)
        {
            ThemeColor on = Renk(palet, cift.Foreground);
            ThemeColor arka = Renk(palet, cift.Background);

            decimal oran = ThemeContrast.Ratio(on, arka);

            if (oran < cift.Threshold)
            {
                ihlaller.Add($"{cift.Criterion}: {cift.Foreground} ↔ {cift.Background} = {oran:0.00}:1 < {cift.Threshold:0.0}:1");
            }
        }

        Assert.True(ihlaller.Count == 0, string.Join(Environment.NewLine, ihlaller));

        // Tarama hiç çift bulamazsa da yeşil kalacağı için, bulduğunun sıfır olmadığını da
        // doğrulamak zorunda (0006'nın dersi).
        Assert.NotEmpty(ThemeTokens.Pairs);
    }

    [Fact(DisplayName = "frontend-002/AC-2: Dolgu vurgusu, metin vurgusu, dolgu üzerindeki metin ve odak halkası dört ayrı token olarak tanımlıdır")]
    public void Tanimlidir_DortAyriVurguTokeni()
    {
        Dictionary<string, string> palet = Palet();

        string[] dortu =
        [
            ThemeTokens.AccentFill,
            ThemeTokens.AccentText,
            ThemeTokens.OnAccentFill,
            ThemeTokens.FocusRing,
        ];

        Assert.All(dortu, token => Assert.True(palet.ContainsKey(token), token));

        // Dolgu, metin vurgusundan ve dolgu üzerindeki metinden FARKLI olmak zorunda:
        // aynı olsalar dört token tek bir anlamın üç kopyası olurdu.
        Assert.NotEqual(palet[ThemeTokens.AccentFill], palet[ThemeTokens.AccentText]);
        Assert.NotEqual(palet[ThemeTokens.AccentFill], palet[ThemeTokens.OnAccentFill]);

        // Dolgu üzerindeki metin dolgudan DEĞİL gövde metninden türetilir.
        Assert.Equal(palet[ThemeTokens.Ink], palet[ThemeTokens.OnAccentFill]);
    }

    [Fact(DisplayName = "frontend-002/AC-5: Tipografi token'ları Latin ve Arapça için iki ayrı yığın tanımlar")]
    public void Tanimlar_IkiTipografiYigini()
    {
        Dictionary<string, string> palet = Ham();

        string latin = palet[ThemeTokens.FontFamilyBase];
        string arapca = palet[ThemeTokens.FontFamilyArabic];

        Assert.Contains("Space Grotesk", latin, StringComparison.Ordinal);
        Assert.Contains("Readex Pro", arapca, StringComparison.Ordinal);
        Assert.Contains("IBM Plex Sans Arabic", arapca, StringComparison.Ordinal);

        // Arapça yığını Latin yığınına EŞİT OLAMAZ: Space Grotesk'te Arapça karakter yok
        // ve tarayıcı sessizce başka bir fonta düşer (tuzak 3).
        Assert.NotEqual(latin, arapca);
        Assert.DoesNotContain("Space Grotesk", arapca, StringComparison.Ordinal);
    }

    /// <summary>
    /// AC-6'nın BİRİNCİ yarısı: tablodaki her token tanımlı. İkinci yarı — hesaplanan
    /// değerin tablodakiyle aynı olması — tablonun altı satırı D4'ü beklediği için henüz
    /// tamamlanamıyor; burada tablonun DEĞER VERDİĞİ satırlar karşılaştırılıyor.
    /// </summary>
    [Fact(DisplayName = "frontend-002/AC-6: Context § Token tablosundaki her token tanımlıdır ve varsayılan temadaki hesaplanan değeri tablodaki değerle aynıdır")]
    public void Tanimlidir_HerToken_VeEsittir_TablodakiDegerlere()
    {
        Dictionary<string, string> ham = Ham();

        List<string> eksik = [.. ThemeTokens.All.Select(token => token.Key).Where(key => !ham.ContainsKey(key))];

        Assert.True(eksik.Count == 0, $"tanımsız token: {string.Join(", ", eksik)}");

        Dictionary<string, string> palet = Palet();

        foreach (KeyValuePair<string, string> beklenen in TablodakiDegerler)
        {
            Assert.Equal(beklenen.Value, palet[beklenen.Key]);
        }
    }

    private static ThemeColor Renk(Dictionary<string, string> palet, string token)
    {
        Assert.True(ThemeContrast.TryParseAny(palet[token], out ThemeColor renk), $"{token}: {palet[token]}");

        return renk;
    }

    /// <summary>`var()` referansları çözülmüş palet; kontrast hesabı için.</summary>
    private static Dictionary<string, string> Palet()
    {
        Dictionary<string, string> ham = Ham();
        Dictionary<string, string> cozulmus = new(ham, StringComparer.Ordinal);

        // Tek seviye yeterli: tabloda yalnız iki türetilmiş token var ve ikisi de doğrudan
        // bir renge bakıyor. Daha derin bir zincir çıkarsa bu döngü onu da çözer.
        for (int tur = 0; tur < 4; tur++)
        {
            foreach (string key in cozulmus.Keys.ToList())
            {
                Match referans = VarReferansi().Match(cozulmus[key]);

                if (referans.Success && cozulmus.TryGetValue(referans.Groups["token"].Value, out string? deger))
                {
                    cozulmus[key] = deger;
                }
            }
        }

        return cozulmus;
    }

    /// <summary>`tokens.css`'teki ham bildirimler — `var()` çözülmemiş hâliyle.</summary>
    private static Dictionary<string, string> Ham()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "frontend", "src", "shared", "design", "tokens.css");

        Assert.True(File.Exists(path), path);

        Dictionary<string, string> tokens = new(StringComparer.Ordinal);

        foreach (Match match in Bildirim().Matches(File.ReadAllText(path)))
        {
            tokens[match.Groups["name"].Value] = match.Groups["value"].Value.Trim();
        }

        Assert.NotEmpty(tokens);

        return tokens;
    }

    [GeneratedRegex(@"^\s*--(?<name>[\w-]+)\s*:\s*(?<value>[^;]+);", RegexOptions.Multiline)]
    private static partial Regex Bildirim();

    [GeneratedRegex(@"^var\(\s*--(?<token>[\w-]+)\s*\)$")]
    private static partial Regex VarReferansi();
}
