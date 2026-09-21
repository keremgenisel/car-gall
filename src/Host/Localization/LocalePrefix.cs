using CarGallery.BuildingBlocks;

namespace CarGallery.Host.Localization;

/// <summary>
/// C7: locale öneki opsiyonel DEĞİLDİR. Öneksiz "/" isteği yapılandırmadaki varsayılan
/// locale'e 301 ile yönlendirilir — yönlendirme Accept-Language'dan GELMEZ (Yasak Listesi 28).
/// Google en-US ile tarar; Accept-Language'a göre yönlendirme Arapça sayfaların hiç
/// indekslenmemesine yol açar.
/// Etkin olmayan bir önek (/de/) 404 döner ve varsayılan locale'e YÖNLENDİRİLMEZ (Yasak 29):
/// yönlendirmek, var olmayan bir sayfayı var gibi göstermektir.
/// </summary>
internal sealed class LocalePrefixResolver
{
    private readonly HashSet<string> enabled;

    internal LocalePrefixResolver(string defaultLocale, IReadOnlyList<string> enabledLocales)
    {
        Result<Locale> parsedDefault = Locale.Create(defaultLocale);
        if (parsedDefault.IsFailure)
        {
            throw new InvalidOperationException($"Varsayılan locale geçersiz: '{defaultLocale}'.");
        }

        Default = parsedDefault.Value;
        enabled = [];

        foreach (string code in enabledLocales)
        {
            Result<Locale> parsed = Locale.Create(code);
            if (parsed.IsFailure)
            {
                throw new InvalidOperationException($"Etkin locale listesinde geçersiz kod: '{code}'.");
            }

            enabled.Add(parsed.Value.Code);
        }

        if (!enabled.Contains(Default.Code))
        {
            throw new InvalidOperationException("Etkin locale listesi varsayılan locale'i içermelidir.");
        }
    }

    internal Locale Default { get; }

    internal bool IsEnabled(string code) => enabled.Contains(code);

    internal Locale Resolve(string code) => Locale.Create(code).Value;
}
