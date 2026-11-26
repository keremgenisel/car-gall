using CarGallery.Modules.Media.Contracts.Assets;

namespace CarGallery.Host.Seo;

/// <summary>
/// `0026` **D3** · **D5** — vitrinde kullanılabilir hâle gelmiş galeri logosu: üst çubuk ve altlığın
/// `logo-small`'u (adres ve GERÇEK boyutu) ve paylaşım önizlemesi ile <c>AutoDealer.logo</c>'nun
/// `logo-large`'ı. Adresler `Media`'nın verdiği hâliyle; mutlaklaştırma belge kurulurken yapılır.
/// </summary>
internal sealed record GalleryLogo(string SmallUrl, int SmallWidth, int SmallHeight, string LargeUrl)
{
    internal const string SmallVariant = "logo-small";
    internal const string LargeVariant = "logo-large";

    /// <summary>
    /// D3 — logo YALNIZ varlık hazır, türü logo ve iki logo türevi varken kullanılır (AC-35,
    /// AC-36). Aksi hâlde <c>null</c>: vitrin bu karttan önceki gibi davranır (R7).
    /// </summary>
    internal static GalleryLogo? From(MediaAssetDto? asset)
    {
        if (asset is not { Status: MediaProcessingStatus.Ready, Kind: MediaAssetKind.Logo })
        {
            return null;
        }

        MediaDerivativeDto? small = asset.Derivatives.FirstOrDefault(turev => turev.Variant == SmallVariant);
        MediaDerivativeDto? large = asset.Derivatives.FirstOrDefault(turev => turev.Variant == LargeVariant);

        return small is null || large is null ? null : new GalleryLogo(small.Url, small.Width, small.Height, large.Url);
    }
}
