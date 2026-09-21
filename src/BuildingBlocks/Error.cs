namespace CarGallery.BuildingBlocks;

/// <summary>
/// Kararlı hata kodu ve yapısal parametreleri. Kullanıcıya gösterilecek metin TAŞIMAZ:
/// metin uçta, koddan çevrilir (docs/conventions.md § 5, Yasak Listesi 23).
/// </summary>
public sealed record Error(string Code, IReadOnlyDictionary<string, string>? Parameters = null)
{
    public static readonly Error None = new(string.Empty);

    public bool IsNone => Code.Length == 0;

    public static Error With(string code, string parameterName, string parameterValue) =>
        new(code, new Dictionary<string, string> { [parameterName] = parameterValue });
}
