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

    /// <summary>3:1 yalnız odak halkasına ait: halka metin değil, arayüz öğesidir.</summary>
    [Fact]
    public void Verir_UcBirEsigini_YalnizOdakHalkasina() =>
        Assert.All(
            ThemeTokens.Pairs.Where(pair => pair.Threshold == 3m),
            pair => Assert.Equal(ThemeTokens.FocusRing, pair.Foreground));

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
