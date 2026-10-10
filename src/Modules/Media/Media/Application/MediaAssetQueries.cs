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
}
