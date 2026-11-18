namespace CarGallery.Modules.VehicleCatalog.Domain;

/// <summary>
/// Agregat kimlikleri modül İÇİNDE güçlü tiplidir; X.Contracts sınırında çıplak Guid'e
/// düşer (docs/conventions.md § 9). Üretim uygulamada, Guid v7 ile — sıralanabilir olması
/// R7'nin son sıralama kırıcısını anlamlı kılar.
/// </summary>
internal readonly record struct VehicleId(Guid Value)
{
    internal static VehicleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

internal readonly record struct ListingId(Guid Value)
{
    internal static ListingId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}

/// <summary>`0023` **D7** — satış geri alma kaydının kimliği.</summary>
internal readonly record struct SaleReversalId(Guid Value)
{
    internal static SaleReversalId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
