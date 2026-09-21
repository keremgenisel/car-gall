using CarGallery.BuildingBlocks;

namespace CarGallery.Modules.VehicleCatalog.Application.Showcase;

/// <summary>
/// Etkin locale listesi ve varsayılan locale DAĞITIM YAPILANDIRMASIDIR, veritabanı kaydı
/// değildir (docs/conventions.md § 11). Kod hiçbir yerde bir dili varsaymaz.
/// </summary>
internal sealed class ShowcaseLocaleOptions
{
    internal ShowcaseLocaleOptions(Locale defaultLocale, IReadOnlyList<Locale> enabled)
    {
        Default = defaultLocale;
        Enabled = enabled;
    }

    internal Locale Default { get; }

    internal IReadOnlyList<Locale> Enabled { get; }

    internal bool IsEnabled(string code) => Enabled.Any(locale => locale.Code == code);

    internal Locale ResolveOrDefault(string code)
    {
        Result<Locale> parsed = Locale.Create(code);
        return parsed.IsSuccess && IsEnabled(parsed.Value.Code) ? parsed.Value : Default;
    }
}
