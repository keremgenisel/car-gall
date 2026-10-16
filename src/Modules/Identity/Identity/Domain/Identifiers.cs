namespace CarGallery.Modules.Identity.Domain;

/// <summary>
/// Agregat kimlikleri modül İÇİNDE güçlü tiplidir; sözleşme sınırında çıplak `Guid`
/// olarak geçer (docs/conventions.md § 9). Üretim uygulama tarafında, `Guid` v7 ile.
/// </summary>
internal readonly record struct StaffId(Guid Value)
{
    internal static StaffId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

internal readonly record struct SessionId(Guid Value)
{
    internal static SessionId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

internal readonly record struct RecoveryCodeId(Guid Value)
{
    internal static RecoveryCodeId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

internal readonly record struct PendingLoginId(Guid Value)
{
    internal static PendingLoginId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

internal readonly record struct TwoFactorAuditEntryId(Guid Value)
{
    internal static TwoFactorAuditEntryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
