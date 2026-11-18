using CarGallery.BuildingBlocks;
using CarGallery.Modules.Dealership.Domain;

namespace CarGallery.Modules.Dealership.Application;

/// <summary>
/// `0020` — kurulumun varsayılan ve etkin dilleri, yapılandırmadan (Bilinen tuzak 4: kod
/// <c>tr</c>'yi varsaymaz). Görünen adın ve şube adının dil anahtarları bu listeye karşı
/// süzülür; etkin olmayan dil bir alan hatasıdır, sessizce saklanan bir metin değil.
/// </summary>
internal sealed record DealershipLocales(Locale Default, IReadOnlyList<Locale> Enabled)
{
    internal const string LocaleNotEnabled = "dealership.locale_not_enabled";

    /// <summary>
    /// Ham dil → metin haritasını etkin dillerle eşler. Etkin olmayan ya da geçersiz dil
    /// <c>&lt;alan&gt;.&lt;dil&gt;</c> alanına hata olarak yazılır.
    /// </summary>
    internal Dictionary<Locale, string?> Filter(
        IReadOnlyDictionary<string, string?>? values,
        string field,
        FieldErrors errors)
    {
        Dictionary<Locale, string?> result = [];

        foreach ((string code, string? value) in values ?? new Dictionary<string, string?>())
        {
            Locale? locale = Enabled.Cast<Locale?>().FirstOrDefault(item => item!.Value.Code == code);

            if (locale is null)
            {
                errors.Add($"{field}.{code}", LocaleNotEnabled);
                continue;
            }

            result[locale.Value] = value;
        }

        return result;
    }
}
