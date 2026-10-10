using System.Text.Json;
using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CarGallery.Modules.VehicleCatalog.Infrastructure.Configurations;

/// <summary>
/// Yazma modelinin değer tipi dönüşümleri (0010a **D10**).
///
/// <para>Agregatlar bu spec'e kadar hiç kaydedilmemişti; buradaki her şey yeni tesisat ve
/// hiçbir domain metodunun davranışını değiştirmiyor.</para>
/// </summary>
internal static class CatalogConverters
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    internal static ValueConverter<VehicleId, Guid> VehicleId { get; } =
        new(id => id.Value, value => new VehicleId(value));

    internal static ValueConverter<ListingId, Guid> ListingId { get; } =
        new(id => id.Value, value => new ListingId(value));

    internal static ValueConverter<Vin, string> Vin { get; } =
        new(vin => vin.Value, value => Domain.Vin.Create(value).Value);

    internal static ValueConverter<BodyTypeSlug, string> BodyTypeSlug { get; } =
        new(slug => slug.Value, value => Domain.BodyTypeSlug.Create(value).Value);

    /// <summary>
    /// `LocalizedText` sınıf ve iç sözlüğü özel; `ToDictionary()`/`From()` üzerinden
    /// jsonb'ye çevriliyor — okuma modelindeki `LocalizedNames` deseninin aynısı.
    /// </summary>
    internal static ValueConverter<LocalizedText, string> LocalizedText { get; } = new(
        text => JsonSerializer.Serialize(text.ToDictionary(), Json),
        json => CarGallery.BuildingBlocks.LocalizedText.From(
            (JsonSerializer.Deserialize<Dictionary<string, string>>(json, Json) ?? new Dictionary<string, string>())
                .Select(pair => new KeyValuePair<Locale, string>(Locale.Create(pair.Key).Value, pair.Value))));

    /// <summary>
    /// `LocalizedText?` için aynı dönüşüm. Opsiyonel alan değerinde taşıyıcı NULL olabilir
    /// (değer locale'li metin değilse) ve EF nullable referans tipini ayrı bir dönüştürücü
    /// olarak istiyor.
    /// </summary>
    internal static ValueConverter<LocalizedText?, string> NullableLocalizedText { get; } = new(
        text => text == null ? "{}" : JsonSerializer.Serialize(text.ToDictionary(), Json),
        json => CarGallery.BuildingBlocks.LocalizedText.From(
            (JsonSerializer.Deserialize<Dictionary<string, string>>(json, Json) ?? new Dictionary<string, string>())
                .Select(pair => new KeyValuePair<Locale, string>(Locale.Create(pair.Key).Value, pair.Value))));

    /// <summary>
    /// Karşılaştırıcı olmadan EF `LocalizedText`'i REFERANSLA karşılaştırır ve içerik
    /// değişikliğini kaçırır — güncelleme sessizce kaybolur. Okuma modelinde aynı tuzağa
    /// karşı aynı önlem alınmıştı.
    /// </summary>
    internal static ValueComparer<LocalizedText> LocalizedTextComparer { get; } = new(
        (left, right) => left != null && right != null
            ? left.ToDictionary().Count == right.ToDictionary().Count
                && !left.ToDictionary().Except(right.ToDictionary()).Any()
            : left == right,
        text => text.ToDictionary().Aggregate(
            0,
            (hash, pair) => HashCode.Combine(hash, pair.Key.GetHashCode(), pair.Value.GetHashCode())),
        text => CarGallery.BuildingBlocks.LocalizedText.From(
            text.ToDictionary().Select(pair => new KeyValuePair<Locale, string>(Locale.Create(pair.Key).Value, pair.Value))));

    /// <summary>
    /// `ListingPrice` TEK kolona çevriliyor ve bu bir EF sınırının sonucu: değer tipinin
    /// tek kurucusu `Money? amount` alıyor ve **EF iç içe karmaşık tipi kurucu
    /// parametresine bağlayamıyor** (denendi, model kurulumunda düşüyor). Karmaşık tip
    /// yolu kapalı olduğu için ya agregat yeniden şekillendirilecekti — C17'nin
    /// yasakladığı şey — ya da dönüşüm.
    ///
    /// <para><b>Biçim ondalık ÖLÇEĞİ korur:</b> `decimal.ToString(InvariantCulture)`
    /// sondaki sıfırları da yazar, yani KWD'nin `1250,500` tutarı `1250.500` olarak gider
    /// ve aynı ölçekle geri gelir (AC-22). Üs ayrıca saklanıyor; ikisinden biri kaybolsa
    /// üç ondalıklı para birimi sessizce bozulurdu.</para>
    ///
    /// <para><b>Neden sorgulanabilirlik kaybı sorun değil:</b> vitrin filtreleri
    /// `showcase_listings` okuma modeli üzerinden çalışıyor ve orada fiyat zaten dört ayrı
    /// kolonda. Yazma modeli yalnız kimlikle okunuyor.</para>
    /// </summary>
    internal static ValueConverter<ListingPrice, string> ListingPrice { get; } = new(
        price => Encode(price),
        value => Decode(value));

    private const string OnRequestMarker = "onrequest";

    private static string Encode(ListingPrice price) =>
        price.IsOnRequest || price.Amount is null
            ? OnRequestMarker
            : string.Join(
                '|',
                price.Amount.Value.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                price.Amount.Value.CurrencyCode,
                price.Amount.Value.MinorUnitExponent.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static ListingPrice Decode(string value)
    {
        if (value == OnRequestMarker)
        {
            return Domain.ListingPrice.OnRequest();
        }

        string[] parts = value.Split('|');

        return Domain.ListingPrice.Of(Money.Create(
            decimal.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
            parts[1],
            int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture)).Value);
    }

    internal static ValueConverter<List<Guid>, string> GuidList { get; } = new(
        ids => JsonSerializer.Serialize(ids, Json),
        json => JsonSerializer.Deserialize<List<Guid>>(json, Json) ?? new List<Guid>());

    internal static ValueComparer<List<Guid>> GuidListComparer { get; } = new(
        (left, right) => left != null && right != null ? left.SequenceEqual(right) : left == right,
        ids => ids.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
        ids => new List<Guid>(ids));
}
