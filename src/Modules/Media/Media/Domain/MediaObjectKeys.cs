namespace CarGallery.Modules.Media.Domain;

/// <summary>
/// D12 — depolama yolu düzeni. İki önek var ve ayrım D16'nın taşıyıcısı:
/// <c>public/</c> CDN'e bağlanır, <c>private/</c> bağlanmaz.
///
/// <para>Orijinal ham saklanıyor (D2) ve ham bayt konum bilgisini taşıyor. R5'in garantisi
/// dışarı ÇIKANI temizlemek; orijinalin kovada durması ama hiçbir public yola düşmemesi
/// bu yüzden bir tasarım kararıdır, bir ayrıntı değil.</para>
///
/// <para>Burada yaşıyor çünkü yol düzeni bir iş kuralıdır: hangi önekin public olduğu
/// `Infrastructure`'ın bilgisi olsaydı, depolama adaptörünü değiştiren biri D16'yı
/// farkında olmadan bozabilirdi.</para>
/// </summary>
internal static class MediaObjectKeys
{
    internal const string PublicPrefix = "public/";

    internal const string PrivatePrefix = "private/";

    internal static string Original(Guid assetId, string extension) =>
        $"{PrivatePrefix}originals/{assetId:D}{extension}";

    internal static string Derivative(Guid assetId, MediaVariant variant) =>
        $"{PublicPrefix}assets/{assetId:D}/{variant.Code}.webp";

    internal static bool IsPubliclyReachable(string objectKey) =>
        objectKey.StartsWith(PublicPrefix, StringComparison.Ordinal);
}
