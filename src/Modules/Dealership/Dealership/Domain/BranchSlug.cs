using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.Dealership.Domain;

/// <summary>
/// Şubenin dilden bağımsız adresi (R6). Küçük harfli ASCII slug.
///
/// Türkçe veya Arapça harf KABUL EDİLMEZ: slug bir adrestir ve Türkçe collation altında
/// `i/İ/ı/I` sessiz eşleşme hatası üretir (Yasak Listesi 27). `Kadıköy-Şubesi` reddedilir,
/// `kadikoy-subesi` kabul edilir (AC-12).
/// </summary>
internal readonly record struct BranchSlug
{
    internal const string InvalidFormat = "branch_slug.invalid_format";

    private BranchSlug(string value) => Value = value;

    internal string Value { get; }

    internal static Result<BranchSlug> Create(string value)
    {
        if (value is not { Length: > 0 and <= 80 })
        {
            return Result.Failure<BranchSlug>(new Error(InvalidFormat));
        }

        foreach (char character in value)
        {
            bool allowed = character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
            if (!allowed)
            {
                return Result.Failure<BranchSlug>(new Error(InvalidFormat));
            }
        }

        if (value[0] == '-' || value[^1] == '-' || value.Contains("--", StringComparison.Ordinal))
        {
            return Result.Failure<BranchSlug>(new Error(InvalidFormat));
        }

        return Result.Success(new BranchSlug(value));
    }

    public override string ToString() => Value;
}
