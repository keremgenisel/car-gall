using System.Text;
using CarGallery.Modules.Content.Domain;

namespace CarGallery.Modules.Content.Application;

/// <summary>
/// `0022` D11 — performans ortamının EN KÖTÜ hâli: etkin her dilde her alan sınırında
/// (başlık 120, açıklama 500 kod noktası) ve açıklamalar satır sonlu. Yalnız geliştirme
/// bayrağıyla çağrılır (<c>Development:SeedHomeTexts</c>); üretim yolunda yoktur.
/// </summary>
internal static class HomeTextDevelopmentSeed
{
    private static readonly IReadOnlyDictionary<string, string> Kelimeler = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["tr"] = "Geçmişi açık, bakımı kayıtlı ve fiyatı net araçlar galerimizde sizi bekliyor",
        ["ar"] = "سيارات بتاريخ واضح وصيانة موثقة وسعر صريح تنتظركم في معرضنا",
        ["en"] = "Cars with an open history, recorded service and a clear price wait for you here",
    };

    /// <summary>Etkin dillerin girdileri; her değer tam sınırında.</summary>
    internal static IReadOnlyList<HomeTextInput> Inputs(IReadOnlyList<string> enabledLocales) =>
    [
        .. enabledLocales.Select(locale => new HomeTextInput(
            locale,
            HomeTextField.All.ToDictionary(
                alan => alan.Key,
                alan => (string?)Metin(Kelimeler.GetValueOrDefault(locale) ?? Kelimeler["en"], alan.MaxLength, alan.IsDescription),
                StringComparer.Ordinal))),
    ];

    /// <summary>Cümle tekrarıyla tam <paramref name="uzunluk"/> kod noktası; açıklamada her cümle yeni satır.</summary>
    internal static string Metin(string cumle, int uzunluk, bool satirSonlu)
    {
        StringBuilder metin = new();
        string ayrac = satirSonlu ? ".\n" : ". ";

        while (ContentRules.Length(metin.ToString()) < uzunluk)
        {
            metin.Append(cumle).Append(ayrac);
        }

        // Son kod noktası nokta: kırpılan yer bir boşluğa denk gelse de değer tam sınırda kalır.
        return string.Concat(metin.ToString().EnumerateRunes().Take(uzunluk - 1).Select(rune => rune.ToString())) + ".";
    }
}
