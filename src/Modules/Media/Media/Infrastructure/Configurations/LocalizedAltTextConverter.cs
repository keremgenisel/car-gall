using System.Text.Json;
using CarGallery.BuildingBlocks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CarGallery.Modules.Media.Infrastructure.Configurations;

/// <summary>
/// R9 — locale başına `alt` metnini jsonb'ye serileştirir.
///
/// <para><b>Modül başına kendi kopyası var ve bu bilinçli</b> (Yasak Listesi 10: "sınır
/// boyunca tekrar bilinçlidir"). `Dealership` ve `VehicleCatalog` da kendi dönüştürücüsünü
/// taşıyor; ortak bir tip `BuildingBlocks`'a konsaydı, bir modülün serileştirme kararı
/// diğerlerinin şemasını bağlardı.</para>
/// </summary>
internal static class LocalizedAltTextStorage
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General);

    internal static ValueConverter<LocalizedText, string> Converter { get; } = new(
        text => Serialize(text),
        json => Rebuild(json));

    /// <summary>
    /// Karşılaştırıcı olmadan EF içerik değişikliğini kaçırır ve `alt` metni güncellemesi
    /// sessizce kaybolur.
    /// </summary>
    internal static ValueComparer<LocalizedText> Comparer { get; } = new(
        (left, right) => Serialize(left) == Serialize(right),
        text => Serialize(text).GetHashCode(StringComparison.Ordinal),
        text => Rebuild(Serialize(text)));

    private static string Serialize(LocalizedText? text) =>
        JsonSerializer.Serialize(text is null ? [] : text.ToDictionary(), Options);

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
