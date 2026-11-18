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

    /// <summary>
    /// `0015` R5 — seçimli sıralamalar. Öne çıkarma ağırlığı YALNIZ varsayılanda geçerlidir
    /// (D7): "fiyat artan" listesinin başında pahalı bir öne çıkan ilan durursa liste yalan
    /// söyler. Eşitlik kırıcı her seçimde aynıdır: en yeni yayınlanan, sonra ilan kimliği
    /// (D8) — sayfa geçişinde ilan tekrarlanmaz ve kaybolmaz.
    /// </summary>
    internal static IOrderedQueryable<ShowcaseListing> InOrder(this IQueryable<ShowcaseListing> query, ShowcaseSort sort) =>
        sort switch
        {
            ShowcaseSort.Newest => query.OrderByDescending(listing => listing.PublishedAt).ThenBy(listing => listing.ListingId),

            // D1 — fiyatı olmayan ilan HER İKİ yönde sona: önce "fiyatı var mı", sonra fiyat.
            // `false < true`; null en son. "Fiyat sorunuz" 0 sayılsaydı artanda başa geçerdi.
            ShowcaseSort.PriceAscending => query
                .OrderBy(listing => listing.PriceAmount == null)
                .ThenBy(listing => listing.PriceAmount)
                .ThenTieBreak(),
            ShowcaseSort.PriceDescending => query
                .OrderBy(listing => listing.PriceAmount == null)
                .ThenByDescending(listing => listing.PriceAmount)
                .ThenTieBreak(),
            ShowcaseSort.MileageAscending => query.OrderBy(listing => listing.MileageKilometers).ThenTieBreak(),
            ShowcaseSort.YearDescending => query.OrderByDescending(listing => listing.ModelYear).ThenTieBreak(),
            ShowcaseSort.YearAscending => query.OrderBy(listing => listing.ModelYear).ThenTieBreak(),
            _ => query.InShowcaseOrder(),
        };

    private static IOrderedQueryable<ShowcaseListing> ThenTieBreak(this IOrderedQueryable<ShowcaseListing> query) =>
        query.ThenByDescending(listing => listing.PublishedAt).ThenBy(listing => listing.ListingId);
}
