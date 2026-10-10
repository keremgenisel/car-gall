using System.Reflection;
using System.Text.Json;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Contracts.Events;

namespace CarGallery.Media.Tests.Contracts;

/// <summary>
/// AC-26 — entegrasyon olayının şeklinin kırılmaması. Olay modül sınırını geçen TEK şeydir
/// (`docs/architecture.md` § 2.B); alan adı değiştiği an outbox'ta bekleyen her eski gövde
/// ayrıştırılamaz hâle gelir ve duyuru sessizce kaybolur.
/// </summary>
public sealed class OlaySekliTests
{
    [Fact(DisplayName = "0011/AC-26: `MediaAssetReady` ve `MediaAssetFailed`'in şekli kırılmaz")]
    public void Kirilmaz_OlaylarinSekli()
    {
        Assert.Equal(
            ["EventId", "OccurredAt", "MediaAssetId", "Url"],
            Alanlar(typeof(MediaAssetReady)));

        Assert.Equal(
            ["EventId", "OccurredAt", "MediaAssetId", "Reason"],
            Alanlar(typeof(MediaAssetFailed)));
    }

    /// <summary>
    /// Olay gövdesi outbox'a JSON olarak yazılıp oradan okunuyor; tur eşitliği
    /// bozulduğunda duyuru kaybolur.
    /// </summary>
    [Fact]
    public void Ayristirilir_Govde_YazildigiGibi()
    {
        MediaAssetReady hazir = new(
            Guid.CreateVersion7(),
            new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7(),
            "https://cdn.ornek.test/public/assets/a/display.webp");

        Assert.Equal(hazir, JsonSerializer.Deserialize<MediaAssetReady>(JsonSerializer.Serialize(hazir)));

        MediaAssetFailed basarisiz = new(
            Guid.CreateVersion7(),
            new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7(),
            "media.watermark_unavailable");

        Assert.Equal(basarisiz, JsonSerializer.Deserialize<MediaAssetFailed>(JsonSerializer.Serialize(basarisiz)));
    }

    /// <summary>
    /// `docs/architecture.md` § 2.B — entegrasyon olayı `record`'dur ve YALNIZCA primitive
    /// taşır. Mimari testler bunu tüm modüller için tarıyor; burada `Media`'nın iki olayı
    /// adıyla sınanıyor, çünkü ikisi bu spec'in sözleşmesi.
    /// </summary>
    [Theory]
    [InlineData(typeof(MediaAssetReady))]
    [InlineData(typeof(MediaAssetFailed))]
    public void Tasir_Olay_YalnizPrimitive(Type olay)
    {
        Assert.True(olay.IsSealed);
        Assert.True(typeof(IIntegrationEvent).IsAssignableFrom(olay));

        // Geçmiş zaman adı (`docs/conventions.md` § 2).
        Assert.True(
            olay.Name.EndsWith("Ready", StringComparison.Ordinal)
            || olay.Name.EndsWith("Failed", StringComparison.Ordinal),
            $"{olay.Name} geçmiş zaman taşımıyor.");

        foreach (PropertyInfo ozellik in olay.GetProperties())
        {
            Type tip = ozellik.PropertyType;

            Assert.True(
                tip.IsPrimitive || tip == typeof(string) || tip == typeof(Guid)
                    || tip == typeof(DateTimeOffset) || tip == typeof(decimal),
                $"{olay.Name}.{ozellik.Name} primitive değil: {tip.Name}");
        }
    }

    private static string[] Alanlar(Type olay) =>
        [.. olay.GetProperties().Select(ozellik => ozellik.Name)];
}
