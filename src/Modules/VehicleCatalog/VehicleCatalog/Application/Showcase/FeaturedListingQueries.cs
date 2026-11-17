using CarGallery.BuildingBlocks;
using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Domain;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleCatalog.Infrastructure.ReadModel;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// `0014` **D3**, **D4** — ana sayfanın öne çıkan bölümü: galerinin panelde öne çıkardığı
/// (`0012c`) ve vitrinde görünen ilanlar, listeyle aynı sırada, en çok altı tane.
///
/// <para>Görünürlük (C3) okuma modelinin kendisinde yaşar: `Taslak`, `Satıldı` ve `Arşiv`
/// ilanlar satır olarak hiç yoktur (AC-23). Öne çıkarılmış ilan altıdan azsa boşluk
/// doldurulmaz (AC-20); hiç yoksa liste boş döner ve bölümü gizlemek ucun işidir (AC-21).</para>
/// </summary>
internal sealed class FeaturedListingQueries(
    VehicleCatalogDbContext context,
    IVehicleModelNameResolver modelNameResolver,
    ShowcaseLocaleOptions localeOptions) : IFeaturedListingQueries
{
    public async Task<IReadOnlyList<ShowcaseListingDto>> GetAsync(string locale, CancellationToken cancellationToken)
    {
        Locale resolved = localeOptions.ResolveOrDefault(locale);

        // "Öne çıkarılmış" agregatın tanımıyla aynı: ağırlık normalin üstünde
        // (`Listing.IsFeatured`). İki tanım ayrışırsa panel "öne çıkarılmış" der, ana
        // sayfa göstermez.
        List<ShowcaseListing> rows = await context.ShowcaseListings
            .AsNoTracking()
            .Where(listing => listing.ShowcaseSortWeight > Listing.NormalWeight)
            .InShowcaseOrder()
            .Take(FeaturedListingLimits.MaxCount)
            .ToListAsync(cancellationToken);

        return await new ShowcaseCards(modelNameResolver, localeOptions).ToDtosAsync(rows, resolved, cancellationToken);
    }
}
