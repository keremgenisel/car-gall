using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// Vitrinin TEK sıralaması (`0001` AC-4): ağırlık azalan, yayın tarihi yeniden eskiye,
/// eşitlikte ilan kimliği küçük olan önce.
///
/// <para>`0014` **D3** ana sayfanın öne çıkan bölümünün listeyle "aynı sırada" olmasını
/// istiyor (AC-18, AC-19). İki sorgu sıralamayı ayrı ayrı yazsaydı, biri değiştiğinde
/// ikisi sessizce ayrışırdı; bu yüzden ikisi de buradan okur.</para>
/// </summary>
internal static class ShowcaseOrdering
{
    internal static IOrderedQueryable<ShowcaseListing> InShowcaseOrder(this IQueryable<ShowcaseListing> query) =>
        query
            .OrderByDescending(listing => listing.ShowcaseSortWeight)
            .ThenByDescending(listing => listing.PublishedAt)
            .ThenBy(listing => listing.ListingId);
}
