using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

/// <summary>
/// Locale → ad sözlüğünü jsonb'ye açıkça serileştirir. Npgsql'in dinamik JSON desteğini
/// global olarak açmak yerine dönüşümü burada tutuyoruz: dönüşüm görünür kalır, veri
/// tipinin davranışı bağlantı yapılandırmasına bağlı olmaz.
/// </summary>
internal static class LocalizedNames
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.General);

    internal static ValueConverter<Dictionary<string, string>, string> Converter { get; } = new(
        names => JsonSerializer.Serialize(names, SerializerOptions),
        json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, SerializerOptions) ?? new Dictionary<string, string>());

    /// <summary>
    /// Karşılaştırıcı olmadan EF sözlüğü referansla karşılaştırır ve içerik değişikliğini
    /// kaçırır — güncelleme sessizce kaybolur.
    /// </summary>
    internal static ValueComparer<Dictionary<string, string>> Comparer { get; } = new(
        (left, right) => left != null && right != null
            ? left.Count == right.Count && !left.Except(right).Any()
            : left == right,
        names => names.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key.GetHashCode(), pair.Value.GetHashCode())),
        names => new Dictionary<string, string>(names));
}
