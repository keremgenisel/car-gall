using System.Globalization;
using CarGallery.BuildingBlocks;

namespace CarGallery.Host.Theme;

/// <summary>
/// Başlangıçta fırlatılan istisnanın metni. **Kullanıcıya görünen metin değildir** —
/// uygulama bu noktada hiç ayağa kalkmıyor ve mesajın tek okuru dağıtımı yapan kişidir;
/// Yasak Listesi 24 arayüz metnini kapsıyor, operatör mesajını değil.
///
/// <para>Mesaj ne olduğunu DÜZELTİLEBİLİR biçimde söyler: hangi token, hangi çift, hangi
/// eşik, ölçülen ne. "Tema yapılandırması geçersiz" diyen bir mesaj, dağıtımı yapan
/// kişiyi on bir anahtarı tek tek denemeye bırakır.</para>
/// </summary>
internal static class ThemeErrorText
{
    internal static string Describe(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        IReadOnlyDictionary<string, string> parameters = error.Parameters ?? new Dictionary<string, string>();

        return error.Code switch
        {
            ThemeErrors.TokenMissing => string.Create(
                CultureInfo.InvariantCulture,
                $"Tema kısmen verilmiş: '{Get(parameters, "token")}' eksik. Tema YA TAM YA HİÇ verilir (frontend-002 R6); hiç verilmediğinde varsayılan palet kullanılır."),

            ThemeErrors.UnknownToken => string.Create(
                CultureInfo.InvariantCulture,
                $"Tanınmayan veya yapılandırılamayan tema anahtarı: '{Get(parameters, "token")}'. Yalnız renk yapılandırılabilir (frontend-002 R5, X9)."),

            ThemeErrors.InvalidColorFormat => string.Create(
                CultureInfo.InvariantCulture,
                $"Tema değeri altı haneli hex değil: '{Get(parameters, "token")}'. Biçim tek tiptir: #RRGGBB (frontend-002 C11)."),

            ThemeErrors.ContrastBelowThreshold => string.Create(
                CultureInfo.InvariantCulture,
                $"Kontrast eşiği karşılanmıyor ({Get(parameters, "criterion")}): '{Get(parameters, "foreground")}' ↔ '{Get(parameters, "background")}' en az {Get(parameters, "threshold")}:1 olmalı, ölçülen {Get(parameters, "measured")}:1."),

            _ => string.Create(CultureInfo.InvariantCulture, $"Tema yapılandırması geçersiz: {error.Code}."),
        };
    }

    private static string Get(IReadOnlyDictionary<string, string> parameters, string key) =>
        parameters.TryGetValue(key, out string? value) ? value : "?";
}
