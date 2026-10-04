namespace CarGallery.Modules.Dealership.Contracts.Branches;

/// <summary>
/// 0009 D2 — galeri bildiriminin hedefi ve şubenin adı.
///
/// <para><b>Neden ayrı bir DTO</b> (D7). Şube e-postası <c>BranchSummaryDto</c>'ya
/// EKLENMEDİ: o tip bugün şube doğrulaması gibi yerlerde kullanılıyor ve oraya e-posta
/// koymak şube iletişim bilgisini ihtiyacı olmayan her tüketiciye açardı. 0005'in
/// <c>ListingSnapshotDto</c>'su aynı sebeple dar tutulmuştu.</para>
///
/// <para><c>Email</c> null olabilir: 0003 şube e-postasını zorunlu kılmıyor. O hâlde
/// bildirim kurulum adresine düşer (0009 AC-5) — kararı çağıran verir, burası yalnız
/// olguyu söyler.</para>
/// </summary>
public sealed record BranchNotificationContactDto(Guid BranchId, string Name, string? Email);

public interface IBranchContactQueries
{
    /// <summary>Var olmayan şube için <c>null</c>.</summary>
    Task<BranchNotificationContactDto?> FindContactAsync(
        Guid branchId,
        string locale,
        CancellationToken cancellationToken);
}
