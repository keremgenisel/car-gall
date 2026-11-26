namespace CarGallery.Modules.VehicleCatalog.Contracts.Showcase;

/// <summary>
/// `0004` **D3** · **D4** · **P9** — sitemap için vitrinde görünen (Yayında ve Rezerve) her ilanın
/// slug'ı. Vitrin okuma modelinden okunur, yani okunduğu andaki vitrindir: yayından kalkan ya da
/// satılan ilan bir sonraki okumada listede yoktur; olaylarla beslenen ayrı bir depo yoktur.
/// </summary>
public interface IShowcaseSitemapQueries
{
    /// <summary>Slug'lar sıralı (ordinal) döner; sitemap'in çıktısı okumadan okumaya kararlıdır.</summary>
    Task<IReadOnlyList<string>> ListVisibleSlugsAsync(CancellationToken cancellationToken);
}
