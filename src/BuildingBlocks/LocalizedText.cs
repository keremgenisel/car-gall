namespace CarGallery.BuildingBlocks;

/// <summary>
/// Locale başına metin. Çeviri, içeriğin sahibi olan modülde durur; merkezi çeviri
/// tablosu yoktur (docs/architecture.md § 3, Yasak Listesi 21).
/// </summary>
public sealed class LocalizedText
{
    private readonly IReadOnlyDictionary<string, string> valuesByLocale;

    private LocalizedText(IReadOnlyDictionary<string, string> valuesByLocale) =>
        this.valuesByLocale = valuesByLocale;

    public static LocalizedText Empty { get; } = new(new Dictionary<string, string>());

    public static LocalizedText From(IEnumerable<KeyValuePair<Locale, string>> values)
    {
        Dictionary<string, string> map = [];
        foreach ((Locale locale, string text) in values)
        {
            map[locale.Code] = text;
        }

        return new LocalizedText(map);
    }

    /// <summary>
    /// İstenen locale yoksa fallback'e düşer; o da yoksa <c>null</c> döner.
    /// Sessiz fallback araç serbest metni için bilinçli bir karardır (docs/architecture.md § 3).
    /// </summary>
    public string? Resolve(Locale requested, Locale fallback)
    {
        if (valuesByLocale.TryGetValue(requested.Code, out string? text))
        {
            return text;
        }

        return valuesByLocale.TryGetValue(fallback.Code, out string? fallbackText) ? fallbackText : null;
    }

    public bool HasValueFor(Locale locale) => valuesByLocale.ContainsKey(locale.Code);
}
