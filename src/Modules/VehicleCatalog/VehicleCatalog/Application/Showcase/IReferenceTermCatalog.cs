namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// `0015` R16 — yakıt ve vitesin kanonik slug kümesi. Kasa tipinin <see cref="IBodyTypeCatalog"/>'u
/// gibi: küme `VehicleReference`'a aittir ve yalnız `Contracts` üzerinden okunur.
/// </summary>
internal interface IReferenceTermCatalog
{
    /// <param name="kind">`ReferenceTermKinds` sabitlerinden biri.</param>
    Task<IReadOnlySet<string>> GetCanonicalSlugsAsync(string kind, CancellationToken cancellationToken);
}
