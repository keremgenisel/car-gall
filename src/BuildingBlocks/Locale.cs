namespace CarGallery.BuildingBlocks;

public enum TextDirection
{
    LeftToRight = 0,
    RightToLeft = 1,
}

/// <summary>
/// Dil + yön + biçimlendirme birimi (docs/domain.md § 1). Etkin locale listesi dağıtım
/// yapılandırmasından gelir; bu tip kodun hiçbir yerinde bir dili VARSAYMAZ.
/// </summary>
public readonly record struct Locale
{
    public const string InvalidCode = "locale.invalid_code";

    /// <summary>Yön dilin özelliğidir, yapılandırmanın değil (docs/architecture.md § 3).</summary>
    private static readonly HashSet<string> RightToLeftLanguages = ["ar", "fa", "he", "ur"];

    private Locale(string code) => Code = code;

    public string Code { get; }

    public TextDirection Direction =>
        RightToLeftLanguages.Contains(Code) ? TextDirection.RightToLeft : TextDirection.LeftToRight;

    public static Result<Locale> Create(string code)
    {
        if (!IsLowercaseAsciiLanguageTag(code))
        {
            return Result.Failure<Locale>(new Error(InvalidCode));
        }

        return Result.Success(new Locale(code));
    }

    public override string ToString() => Code;

    private static bool IsLowercaseAsciiLanguageTag(string code)
    {
        if (code is not { Length: 2 or 3 })
        {
            return false;
        }

        foreach (char character in code)
        {
            if (character is < 'a' or > 'z')
            {
                return false;
            }
        }

        return true;
    }
}
