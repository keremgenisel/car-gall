using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0021` — `VehicleReference`'ın marka yüzeyinin sahtesi. Catalog testleri modülün kendi
/// kuralını (marka → model kimliği kümesi → okuma modeli süzgeci) ölçer; markanın kendisi
/// `VehicleReference.Tests`'te gerçek PostgreSQL üzerinde ölçülür.
/// </summary>
internal sealed class FakeBrandQueries(params FakeBrand[] brands) : IVehicleBrandQueries
{
    public Task<IReadOnlyList<VehicleBrandDto>> GetActiveAsync(string locale, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<VehicleBrandDto>>([]);

    public Task<IReadOnlyList<ShowcaseBrandDto>> GetBrandsOfModelsAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ShowcaseBrandDto>>(
        [
            .. brands
                .Where(brand => brand.ModelIds.Any(vehicleModelIds.Contains))
                .OrderBy(brand => brand.Slug, StringComparer.Ordinal)
                .Select(brand => brand.Dto(locale))
        ]);

    public Task<IReadOnlyList<ShowcaseBrandDto>> FindBySlugsAsync(
        IReadOnlyCollection<string> slugs,
        string locale,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ShowcaseBrandDto>>(
            [.. brands.Where(brand => slugs.Contains(brand.Slug)).Select(brand => brand.Dto(locale))]);

    public Task<BrandModelSetDto> GetModelSetAsync(IReadOnlyCollection<string> slugs, CancellationToken cancellationToken)
    {
        List<FakeBrand> known = [.. brands.Where(brand => slugs.Contains(brand.Slug))];
        return Task.FromResult(new BrandModelSetDto(
            [.. known.Select(brand => brand.Slug)],
            [.. known.SelectMany(brand => brand.ModelIds)]));
    }
}

internal sealed record FakeBrand(string Slug, string CanonicalName, bool ShowOnHome, IReadOnlyList<Guid> ModelIds, string? ArabicName = null)
{
    internal ShowcaseBrandDto Dto(string locale) =>
        new(Slug, locale == "ar" && ArabicName is not null ? ArabicName : CanonicalName, CanonicalName, ShowOnHome);
}
