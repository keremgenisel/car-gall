using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Contracts.Events;
using CarGallery.Modules.VehicleCatalog.Application.Showcase;
using CarGallery.Modules.VehicleCatalog.Tests.Application;

namespace CarGallery.Modules.VehicleCatalog.Tests.Integration;

/// <summary>
/// `0012c` **AC-34** — "istek başına bir kez" bir SAYIDIR ve sayılmadan doğrulanamaz.
///
/// <para>Bu sarmalayıcılar davranışı değiştirmiyor, yalnız kaç kez sorulduğunu sayıyor.
/// Ölçülen şey bir performans sezgisi değil, kararın kendisi: 25 satırlık bir seçim
/// `Dealership`'e şube başına bir kez sormalı, araç başına değil.</para>
/// </summary>
internal sealed class SayanSubeSorgusu : IBranchQueries
{
    private readonly SahteSubeSorgusu ic = new();

    internal int AdOkumaSayisi { get; private set; }

    public Task<BranchSummaryDto?> FindAsync(Guid branchId, string locale, CancellationToken cancellationToken) =>
        ic.FindAsync(branchId, locale, cancellationToken);

    public Task<IReadOnlyList<BranchSummaryDto>> ListAsync(string locale, CancellationToken cancellationToken) =>
        ic.ListAsync(locale, cancellationToken);

    public Task<IReadOnlyDictionary<string, string>?> FindNamesAsync(
        Guid branchId,
        CancellationToken cancellationToken)
    {
        AdOkumaSayisi++;

        return ic.FindNamesAsync(branchId, cancellationToken);
    }
}

/// <summary>Kanonik model adı okumasını sayar (**AC-34**).</summary>
internal sealed class SayanModelAdCozucu(string kanonikAd = "Toyota Corolla") : IVehicleModelNameResolver
{
    private readonly FakeModelNameResolver ic = new(kanonikAd);

    internal int KanonikOkumaSayisi { get; private set; }

    public Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        Locale locale,
        CancellationToken cancellationToken) =>
        ic.ResolveAsync(vehicleModelIds, locale, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, string>> ResolveCanonicalNamesAsync(
        IReadOnlyCollection<Guid> vehicleModelIds,
        CancellationToken cancellationToken)
    {
        KanonikOkumaSayisi++;

        return ic.ResolveCanonicalNamesAsync(vehicleModelIds, cancellationToken);
    }
}
