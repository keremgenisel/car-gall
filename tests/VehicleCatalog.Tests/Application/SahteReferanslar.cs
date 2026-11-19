using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleReference.Contracts;

namespace CarGallery.Modules.VehicleCatalog.Tests.Application;

/// <summary>
/// Modül sınırının ötesindeki üç yüzey sahtelenir (docs/testing.md § 1). Sınanan şey
/// referans verinin nasıl okunduğu değil, okunanla ne yapıldığı.
/// </summary>
internal sealed class SahteSubeSorgusu(bool bulunur = true, IReadOnlySet<Guid>? kapatilmis = null) : IBranchQueries
{
    /// <summary>`0020` D14 — <paramref name="kapatilmis"/>'teki şubeler `Closed` döner.</summary>
    public Task<BranchSummaryDto?> FindAsync(Guid branchId, string locale, CancellationToken cancellationToken) =>
        Task.FromResult(bulunur
            ? new BranchSummaryDto(
                branchId,
                "Merkez Şube",
                kapatilmis?.Contains(branchId) == true ? BranchStatusCodes.Closed : BranchStatusCodes.Active)
            : null);

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

/// <summary>
/// `0021` — <paramref name="secilebilir"/> modelin YENİ seçim olarak kabul edilip
/// edilmediğini söyler (pasif model ya da pasif markanın modeli <c>false</c>).
/// </summary>
internal sealed class SahteModelSorgusu(bool bulunur = true, bool secilebilir = true) : IVehicleModelQueries
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

    public Task<IReadOnlyDictionary<Guid, VehicleModelPartsDto>> ResolvePartsAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        string locale,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, VehicleModelPartsDto>>(new Dictionary<Guid, VehicleModelPartsDto>());

    public Task<bool?> FindSelectableAsync(Guid vehicleModelId, CancellationToken cancellationToken) =>
        Task.FromResult<bool?>(bulunur ? secilebilir : null);
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
