using CarGallery.Modules.Media.Contracts.Assets;
using CarGallery.Modules.Media.Domain;
using CarGallery.Modules.Media.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.Media.Application;

/// <summary>
/// D11 — türev boyutlarına ihtiyaç duyan tüketicinin okuduğu dar yüzey.
/// `docs/architecture.md` § 2.A'nın saydığı meşru senkron okuma: `VehicleCatalog → Media`.
///
/// <para><b>Orijinalin yolu DÖNMEZ</b> (D16): ham dosya konum bilgisi taşıyor ve R5'in
/// garantisi dışarı çıkanı temizlemek. `MediaAssetDto`'da öyle bir alan yok — gizlenmiyor,
/// hiç bulunmuyor (Yasak Listesi 41'in aynı mantığı).</para>
/// </summary>
internal sealed class MediaAssetQueries(MediaDbContext context, MediaUrlResolver urls) : IMediaAssetQueries
{
    public async Task<MediaAssetDto?> FindAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        MediaAsset? asset = await context.Assets
            .AsNoTracking()
            .Include(value => value.Derivatives)
            .FirstOrDefaultAsync(value => value.Id == mediaAssetId, cancellationToken);

        if (asset is null)
        {
            return null;
        }

        return new MediaAssetDto(
            asset.Id,
            asset.Status,
            asset.FailureReason,
            asset.AltText.ToDictionary(),
            [.. asset.Derivatives.Select(turev => new MediaDerivativeDto(
                turev.VariantCode,
                turev.Width,
                turev.Height,
                urls.Resolve(turev.ObjectKey).ToString()))]);
    }

    public async Task<bool> AreAllReadyAsync(
        IReadOnlyCollection<Guid> mediaAssetIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mediaAssetIds);

        // Boş küme `false` (arayüzdeki gerekçe): A2 en az bir varlık istiyor.
        if (mediaAssetIds.Count == 0)
        {
            return false;
        }

        // Tekilleştirilmiş kimlikle sayıyoruz: çağıran aynı kimliği iki kez verdiğinde
        // eşitlik tutmaz ve sorgu "hazır değil" derdi.
        Guid[] tekil = [.. mediaAssetIds.Distinct()];

        int hazir = await context.Assets
            .AsNoTracking()
            .CountAsync(
                asset => tekil.Contains(asset.Id) && asset.Status == MediaProcessingStatus.Ready,
                cancellationToken);

        return hazir == tekil.Length;
    }
}
