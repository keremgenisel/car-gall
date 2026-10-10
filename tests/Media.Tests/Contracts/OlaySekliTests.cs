using System.Reflection;
using System.Text.Json;
using CarGallery.BuildingBlocks.Outbox;
using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Contracts.Events;

namespace CarGallery.Media.Tests.Contracts;

/// <summary>
/// AC-26 — entegrasyon olayının şeklinin kırılmaması. Olay modül sınırını geçen TEK şeydir
/// (`docs/architecture.md` § 2.B); alan adı değiştiği an outbox'ta bekleyen her eski gövde
/// ayrıştırılamaz hâle gelir ve duyuru sessizce kaybolur.
/// </summary>
public sealed class OlaySekliTests
{
    [Fact(DisplayName = "0011/AC-34: `MediaAssetReady` ve `MediaAssetFailed`'in şekli kırılmaz")]
    public void Kirilmaz_OlaylarinSekli()
    {
        Assert.Equal(
            ["EventId", "OccurredAt", "MediaAssetId", "Derivatives"],
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
            [new MediaDerivativePayload("display", "https://cdn.ornek.test/public/assets/a/display.webp", 800, 533)]);

        MediaAssetReady geri = JsonSerializer.Deserialize<MediaAssetReady>(
            JsonSerializer.Serialize(hazir))!;

        // `record` eşitliği koleksiyonu REFERANSLA karşılaştırır; tur eşitliği alan alan
        // ölçülüyor. Aksi hâlde bu test hiçbir zaman yeşil olmazdı ve "şekli korundu"
        // iddiası boş kalırdı.
        Assert.Equal(hazir.EventId, geri.EventId);
        Assert.Equal(hazir.MediaAssetId, geri.MediaAssetId);
        Assert.Equal(hazir.Derivatives, geri.Derivatives);

        MediaAssetFailed basarisiz = new(
            Guid.CreateVersion7(),
            new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero),
            Guid.CreateVersion7(),
            MediaFailureReason.WatermarkUnavailable);

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
            Assert.True(
                Izinli(ozellik.PropertyType),
                $"{olay.Name}.{ozellik.Name} ne primitive ne kendi DTO'su: {ozellik.PropertyType.Name}");
        }
    }

    /// <summary>
    /// `docs/architecture.md` § 2.B — entegrasyon olayı *"yalnızca primitive **ve kendi
    /// DTO'ları**"* taşır. İlk yazımda bu test yalnız primitive'e izin veriyordu ve iki
    /// olay da all-primitive olduğu için fark etmemişti; AC-29 türev kümesini, AC-33
    /// kapalı sebep kümesini getirdiğinde test haklı olmayan bir sebeple kırmızı verdi.
    ///
    /// <para>"Kendi DTO'su" DAR tanımlanıyor: tip bu modülün `Contracts` ad alanında
    /// olmalı. Başka modülün tipi ya da keyfi bir koleksiyon elemanı geçmez — sınırı
    /// geçen tek şeyin bu modülün kendi yüzeyi olduğu iddiası aksi hâlde boşalırdı.</para>
    /// </summary>
    private static bool Izinli(Type tip)
    {
        if (tip.IsPrimitive || tip == typeof(string) || tip == typeof(Guid)
            || tip == typeof(DateTimeOffset) || tip == typeof(decimal))
        {
            return true;
        }

        // Kendi ad alanındaki enum ya da record (ör. `MediaFailureReason`,
        // `MediaDerivativePayload`).
        if (KendiSozlesmesi(tip))
        {
            return true;
        }

        // Kendi DTO'sunun salt okunur listesi.
        if (tip.IsGenericType
            && tip.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            return KendiSozlesmesi(tip.GenericTypeArguments[0]);
        }

        return false;
    }

    private static bool KendiSozlesmesi(Type tip) =>
        (tip.Namespace ?? string.Empty)
            .StartsWith("CarGallery.Modules.Media.Contracts", StringComparison.Ordinal);

    private static string[] Alanlar(Type olay) =>
        [.. olay.GetProperties().Select(ozellik => ozellik.Name)];
}
