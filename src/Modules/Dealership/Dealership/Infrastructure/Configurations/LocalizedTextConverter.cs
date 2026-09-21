using System.Text.Json;
using CarGallery.BuildingBlocks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CarGallery.Modules.Dealership.Infrastructure.Configurations;

/// <summary>
/// Locale başına metni jsonb'ye açıkça serileştirir. Çeviriler bu modülün kendi
/// verisidir; merkezi bir `translations` tablosunda toplanmaz (C12, Yasak Listesi 21).
/// </summary>
internal static class LocalizedTextStorage
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

    internal static ValueConverter<LocalizedText, string> Converter { get; } = new(
        text => JsonSerializer.Serialize<Dictionary<string, string>>(text.ToDictionary(), Options),
        json => Rebuild(json));

    /// <summary>
    /// Karşılaştırıcı olmadan EF içerik değişikliğini kaçırır ve güncelleme sessizce kaybolur.
    /// </summary>
    internal static ValueComparer<LocalizedText> Comparer { get; } = new(
        (left, right) => Serialize(left) == Serialize(right),
        text => Serialize(text).GetHashCode(StringComparison.Ordinal),
        text => Rebuild(Serialize(text)));

    private static string Serialize(LocalizedText? text)
    {
        Dictionary<string, string> map = text is null ? [] : text.ToDictionary();
        return JsonSerializer.Serialize(map, Options);
    }

    private static LocalizedText Rebuild(string json)
    {
        Dictionary<string, string> map =
            JsonSerializer.Deserialize<Dictionary<string, string>>(json, Options) ?? [];

        List<KeyValuePair<Locale, string>> values = [];

        foreach ((string code, string value) in map)
        {
            Result<Locale> locale = Locale.Create(code);

            if (locale.IsSuccess)
            {
                values.Add(new KeyValuePair<Locale, string>(locale.Value, value));
            }
        }

        return LocalizedText.From(values);
    }
}
