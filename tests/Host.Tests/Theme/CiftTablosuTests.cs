using CarGallery.Host.Theme;

namespace CarGallery.Host.Tests.Theme;

/// <summary>
/// Çift tablosunun kendi tutarlılığı. Tablo kapının TEK kaynağı (C12); tutarsız bir tablo,
/// hiçbir testin göremediği bir kapı deliği demektir.
/// </summary>
public sealed class CiftTablosuTests
{
    [Fact]
    public void Tanimlidir_CiftlerinHerTokeni()
    {
        List<string> tanimsiz =
        [
            .. ThemeTokens.Pairs
                .SelectMany(pair => new[] { pair.Foreground, pair.Background })
                .Distinct(StringComparer.Ordinal)
                .Where(key => ThemeTokens.All.All(token => token.Key != key))
        ];

        Assert.Empty(tanimsiz);
    }

    /// <summary>
    /// <b>D13'ün garantisi.</b> Kapı bir çifti ancak iki ucunu da BİLİYORSA denetleyebilir;
    /// bir ucu `tokens.css`'te sabit kalırsa host onu bilemez (R6) ve çift sessizce
    /// denetimsiz kalır. Bu test, sabit bir token taşıyan bir çift eklendiği an kırılır.
    /// </summary>
    [Fact]
    public void Coezulur_HerCiftTokeni_YapilandirilabilirBirTokena()
    {
        List<string> bilinmeyen =
        [
            .. ThemeTokens.Pairs
                .SelectMany(pair => new[] { pair.Foreground, pair.Background })
                .Distinct(StringComparer.Ordinal)
                .Where(key => !ThemeTokens.ConfigurableKeys.Contains(ThemeTokens.Resolve(key), StringComparer.Ordinal))
        ];

        Assert.Empty(bilinmeyen);
    }

    /// <summary>Eşik yalnız iki değer alır: metin 4.5:1, arayüz öğesi 3:1 (§ 7).</summary>
    [Fact]
    public void Kullanir_YalnizIkiEsik() =>
        Assert.Equal<decimal[]>([3m, 4.5m], [.. ThemeTokens.Pairs.Select(pair => pair.Threshold).Distinct().Order()]);

    /// <summary>
    /// 3:1 yalnız arayüz öğelerine ait, metne değil: odak halkası ve `0016b`'nin şemasındaki
    /// dört durum (C2). Başka bir token 3:1 alırsa ya metindir (4.5 gerekir) ya da bu liste
    /// bilinçli genişletilmelidir.
    /// </summary>
    [Fact]
    public void Verir_UcBirEsigini_YalnizArayuzOgelerine() =>
        Assert.All(
            ThemeTokens.Pairs.Where(pair => pair.Threshold == 3m),
            pair => Assert.Contains(
                pair.Foreground,
                new[]
                {
                    ThemeTokens.FocusRing,
                    ThemeTokens.PaintOriginal,
                    ThemeTokens.PaintLocalPainted,
                    ThemeTokens.PaintPainted,
                    ThemeTokens.PaintReplaced,
                }));

    /// <summary>
    /// Triyaj BULGU-2 — `tokens.css`'teki her renk token'ı kapalı listededir. Listenin dışında
    /// kalan bir renk, tema zemini değiştirdiğinde kontrastı kimsenin sormadığı bir renktir.
    /// </summary>
    [Fact]
    public void Kapsar_TokensCsstekiHerRenkTokenini()
    {
        string tokens = File.ReadAllText(Path.Combine(CozumKoku(), "frontend", "src", "shared", "design", "tokens.css"));

        List<string> disarida =
        [
            .. System.Text.RegularExpressions.Regex.Matches(tokens, @"^\s*--(color-[a-z-]+)\s*:", System.Text.RegularExpressions.RegexOptions.Multiline)
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .Where(key => ThemeTokens.All.All(token => token.Key != key))
        ];

        Assert.Empty(disarida);
    }

    /// <summary>Şemanın dört durumunun HER BİRİ şema zemini çiftinde (AC-25'in kapı yarısı).</summary>
    [Fact]
    public void Denetler_SemaninDortDurumunu_ZemineKarsi()
    {
        foreach (string durum in new[] { ThemeTokens.PaintOriginal, ThemeTokens.PaintLocalPainted, ThemeTokens.PaintPainted, ThemeTokens.PaintReplaced })
        {
            Assert.Contains(ThemeTokens.Pairs, pair => pair.Foreground == durum && pair.Background == ThemeTokens.Surface && pair.Threshold == 3m);
        }
    }

    private static string CozumKoku()
    {
        DirectoryInfo? dizin = new(AppContext.BaseDirectory);

        while (dizin is not null && !File.Exists(Path.Combine(dizin.FullName, "CarGallery.slnx")))
        {
            dizin = dizin.Parent;
        }

        return dizin?.FullName ?? throw new InvalidOperationException("CarGallery.slnx bulunamadı.");
    }

    [Fact]
    public void Tasir_KriterNumarasi_HerCift() =>
        Assert.All(ThemeTokens.Pairs, pair => Assert.StartsWith("AC-", pair.Criterion, StringComparison.Ordinal));

    /// <summary>Türetilmiş token dolgudan DEĞİL gövde metninden türetilir (Token tablosu).</summary>
    [Fact]
    public void Turetilir_DolguUzerindekiMetin_GovdeMetninden()
    {
        Assert.Equal(ThemeTokens.Ink, ThemeTokens.Resolve(ThemeTokens.OnAccentFill));
        Assert.Equal(ThemeTokens.AccentText, ThemeTokens.Resolve(ThemeTokens.FocusRing));
    }
}
