namespace CarGallery.Modules.Content.Application;

/// <summary>
/// Kurulumun etkin dilleri. `0017` R9/D16 — eksik dil yalnız ETKİN dillerde sayılır;
/// etkin olmayan dile yazma da okuma da yapılmaz.
/// </summary>
internal sealed record ContentLocaleOptions(string Default, IReadOnlyList<string> Enabled)
{
    /// <summary>Dil etkin mi.</summary>
    internal bool IsEnabled(string? locale) =>
        locale is not null && Enabled.Contains(locale, StringComparer.Ordinal);
}
