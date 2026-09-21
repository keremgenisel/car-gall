using System.ComponentModel.DataAnnotations;

namespace CarGallery.Host;

/// <summary>
/// Kurulum bazlı değerler DAĞITIM YAPILANDIRMASIDIR, veritabanı kaydı değildir
/// (docs/conventions.md § 11). Eksik yapılandırmayla uygulama ayağa kalkmaz.
/// </summary>
internal sealed class LocalizationOptions
{
    internal const string SectionName = "Localization";

    [Required(AllowEmptyStrings = false)]
    public string DefaultLocale { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string[] EnabledLocales { get; set; } = [];
}
