using CarGallery.Modules.VehicleCatalog.Contracts.Showcase;
using CarGallery.Modules.VehicleCatalog.Infrastructure;
using CarGallery.Modules.VehicleReference.Contracts;
using Microsoft.EntityFrameworkCore;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// `0021` D9 · D15 — stoğu olan markalar yayındaki ilanların model kimliklerinden türetilir:
/// okuma modeli markayı taşımaz (C1), model kimliğini taşır ve modelin markası hiç değişmez
/// (D14). Bu yüzden bir markanın adı değişince eksen kendiliğinden yeni adı gösterir.
///
/// <para>İki okuma: okuma modelinden tekil model kimlikleri (vitrin ölçeğinde dar bir tarama)
/// ve `VehicleReference.Contracts` üzerinden markaları — tek atlama, salt okuma.</para>
/// </summary>
internal sealed class ShowcaseBrandQueries(
    VehicleCatalogDbContext context,
    IVehicleBrandQueries brands,
    ShowcaseLocaleOptions localeOptions) : IShowcaseBrandQueries
{
    public async Task<IReadOnlyList<ShowcaseBrandOptionDto>> GetForListAsync(
        string locale,
        IReadOnlyList<string> selectedSlugs,
        CancellationToken cancellationToken)
    {
        string code = localeOptions.ResolveOrDefault(locale).Code;
        List<ShowcaseBrandDto> stocked = [.. await StockedAsync(code, cancellationToken)];

        // D21 — adresle gelen, var olan ama stoğu kalmamış marka eksende görünür: ziyaretçi
        // seçimi kaldırabilsin. Var olmayan değer `FindBySlugsAsync`'ten dönmez ve yok sayılır.
        List<string> missing =
        [
            .. selectedSlugs
                .Where(slug => slug.Length > 0 && stocked.TrueForAll(brand => brand.Slug != slug))
                .Distinct(StringComparer.Ordinal)
        ];

        if (missing.Count > 0)
        {
            stocked.AddRange(await brands.FindBySlugsAsync(missing, code, cancellationToken));
        }

        return
        [
            .. stocked
                .OrderBy(brand => brand.CanonicalName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(brand => brand.CanonicalName, StringComparer.Ordinal)
                .Select(brand => new ShowcaseBrandOptionDto(brand.Slug, brand.Label))
        ];
    }

    /// <summary>R13 — "ana sayfada göster" işaretli VE stoğu olan markalar (AC-45…AC-47).</summary>
    public async Task<IReadOnlyList<ShowcaseBrandOptionDto>> GetForHomeAsync(
        string locale,
        CancellationToken cancellationToken)
    {
        string code = localeOptions.ResolveOrDefault(locale).Code;

        return
        [
            .. (await StockedAsync(code, cancellationToken))
                .Where(brand => brand.ShowOnHome)
                .Select(brand => new ShowcaseBrandOptionDto(brand.Slug, brand.Label))
        ];
    }

    private async Task<IReadOnlyList<ShowcaseBrandDto>> StockedAsync(string locale, CancellationToken cancellationToken)
    {
        List<Guid> modelIds = await context.ShowcaseListings
            .AsNoTracking()
            .Select(listing => listing.VehicleModelId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await brands.GetBrandsOfModelsAsync(modelIds, locale, cancellationToken);
    }
}
