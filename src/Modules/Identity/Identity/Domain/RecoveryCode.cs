using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Kurtarma kodu (docs/domain.md — **Kurtarma Kodu** / `RecoveryCode`). Her kod **tek
/// kullanımlıktır** (R4/R5): yeniden kullanılabilir bir kod, yazılı bir yedek paroladır
/// ve ikinci adımın değerini sıfırlar (§ Bilinen tuzaklar 1).
///
/// Kod **tek yönlü özetlenerek** saklanır (C2b) — `0006` C6'nın deseni, mevcut
/// `IPasswordHasher`. Özetin iterasyon sayısı parolanınkinden **ayrıdır** ve
/// yapılandırmadan gelir (D2/D38): girilen bir kodu saklanan on özetin hepsiyle
/// karşılaştırmak on özetleme demek ve üretim parolası değeriyle bu tek girişte
/// saniyeler sürerdi. Düşük maliyet burada güvenli, çünkü kod **sistem üretimi ve yüksek
/// entropili**; tahmin edilebilir bir insan parolası değil (`Session.HashToken`'ın
/// gerekçesiyle aynı).
/// </summary>
internal sealed class RecoveryCode : Entity<RecoveryCodeId>
{
    private RecoveryCode(RecoveryCodeId id, StaffId staffId, PasswordHash codeHash, DateTimeOffset createdAt)
        : base(id)
    {
        StaffId = staffId;
        CodeHash = codeHash;
        CreatedAt = createdAt;
    }

    /// <summary>EF Core'un okuma yolu; iş kodundan çağrılmaz.</summary>
    private RecoveryCode()
        : base(default)
    {
    }

    internal StaffId StaffId { get; private set; }

    internal PasswordHash CodeHash { get; private set; }

    internal DateTimeOffset CreatedAt { get; private set; }

    internal DateTimeOffset? UsedAt { get; private set; }

    internal bool IsAvailable => UsedAt is null;

    internal static RecoveryCode Issue(StaffId staffId, PasswordHash codeHash, DateTimeOffset createdAt) =>
        new(RecoveryCodeId.New(), staffId, codeHash, createdAt);

    /// <summary>
    /// Kullanılan kod SİLİNMEZ, işaretlenir: silinmiş bir satır "bu kod hiç var olmadı"
    /// ile "bu kod kullanıldı"yı ayırt ettirmez ve kalan sayıyı (D24) yanlış gösterirdi.
    /// </summary>
    internal void MarkUsed(DateTimeOffset now) => UsedAt ??= now;
}
