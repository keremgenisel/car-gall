using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// Modül sınırının ötesindeki üç yüzey sahtelenir (docs/testing.md § 1). Sınanan şey
/// referans verinin nasıl okunduğu değil, okunanla ne yapıldığı.
/// </summary>
internal sealed class SahteSubeSorgusu(bool bulunur = true) : IBranchQueries
{
    public Task<BranchSummaryDto?> FindAsync(Guid branchId, string locale, CancellationToken cancellationToken) =>
        Task.FromResult(bulunur ? new BranchSummaryDto(branchId, "Merkez Şube", "Active") : null);

    public Task<IReadOnlyList<BranchSummaryDto>> ListAsync(string locale, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BranchSummaryDto>>([]);

    /// <summary>
    /// `0012b` **D40** — vitrin yansıtmasının okuduğu locale → ad haritası. Sahte üç dili
    /// de veriyor: tek dilli bir sahte, "satır tek anahtarlı kalıyor" kusurunu testten
    /// GİZLERDİ — kusurun kendisi tam olarak buydu.
    /// </summary>
    public Task<IReadOnlyDictionary<string, string>?> FindNamesAsync(
        Guid branchId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, string>?>(bulunur
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["tr"] = "Merkez Şube",
                ["ar"] = "الفرع الرئيسي",
                ["en"] = "Main Branch",
            }
            : null);
}

internal sealed class SahteKasaSorgusu(bool bulunur = true) : IBodyTypeQueries
{
    public Task<IReadOnlyList<BodyTypeDto>> GetAllAsync(string locale, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BodyTypeDto>>([new BodyTypeDto("suv", "SUV")]);

    public Task<bool> ExistsAsync(string slug, CancellationToken cancellationToken) => Task.FromResult(bulunur);
}

internal sealed class SahteModelSorgusu(bool bulunur = true) : IVehicleModelQueries
{
    public Task<IReadOnlyDictionary<Guid, string>> ResolveNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

    public Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string>>(new Dictionary<Guid, string>());

    public Task<IReadOnlyList<VehicleModelDto>> GetAllAsync(string locale, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<VehicleModelDto>>([]);

    public Task<bool> ExistsAsync(Guid vehicleModelId, CancellationToken cancellationToken) =>
        Task.FromResult(bulunur);
}

/// <summary>Eksik olarak bildirilecek türler; boşsa hepsi var sayılır.</summary>
internal sealed class SahteTerimSorgusu(params string[] eksikTurler) : IReferenceTermQueries
{
    public Task<IReadOnlyDictionary<(string Kind, string Slug), string>> ResolveLabelsAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        string locale,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<(string, string), string>>(new Dictionary<(string, string), string>());

    public Task<IReadOnlyList<ReferenceTermDto>> GetByKindAsync(
        string kind,
        string locale,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReferenceTermDto>>([]);

    public Task<IReadOnlyList<(string Kind, string Slug)>> FindMissingAsync(
        IReadOnlyCollection<(string Kind, string Slug)> terms,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<(string, string)>>(
            [.. terms.Where(term => eksikTurler.Contains(term.Kind, StringComparer.Ordinal))]);
}
