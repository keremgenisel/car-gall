namespace CarGallery.Modules.Identity.Application;

/// <summary>
/// C4 — `Identity → Dealership` senkron çağrısının TEK amacı: şube kimliği geçerli mi.
/// Salt okuma, tek atlama, aynı istek içinde (docs/architecture.md § 2.A).
/// Uygulaması Infrastructure'da ve yalnız `Dealership.Contracts` üzerinden konuşur;
/// domain bu portu hiç bilmez.
/// </summary>
internal interface IBranchValidator
{
    Task<bool> ExistsAsync(Guid branchId, CancellationToken cancellationToken);
}
