namespace CarGallery.Modules.Leads.Domain;

/// <summary>
/// Agregat kimliği modül İÇİNDE güçlü tiplidir; sözleşme sınırında çıplak `Guid` olarak
/// geçer (docs/conventions.md § 9). Üretim uygulama tarafında, `Guid` v7 ile.
/// </summary>
internal readonly record struct LeadId(Guid Value)
{
    internal static LeadId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
