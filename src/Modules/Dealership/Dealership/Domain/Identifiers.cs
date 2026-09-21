namespace CarGallery.Modules.Dealership.Domain;

internal readonly record struct BranchId(Guid Value)
{
    internal static BranchId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
