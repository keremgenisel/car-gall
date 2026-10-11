using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// Model adı referans veridir ve VehicleReference'a aittir (docs/architecture.md § 3).
/// Bu arayüz Catalog'un dışa bakan portudur; uygulaması Infrastructure'da, yalnızca
/// VehicleReference.Contracts üzerinden konuşur. Domain bu portu hiç bilmez.
/// </summary>
internal interface IVehicleModelNameResolver
{
    Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        Locale locale,
        CancellationToken cancellationToken);

    /// <summary>
    /// `0012b` **D13** — ilanın adresi KANONİK addan türüyor ve adres her dilde AYNIdır
    /// (`docs/architecture.md` § 3). Locale'li addan türetilseydi aynı ilanın üç ayrı
    /// adresi olurdu; Arapça bir kanonik ad ise yalnız yıl ve sonekten oluşan anlamsız bir
    /// adres üretirdi — `ListingSlug` bunu `invalid_source` ile reddediyor.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        CancellationToken cancellationToken);
}
