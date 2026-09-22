using CarGallery.Modules.Identity.Application;
using CarGallery.Modules.Dealership.Contracts.Events;

namespace CarGallery.Modules.Identity.Infrastructure;

/// <summary>
/// Application'daki portun dışa bakan adaptörü. `Dealership`'e YALNIZCA `Contracts`
/// üzerinden, salt okuma, tek atlama ve aynı istek içinde konuşur — senkron doğrudan
/// çağrının üç koşulu da sağlanır (C4, docs/architecture.md § 2.A).
///
/// Locale parametresi var olan imzanın gereği; doğrulama şubenin ADINI kullanmaz,
/// yalnız var olup olmadığına bakar.
/// </summary>
internal sealed class BranchValidator(IBranchQueries queries, IdentityLocaleOptions locales) : IBranchValidator
{
    public async Task<bool> ExistsAsync(Guid branchId, CancellationToken cancellationToken) =>
        await queries.FindAsync(branchId, locales.Default.Code, cancellationToken) is not null;
}
